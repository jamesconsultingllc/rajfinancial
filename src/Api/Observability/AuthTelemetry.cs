using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Observability;

/// <summary>
///     Authentication/authorization OTel instruments shared by auth-related middleware
///     and functions. Exposes wrapper methods so callers don't take a direct dependency
///     on <see cref="Counter{T}" /> / <see cref="ActivitySource" /> (keeps callers under
///     Sonar S1200) and so the counters are registered exactly once per process.
/// </summary>
internal static class AuthTelemetry
{
    internal const string SourceName = ObservabilityDomains.Auth;

    private const string SuccessesInstrument = "auth.successes.count";
    private const string FailuresInstrument = "auth.failures.count";

    private static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);

    private static readonly Counter<long> Successes =
        Meter.CreateCounter<long>(SuccessesInstrument);

    private static readonly Counter<long> Failures =
        Meter.CreateCounter<long>(FailuresInstrument);

    internal static Activity? StartActivity(string name) => ActivitySource.StartActivity(name);

    internal static void RecordSuccess(params KeyValuePair<string, object?>[] tags) =>
        Successes.Add(1, tags);

    internal static void RecordFailure(params KeyValuePair<string, object?>[] tags) =>
        Failures.Add(1, tags);
}
