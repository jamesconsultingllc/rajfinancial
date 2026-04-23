using RajFinancial.Shared.HealthContract;

namespace RajFinancial.Api.HealthChecks;

/// <summary>
///     Canonical names for registered health checks. Kept in one place so registration
///     (<see cref="Configuration.HealthCheckRegistration"/>) and any downstream
///     reporting / filtering references the same identifier.
/// </summary>
internal static class HealthCheckNames
{
    internal const string Database = HealthCheckContract.DatabaseCheckName;
    internal const string Config = HealthCheckContract.ConfigCheckName;
    internal const string AuthValidator = HealthCheckContract.AuthValidatorCheckName;
}

