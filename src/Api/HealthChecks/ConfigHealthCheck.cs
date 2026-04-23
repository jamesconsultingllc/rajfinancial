using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.HealthChecks;

/// <summary>
///     Readiness probe that verifies critical configuration is present and not still
///     holding placeholder values.
/// </summary>
/// <remarks>
///     Covers Entra CIAM, App Role GUIDs, and (outside Development) the Application
///     Insights connection string used by the OpenTelemetry Azure Monitor exporter.
///     Detailed missing-key list is written to the logs (server-side) but NOT returned
///     to anonymous callers outside Development — the /health/ready endpoint is
///     publicly reachable and we don't want to disclose internal config structure.
/// </remarks>
public sealed partial class ConfigHealthCheck(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<ConfigHealthCheck> logger) : IHealthCheck
{
    private const string PLACEHOLDER_VALUE = "<SET-IN-ENVIRONMENT>";

    private static readonly string[] RequiredKeys =
    [
        ConfigurationKeys.EntraInstance,
        ConfigurationKeys.EntraDomain,
        ConfigurationKeys.EntraTenantId,
        ConfigurationKeys.EntraClientId,
        ConfigurationKeys.AppRoleClient,
        ConfigurationKeys.AppRoleAdministrator,
        ConfigurationKeys.AppRoleAdvisor,
    ];

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        foreach (var key in RequiredKeys)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value) || value == PLACEHOLDER_VALUE)
                missing.Add(key);
        }

        if (!environment.IsDevelopment())
        {
            var aiConn = configuration[ConfigurationKeys.ApplicationInsightsConnectionString];
            if (string.IsNullOrWhiteSpace(aiConn))
                missing.Add(ConfigurationKeys.ApplicationInsightsConnectionString);
        }

        if (missing.Count == 0)
            return Task.FromResult(HealthCheckResult.Healthy("Required configuration present"));

        // Full detail goes to logs (server-side, safe). Public response stays generic
        // outside Development so anonymous callers can't enumerate required config.
        LogConfigurationMissing(missing.Count, string.Join(", ", missing));

        var description = environment.IsDevelopment()
            ? "Missing or placeholder configuration: " + string.Join(", ", missing)
            : $"Required configuration missing ({missing.Count} key(s))";

        return Task.FromResult(new HealthCheckResult(
            context.Registration.FailureStatus, description));
    }

    [LoggerMessage(EventId = 9902, Level = LogLevel.Warning,
        Message = "Config health check failed — {MissingCount} missing key(s): {MissingKeys}")]
    private partial void LogConfigurationMissing(int missingCount, string missingKeys);
}
