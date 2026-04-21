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

    // Tag keys — centralized so middleware classes never inline meaningful strings.
    internal const string MiddlewareTag = "middleware";
    internal const string MiddlewareNameTag = "middleware.name";
    internal const string CodeFunctionTag = "code.function";
    internal const string ExceptionTypeTag = "exception.type";
    internal const string HttpStatusCodeTag = "http.status_code";

    // Activity (span) names.
    internal const string ActivityException = "Middleware.Exception";
    internal const string ActivityAuthorization = "Middleware.Authorization";
    internal const string ActivityContentNegotiation = "Middleware.ContentNegotiation";
    internal const string ActivityValidation = "Middleware.Validation";
    internal const string ActivityUserProfileProvisioning = "Middleware.UserProfileProvisioning";

    private static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);

    private static readonly Counter<long> Exceptions =
        Meter.CreateCounter<long>(ExceptionsInstrument);

    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>(DurationInstrument);

    internal static Activity? StartActivity(string name) => ActivitySource.StartActivity(name);

    // Uses TagList (struct) to avoid per-call KeyValuePair array allocation on the hot path.
    internal static void RecordException(string middlewareName, string? exceptionType, int statusCode)
    {
        var tags = new TagList
        {
            { MiddlewareTag, middlewareName },
            { ExceptionTypeTag, exceptionType },
            { HttpStatusCodeTag, statusCode },
        };
        Exceptions.Add(1, tags);
    }

    internal static void RecordDuration(string middlewareName, double elapsedMs)
    {
        var tags = new TagList { { MiddlewareTag, middlewareName } };
        Duration.Record(elapsedMs, tags);
    }
}
