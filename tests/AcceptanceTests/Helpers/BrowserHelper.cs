// ============================================================================
// RAJ Financial - Browser Helper
// ============================================================================
// Shared helper for launching browsers based on BROWSER environment variable
// ============================================================================

using Microsoft.Playwright;

namespace RajFinancial.AcceptanceTests.Helpers;

/// <summary>
///     Helper class for launching Playwright browsers based on environment configuration.
/// </summary>
public static class BrowserHelper
{
    /// <summary>
    ///     Launches a browser based on the BROWSER environment variable.
    ///     Defaults to Chromium if not specified.
    /// </summary>
    /// <param name="playwright">The Playwright instance.</param>
    /// <param name="headless">Whether to run in headless mode. Defaults to true.</param>
    /// <returns>The launched browser instance.</returns>
    public static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright, bool? headless = null)
    {
        var browserName = Environment.GetEnvironmentVariable("BROWSER")?.ToLowerInvariant() ?? "chromium";
        var isHeadless = headless ?? Environment.GetEnvironmentVariable("HEADED") != "true";

        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = isHeadless
        };

        var browser = browserName switch
        {
            "firefox" => await playwright.Firefox.LaunchAsync(launchOptions),
            "webkit" => await playwright.Webkit.LaunchAsync(launchOptions),
            "msedge" => await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = isHeadless,
                Channel = "msedge"
            }),
            _ => await playwright.Chromium.LaunchAsync(launchOptions) // chromium is default
        };

        Console.WriteLine($"Playwright launched browser: {browserName}");
        return browser;
    }
}