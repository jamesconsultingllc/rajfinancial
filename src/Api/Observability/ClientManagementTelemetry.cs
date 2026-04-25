using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Observability;

/// <summary>
///     Client-management OTel instruments shared by
///     <see cref="RajFinancial.Api.Functions.ClientManagementFunctions" /> and
///     <see cref="RajFinancial.Api.Services.ClientManagement.ClientManagementService" />.
///     Business counters (clientmanagement.grants.created.count /
///     clientmanagement.grants.revoked.count) are owned by
///     <see cref="RajFinancial.Api.Data.Interceptors.BusinessEventsInterceptor"/>
///     and exposed via <see cref="TelemetryMeters"/>. Only the validation-time
///     <c>clientmanagement.self_assignment.blocked.count</c> counter lives here —
///     it fires before SaveChangesAsync and therefore cannot come from the
///     interceptor.
/// </summary>
internal static class ClientManagementTelemetry
{
    internal const string SourceName = ObservabilityDomains.ClientManagement;

    private const string SelfAssignmentBlockedInstrument = "clientmanagement.self_assignment.blocked.count";

    // Tag keys.
    internal const string UserIdTag = "user.id";
    internal const string UserIsAdminTag = "user.is_admin";
    internal const string GrantIdTag = "grant.id";
    internal const string GrantTypeTag = "grant.type";
    internal const string GrantsCountTag = "grants.count";
    internal const string ClientUserIdTag = "client.user_id";
    internal const string GrantorUserIdTag = "grant.grantor_user_id";

    // Activity (span) names follow the Phase 1c decision (Option b2, see
    // docs/plans/phase-1c-span-validation.md §7):
    //   - Function-layer span:  ClientManagement.<Op>
    //   - Service-layer span:   ClientManagement.<Op>.Service
    internal const string ActivityAssignClient = "ClientManagement.AssignClient";
    internal const string ActivityGetClients = "ClientManagement.GetClients";
    internal const string ActivityRemoveClient = "ClientManagement.RemoveClient";
    internal const string ActivityAssignClientService = "ClientManagement.AssignClient.Service";
    internal const string ActivityGetClientsService = "ClientManagement.GetClients.Service";
    internal const string ActivityGetGrantByIdService = "ClientManagement.GetGrantById.Service";
    internal const string ActivityRemoveClientAccessService = "ClientManagement.RemoveClientAccess.Service";

    // Error/response codes used by the function endpoints.
    internal const string SelfAssignmentNotAllowedCode = "SELF_ASSIGNMENT_NOT_ALLOWED";
    internal const string ResourceNotFoundCode = "RESOURCE_NOT_FOUND";

    // Role names used by the defense-in-depth role check.
    internal const string RoleAdvisor = "Advisor";
    internal const string RoleAdministrator = "Administrator";

    private static readonly ActivitySource ActivitySource = new(SourceName);

    // Reuse the shared Meter so we never duplicate Meter names. Validation-time
    // counter only — successful grant create/revoke counters live on the
    // BusinessEventsInterceptor.
    private static readonly Counter<long> SelfAssignmentBlocked =
        TelemetryMeters.ClientManagementMeter.CreateCounter<long>(SelfAssignmentBlockedInstrument);

    internal static Activity? StartActivity(string name) => ActivitySource.StartActivity(name);

    internal static void RecordSelfAssignmentBlocked() => SelfAssignmentBlocked.Add(1);
}
