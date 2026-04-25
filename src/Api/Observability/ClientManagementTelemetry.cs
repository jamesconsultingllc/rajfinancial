using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Observability;

/// <summary>
///     Client-management OTel instruments shared by
///     <see cref="RajFinancial.Api.Functions.ClientManagementFunctions" /> and
///     <see cref="RajFinancial.Api.Services.ClientManagement.ClientManagementService" />. Exposes wrapper methods
///     so callers don't take a direct dependency on <see cref="Counter{T}" /> /
///     <see cref="ActivitySource" /> (keeps callers under Sonar S1200) and so the counters are
///     registered exactly once per process (avoids the duplicate-Meter emission bug).
/// </summary>
internal static class ClientManagementTelemetry
{
    internal const string SourceName = ObservabilityDomains.ClientManagement;

    private const string GrantsCreatedInstrument = "clientmanagement.grants.created.count";
    private const string GrantsRevokedInstrument = "clientmanagement.grants.revoked.count";
    private const string SelfAssignmentBlockedInstrument = "clientmanagement.self_assignment.blocked.count";

    // NOTE: Per-domain business counters (clientmanagement.*) are tactical and expected to be
    // consolidated into a centralized counter registry via ADO #628. Do not add new
    // per-domain counters in other domains without first checking that work item.
    // See AssetsTelemetry.cs:9-16 and UserProfileTelemetry.cs:9-17 for the same guidance.

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
    private static readonly Meter Meter = new(SourceName);

    private static readonly Counter<long> GrantsCreated =
        Meter.CreateCounter<long>(GrantsCreatedInstrument);

    private static readonly Counter<long> GrantsRevoked =
        Meter.CreateCounter<long>(GrantsRevokedInstrument);

    private static readonly Counter<long> SelfAssignmentBlocked =
        Meter.CreateCounter<long>(SelfAssignmentBlockedInstrument);

    internal static Activity? StartActivity(string name) => ActivitySource.StartActivity(name);

    internal static void RecordGrantCreated() => GrantsCreated.Add(1);

    internal static void RecordGrantRevoked() => GrantsRevoked.Add(1);

    internal static void RecordSelfAssignmentBlocked() => SelfAssignmentBlocked.Add(1);
}
