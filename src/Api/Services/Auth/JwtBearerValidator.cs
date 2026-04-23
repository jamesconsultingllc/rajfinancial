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

        var configResult = await LoadDiscoveryAsync(cancellationToken);
        if (configResult.Failure is { } discoveryFailure)
            return discoveryFailure;

        var validationParameters = BuildValidationParameters(configResult.Config!);
        return await ValidateWithRefreshAsync(bearerToken, validationParameters, cancellationToken);
    }

    private async Task<(OpenIdConnectConfiguration? Config, JwtValidationResult? Failure)> LoadDiscoveryAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return (await configurationManager.GetConfigurationAsync(cancellationToken), null);
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
            return (null, JwtValidationResult.Failure(AuthTelemetry.ReasonDiscoveryUnavailable));
        }
    }

    private TokenValidationParameters BuildValidationParameters(OpenIdConnectConfiguration config) => new()
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

    private async Task<JwtValidationResult> ValidateWithRefreshAsync(
        string bearerToken,
        TokenValidationParameters validationParameters,
        CancellationToken cancellationToken)
    {
        var hasRefreshedConfiguration = false;
        while (true)
        {
            try
            {
                var principal = handler.ValidateToken(bearerToken, validationParameters, out _);
                return JwtValidationResult.Success(principal);
            }
            catch (System.Exception ex) when (ex is not OperationCanceledException
                && !hasRefreshedConfiguration && IsKeyNotFound(ex))
            {
                // The token's kid isn't in our cached discovery document. Force a refresh
                // and retry once so routine key rotation doesn't fail authentication until
                // the automatic refresh interval elapses.
                if (!await TryRefreshSigningKeysAsync(validationParameters, cancellationToken, ex))
                    return JwtValidationResult.Failure(AuthTelemetry.ReasonInvalidSignature);
                hasRefreshedConfiguration = true;
            }
            catch (System.Exception ex) when (ex is not OperationCanceledException)
            {
                var reason = MapValidationException(ex);
                if (reason is null)
                    throw; // Unknown exception type — preserve original stack via in-catch rethrow.

                LogValidationFailed(reason, ex);
                return JwtValidationResult.Failure(reason);
            }
        }
    }

    private static bool IsKeyNotFound(System.Exception ex) =>
        ex is SecurityTokenSignatureKeyNotFoundException
        || (ex is SecurityTokenInvalidSignatureException inv
            && inv.InnerException is SecurityTokenSignatureKeyNotFoundException);

    private static string? MapValidationException(System.Exception ex) => ex switch
    {
        SecurityTokenExpiredException => AuthTelemetry.ReasonExpired,
        SecurityTokenInvalidSignatureException => AuthTelemetry.ReasonInvalidSignature,
        SecurityTokenInvalidAudienceException => AuthTelemetry.ReasonInvalidAudience,
        SecurityTokenInvalidIssuerException => AuthTelemetry.ReasonInvalidIssuer,
        SecurityTokenException => AuthTelemetry.ReasonInvalidToken,
        // ArgumentException is thrown for malformed tokens (e.g. wrong segment count).
        ArgumentException => AuthTelemetry.ReasonMalformed,
        _ => null,
    };

    /// <summary>
    ///     Forces the OIDC configuration manager to refresh its cached discovery document
    ///     and copies the fresh issuer / signing keys onto <paramref name="validationParameters"/>.
    ///     Returns <c>false</c> when the refresh itself fails so the caller can map the
    ///     outcome to <see cref="AuthTelemetry.ReasonInvalidSignature"/> instead of spinning
    ///     on a doomed retry.
    /// </summary>
    private async Task<bool> TryRefreshSigningKeysAsync(
        TokenValidationParameters validationParameters,
        CancellationToken cancellationToken,
        System.Exception triggeringException)
    {
        try
        {
            configurationManager.RequestRefresh();
            var refreshed = await configurationManager.GetConfigurationAsync(cancellationToken);
            validationParameters.ValidIssuer = refreshed.Issuer;
            validationParameters.IssuerSigningKeys = refreshed.SigningKeys;
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (System.Exception ex) when (ex is HttpRequestException or InvalidOperationException or OperationCanceledException)
        {
            LogValidationFailed(AuthTelemetry.ReasonInvalidSignature, triggeringException);
            return false;
        }
    }

    [LoggerMessage(EventId = 1106, Level = LogLevel.Warning,
        Message = "JWT validation failed: {Reason}")]
    private partial void LogValidationFailed(string reason, System.Exception ex);
}
