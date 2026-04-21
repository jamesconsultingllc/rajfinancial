// ============================================================================
// RAJ Financial - UserProfile Telemetry
// ============================================================================
// Centralized ActivitySource, Meter, instrument names, tag keys and activity
// names for the UserProfile domain. Extracted out of UserProfileService and
// ProfileFunctions to satisfy the Services_ShouldNotHavePrivateStaticMethods
// architecture invariant and the AGENT.md "No Magic Strings or Numbers" rule.
//
// NOTE for future contributors (tracked by ADO #628):
// The three business counters below are STAGING for this PR only:
//   - userprofile.jit.provisioned.count
//   - userprofile.sync.count
//   - userprofile.concurrent.conflicts.count
// These will be removed/consolidated once #628 lands an EF
// SaveChangesInterceptor that emits equivalent business counters centrally
// (tagged by db.entity_type). Do NOT add new per-domain counter helpers in
// other domains; let #628 do them centrally.
//
// The histogram `userprofile.ensure.duration.ms` and the ActivitySource /
// activity names stay - they measure the EnsureProfileExists request path,
// not SaveChanges, and are outside the scope of #628.
// ============================================================================

using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Services.UserProfile;

/// <summary>
///     Owns all OpenTelemetry primitives for the UserProfile instrumentation domain.
/// </summary>
internal static class UserProfileTelemetry
{
    // Activity names (service layer)
    internal const string ActivityEnsureProfileExists = "UserProfile.EnsureProfileExists";
    internal const string ActivityGetById = "UserProfile.GetById";
    internal const string ActivityUpdateProfile = "UserProfile.UpdateProfile";

    // Tag keys
    internal const string TagUserId = "user.id";
    internal const string TagUserOid = "user.oid";
    internal const string TagUserTenantId = "user.tenant_id";

    // Metric instrument names
    private const string METRIC_JIT_PROVISIONED = "userprofile.jit.provisioned.count";
    private const string METRIC_SYNC_COUNT = "userprofile.sync.count";
    private const string METRIC_CONCURRENT_CONFLICTS = "userprofile.concurrent.conflicts.count";
    private const string METRIC_ENSURE_DURATION = "userprofile.ensure.duration.ms";

    internal static readonly ActivitySource ActivitySource = new(ObservabilityDomains.UserProfile);
    private static readonly Meter Meter = new(ObservabilityDomains.UserProfile);

    internal static readonly Counter<long> JitProvisioned =
        Meter.CreateCounter<long>(METRIC_JIT_PROVISIONED);

    internal static readonly Counter<long> SyncCount =
        Meter.CreateCounter<long>(METRIC_SYNC_COUNT);

    internal static readonly Counter<long> ConcurrentConflicts =
        Meter.CreateCounter<long>(METRIC_CONCURRENT_CONFLICTS);

    internal static readonly Histogram<double> EnsureDuration =
        Meter.CreateHistogram<double>(METRIC_ENSURE_DURATION);
}
