// ============================================================================
// RAJ Financial - Playwright Browser Hooks
// ============================================================================
// Reqnroll hooks for managing Playwright browser lifecycle
// ============================================================================

using Microsoft.Playwright;
using Reqnroll;
using System.IO;

namespace RajFinancial.AcceptanceTests.Hooks;

/// <summary>
/// Hooks for managing Playwright browser and page lifecycle.
/// </summary>
[Binding]
public class PlaywrightHooks(ScenarioContext scenarioContext)
{
    private static IPlaywright? playwright;
    private static IBrowser? browser;
    private static readonly Dictionary<string, string> testUserEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Client"] = "test-client@rajfinancialdev.onmicrosoft.com",
        ["Advisor"] = "test-advisor@rajfinancialdev.onmicrosoft.com",
        ["Administrator"] = "test-admin@rajfinancialdev.onmicrosoft.com",
        ["Viewer"] = "test-viewer@rajfinancialdev.onmicrosoft.com"
    };

    /// <summary>
    /// Gets the shared browser instance for creating contexts.
    /// </summary>
    public static IBrowser Browser => browser ?? throw new InvalidOperationException("Browser has not been initialized.");

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
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("HEADED") != "true"
        });

        // Pre-generate storage state files locally when paths are configured
        foreach (var kvp in TestConfiguration.Instance.TestUsers)
        {
            var role = kvp.Key;
            var storagePath = TestConfiguration.Instance.GetStorageStatePath(role);
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                continue;
            }

            if (!testUserEmails.TryGetValue(role, out var email))
            {
                continue;
            }

            await EnsureStorageStateAsync(role, email, storagePath);
        }
    }

    /// <summary>
    /// Creates a new page before each scenario.
    /// </summary>
    [BeforeScenario]
    public async Task BeforeScenario()
    {
        var context = await browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();
        
        scenarioContext.Set(context, "BrowserContext");
        scenarioContext.Set(page, "Page");
    }

    /// <summary>
    /// Closes the page after each scenario.
    /// </summary>
    [AfterScenario]
    public async Task AfterScenario()
    {
        if (scenarioContext.TryGetValue<IPage>("Page", out var page))
        {
            // Take screenshot on failure
            if (scenarioContext.TestError != null)
            {
                var screenshotPath = Path.Combine(
                    "TestResults", 
                    "Screenshots",
                    $"{scenarioContext.ScenarioInfo.Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                
                Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
            }
            
            await page.CloseAsync();
        }

        if (scenarioContext.TryGetValue<IBrowserContext>("BrowserContext", out var context))
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
        if (browser != null)
        {
            await browser.CloseAsync();
            await browser.DisposeAsync();
        }
        playwright?.Dispose();
    }

    /// <summary>
    /// Generates or validates a Playwright storage state file for the specified role.
    /// </summary>
    /// <param name="role">Role name (must match configured user).</param>
    /// <param name="email">User email for login.</param>
    /// <param name="storagePath">Target storage state file path.</param>
    public static async Task EnsureStorageStateAsync(string role, string email, string storagePath)
    {
        await ValidateOrRegenerateStorageStateAsync(role, email, storagePath);
    }

    /// <summary>
    /// Validates an existing storage state by navigating to the base URL; if invalid or missing, regenerates it.
    /// </summary>
    /// <param name="role">Role name.</param>
    /// <param name="email">User email.</param>
    /// <param name="storagePath">Storage state file path.</param>
    /// <returns>True if storage state is valid after validation/regeneration; otherwise false.</returns>
    public static async Task<bool> ValidateOrRegenerateStorageStateAsync(string role, string email, string storagePath)
    {
        if (!File.Exists(storagePath))
        {
            return await GenerateStorageStateAsync(role, email, storagePath);
        }

        var context = await browser!.NewContextAsync(new BrowserNewContextOptions
        {
            StorageStatePath = storagePath,
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(500);

        var url = page.Url;
        var isLogin = url.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                      url.Contains("signin", StringComparison.OrdinalIgnoreCase) ||
                      await page.Locator("text=Log in").First.IsVisibleAsync();

        await context.CloseAsync();

        if (!isLogin)
        {
            return true;
        }

        return await GenerateStorageStateAsync(role, email, storagePath);
    }

    /// <summary>
    /// Generates a Playwright storage state file for the specified role using interactive login.
    /// </summary>
    /// <param name="role">Role name.</param>
    /// <param name="email">User email for login.</param>
    /// <param name="storagePath">Target storage state file path.</param>
    private static async Task<bool> GenerateStorageStateAsync(string role, string email, string storagePath)
    {
        var password = TestConfiguration.Instance.GetPassword(role)
            ?? Environment.GetEnvironmentVariable($"TEST_{role.ToUpperInvariant()}_PASSWORD");

        if (string.IsNullOrWhiteSpace(password))
        {
            return false; // no creds; skip generation
        }

        Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);

        var context = await browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);

        var loginButton = page.Locator("text=Log in").First;
        if (!await loginButton.IsVisibleAsync())
        {
            await context.CloseAsync();
            return false;
        }

        var loginPage = await page.RunAndWaitForPopupAsync(async () =>
        {
            await loginButton.ClickAsync();
        });

        await HandleEntraLoginPage(loginPage, email, password);

        await page.WaitForTimeoutAsync(3000);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await context.StorageStateAsync(new() { Path = storagePath });
        await context.CloseAsync();
        return true;
    }

    /// <summary>
    /// Handles the Microsoft Entra login flow for creating storage state during hooks.
    /// </summary>
    /// <param name="loginPage">Popup page hosting the login UI.</param>
    /// <param name="email">Email address.</param>
    /// <param name="password">Password.</param>
    public static async Task HandleEntraLoginPage(IPage loginPage, string email, string password)
    {
        await loginPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await loginPage.WaitForTimeoutAsync(500);

        var emailInput = loginPage.Locator("input[type='email'], input[name='loginfmt']");
        if (await emailInput.IsVisibleAsync())
        {
            await emailInput.FillAsync(email);
            await loginPage.ClickAsync("input[type='submit'], button[type='submit']");
        }

        await loginPage.WaitForSelectorAsync("input[type='password']", new() { Timeout = 15000, State = WaitForSelectorState.Visible });
        await loginPage.FillAsync("input[type='password']", password);
        var submitButton = loginPage.Locator("input[type='submit'], button[type='submit'], button:has-text('Sign in'), button:has-text('Next')");
        await submitButton.ClickAsync();

        try
        {
            var noButton = loginPage.Locator("#idBtn_Back");
            await noButton.WaitForAsync(new() { Timeout = 8000 });
            if (await noButton.IsVisibleAsync())
            {
                await noButton.ClickAsync();
            }
        }
        catch
        {
            // Ignore if prompt not shown
        }
    }
}

/// <summary>
/// Extension methods for accessing Playwright objects from scenarioContext.
/// </summary>
public static class ScenarioContextExtensions
{
    public static IPage GetPage(this ScenarioContext context) => 
        context.Get<IPage>("Page");
    
    public static IBrowserContext GetBrowserContext(this ScenarioContext context) => 
        context.Get<IBrowserContext>("BrowserContext");
}
