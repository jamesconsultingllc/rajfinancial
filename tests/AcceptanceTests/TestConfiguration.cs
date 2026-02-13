// ============================================================================
// RAJ Financial - Test Configuration
// ============================================================================
// Configuration settings for acceptance tests loaded from appsettings.json
// ============================================================================

using Microsoft.Extensions.Configuration;

namespace RajFinancial.AcceptanceTests;

/// <summary>
///     Test configuration loaded from appsettings.json and appsettings.local.json.
/// </summary>
public class TestConfiguration
{
    private static readonly Lazy<TestConfiguration> LazyInstance = new(Load);

    /// <summary>
    ///     Gets the singleton instance of the test configuration.
    /// </summary>
    public static TestConfiguration Instance => LazyInstance.Value;

    /// <summary>
    ///     Base URL for the application under test.
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:7161";

    /// <summary>
    ///     Test user configurations by role.
    /// </summary>
    public Dictionary<string, TestUserConfig> TestUsers { get; set; } = new();

    /// <summary>
    ///     IMAP server hostname for email verification (e.g., "imap.yandex.com").
    ///     Required for E2E tests that verify email signup flows.
    /// </summary>
    public string? ImapHost { get; set; }

    /// <summary>
    ///     IMAP server port (typically 993 for SSL/TLS).
    /// </summary>
    public int ImapPort { get; set; } = 993;

    /// <summary>
    ///     IMAP username/email for authentication (e.g., "test@rajlegacy.org").
    /// </summary>
    public string? ImapUsername { get; set; }

    /// <summary>
    ///     IMAP password or app-specific password for authentication.
    /// </summary>
    public string? ImapPassword { get; set; }

    /// <summary>
    ///     Gets the password for a test user by role.
    /// </summary>
    public string? GetPassword(string role)
    {
        if (TestUsers.TryGetValue(role, out var user))
            return string.IsNullOrEmpty(user.Password) ? null : user.Password;
        return null;
    }

    /// <summary>
    ///     Gets the storage state path for a test user by role, checking configuration and environment variables.
    /// </summary>
    /// <param name="role">The role of the test user.</param>
    /// <returns>The storage state file path if configured; otherwise, null.</returns>
    public string? GetStorageStatePath(string role)
    {
        if (TestUsers.TryGetValue(role, out var user) && !string.IsNullOrWhiteSpace(user.StorageStatePath))
            return user.StorageStatePath;

        var envPath = Environment.GetEnvironmentVariable($"TEST_{role.ToUpperInvariant()}_STORAGE_STATE");
        return string.IsNullOrWhiteSpace(envPath) ? null : envPath;
    }

    /// <summary>
    ///     Loads configuration from appsettings.json and appsettings.local.json.
    /// </summary>
    private static TestConfiguration Load()
    {
        var basePath = AppContext.BaseDirectory;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", true, false)
            .AddJsonFile("appsettings.local.json", true, false)
            .AddEnvironmentVariables() // Also support env vars for CI/CD
            .Build();

        var settings = new TestConfiguration();
        configuration.GetSection("TestSettings").Bind(settings);

        // Override with environment variables if present
        var envBaseUrl = Environment.GetEnvironmentVariable("BASE_URL");
        if (!string.IsNullOrEmpty(envBaseUrl)) settings.BaseUrl = envBaseUrl;

        return settings;
    }
}