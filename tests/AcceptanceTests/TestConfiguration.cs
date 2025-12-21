// ============================================================================
// RAJ Financial - Test Configuration
// ============================================================================
// Configuration settings for acceptance tests loaded from appsettings.json
// ============================================================================

using Microsoft.Extensions.Configuration;

namespace RajFinancial.AcceptanceTests;

/// <summary>
/// Test configuration loaded from appsettings.json and appsettings.local.json.
/// </summary>
public class TestConfiguration
{
    private static readonly Lazy<TestConfiguration> _instance = new(() => Load());
    
    /// <summary>
    /// Gets the singleton instance of the test configuration.
    /// </summary>
    public static TestConfiguration Instance => _instance.Value;

    /// <summary>
    /// Base URL for the application under test.
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:7161";

    /// <summary>
    /// Test user configurations by role.
    /// </summary>
    public Dictionary<string, TestUserConfig> TestUsers { get; set; } = new();

    /// <summary>
    /// Gets the password for a test user by role.
    /// </summary>
    public string? GetPassword(string role)
    {
        if (TestUsers.TryGetValue(role, out var user))
        {
            return string.IsNullOrEmpty(user.Password) ? null : user.Password;
        }
        return null;
    }

    /// <summary>
    /// Loads configuration from appsettings.json and appsettings.local.json.
    /// </summary>
    private static TestConfiguration Load()
    {
        var basePath = AppContext.BaseDirectory;
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables() // Also support env vars for CI/CD
            .Build();

        var settings = new TestConfiguration();
        configuration.GetSection("TestSettings").Bind(settings);
        
        // Override with environment variables if present
        var envBaseUrl = Environment.GetEnvironmentVariable("BASE_URL");
        if (!string.IsNullOrEmpty(envBaseUrl))
        {
            settings.BaseUrl = envBaseUrl;
        }

        return settings;
    }
}

/// <summary>
/// Configuration for a test user.
/// </summary>
public class TestUserConfig
{
    /// <summary>
    /// Password for the test user.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
