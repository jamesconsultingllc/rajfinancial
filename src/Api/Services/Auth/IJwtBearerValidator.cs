using System.Security.Claims;

namespace RajFinancial.Api.Services.Auth;

/// <summary>
///     Outcome of a JWT bearer token validation attempt.
/// </summary>
/// <param name="Principal">
///     The validated, authenticated principal on success; <c>null</c> on failure.
/// </param>
/// <param name="FailureReason">
///     Short, low-cardinality reason code for failure (e.g.
///     <c>expired</c>, <c>invalid_signature</c>, <c>discovery_unavailable</c>) suitable
///     as an OTel metric tag value. <c>null</c> on success. Match values exposed on
///     <see cref="RajFinancial.Api.Observability.AuthTelemetry"/> so dashboards can
///     pivot on the underlying cause.
/// </param>
public readonly record struct JwtValidationResult(ClaimsPrincipal? Principal, string? FailureReason)
{
    /// <summary>Convenience factory for the successful outcome.</summary>
    public static JwtValidationResult Success(ClaimsPrincipal principal) => new(principal, null);

    /// <summary>Convenience factory for the failed outcome.</summary>
    public static JwtValidationResult Failure(string reason) => new(null, reason);
}

/// <summary>
///     Validates an incoming bearer JWT and returns the resulting
///     <see cref="ClaimsPrincipal"/>, or a failure reason for telemetry.
/// </summary>
/// <remarks>
///     Two implementations exist:
///     <list type="bullet">
///         <item>
///             <see cref="JwtBearerValidator"/> — production validator that performs signature,
///             issuer, audience and lifetime validation against the Entra External ID
///             OpenID Connect discovery document.
///         </item>
///         <item>
///             <see cref="LocalUnsignedJwtValidator"/> — local-only validator used by
///             integration tests running against an unsigned test harness. Gated by the
///             <c>AUTH__USE_UNSIGNED_LOCAL_VALIDATOR</c> environment variable and rejected
///             at startup if <c>WEBSITE_SITE_NAME</c> is set (i.e. running on App Service).
///         </item>
///     </list>
/// </remarks>
public interface IJwtBearerValidator
{
    /// <summary>
    ///     Validates the supplied bearer token.
    /// </summary>
    /// <param name="bearerToken">
    ///     Raw JWT (the value that follows <c>Bearer </c> in the <c>Authorization</c> header).
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token. Implementations MUST allow <see cref="OperationCanceledException"/>
    ///     to propagate when the token is cancelled — they should never silently return a
    ///     "discovery unavailable" failure for a cancelled request.
    /// </param>
    /// <returns>
    ///     A <see cref="JwtValidationResult"/> with a populated <c>Principal</c> on success
    ///     or a low-cardinality <c>FailureReason</c> on failure. Implementations must not
    ///     throw on validation failure — return a failure result so the middleware can emit
    ///     a 401 via the standard chain.
    /// </returns>
    Task<JwtValidationResult> ValidateAsync(string bearerToken, CancellationToken cancellationToken);
}

