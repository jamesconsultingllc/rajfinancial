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

    // Tag keys (shared across auth functions and middleware so the metric
    // dimensions stay consistent and don't split on typos).
    internal const string EndpointTag = "endpoint";
    internal const string ReasonTag = "reason";
    internal const string SourceTag = "source";
    internal const string HttpRouteTag = "http.route";
    internal const string HttpMethodTag = "http.method";
    internal const string AuthOutcomeTag = "auth.outcome";
    internal const string AuthRolesCountTag = "auth.roles.count";

    // Outcomes / reasons.
    internal const string OutcomeMissingContext = "missing_context";

    // Activity names.
    internal const string ActivityGetMe = "Auth.GetMe";
    internal const string ActivityGetRoles = "Auth.GetRoles";
    internal const string ActivityStatus = "Auth.Status";
    internal const string ActivityClient = "Auth.Client";
    internal const string ActivityAdmin = "Auth.Admin";
    internal const string ActivityAuthenticate = "Auth.Authenticate";

    // Routes / endpoints.
    internal const string RouteAuthMe = "auth/me";
    internal const string RouteAuthRoles = "auth/roles";
    internal const string RouteAuthStatus = "auth/status";
    internal const string RouteAuthClient = "auth/client";
    internal const string RouteAuthAdmin = "auth/admin";

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
