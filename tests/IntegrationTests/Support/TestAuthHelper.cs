using Microsoft.Extensions.Configuration;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Centralizes dual-mode authentication for integration tests.
/// Uses unsigned JWTs for local testing and ROPC tokens for remote/production endpoints.
/// </summary>
/// <remarks>
/// Test user emails are read from configuration (<c>Entra:TestUsers:{Role}</c>) so that
/// each environment (dev, prod) can supply its own Entra test user accounts.
/// Passwords are read from environment variables (<c>TEST_{ROLE}_PASSWORD</c>).
/// </remarks>
public class TestAuthHelper
{
    private readonly FunctionsHostFixture fixture;
    private readonly RopcTokenProvider ropcProvider;
    private readonly IConfiguration configuration;

    /// <summary>
    /// Maps logical test role names to environment variable names for passwords.
    /// </summary>
    private static readonly Dictionary<string, string> roleToPasswordEnvVar = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Client"] = "TEST_CLIENT_PASSWORD",
        ["Administrator"] = "TEST_ADMINISTRATOR_PASSWORD",
        ["Advisor"] = "TEST_ADVISOR_PASSWORD"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAuthHelper"/> class.
    /// </summary>
    /// <param name="fixture">The Functions host fixture providing base URL and locality info.</param>
    /// <param name="ropcProvider">The ROPC token provider for acquiring real Entra tokens.</param>
    /// <param name="configuration">Configuration providing test user email mappings.</param>
    public TestAuthHelper(FunctionsHostFixture fixture, RopcTokenProvider ropcProvider, IConfiguration configuration)
    {
        this.fixture = fixture;
        this.ropcProvider = ropcProvider;
        this.configuration = configuration;
    }

    /// <summary>
    /// Gets a Bearer token for the specified role.
    /// Uses unsigned JWT for localhost, ROPC for remote endpoints.
    /// </summary>
    /// <param name="email">The email to embed in the token (used for unsigned JWTs).</param>
    /// <param name="role">The role to assign (e.g., "Client", "Administrator", "Advisor").</param>
    /// <param name="userId">Optional explicit user ID for the token's <c>oid</c> claim. When null, a deterministic ID is derived from the email.</param>
    /// <returns>A Bearer token string.</returns>
    /// <exception cref="InvalidOperationException">
    /// When targeting a remote endpoint but ROPC is not configured, or when the test user
    /// email or password is not configured for the given role.
    /// </exception>
    public async Task<string> GetTokenForRoleAsync(string email, string role, string? userId = null)
    {
        if (fixture.IsLocal)
            return TestClaimsBuilder.JwtForUser(email, userId, role);

        if (!ropcProvider.IsConfigured)
            throw new InvalidOperationException(
                "Cannot authenticate against remote endpoint: ROPC is not configured. " +
                "Set Entra:TenantId, Entra:RopcClientId, and Entra:ApiScope " +
                "(or the equivalent Entra__* environment variables).");
        var entraEmail = configuration[$"Entra:TestUsers:{role}"];
        if (string.IsNullOrWhiteSpace(entraEmail))
            throw new InvalidOperationException(
                $"Entra test user email not configured for role '{role}'. " +
                $"Set Entra:TestUsers:{role} in appsettings.json or the Entra__TestUsers__{role} environment variable.");

        var passwordEnvVar = roleToPasswordEnvVar.GetValueOrDefault(role)
            ?? throw new InvalidOperationException($"No password env var mapped for role '{role}'");

        var password = Environment.GetEnvironmentVariable(passwordEnvVar)
            ?? throw new InvalidOperationException(
                $"Environment variable '{passwordEnvVar}' is required for ROPC auth in production mode.");

        return await ropcProvider.GetTokenAsync(entraEmail, password);
    }

    /// <summary>
    /// Gets a Bearer token for an administrator.
    /// </summary>
    /// <param name="email">The email to embed in unsigned JWTs (local mode only).</param>
    /// <param name="userId">Optional explicit user ID for the token's <c>oid</c> claim.</param>
    /// <returns>A Bearer token string.</returns>
    public async Task<string> GetAdminTokenAsync(string email = "admin@example.com", string? userId = null)
    {
        return await GetTokenForRoleAsync(email, "Administrator", userId);
    }

    /// <summary>
    /// Gets a Bearer token for a regular user (Client role).
    /// </summary>
    /// <param name="email">The email to embed in unsigned JWTs (local mode only).</param>
    /// <param name="userId">Optional explicit user ID for the token's <c>oid</c> claim.</param>
    /// <returns>A Bearer token string.</returns>
    public async Task<string> GetUserTokenAsync(string email = "user@example.com", string? userId = null)
    {
        return await GetTokenForRoleAsync(email, "Client", userId);
    }
}
