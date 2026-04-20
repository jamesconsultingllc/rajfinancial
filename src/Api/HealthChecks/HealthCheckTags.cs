namespace RajFinancial.Api.HealthChecks;

/// <summary>
///     Canonical tag names used to group health checks. The <c>/health/ready</c>
///     endpoint filters checks by <see cref="Ready"/>; other probes (liveness) skip them.
/// </summary>
internal static class HealthCheckTags
{
    internal const string Ready = "ready";
}
