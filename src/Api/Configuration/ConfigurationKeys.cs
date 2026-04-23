namespace RajFinancial.Api.Configuration;

/// <summary>
///     Canonical names of configuration keys read by the API at startup or at runtime.
/// </summary>
/// <remarks>
///     Centralising these avoids duplicated string literals across
///     <see cref="RajFinancial.Api.HealthChecks.ConfigHealthCheck"/>, <c>Program.cs</c>,
///     and options binding extensions. See AGENT.md "No Magic Strings or Numbers".
/// </remarks>
internal static class ConfigurationKeys
{
    internal const string EntraInstance = "EntraExternalId:Instance";
    internal const string EntraTenantId = "EntraExternalId:TenantId";
    internal const string EntraClientId = "EntraExternalId:ClientId";
    internal const string EntraValidAudiences = "EntraExternalId:ValidAudiences";
    internal const string AppRoleClient = "AppRoles:Client";
    internal const string AppRoleAdministrator = "AppRoles:Administrator";
    internal const string AppRoleAdvisor = "AppRoles:Advisor";

    /// <summary>Application Insights connection string used by the OTel Azure Monitor exporter.</summary>
    internal const string ApplicationInsightsConnectionString = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    ///     Canonical placeholder used in appsettings.json to indicate a configuration value
    ///     must be supplied via environment-specific settings. Readiness probes reject values
    ///     equal to this placeholder so production deployments fail fast on missing wiring.
    /// </summary>
    internal const string PlaceholderValue = "<SET-IN-ENVIRONMENT>";
}
