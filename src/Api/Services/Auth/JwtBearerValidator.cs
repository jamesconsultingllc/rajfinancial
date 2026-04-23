using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RajFinancial.Api.Configuration;
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
    private readonly EntraExternalIdOptions options = options.Value;
    private readonly JwtSecurityTokenHandler handler = new() { MapInboundClaims = false };

    /// <inheritdoc/>
    public async Task<ClaimsPrincipal?> ValidateAsync(string bearerToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            return null;

        OpenIdConnectConfiguration config;
        try
        {
            config = await configurationManager.GetConfigurationAsync(cancellationToken);
        }
        catch (System.Exception ex) when (ex is HttpRequestException or InvalidOperationException or OperationCanceledException)
        {
            LogValidationFailed("discovery_unavailable", ex);
            return null;
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
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "name",
            RoleClaimType = "roles",
        };

        try
        {
            var principal = handler.ValidateToken(bearerToken, validationParameters, out _);
            return principal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            LogValidationFailed("expired", ex);
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            LogValidationFailed("invalid_signature", ex);
            return null;
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            LogValidationFailed("invalid_audience", ex);
            return null;
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            LogValidationFailed("invalid_issuer", ex);
            return null;
        }
        catch (SecurityTokenException ex)
        {
            LogValidationFailed("invalid_token", ex);
            return null;
        }
        catch (ArgumentException ex)
        {
            // Thrown for malformed tokens (e.g. wrong segment count).
            LogValidationFailed("malformed", ex);
            return null;
        }
    }

    [LoggerMessage(EventId = 1106, Level = LogLevel.Warning,
        Message = "JWT validation failed: {Reason}")]
    private partial void LogValidationFailed(string reason, System.Exception ex);
}
