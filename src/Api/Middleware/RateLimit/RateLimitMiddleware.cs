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
            string.IsNullOrEmpty(userId))
        {
            LogMissingUserId(context.FunctionDefinition.Name, policy.Kind);
            await next(context);
            return;
        }

        using var activity = RateLimitTelemetry.StartActivity(RateLimitTelemetry.ActivityCheck);
        activity?.SetTag(RateLimitTelemetry.PolicyKindTag, policy.Kind.ToString());
        activity?.SetTag(RateLimitTelemetry.CodeFunctionTag, context.FunctionDefinition.Name);

        var userIdHash = UserIdHashing.Hash(userId);
        var sw = Stopwatch.StartNew();
        var decision = await ConsumeAsync(context, userIdHash, policy, sw);
        if (sw.IsRunning) sw.Stop();
        RateLimitTelemetry.RecordStoreDuration(sw.Elapsed.TotalMilliseconds);

        activity?.SetTag(RateLimitTelemetry.OutcomeTag,
            decision.Allowed ? RateLimitTelemetry.OutcomeAllowed : RateLimitTelemetry.OutcomeRejected);

        if (decision.Allowed)
        {
            RateLimitTelemetry.RecordAllowed(policy.Kind);
            if (decision.StoreUnavailable)
                LogFailedOpen(context.FunctionDefinition.Name, userIdHash);
            await next(context);
            return;
        }

        RateLimitTelemetry.RecordRejected(policy.Kind, decision.Window, policy.FailureMode, decision.StoreUnavailable);
        activity?.SetTag(RateLimitTelemetry.WindowTag, decision.Window.ToString());

        if (decision.StoreUnavailable)
            LogFailedClosed(context.FunctionDefinition.Name, userIdHash, (int)decision.RetryAfter.TotalSeconds);
        else
            LogRejected(context.FunctionDefinition.Name, userIdHash, decision.Window, (int)decision.RetryAfter.TotalSeconds);

        throw new RateLimitedException(decision.RetryAfter, decision.StoreUnavailable, decision.Window);
    }

    private async Task<RateLimitDecision> ConsumeAsync(
        FunctionContext context,
        string userIdHash,
        RateLimitPolicy policy,
        Stopwatch sw)
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
            sw.Stop();
            RateLimitTelemetry.RecordStoreError(ex.GetType().Name);
            LogStoreUnhandled(ex, context.FunctionDefinition.Name, policy.FailureMode);
            return policy.FailureMode == RateLimitFailureMode.FailClosed
                ? RateLimitDecision.RejectFailClosed(TimeSpan.FromSeconds(60))
                : RateLimitDecision.AllowFailOpen();
        }
    }
}
