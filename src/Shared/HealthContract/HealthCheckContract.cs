namespace RajFinancial.Shared.HealthContract;

/// <summary>
///     Public contract names for the API readiness probe at <c>/api/health/ready</c>.
///     Centralised in <c>RajFinancial.Shared</c> so the API host AND the integration
///     test harness reference a single source of truth — no string drift between the
///     server-side payload and the client-side assertions.
/// </summary>
public static class HealthCheckContract
{
    /// <summary>Name of the database health check entry.</summary>
    public const string DatabaseCheckName = "database";

    /// <summary>Name of the configuration health check entry.</summary>
    public const string ConfigCheckName = "config";

    /// <summary>Name of the auth-validator health check entry.</summary>
    public const string AuthValidatorCheckName = "auth_validator";

    /// <summary>
    ///     Key under which the active <c>IJwtBearerValidator</c> implementation name is
    ///     reported in the <c>Data</c> dictionary of the auth-validator check.
    /// </summary>
    public const string AuthValidatorDataKey = "auth.validator";

    /// <summary>Validator-name value: production validator with full token validation.</summary>
    public const string AuthValidatorJwt = "jwt";

    /// <summary>Validator-name value: local-only unsigned validator (DEV ONLY).</summary>
    public const string AuthValidatorUnsignedLocal = "unsigned_local";
}
