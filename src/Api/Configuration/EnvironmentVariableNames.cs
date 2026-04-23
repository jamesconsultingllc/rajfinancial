namespace RajFinancial.Api.Configuration;

/// <summary>
///     Canonical names of environment variables read directly by the application
///     (i.e. outside of <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
///     binding). Centralising these prevents typos across <c>Program.cs</c>, error
///     messages, and any future tooling that needs to inspect them.
/// </summary>
internal static class EnvironmentVariableNames
{
    /// <summary>
    ///     Toggles the unsigned local-only JWT validator. Must NEVER be set on
    ///     Azure App Service. <c>Program.cs</c> refuses to start if this is "true"
    ///     and <see cref="WebsiteSiteName"/> is also set.
    /// </summary>
    internal const string UseUnsignedLocalValidator = "AUTH__USE_UNSIGNED_LOCAL_VALIDATOR";

    /// <summary>
    ///     Reserved env var injected by Azure App Service. Its presence is the
    ///     canonical signal that the host is running on App Service rather than
    ///     a local <c>func start</c>.
    /// </summary>
    internal const string WebsiteSiteName = "WEBSITE_SITE_NAME";
}
