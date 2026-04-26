using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using RajFinancial.Api.Services.UserProfile;

namespace RajFinancial.Api.Middleware;

/// <summary>
///     Tags the current OpenTelemetry <see cref="Activity"/> with the
///     authenticated user's id, tenant id, role, the function's route template,
///     and any allow-listed route values extracted from the function trigger
///     binding data.
/// </summary>
/// <remarks>
///     <para>
///         Runs <b>after</b> <see cref="UserProfileProvisioningMiddleware" />
///         (so the user context is populated by <see cref="AuthenticationMiddleware" />)
///         and <b>before</b> <see cref="Authorization.AuthorizationMiddleware" />
///         so authorization-related spans inherit the enriched tags.
///     </para>
///     <para>
///         Tagging is best-effort: missing claims/route values/function metadata
///         are skipped silently and never block the pipeline.
///     </para>
///     <para>
///         The route-value allow-list mirrors the binding keys that Azure
///         Functions HTTP triggers actually populate from route templates such
///         as <c>assets/{id}</c>, <c>entities/{id}/roles/{roleId}</c>, and
///         <c>auth/clients/{id}</c>. Because the same binding key (<c>id</c>)
///         is reused across resources, dashboards must combine
///         <c>route.id</c> with <c>route.template</c> to disambiguate.
///     </para>
/// </remarks>
public sealed class TelemetryEnrichmentMiddleware : IFunctionsWorkerMiddleware
{
    private const string TagUserId = "user.id";
    private const string TagUserTenantId = "user.tenant_id";
    private const string TagUserRole = "user.role";
    private const string TagRouteTemplate = "route.template";

    private const string HttpTriggerBindingType = "httpTrigger";

    // Real Azure Functions HTTP triggers in this codebase use these binding
    // keys (derived from `{id}`, `{roleId}`, and `{userId}` in the route
    // templates declared on `[HttpTrigger]`). Earlier semantic names like
    // `assetId`/`entityId` never appear in the runtime BindingData and were a
    // source of silently-dropped tags.
    private static readonly string[] RouteValueAllowList =
    [
        "id",
        "roleId",
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

        var roles = context.GetUserRoles();
        if (roles.Count > 0)
        {
            activity.SetTag(TagUserRole, roles.MapHighestPriority().ToString());
        }

        TagRouteTemplateIfHttpTrigger(activity, context);

        var bindingContext = SafeGetBindingContext(context);
        if (bindingContext is not null)
        {
            var bindingData = bindingContext.BindingData;
            foreach (var routeKey in RouteValueAllowList)
            {
                if (bindingData.TryGetValue(routeKey, out var value) && value is not null)
                {
                    activity.SetTag($"route.{routeKey}", value.ToString());
                }
            }
        }
    }

    private static void TagRouteTemplateIfHttpTrigger(Activity activity, FunctionContext context)
    {
        var definition = SafeGetFunctionDefinition(context);
        if (definition is null)
        {
            return;
        }

        var hasHttpTrigger = false;
        foreach (var binding in definition.InputBindings.Values)
        {
            if (string.Equals(binding.Type, HttpTriggerBindingType, StringComparison.OrdinalIgnoreCase))
            {
                hasHttpTrigger = true;
                break;
            }
        }

        if (!hasHttpTrigger)
        {
            return;
        }

        // The Azure Functions worker SDK does not surface the route template
        // string on `BindingMetadata`. The function's name (set via
        // `[Function("GetAssetById")]`) is the next best stable identifier and
        // gives dashboards a 1:1 distinguisher per endpoint.
        var name = definition.Name;
        if (!string.IsNullOrEmpty(name))
        {
            activity.SetTag(TagRouteTemplate, name);
        }
    }

    private static FunctionDefinition? SafeGetFunctionDefinition(FunctionContext context)
    {
        try
        {
            return context.FunctionDefinition;
        }
        catch
        {
            return null;
        }
    }

    private static BindingContext? SafeGetBindingContext(FunctionContext context)
    {
        try
        {
            return context.BindingContext;
        }
        catch
        {
            return null;
        }
    }
}
