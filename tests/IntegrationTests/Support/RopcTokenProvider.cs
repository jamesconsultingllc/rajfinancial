using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Acquires real Entra External ID tokens via the ROPC flow for production integration tests.
/// Tokens are cached per username for the duration of the test run to minimize Entra round-trips.
/// </summary>
/// <remarks>
/// ROPC is used ONLY for automated testing — never in production application code.
/// Requires a public client app registration with ROPC enabled in Entra External ID.
/// </remarks>
public class RopcTokenProvider
{
    private readonly string? tenantId;
    private readonly string? clientId;
    private readonly string? apiScope;
    private readonly ConcurrentDictionary<string, AuthenticationResult> tokenCache = new();
    private readonly Lazy<IPublicClientApplication> app;

    public RopcTokenProvider(IConfiguration configuration)
    {
        tenantId = configuration["Entra:TenantId"];
        clientId = configuration["Entra:RopcClientId"];
        apiScope = configuration["Entra:ApiScope"];

        // Thread-safe lazy initialization of the MSAL public client application.
        // The factory validates config on first access rather than relying on null-forgiving operators.
        app = new Lazy<IPublicClientApplication>(() =>
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "ROPC is not configured. Set Entra:TenantId, Entra:RopcClientId, and Entra:ApiScope " +
                    "in appsettings.json (or Entra__TenantId, Entra__RopcClientId, Entra__ApiScope as environment variables).");
            }

            return PublicClientApplicationBuilder
                .Create(clientId!)
                // Entra External ID tenants use the {tenantSubdomain}.ciamlogin.com authority.
                // The TenantId value should be the tenant subdomain (e.g., "contoso"), not a GUID.
                .WithAuthority($"https://{tenantId!}.ciamlogin.com/")
                .Build();
        });
    }

    /// <summary>
    /// Returns true when all required ROPC configuration values are present.
    /// When false, callers should fall back to unsigned test JWTs (dev mode).
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(tenantId) &&
        !string.IsNullOrWhiteSpace(clientId) &&
        !string.IsNullOrWhiteSpace(apiScope);

    /// <summary>
    /// Acquires a Bearer token for the given username/password via ROPC.
    /// Results are cached per username for the lifetime of this provider instance.
    /// </summary>
    /// <param name="username">The Entra user's email/UPN.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="scopes">Optional scopes override. Defaults to the configured API scope.</param>
    /// <returns>A Bearer token string suitable for the Authorization header.</returns>
    /// <exception cref="InvalidOperationException">When ROPC is not configured.</exception>
    /// <exception cref="MsalException">When authentication fails.</exception>
    public async Task<string> GetTokenAsync(string username, string password, string[]? scopes = null)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "ROPC is not configured. Set Entra:TenantId, Entra:RopcClientId, and Entra:ApiScope " +
                "in appsettings.json (or Entra__TenantId, Entra__RopcClientId, Entra__ApiScope as environment variables).");

        // Return cached token if still valid (10-minute buffer prevents edge-case expiry races)
        if (tokenCache.TryGetValue(username, out var cached) && cached.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(10))
            return cached.AccessToken;

#pragma warning disable CS0618 // ROPC is deprecated but required for headless CI test auth against Entra External ID
        var result = await app.Value.AcquireTokenByUsernamePassword(
            scopes ?? [apiScope!],
            username,
            password)
            .ExecuteAsync();
#pragma warning restore CS0618

        tokenCache[username] = result;
        return result.AccessToken;
    }
}
