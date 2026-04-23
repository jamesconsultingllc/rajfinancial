using RajFinancial.Shared.HealthContract;

namespace RajFinancial.Api.Services.Auth;

/// <summary>
///     Canonical names for the <see cref="IJwtBearerValidator"/> implementations
///     surfaced via the <c>auth_validator</c> readiness health check. Backed by
///     <see cref="HealthCheckContract"/> so the API and integration tests share a
///     single source of truth.
/// </summary>
public static class AuthValidatorNames
{
    /// <summary>Production validator that performs full signature/issuer/audience/lifetime checks.</summary>
    public const string Jwt = HealthCheckContract.AuthValidatorJwt;

    /// <summary>Local-only validator that skips signature verification (DEV ONLY).</summary>
    public const string UnsignedLocal = HealthCheckContract.AuthValidatorUnsignedLocal;

    /// <summary>Health-check <c>Data</c> dictionary key under which the active validator name is reported.</summary>
    public const string DataKey = HealthCheckContract.AuthValidatorDataKey;
}

