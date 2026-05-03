using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.RateLimit;
using RajFinancial.Api.Services.RateLimit.Storage;

namespace RajFinancial.Api.Middleware.RateLimit;

/// <summary>
///     Per-endpoint rate-limit gate. Resolves a <see cref="RateLimitPolicy" /> via
///     <see cref="IRateLimitPolicyResolver" />; if a policy applies, hashes the
///     authenticated user id and consults <see cref="IRateLimitStore" />. Throws
///     <see cref="RateLimitedException" /> on rejection so <see cref="ExceptionMiddleware" />
///     produces the canonical <c>429</c> / <c>503</c> + <c>Retry-After</c> response.
/// </summary>
public partial class RateLimitMiddleware(
    IRateLimitPolicyResolver policyResolver,
    IRateLimitStore store,
    ILogger<RateLimitMiddleware> logger)
    : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var policy = policyResolver.Resolve(context);

        if (policy.IsNoOp)
        {
            await next(context);
            return;
        }

        if (!context.Items.TryGetValue(FunctionContextKeys.UserId, out var userIdObj) ||
            userIdObj is not string userId ||
            string.IsNullOrWhiteSpace(userId))
        {
            LogMissingUserId(context.FunctionDefinition.Name, policy.Kind);
            await next(context);
            return;
        }

        using var activity = RateLimitTelemetry.StartActivity(RateLimitTelemetry.ActivityCheck);
        activity?.SetTag(RateLimitTelemetry.PolicyKindTag, policy.Kind.ToString());
        activity?.SetTag(RateLimitTelemetry.CodeFunctionTag, context.FunctionDefinition.Name);

        var userIdHash = UserIdHashing.Hash(userId);
        var decision = await ConsumeAsync(context, userIdHash, policy);

        activity?.SetTag(RateLimitTelemetry.OutcomeTag, ResolveOutcomeTag(decision));

        if (decision.Allowed)
        {
            RateLimitTelemetry.RecordAllowed(policy.Kind, decision.StoreUnavailable);
            if (decision.StoreUnavailable)
                LogFailedOpen(context.FunctionDefinition.Name, userIdHash);
            await next(context);
            return;
        }

        RateLimitTelemetry.RecordRejected(policy.Kind, decision.Window, policy.FailureMode, decision.StoreUnavailable);
        activity?.SetTag(RateLimitTelemetry.WindowTag, decision.Window.ToString());

        var retryAfterSeconds = RateLimitResponseHelper.RetryAfterSeconds(decision.RetryAfter);
        if (decision.StoreUnavailable)
            LogFailedClosed(context.FunctionDefinition.Name, userIdHash, retryAfterSeconds);
        else
            LogRejected(context.FunctionDefinition.Name, userIdHash, decision.Window, retryAfterSeconds);

        throw new RateLimitedException(decision.RetryAfter, decision.StoreUnavailable, decision.Window);
    }

    /// <summary>
    ///     Outcome tag used on the activity and metric — must match the tag emitted
    ///     by <see cref="RateLimitTelemetry.RecordRejected" /> so traces and metrics
    ///     agree (<c>store_error</c> takes precedence over <c>allowed</c>/<c>rejected</c>).
    /// </summary>
    private static string ResolveOutcomeTag(RateLimitDecision decision)
    {
        if (decision.StoreUnavailable)
            return RateLimitTelemetry.OutcomeStoreError;
        return decision.Allowed
            ? RateLimitTelemetry.OutcomeAllowed
            : RateLimitTelemetry.OutcomeRejected;
    }

    private async Task<RateLimitDecision> ConsumeAsync(
        FunctionContext context,
        string userIdHash,
        RateLimitPolicy policy)
    {
        try
        {
            return await store.TryConsumeAsync(userIdHash, policy, context.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (System.Exception ex)
        {
            RateLimitTelemetry.RecordStoreError(ex.GetType().Name);
            LogStoreUnhandled(ex, context.FunctionDefinition.Name, policy.FailureMode);
            return policy.FailureMode == RateLimitFailureMode.FailClosed
                ? RateLimitDecision.RejectFailClosed(TimeSpan.FromSeconds(60))
                : RateLimitDecision.AllowFailOpen();
        }
    }
}
