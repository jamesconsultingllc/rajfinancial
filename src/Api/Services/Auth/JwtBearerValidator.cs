using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Observability;

namespace RajFinancial.Api.Services.Auth;

/// <summary>
///     Production JWT bearer validator that performs full signature, issuer,
///     audience and lifetime validation against the Entra External ID OpenID
///     Connect discovery document.
/// </summary>
/// <remarks>
///     <para>
///         The signing keys and canonical issuer are fetched on demand via an injected
///         <see cref="IConfigurationManager{OpenIdConnectConfiguration}"/> that caches the
///         discovery document and performs automatic key rotation.
///     </para>
///     <para>
///         <see cref="TokenValidationParameters"/> are built per call so the validator
///         always picks up the latest signing keys and never pins a stale snapshot.
///     </para>
///     <para>
///         <see cref="JwtSecurityTokenHandler.MapInboundClaims"/> is explicitly set to
///         <c>false</c> so the resulting principal carries the original claim types
///         (<c>oid</c>, <c>emails</c>, <c>roles</c>, ...) — matching the unsigned fallback
///         parse used elsewhere and keeping the downstream claim helpers consistent.
///     </para>
/// </remarks>
internal sealed partial class JwtBearerValidator(
    IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
    IOptions<EntraExternalIdOptions> options,
    ILogger<JwtBearerValidator> logger) : IJwtBearerValidator
{
    /// <summary>
    ///     Allowance for clock skew between the token issuer and this host. Matches the
    ///     long-standing default in <c>Microsoft.AspNetCore.Authentication.JwtBearer</c>.
    /// </summary>
    internal static readonly TimeSpan DefaultClockSkew = TimeSpan.FromMinutes(5);

    private readonly EntraExternalIdOptions options = options.Value;
    private readonly JwtSecurityTokenHandler handler = new() { MapInboundClaims = false };

    /// <inheritdoc/>
    public async Task<JwtValidationResult> ValidateAsync(string bearerToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            return JwtValidationResult.Failure(AuthTelemetry.ReasonMalformed);

        OpenIdConnectConfiguration config;
        try
        {
            config = await configurationManager.GetConfigurationAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Honour caller cancellation — never silently swallow into a
            // discovery_unavailable failure that would let the pipeline run
            // as an unauthenticated request.
            throw;
        }
        catch (System.Exception ex) when (ex is HttpRequestException or InvalidOperationException or OperationCanceledException)
        {
            LogValidationFailed(AuthTelemetry.ReasonDiscoveryUnavailable, ex);
            return JwtValidationResult.Failure(AuthTelemetry.ReasonDiscoveryUnavailable);
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config.Issuer,
            ValidateAudience = true,
            ValidAudiences = options.ValidAudiences,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = config.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = DefaultClockSkew,
            NameClaimType = JwtClaimNames.Name,
            RoleClaimType = JwtClaimNames.Roles,
            // Match LocalUnsignedJwtValidator so the resulting identity's AuthenticationType
            // is "Bearer" in both modes. Keeps any downstream code that branches on
            // Identity.AuthenticationType behaving identically.
            AuthenticationType = HttpHeaderNames.BearerSchemePrefix.TrimEnd(),
        };

        try
        {
            var principal = handler.ValidateToken(bearerToken, validationParameters, out _);
            return JwtValidationResult.Success(principal);
        }
        catch (SecurityTokenExpiredException ex)
        {
            LogValidationFailed(AuthTelemetry.ReasonExpired, ex);
            return JwtValidationResult.Failure(AuthTelemetry.ReasonExpired);
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            LogValidationFailed(AuthTelemetry.ReasonInvalidSignature, ex);
            return JwtValidationResult.Failure(AuthTelemetry.ReasonInvalidSignature);
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            LogValidationFailed(AuthTelemetry.ReasonInvalidAudience, ex);
            return JwtValidationResult.Failure(AuthTelemetry.ReasonInvalidAudience);
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            LogValidationFailed(AuthTelemetry.ReasonInvalidIssuer, ex);
            return JwtValidationResult.Failure(AuthTelemetry.ReasonInvalidIssuer);
        }
        catch (SecurityTokenException ex)
        {
            LogValidationFailed(AuthTelemetry.ReasonInvalidToken, ex);
            return JwtValidationResult.Failure(AuthTelemetry.ReasonInvalidToken);
        }
        catch (ArgumentException ex)
        {
            // Thrown for malformed tokens (e.g. wrong segment count).
            LogValidationFailed(AuthTelemetry.ReasonMalformed, ex);
            return JwtValidationResult.Failure(AuthTelemetry.ReasonMalformed);
        }
    }

    [LoggerMessage(EventId = 1106, Level = LogLevel.Warning,
        Message = "JWT validation failed: {Reason}")]
    private partial void LogValidationFailed(string reason, System.Exception ex);
}
