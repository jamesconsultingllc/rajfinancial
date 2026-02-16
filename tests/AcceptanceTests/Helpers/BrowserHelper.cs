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
    ///     Applies CI stability flags in headless mode (--disable-gpu, --disable-dev-shm-usage,
    ///     --no-sandbox, --disable-dbus) to prevent crashes on Linux CI runners.
    /// </summary>
    /// <param name="playwright">The Playwright instance.</param>
    /// <param name="headless">Whether to run in headless mode. Defaults to true.</param>
    /// <returns>The launched browser instance.</returns>
    public static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright, bool? headless = null)
    {
        var browserName = Environment.GetEnvironmentVariable("BROWSER")?.ToLowerInvariant() ?? "chromium";
        var isHeadless = headless ?? Environment.GetEnvironmentVariable("HEADED") != "true";

        // CI stability flags — --disable-dbus prevents Edge/Chrome from connecting to
        // missing D-Bus services (UPower, theme sync) on headless Linux CI runners.
        var ciArgs = isHeadless
            ? new[] { "--disable-gpu", "--disable-dev-shm-usage", "--no-sandbox", "--disable-dbus" }
            : Array.Empty<string>();

        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = isHeadless,
            Args = ciArgs
        };

        var browser = browserName switch
        {
            "firefox" => await playwright.Firefox.LaunchAsync(launchOptions),
            "webkit" => await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = isHeadless
            }),
            "msedge" => await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = isHeadless,
                Channel = "msedge",
                Args = ciArgs
            }),
            _ => await playwright.Chromium.LaunchAsync(launchOptions) // chromium is default
        };

        Console.WriteLine($"Playwright launched browser: {browserName}");
        return browser;
    }
}