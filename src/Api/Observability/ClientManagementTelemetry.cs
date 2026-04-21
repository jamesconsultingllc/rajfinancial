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

    private const string GrantsCreatedInstrument = "clientmgmt.grants.created.count";
    private const string GrantsRevokedInstrument = "clientmgmt.grants.revoked.count";
    private const string SelfAssignmentBlockedInstrument = "clientmgmt.self_assignment.blocked.count";

    // NOTE: Per-domain business counters (clientmgmt.*) are tactical and expected to be
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

    // Activity (span) names. Endpoint spans are emitted by the HTTP function; service spans are
    // emitted by the domain service. Keeping them distinct makes nested traces easier to read
    // (endpoint parent → service child), per review feedback on PR #86.
    internal const string ActivityAssignClient = "ClientMgmt.AssignClient";
    internal const string ActivityGetClients = "ClientMgmt.GetClients";
    internal const string ActivityRemoveClient = "ClientMgmt.RemoveClient";
    internal const string ActivityAssignClientService = "ClientMgmt.AssignClient.Service";
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
