using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Services.RateLimit;

/// <summary>
///     OTel instruments for the rate-limit domain. Single source of truth — middleware,
///     store, and timer all emit through this class so counters cannot drift.
/// </summary>
/// <remarks>
///     Following AGENTS.md: ActivitySource and Meter share the
///     <see cref="ObservabilityDomains.RateLimit" /> name. The <c>user.id</c> tag is
///     intentionally OMITTED from metrics (high cardinality) — user identity goes on
///     traces and structured warning logs only.
/// </remarks>
internal static class RateLimitTelemetry
{
    internal const string SourceName = ObservabilityDomains.RateLimit;

    // Activity (span) names.
    internal const string ActivityCheck = "RateLimit.Check";
    internal const string ActivityStoreTryConsume = "RateLimit.Store.TryConsume";
    internal const string ActivityCleanup = "RateLimit.Cleanup";

    // Tag keys — centralized so producers never inline meaningful strings.
    internal const string PolicyKindTag = "ratelimit.policy.kind";
    internal const string OutcomeTag = "ratelimit.outcome";
    internal const string WindowTag = "ratelimit.window";
    internal const string FailureModeTag = "ratelimit.failure_mode";
    internal const string ErrorTypeTag = "error.type";
    internal const string CodeFunctionTag = "code.function";
    internal const string CleanupOlderThanTag = "ratelimit.cleanup.older_than";
    internal const string CleanupRowsDeletedTag = "ratelimit.cleanup.rows_deleted";

    // Outcome tag values.
    internal const string OutcomeAllowed = "allowed";
    internal const string OutcomeRejected = "rejected";
    internal const string OutcomeStoreError = "store_error";

    // Instrument names (dotted lowercase per AGENTS.md).
    private const string RequestsAllowedInstrument = "ratelimit.requests.allowed.count";
    private const string RequestsRejectedInstrument = "ratelimit.requests.rejected.count";
    private const string StoreErrorsInstrument = "ratelimit.store.errors.count";
    private const string StoreDurationInstrument = "ratelimit.store.duration.ms";
    private const string CleanupDurationInstrument = "ratelimit.cleanup.duration.ms";
    private const string CleanupRowsDeletedInstrument = "ratelimit.cleanup.rows_deleted.count";

    private static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);

    private static readonly Counter<long> RequestsAllowed = Meter.CreateCounter<long>(RequestsAllowedInstrument);
    private static readonly Counter<long> RequestsRejected = Meter.CreateCounter<long>(RequestsRejectedInstrument);
    private static readonly Counter<long> StoreErrors = Meter.CreateCounter<long>(StoreErrorsInstrument);
    private static readonly Histogram<double> StoreDuration = Meter.CreateHistogram<double>(StoreDurationInstrument);
    private static readonly Histogram<double> CleanupDuration = Meter.CreateHistogram<double>(CleanupDurationInstrument);
    private static readonly Counter<long> CleanupRowsDeleted = Meter.CreateCounter<long>(CleanupRowsDeletedInstrument);

    internal static Activity? StartActivity(string name) => ActivitySource.StartActivity(name);

    internal static void RecordAllowed(RateLimitPolicyKind kind, bool storeUnavailable)
    {
        var tags = new TagList
        {
            { PolicyKindTag, kind.ToString() },
            { OutcomeTag, storeUnavailable ? OutcomeStoreError : OutcomeAllowed },
        };
        RequestsAllowed.Add(1, tags);
    }

    internal static void RecordRejected(RateLimitPolicyKind kind, RateLimitWindow window, RateLimitFailureMode failureMode, bool storeUnavailable)
    {
        var tags = new TagList
        {
            { PolicyKindTag, kind.ToString() },
            { OutcomeTag, storeUnavailable ? OutcomeStoreError : OutcomeRejected },
            { WindowTag, window.ToString() },
            { FailureModeTag, failureMode.ToString() },
        };
        RequestsRejected.Add(1, tags);
    }

    internal static void RecordStoreError(string errorType)
    {
        var tags = new TagList { { ErrorTypeTag, errorType } };
        StoreErrors.Add(1, tags);
    }

    internal static void RecordStoreDuration(double elapsedMs)
    {
        StoreDuration.Record(elapsedMs);
    }

    internal static void RecordCleanupDuration(double elapsedMs)
    {
        CleanupDuration.Record(elapsedMs);
    }

    internal static void RecordCleanupRowsDeleted(long count)
    {
        CleanupRowsDeleted.Add(count);
    }
}
