using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;
using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Services.Authorization;

/// <summary>
/// Centralizes <see cref="ActivitySource"/>, <see cref="Meter"/>, and tag/instrument
/// names for the Authorization domain so <see cref="AuthorizationService"/> stays free
/// of private static helpers (architecture invariant) and so every tag key / metric
/// name is declared once.
/// </summary>
internal static class AuthorizationTelemetry
{
    // Activity + instrument names
    private const string ACTIVITY_CHECK_ACCESS = "Authorization.CheckAccess";
    private const string COUNTER_ALLOWED = "authorization.allowed.count";
    private const string COUNTER_DENIED = "authorization.denied.count";
    private const string HISTOGRAM_CHECK_DURATION = "authorization.check.duration.ms";

    // Tag keys — activity tags and metric tags share these so a typo/drift can't
    // split a dimension in Azure Monitor (e.g., `authz.tier` vs `authz_tier`).
    private const string TAG_USER_ID = "user.id";
    private const string TAG_RESOURCE_TYPE = "resource.type";
    private const string TAG_RESOURCE_OWNER_ID = "resource.owner.id";
    private const string TAG_AUTHZ_REQUIRED_LEVEL = "authz.required_level";
    private const string TAG_AUTHZ_GRANTED_LEVEL = "authz.granted_level";
    private const string TAG_AUTHZ_TIER = "authz.tier";
    private const string TAG_AUTHZ_REASON = "authz.reason";

    // authz.tier tag values — the tier that decided access.
    internal const string TIER_OWNER = "owner";
    internal const string TIER_GRANT = "grant";
    internal const string TIER_ADMIN = "admin";
    internal const string TIER_DENIED = "denied";

    private const string ACCESS_DENIED_STATUS_DESCRIPTION = "Access denied";

    private static readonly ActivitySource ActivitySource = new(ObservabilityDomains.Authorization);
    private static readonly Meter Meter = new(ObservabilityDomains.Authorization);

    private static readonly Counter<long> AuthzAllowed =
        Meter.CreateCounter<long>(COUNTER_ALLOWED);

    private static readonly Counter<long> AuthzDenied =
        Meter.CreateCounter<long>(COUNTER_DENIED);

    private static readonly Histogram<double> AuthzCheckDuration =
        Meter.CreateHistogram<double>(HISTOGRAM_CHECK_DURATION);

    /// <summary>
    /// Starts the <c>Authorization.CheckAccess</c> activity and tags it with the
    /// request envelope (user, resource type, owner, required level).
    /// </summary>
    internal static Activity? StartCheckAccess(
        Guid requestingUserId,
        string resourceType,
        Guid resourceOwnerId,
        AccessType requiredLevel)
    {
        var activity = ActivitySource.StartActivity(ACTIVITY_CHECK_ACCESS);
        if (activity is not null)
        {
            activity.SetTag(TAG_USER_ID, requestingUserId);
            activity.SetTag(TAG_RESOURCE_TYPE, resourceType);
            activity.SetTag(TAG_RESOURCE_OWNER_ID, resourceOwnerId);
            activity.SetTag(TAG_AUTHZ_REQUIRED_LEVEL, requiredLevel.ToString());
        }

        return activity;
    }

    /// <summary>
    /// Tags the activity with the granted access level (Tier 2 only).
    /// </summary>
    internal static void SetGrantedLevel(Activity? activity, AccessType grantedLevel)
        => activity?.SetTag(TAG_AUTHZ_GRANTED_LEVEL, grantedLevel.ToString());

    /// <summary>
    /// Tags the activity with the deciding tier and emits an allowed-count increment.
    /// Uses <see cref="TagList"/> to avoid allocating a params array on every call.
    /// </summary>
    internal static void RecordAllowed(Activity? activity, string tier, AccessDecisionReason reason)
    {
        activity?.SetTag(TAG_AUTHZ_TIER, tier);
        var tags = new TagList
        {
            { TAG_AUTHZ_TIER, tier },
            { TAG_AUTHZ_REASON, reason.ToString() },
        };
        AuthzAllowed.Add(1, tags);
    }

    /// <summary>
    /// Tags the activity as denied (sets <see cref="ActivityStatusCode.Error"/>) and
    /// emits a denied-count increment.
    /// </summary>
    internal static void RecordDenied(Activity? activity, AccessDecisionReason reason)
    {
        activity?.SetTag(TAG_AUTHZ_TIER, TIER_DENIED);
        activity?.SetStatus(ActivityStatusCode.Error, ACCESS_DENIED_STATUS_DESCRIPTION);
        var tags = new TagList
        {
            { TAG_AUTHZ_TIER, TIER_DENIED },
            { TAG_AUTHZ_REASON, reason.ToString() },
        };
        AuthzDenied.Add(1, tags);
    }

    /// <summary>Records the end-to-end duration of a <c>CheckAccessAsync</c> call.</summary>
    internal static void RecordCheckDuration(double milliseconds)
        => AuthzCheckDuration.Record(milliseconds);
}
