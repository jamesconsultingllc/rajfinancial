using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace RajFinancial.Api.Middleware;

/// <summary>
///     Tags the current OpenTelemetry <see cref="Activity"/> with the
///     authenticated user's id, tenant id, and any allow-listed route values
///     extracted from the function trigger binding data.
/// </summary>
/// <remarks>
///     <para>
///         Runs <b>after</b> <see cref="UserProfileProvisioningMiddleware" />
///         (so the user context is populated by <see cref="AuthenticationMiddleware" />)
///         and <b>before</b> <see cref="Authorization.AuthorizationMiddleware" />
///         so authorization-related spans inherit the enriched tags.
///     </para>
///     <para>
///         Tagging is best-effort: missing claims/route values are skipped
///         silently and never block the pipeline.
///     </para>
/// </remarks>
public sealed class TelemetryEnrichmentMiddleware : IFunctionsWorkerMiddleware
{
    private const string TagUserId = "user.id";
    private const string TagUserTenantId = "user.tenant_id";

    private static readonly string[] RouteValueAllowList =
    [
        "assetId",
        "entityId",
        "entityRoleId",
        "grantId",
        "userId",
    ];

    /// <inheritdoc />
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var activity = Activity.Current;
        if (activity is not null)
        {
            EnrichActivity(activity, context);
        }

        await next(context);
    }

    private static void EnrichActivity(Activity activity, FunctionContext context)
    {
        var userId = context.GetUserIdAsGuid();
        if (userId is not null)
        {
            activity.SetTag(TagUserId, userId.Value.ToString());
        }

        var tenantId = context.GetTenantId();
        if (tenantId is not null)
        {
            activity.SetTag(TagUserTenantId, tenantId.Value.ToString());
        }

        var bindingData = context.BindingContext.BindingData;
        foreach (var routeKey in RouteValueAllowList)
        {
            if (bindingData.TryGetValue(routeKey, out var value) && value is not null)
            {
                activity.SetTag($"route.{routeKey}", value.ToString());
            }
        }
    }
}
