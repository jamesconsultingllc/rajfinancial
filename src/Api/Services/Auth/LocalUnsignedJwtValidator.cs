using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RajFinancial.Api.Middleware;

namespace RajFinancial.Api.Services.Auth;

/// <summary>
///     Local-only validator that parses a JWT without verifying its signature.
///     Used exclusively by the integration-test harness that mints unsigned tokens
///     against a local Functions host — it mirrors the historical behaviour of
///     <c>AuthenticationMiddleware.TryParseJwtFromAuthorizationHeaderAsync</c> on
///     development hosts.
/// </summary>
/// <remarks>
///     <para>
///         Registration is gated by the <c>AUTH__USE_UNSIGNED_LOCAL_VALIDATOR</c>
///         environment variable. <see cref="Program"/> throws at startup if that
///         variable is set while <c>WEBSITE_SITE_NAME</c> is also set, guaranteeing
///         this type is never active on Azure App Service.
///     </para>
///     <para>
///         <see cref="JwtSecurityTokenHandler.MapInboundClaims"/> is set to <c>false</c>
///         so the resulting principal carries the original claim types — identical to
///         what <see cref="JwtBearerValidator"/> returns.
///     </para>
/// </remarks>
internal sealed partial class LocalUnsignedJwtValidator(
    ILogger<LocalUnsignedJwtValidator> logger) : IJwtBearerValidator
{
    private readonly JwtSecurityTokenHandler handler = new() { MapInboundClaims = false };

    /// <inheritdoc/>
    public Task<JwtValidationResult> ValidateAsync(string bearerToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            return Task.FromResult(JwtValidationResult.Failure(Observability.AuthTelemetry.ReasonMalformed));

        try
        {
            var jwt = handler.ReadJwtToken(bearerToken);
            // ClaimsIdentity authentication type so the local test principal is marked
            // as authenticated and clearly associated with a bearer token.
            var identity = new ClaimsIdentity(jwt.Claims, authenticationType: HttpHeaderNames.BearerSchemePrefix.TrimEnd());
            return Task.FromResult(JwtValidationResult.Success(new ClaimsPrincipal(identity)));
        }
        catch (System.Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException)
        {
            LogParseFailed(ex);
            return Task.FromResult(JwtValidationResult.Failure(Observability.AuthTelemetry.ReasonMalformed));
        }
    }

    [LoggerMessage(EventId = 1104, Level = LogLevel.Warning,
        Message = "Failed to parse unsigned JWT from Authorization header")]
    private partial void LogParseFailed(System.Exception ex);
}
