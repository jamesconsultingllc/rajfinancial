namespace RajFinancial.Api.HealthChecks;

/// <summary>
///     Canonical names for registered health checks. Kept in one place so registration
///     (<see cref="Configuration.HealthCheckRegistration"/>) and any downstream
///     reporting / filtering references the same identifier.
/// </summary>
internal static class HealthCheckNames
{
    internal const string Database = "database";
    internal const string Config = "config";
    internal const string AuthValidator = "auth_validator";
}
