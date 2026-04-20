namespace RajFinancial.Api.Configuration;

/// <summary>
///     Operational constants for observability wiring (file logging, tracing sampler).
/// </summary>
/// <remarks>
///     Extracted per AGENT.md "No Magic Strings or Numbers". Values are pinned in code
///     today; if tuning per-environment becomes common, promote to an options object
///     bound from configuration.
/// </remarks>
internal static class ObservabilityConstants
{
    /// <summary>Rolling file log path template used by NReco.Logging.File in Development.</summary>
    internal const string FileLogPathTemplate = "logs/rajfinancial-{Date}.log";

    /// <summary>Maximum number of rolled log files retained before the oldest is deleted.</summary>
    internal const int MaxRollingFiles = 7;

    /// <summary>Per-file size cap (10 MiB) before rolling to the next file.</summary>
    internal const long FileSizeLimitBytes = 10L * 1024L * 1024L;

    /// <summary>
    ///     Default trace sampling ratio (0.0–1.0) used outside Development. 10% keeps
    ///     Azure Monitor ingestion cost predictable while preserving trace coherency via
    ///     <c>ParentBasedSampler</c>. Tune if traffic/cost profile changes.
    /// </summary>
    internal const double DefaultTraceSampleRatio = 0.1;
}
