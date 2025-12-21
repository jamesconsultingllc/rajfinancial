// ============================================================================
// RAJ Financial - Playwright Browser Hooks
// ============================================================================
// Reqnroll hooks for managing Playwright browser lifecycle
// ============================================================================

using Microsoft.Playwright;
using Reqnroll;

namespace RajFinancial.AcceptanceTests.Hooks;

/// <summary>
/// Hooks for managing Playwright browser and page lifecycle.
/// </summary>
[Binding]
public class PlaywrightHooks
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private readonly ScenarioContext _scenarioContext;

    public PlaywrightHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    /// <summary>
    /// Gets the base URL for the application from configuration.
    /// </summary>
    public static string BaseUrl => TestConfiguration.Instance.BaseUrl;

    /// <summary>
    /// Initializes Playwright before all tests.
    /// </summary>
    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("HEADED") != "true"
        });
    }

    /// <summary>
    /// Creates a new page before each scenario.
    /// </summary>
    [BeforeScenario]
    public async Task BeforeScenario()
    {
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();
        
        _scenarioContext.Set(context, "BrowserContext");
        _scenarioContext.Set(page, "Page");
    }

    /// <summary>
    /// Closes the page after each scenario.
    /// </summary>
    [AfterScenario]
    public async Task AfterScenario()
    {
        if (_scenarioContext.TryGetValue<IPage>("Page", out var page))
        {
            // Take screenshot on failure
            if (_scenarioContext.TestError != null)
            {
                var screenshotPath = Path.Combine(
                    "TestResults", 
                    "Screenshots",
                    $"{_scenarioContext.ScenarioInfo.Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                
                Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
            }
            
            await page.CloseAsync();
        }

        if (_scenarioContext.TryGetValue<IBrowserContext>("BrowserContext", out var context))
        {
            await context.CloseAsync();
        }
    }

    /// <summary>
    /// Cleans up Playwright after all tests.
    /// </summary>
    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
    }
}

/// <summary>
/// Extension methods for accessing Playwright objects from ScenarioContext.
/// </summary>
public static class ScenarioContextExtensions
{
    public static IPage GetPage(this ScenarioContext context) => 
        context.Get<IPage>("Page");
    
    public static IBrowserContext GetBrowserContext(this ScenarioContext context) => 
        context.Get<IBrowserContext>("BrowserContext");
}
