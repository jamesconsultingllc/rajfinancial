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

    private static readonly Dictionary<string, string> TestUserEmails = new(StringComparer.OrdinalIgnoreCase)
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

            if (!TestUserEmails.TryGetValue(role, out var email)) continue;

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
            // Capture debug screenshot and page HTML
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var debugPath = Path.Combine(Path.GetTempPath(), $"auth-failed-{role}-{timestamp}.png");
            var htmlPath = Path.Combine(Path.GetTempPath(), $"auth-failed-{role}-{timestamp}.html");

            await page.ScreenshotAsync(new PageScreenshotOptions { Path = debugPath, FullPage = true });
            var pageHtml = await page.ContentAsync();
            await File.WriteAllTextAsync(htmlPath, pageHtml);

            Console.WriteLine($"Auth failed for {role}. Screenshot: {debugPath}, HTML: {htmlPath}");
            Console.WriteLine($"Current URL: {page.Url}");
            Console.WriteLine($"Page title: {await page.TitleAsync()}");
            await context.CloseAsync();
            return false;
        }

        // With MSAL configured to use localStorage, Playwright's native storage state captures tokens.
        // Debug: Check localStorage
        var localStorageKeys = await page.EvaluateAsync<string[]>("() => Object.keys(localStorage)");
        Console.WriteLine($"[{role}] localStorage keys: {localStorageKeys.Length}");
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
        await loginPage.WaitForTimeoutAsync(1000);

        // Entra External ID (CIAM) may show email and password on the same page,
        // or may have a two-step flow. Handle both cases.
        
        // Step 1: Look for email input field
        var emailSelectors = new[]
        {
            "input[name='username']",               // Entra External ID (CIAM) - type="text"
            "input[data-testid='iusernameInput']",  // Entra External ID (older)
            "input[type='email']",
            "input[name='loginfmt']",               // Entra ID (B2B/workforce)
            "input[name='email']"
        };

        var emailInput = await FindVisibleLocatorAsync(loginPage, emailSelectors, 3000);

        if (emailInput != null)
        {
            await emailInput.FillAsync(email);
            
            // Check if password field is already visible (same-page flow)
            var passwordVisible = await loginPage.Locator("input[data-testid='ipasswordInput'], input[type='password']")
                .First.IsVisibleAsync();
            
            if (!passwordVisible)
            {
                // Two-step flow: click Next to proceed to password
                var nextButton = loginPage.Locator("input[type='submit'], button[type='submit'], button[name='idSIButton9']").First;
                await nextButton.ClickAsync();
                await loginPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await loginPage.WaitForTimeoutAsync(1000);
            }
        }

        // Step 2: Wait for and fill password field
        var passwordSelectors = new[]
        {
            "input[name='password']",               // Entra External ID (CIAM)
            "input[data-testid='ipasswordInput']",  // Entra External ID (older)
            "input[type='password']",
            "input[name='passwd']"                   // Entra ID (B2B/workforce)
        };

        var passwordInput = await FindVisibleLocatorAsync(loginPage, passwordSelectors, 10000);

        if (passwordInput == null)
        {
            // Capture debug screenshot and page HTML for diagnostics
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var screenshotPath = Path.Combine(Path.GetTempPath(), $"entra-login-debug-{timestamp}.png");
            var htmlPath = Path.Combine(Path.GetTempPath(), $"entra-login-debug-{timestamp}.html");

            await loginPage.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });

            var pageHtml = await loginPage.ContentAsync();
            await File.WriteAllTextAsync(htmlPath, pageHtml);

            Console.WriteLine($"Debug screenshot: {screenshotPath}");
            Console.WriteLine($"Debug HTML: {htmlPath}");
            Console.WriteLine($"Current URL: {loginPage.Url}");
            Console.WriteLine($"Page title: {await loginPage.TitleAsync()}");

            throw new TimeoutException(
                $"Password field not found. Screenshot: {screenshotPath}, HTML: {htmlPath}. Current URL: {loginPage.Url}");
        }

        await passwordInput.FillAsync(password);
        
        // Step 3: Submit the form
        var submitButton = loginPage.Locator(
            "input[type='submit'], button[type='submit'], button:has-text('Sign in'), button[name='idSIButton9']").First;
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

        // Step 4: For redirect flow: wait for redirect back to the app
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
            // URL check failed - capture debug screenshot and HTML
            Console.WriteLine("Warning: Login redirect did not complete within 30s.");
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var debugPath = Path.Combine(Path.GetTempPath(),
                    $"login-redirect-timeout-{timestamp}.png");
                var htmlPath = Path.Combine(Path.GetTempPath(),
                    $"login-redirect-timeout-{timestamp}.html");

                await loginPage.ScreenshotAsync(new PageScreenshotOptions { Path = debugPath });
                var pageHtml = await loginPage.ContentAsync();
                await File.WriteAllTextAsync(htmlPath, pageHtml);

                Console.WriteLine($"Debug screenshot: {debugPath}");
                Console.WriteLine($"Debug HTML: {htmlPath}");
                Console.WriteLine($"Current URL: {loginPage.Url}");
            }
            catch
            {
                /* Ignore screenshot/HTML capture errors */
            }
        }
    }

    /// <summary>
    ///     Finds the first visible locator from a list of CSS selectors.
    /// </summary>
    /// <param name="page">The page to search.</param>
    /// <param name="selectors">CSS selectors to try in order.</param>
    /// <param name="timeoutMs">Timeout in milliseconds for each selector.</param>
    /// <returns>The first visible locator, or null if none found.</returns>
    private static async Task<ILocator?> FindVisibleLocatorAsync(IPage page, string[] selectors, int timeoutMs)
    {
        foreach (var selector in selectors)
        {
            var locator = page.Locator(selector).First;
            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions
                    { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
                return locator;
            }
            catch
            {
                // Try next selector
            }
        }

        return null;
    }
}