// ============================================================================
// RAJ Financial - UserProfile Telemetry
// ============================================================================
// Centralized ActivitySource, instrument names, tag keys and activity names
// for the UserProfile domain. Extracted out of UserProfileService and
// ProfileFunctions to satisfy the Services_ShouldNotHavePrivateStaticMethods
// architecture invariant and the AGENTS.md "No Magic Strings or Numbers" rule.
//
// Business counters (userprofile.jit.provisioned.count / userprofile.sync.count
// / userprofile.concurrent.conflicts.count) are owned by
// `RajFinancial.Api.Data.Interceptors.BusinessEventsInterceptor` and exposed
// via `RajFinancial.Api.Observability.TelemetryMeters`. The histogram below
// (`userprofile.ensure.duration.ms`) measures the EnsureProfileExists request
// path and stays here.
// ============================================================================

using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Observability;

namespace RajFinancial.Api.Services.UserProfile;

/// <summary>
///     Owns the UserProfile-domain ActivitySource and EnsureDuration histogram.
///     Business counters live on <see cref="TelemetryMeters"/> and are emitted
///     by <see cref="RajFinancial.Api.Data.Interceptors.BusinessEventsInterceptor"/>.
/// </summary>
internal static class UserProfileTelemetry
{
    // Activity names (service layer)
    internal const string ActivityEnsureProfileExistsService = "UserProfile.EnsureProfileExists.Service";
    internal const string ActivityGetById = "UserProfile.GetById";
    internal const string ActivityUpdateProfile = "UserProfile.UpdateProfile";

    // Tag keys
    internal const string TagUserId = "user.id";
    internal const string TagUserTenantId = "user.tenant_id";

    // Metric instrument names
    private const string METRIC_ENSURE_DURATION = "userprofile.ensure.duration.ms";

    internal static readonly ActivitySource ActivitySource = new(ObservabilityDomains.UserProfile);

    // Reuse the shared Meter instance so we never end up with two Meter
    // objects sharing a name (which would double-emit).
    internal static readonly Histogram<double> EnsureDuration =
        TelemetryMeters.UserProfileMeter.CreateHistogram<double>(METRIC_ENSURE_DURATION);
}
