using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Acquires real Entra External ID tokens via the ROPC flow for production integration tests.
/// Tokens are cached per username for the duration of the test run to minimize Entra round-trips.
/// Uses single-flight pattern to prevent concurrent duplicate token requests that trigger AAD throttling.
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
    private readonly ConcurrentDictionary<string, AuthenticationResult> tokenCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Lazy<Task<AuthenticationResult>>> inFlight = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lazy<IPublicClientApplication> app;

    private const int MAX_RETRIES = 3;
    private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(2);

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

            // Entra External ID (CIAM) authority requires the format:
            //   https://{subdomain}.ciamlogin.com/{subdomain}.onmicrosoft.com
            // Accept TenantId as subdomain, subdomain.onmicrosoft.com, or subdomain.ciamlogin.com.
            var tenantSubdomain = tenantId!
                .Replace(".onmicrosoft.com", "", StringComparison.OrdinalIgnoreCase)
                .Replace(".ciamlogin.com", "", StringComparison.OrdinalIgnoreCase);

            if (Guid.TryParse(tenantSubdomain, out _))
            {
                throw new InvalidOperationException(
                    $"Entra:TenantId is a GUID ('{tenantId}'). " +
                    "Entra External ID (CIAM) requires the tenant subdomain (e.g., 'rajfinancialdev'), " +
                    "not the directory/tenant GUID. Update the ENTRA_TENANT_ID secret to the subdomain value.");
            }

            return PublicClientApplicationBuilder
                .Create(clientId!)
                .WithAuthority($"https://{tenantSubdomain}.ciamlogin.com/{tenantSubdomain}.onmicrosoft.com")
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
    /// Uses a single-flight pattern: only one token request per user is in-flight at a time.
    /// All concurrent callers for the same user await the same request.
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

        var effectiveScopes = scopes ?? [apiScope!];

        // Fast path: return cached token if still valid (10-minute buffer prevents edge-case expiry races)
        if (tokenCache.TryGetValue(username, out var cached) && cached.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(10))
            return cached.AccessToken;

        // Single-flight: all concurrent callers for the same user share one in-flight request.
        // This prevents the stampede of parallel ROPC calls that triggers AAD throttling.
        var lazyTask = inFlight.GetOrAdd(username, x => new Lazy<Task<AuthenticationResult>>(
            () => AcquireTokenWithRetryAsync(x, password, effectiveScopes)));

        try
        {
            var result = await lazyTask.Value;
            tokenCache[username] = result;
            return result.AccessToken;
        }
        catch
        {
            // Remove failed entry so the next caller can retry
            inFlight.TryRemove(username, out _);
            throw;
        }
        finally
        {
            // Clean up completed in-flight entry (successful ones stay in tokenCache)
            if (lazyTask is { IsValueCreated: true, Value.IsCompletedSuccessfully: true })
                inFlight.TryRemove(username, out _);
        }
    }

    /// <summary>
    /// Acquires a token, trying MSAL's silent cache first, then ROPC with retry/backoff for throttling.
    /// </summary>
    private async Task<AuthenticationResult> AcquireTokenWithRetryAsync(
        string username, string password, string[] scopes)
    {
        // Try MSAL's built-in token cache first (handles refresh tokens automatically)
        var accounts = await app.Value.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a =>
            string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));

        if (account != null)
        {
            try
            {
                return await app.Value.AcquireTokenSilent(scopes, account).ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                // Silent acquisition failed — fall through to ROPC
            }
        }

        // ROPC with exponential backoff + jitter for throttling errors
        for (int attempt = 0; ; attempt++)
        {
            try
            {
#pragma warning disable CS0618 // ROPC is deprecated but required for headless CI test auth against Entra External ID
                return await app.Value.AcquireTokenByUsernamePassword(scopes, username, password)
                    .ExecuteAsync();
#pragma warning restore CS0618
            }
            catch (MsalServiceException ex) when (IsThrottlingError(ex) && attempt < MAX_RETRIES)
            {
                var delay = GetRetryDelay(ex, attempt);
                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// Determines if an MSAL exception indicates AAD throttling.
    /// </summary>
    private static bool IsThrottlingError(MsalServiceException ex)
    {
        return ex.StatusCode == 429
            || ex.StatusCode is >= 500 and < 600
            || ex.ErrorCode == "throttled_request"
            || ex.Message.Contains("throttled by AAD", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calculates retry delay using exponential backoff with jitter, respecting Retry-After if present.
    /// </summary>
    private static TimeSpan GetRetryDelay(MsalServiceException ex, int attempt)
    {
        // Use server-provided Retry-After if available
        if (ex.Headers != null)
        {
            foreach (var header in ex.Headers)
            {
                if (string.Equals(header.Key, "Retry-After", StringComparison.OrdinalIgnoreCase)
                    && header.Value.Any()
                    && int.TryParse(header.Value.First(), out var retryAfterSeconds)
                    && retryAfterSeconds > 0)
                {
                    return TimeSpan.FromSeconds(retryAfterSeconds);
                }
            }
        }

        // Exponential backoff: 2s, 4s, 8s + random jitter up to 1s
        var backoff = BaseRetryDelay * Math.Pow(2, attempt);
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
        return backoff + jitter;
    }
}
