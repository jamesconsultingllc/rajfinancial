using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Observability;

/// <summary>
///     Client-management OTel instruments shared by
///     <see cref="Functions.ClientManagementFunctions" /> and
///     <see cref="Services.ClientManagement.ClientManagementService" />. Exposes wrapper methods
///     so callers don't take a direct dependency on <see cref="Counter{T}" /> /
///     <see cref="ActivitySource" /> (keeps callers under Sonar S1200) and so the counters are
///     registered exactly once per process (avoids the duplicate-Meter emission bug).
/// </summary>
internal static class ClientManagementTelemetry
{
    internal const string SourceName = ObservabilityDomains.ClientManagement;

    private const string GrantsCreatedInstrument = "clientmgmt.grants.created.count";
    private const string GrantsRevokedInstrument = "clientmgmt.grants.revoked.count";
    private const string SelfAssignmentBlockedInstrument = "clientmgmt.self_assignment.blocked.count";

    // Tag keys.
    internal const string UserIdTag = "user.id";
    internal const string UserIsAdminTag = "user.is_admin";
    internal const string GrantIdTag = "grant.id";
    internal const string GrantTypeTag = "grant.type";
    internal const string GrantsCountTag = "grants.count";
    internal const string ClientUserIdTag = "client.user_id";

    // Activity (span) names.
    internal const string ActivityAssignClient = "ClientMgmt.AssignClient";
    internal const string ActivityGetClients = "ClientMgmt.GetClients";
    internal const string ActivityRemoveClient = "ClientMgmt.RemoveClient";
    internal const string ActivityGetClientAssignments = "ClientMgmt.GetClientAssignments";
    internal const string ActivityGetGrantById = "ClientMgmt.GetGrantById";
    internal const string ActivityRemoveClientAccess = "ClientMgmt.RemoveClientAccess";

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
