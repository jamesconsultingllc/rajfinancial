// ============================================================================
// RAJ Financial - Playwright Browser Hooks
// ============================================================================
// Reqnroll hooks for managing Playwright browser lifecycle
// ============================================================================

using Microsoft.Playwright;
using Reqnroll;

namespace RajFinancial.AcceptanceTests.Hooks;

/// <summary>
///     Hooks for managing Playwright browser and page lifecycle.
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
    ///     Gets the shared browser instance for creating contexts.
    /// </summary>
    public static IBrowser Browser =>
        browser ?? throw new InvalidOperationException("Browser has not been initialized.");

    /// <summary>
    ///     Gets the base URL for the application from configuration.
    /// </summary>
    public static string BaseUrl => TestConfiguration.Instance.BaseUrl;

    /// <summary>
    ///     Initializes Playwright before all tests.
    /// </summary>
    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        playwright = await Playwright.CreateAsync();

        // Get browser type from environment variable (default: chromium)
        // Options: chromium, firefox, webkit, msedge, chrome
        var browserType = Environment.GetEnvironmentVariable("BROWSER")?.ToLowerInvariant() ?? "chromium";
        var headless = Environment.GetEnvironmentVariable("HEADED") != "true";

        // For Edge and Chrome, we use Chromium with a channel
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Channel = browserType switch
            {
                "msedge" => "msedge",
                "chrome" => "chrome",
                _ => null // Use default Chromium
            }
        };

        browser = browserType switch
        {
            "firefox" => await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless }),
            "webkit" => await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless }),
            _ => await playwright.Chromium
                .LaunchAsync(launchOptions) // chromium, msedge, chrome all use Chromium engine
        };

        Console.WriteLine($"?? Browser: {browserType}, Headless: {headless}");

        // Pre-generate storage state files locally when paths are configured
        foreach (var kvp in TestConfiguration.Instance.TestUsers)
        {
            var role = kvp.Key;
            var storagePath = TestConfiguration.Instance.GetStorageStatePath(role);
            if (string.IsNullOrWhiteSpace(storagePath)) continue;

            if (!testUserEmails.TryGetValue(role, out var email)) continue;

            await EnsureStorageStateAsync(role, email, storagePath);
        }
    }

    /// <summary>
    ///     Creates a new page before each scenario.
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
    ///     Closes the page after each scenario.
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

        if (scenarioContext.TryGetValue<IBrowserContext>("BrowserContext", out var context)) await context.CloseAsync();
    }

    /// <summary>
    ///     Cleans up Playwright after all tests.
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
    ///     Generates or validates a Playwright storage state file for the specified role.
    /// </summary>
    /// <param name="role">Role name (must match configured user).</param>
    /// <param name="email">User email for login.</param>
    /// <param name="storagePath">Target storage state file path.</param>
    public static async Task EnsureStorageStateAsync(string role, string email, string storagePath)
    {
        await ValidateOrRegenerateStorageStateAsync(role, email, storagePath);
    }

    /// <summary>
    ///     Validates an existing storage state by navigating to the base URL; if invalid or missing, regenerates it.
    /// </summary>
    /// <param name="role">Role name.</param>
    /// <param name="email">User email.</param>
    /// <param name="storagePath">Storage state file path.</param>
    /// <returns>True if storage state is valid after validation/regeneration; otherwise false.</returns>
    public static async Task<bool> ValidateOrRegenerateStorageStateAsync(string role, string email, string storagePath)
    {
        if (!File.Exists(storagePath)) return await GenerateStorageStateAsync(role, email, storagePath);

        // With MSAL configured to use localStorage, Playwright's native storage state works.
        // Create context with the saved storage state.
        var context = await browser!.NewContextAsync(new BrowserNewContextOptions
        {
            StorageStatePath = storagePath,
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(2000);

        var url = page.Url;
        // Check if we're on a login page or if Sign In button is visible (meaning not authenticated)
        var isLogin = url.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                      url.Contains("signin", StringComparison.OrdinalIgnoreCase) ||
                      await page.Locator("text=Sign In").First.IsVisibleAsync() ||
                      await page.Locator("a[href*='authentication/login']").First.IsVisibleAsync();

        await context.CloseAsync();

        if (!isLogin) return true;

        // Storage state was stale - regenerate
        return await GenerateStorageStateAsync(role, email, storagePath);
    }

    /// <summary>
    ///     Generates a Playwright storage state file for the specified role using interactive login.
    /// </summary>
    /// <param name="role">Role name.</param>
    /// <param name="email">User email for login.</param>
    /// <param name="storagePath">Target storage state file path.</param>
    private static async Task<bool> GenerateStorageStateAsync(string role, string email, string storagePath)
    {
        var password = TestConfiguration.Instance.GetPassword(role)
                       ?? Environment.GetEnvironmentVariable($"TEST_{role.ToUpperInvariant()}_PASSWORD");

        if (string.IsNullOrWhiteSpace(password)) return false; // no creds; skip generation

        Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);

        var context = await browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);

        // Look for Sign In button (updated from "Log in")
        var loginButton = page.Locator("text=Sign In").First;
        if (!await loginButton.IsVisibleAsync())
            // Fallback to href-based selector
            loginButton = page.Locator("a[href*='authentication/login']").First;

        if (!await loginButton.IsVisibleAsync())
        {
            await context.CloseAsync();
            return false;
        }

        // Click login - redirect flow navigates the page to Entra ID
        await loginButton.ClickAsync();

        // Wait for redirect to Entra login page
        await page.WaitForURLAsync(url =>
                url.Contains("ciamlogin.com") ||
                url.Contains("login.microsoftonline.com") ||
                url.Contains("b2clogin.com"),
            new PageWaitForURLOptions { Timeout = 15000 });

        // Handle the Entra ID login on the same page (redirect flow)
        await HandleEntraLoginPage(page, email, password);

        // Wait for MSAL to process the auth response and store tokens in localStorage
        // This is critical - we need to wait for the main page to be fully authenticated
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for authenticated UI to appear (logout button or sidebar)
        var maxWaitMs = 30000;
        var waitedMs = 0;
        var authenticated = false;
        while (waitedMs < maxWaitMs)
        {
            await page.WaitForTimeoutAsync(500);
            waitedMs += 500;

            // Check for signs of successful auth
            var logoutVisible = await page.Locator("text=Log out").First.IsVisibleAsync();
            var sidebarVisible = await page.Locator(".raj-sidebar, nav[aria-label]").First.IsVisibleAsync();
            var url = page.Url;

            if (logoutVisible || sidebarVisible ||
                url.Contains("/dashboard") || url.Contains("/admin") ||
                url.Contains("/advisor") || url.Contains("/client"))
            {
                authenticated = true;
                break;
            }
        }

        if (!authenticated)
        {
            // Take a debug screenshot
            var debugPath = Path.Combine(Path.GetTempPath(), $"auth-failed-{role}-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = debugPath, FullPage = true });
            Console.WriteLine($"Auth failed for {role}. Debug screenshot: {debugPath}");
            await context.CloseAsync();
            return false;
        }

        // With MSAL configured to use localStorage, Playwright's native storage state captures tokens.
        // Debug: Check localStorage
        var localStorageKeys = await page.EvaluateAsync<string[]>("() => Object.keys(localStorage)");
        Console.WriteLine($"[{role}] localStorage keys: {localStorageKeys?.Length ?? 0}");
        Console.WriteLine($"[{role}] Current URL: {page.Url}");

        // Save storage state using Playwright's native method
        await context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = storagePath });

        Console.WriteLine($"[{role}] Storage state saved");

        await context.CloseAsync();
        return true;
    }

    /// <summary>
    ///     Handles the Microsoft Entra login flow (works for both redirect and popup flows).
    /// </summary>
    /// <param name="loginPage">Page hosting the login UI.</param>
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

        await loginPage.WaitForSelectorAsync("input[type='password']",
            new PageWaitForSelectorOptions { Timeout = 15000, State = WaitForSelectorState.Visible });
        await loginPage.FillAsync("input[type='password']", password);
        var submitButton =
            loginPage.Locator(
                "input[type='submit'], button[type='submit'], button:has-text('Sign in'), button:has-text('Next')");
        await submitButton.ClickAsync();

        // Handle "Stay signed in?" or "Keep me signed in" prompt
        try
        {
            // Try multiple selectors for the "No" / "Don't show again" buttons
            var noButton = loginPage
                .Locator("#idBtn_Back, #declineButton, button:has-text('No'), button:has-text(\"Don't show\")").First;
            await noButton.WaitForAsync(new LocatorWaitForOptions
                { Timeout = 8000, State = WaitForSelectorState.Visible });
            if (await noButton.IsVisibleAsync()) await noButton.ClickAsync();
        }
        catch
        {
            // Prompt not shown - may auto-redirect or use different flow
        }

        // For redirect flow: wait for redirect back to the app
        // For popup flow: the popup will close automatically
        try
        {
            await loginPage.WaitForURLAsync(url =>
                    !url.Contains("ciamlogin.com") &&
                    !url.Contains("login.microsoftonline.com") &&
                    !url.Contains("b2clogin.com"),
                new PageWaitForURLOptions { Timeout = 30000 });
        }
        catch
        {
            // URL check failed - take debug screenshot
            Console.WriteLine("Warning: Login redirect did not complete within 30s.");
            try
            {
                var debugPath = Path.Combine(Path.GetTempPath(),
                    $"login-redirect-timeout-{DateTime.Now:yyyyMMdd-HHmmss}.png");
                await loginPage.ScreenshotAsync(new PageScreenshotOptions { Path = debugPath });
                Console.WriteLine($"Debug screenshot saved to: {debugPath}");
            }
            catch
            {
                /* Ignore screenshot errors */
            }
        }
    }
}