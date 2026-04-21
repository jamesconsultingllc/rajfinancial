using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Middleware;

/// <summary>
///     Middleware-level OTel instruments shared by middleware classes.
///     Exposes wrapper methods so consumers don't take a direct dependency on
///     <see cref="Counter{T}" />, <see cref="Histogram{T}" />, and
///     <see cref="ActivitySource" /> (keeps callers under Sonar S1200).
/// </summary>
internal static class MiddlewareTelemetry
{
    internal const string SourceName = ObservabilityDomains.Middleware;

    private const string ExceptionsInstrument = "middleware.exceptions.count";
    private const string DurationInstrument = "middleware.duration.ms";

    private static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);

    private static readonly Counter<long> Exceptions =
        Meter.CreateCounter<long>(ExceptionsInstrument);

    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>(DurationInstrument);

    internal static Activity? StartActivity(string name) => ActivitySource.StartActivity(name);

    internal static void RecordException(string middlewareName, string? exceptionType, int statusCode)
    {
        Exceptions.Add(1,
            new KeyValuePair<string, object?>("middleware", middlewareName),
            new KeyValuePair<string, object?>("exception.type", exceptionType),
            new KeyValuePair<string, object?>("http.status_code", statusCode));
    }

    internal static void RecordDuration(string middlewareName, double elapsedMs)
    {
        Duration.Record(elapsedMs,
            new KeyValuePair<string, object?>("middleware", middlewareName));
    }
}
