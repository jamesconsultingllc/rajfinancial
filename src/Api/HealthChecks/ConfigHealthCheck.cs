using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace RajFinancial.Api.HealthChecks;

/// <summary>
///     Readiness probe that verifies critical configuration is present and not still
///     holding placeholder values.
/// </summary>
/// <remarks>
///     Covers Entra CIAM, App Role GUIDs, and (outside Development) the Application
///     Insights connection string used by the OpenTelemetry Azure Monitor exporter.
/// </remarks>
public sealed class ConfigHealthCheck(
    IConfiguration configuration,
    IHostEnvironment environment) : IHealthCheck
{
    private const string PlaceholderValue = "<SET-IN-ENVIRONMENT>";

    private static readonly string[] RequiredKeys =
    [
        "EntraExternalId:Instance",
        "EntraExternalId:Domain",
        "AppRoles:Client",
        "AppRoles:Administrator",
        "AppRoles:Advisor",
    ];

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        foreach (var key in RequiredKeys)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value) || value == PlaceholderValue)
                missing.Add(key);
        }

        if (!environment.IsDevelopment())
        {
            var aiConn = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            if (string.IsNullOrWhiteSpace(aiConn))
                missing.Add("APPLICATIONINSIGHTS_CONNECTION_STRING");
        }

        if (missing.Count == 0)
            return Task.FromResult(HealthCheckResult.Healthy("Required configuration present"));

        var description = "Missing or placeholder configuration: " + string.Join(", ", missing);
        return Task.FromResult(HealthCheckResult.Unhealthy(description));
    }
}
