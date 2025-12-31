// ============================================================================
// RAJ Financial - Navigation Step Definitions
// ============================================================================
// Reqnroll step definitions for Navigation.feature
//
// Role Model:
//   - Client: Standard user who owns their financial data
//   - Administrator: Platform staff with system-wide access
//
// Fine-grained access control (who can see whose data) is handled via
// DataAccessGrant entities at the API level, not through additional roles.
// ============================================================================

using Microsoft.Playwright;
using RajFinancial.AcceptanceTests.Hooks;
using Reqnroll;

namespace RajFinancial.AcceptanceTests.StepDefinitions;

/// <summary>
///     Step definitions for navigation scenarios.
/// </summary>
[Binding]
public class NavigationSteps(ScenarioContext scenarioContext)
{
    /// <summary>
    ///     Test user emails by role (not sensitive - can be hardcoded).
    ///     Note: Only Client and Administrator roles are defined.
    ///     Data sharing is handled via DataAccessGrant entities.
    /// </summary>
    private static readonly Dictionary<string, string> testUserEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Client"] = "test-client@rajfinancialdev.onmicrosoft.com",
        ["Administrator"] = "test-admin@rajfinancialdev.onmicrosoft.com"
    };

    private IPage Page => scenarioContext.GetPage();

    [Given(@"I am logged in as a ""(.*)""")]
    [Given(@"I am logged in as an ""(.*)""")]
    public async Task GivenIAmLoggedInAs(string role)
    {
        scenarioContext.Set(role, "UserRole");

        var email = GetEmailForRole(role);
        scenarioContext.Set(email, "LoggedInEmail");

        if (await TryLoginWithStorageStateAsync(role, email)) return;

        var password = GetPasswordForRole(role);

        if (string.IsNullOrEmpty(password))
            throw new InconclusiveException(
                $"Test password not configured for role '{role}'. " +
                $"Set password in appsettings.local.json or TEST_{role.ToUpper()}_PASSWORD environment variable.");

        await PerformInteractiveLoginAsync(email, password);
    }

    private static string GetEmailForRole(string role)
    {
        if (!testUserEmails.TryGetValue(role, out var email))
            throw new ArgumentException(
                $"Unknown test role: '{role}'. Valid roles: {string.Join(", ", testUserEmails.Keys)}");
        return email;
    }

    private static string? GetPasswordForRole(string role)
    {
        return TestConfiguration.Instance.GetPassword(role)
               ?? Environment.GetEnvironmentVariable($"TEST_{role.ToUpper()}_PASSWORD");
    }

    private async Task<bool> TryLoginWithStorageStateAsync(string role, string email)
    {
        var storageStatePath = TestConfiguration.Instance.GetStorageStatePath(role);
        if (string.IsNullOrWhiteSpace(storageStatePath)) return false;

        var storageValid = await PlaywrightHooks.ValidateOrRegenerateStorageStateAsync(role, email, storageStatePath);
        if (!storageValid) return false;

        var currentViewport = await CloseExistingPageAndGetViewportAsync();
        await CloseExistingContextAsync();

        var context = await PlaywrightHooks.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            StorageStatePath = storageStatePath,
            ViewportSize = currentViewport ?? new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        scenarioContext.Set(context, "BrowserContext");
        scenarioContext.Set(page, "Page");

        await page.GotoAsync(PlaywrightHooks.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1000);

        await WaitForAuthenticatedState(page);
        return true;
    }

    private async Task<ViewportSize?> CloseExistingPageAndGetViewportAsync()
    {
        if (!scenarioContext.TryGetValue<IPage>("Page", out var existingPage)) return null;

        ViewportSize? currentViewport = null;
        var pageViewport = existingPage.ViewportSize;
        if (pageViewport != null)
            currentViewport = new ViewportSize { Width = pageViewport.Width, Height = pageViewport.Height };
        await existingPage.CloseAsync();
        return currentViewport;
    }

    private async Task CloseExistingContextAsync()
    {
        if (scenarioContext.TryGetValue<IBrowserContext>("BrowserContext", out var existingContext))
            await existingContext.CloseAsync();
    }

    private async Task PerformInteractiveLoginAsync(string email, string password)
    {
        await Page.GotoAsync(PlaywrightHooks.BaseUrl);
        await Page.WaitForTimeoutAsync(2000);

        // Look for Sign In button (updated from old "Sign In / Sign Up" text)
        var loginButton = Page.Locator("text=Sign In").First;
        if (!await loginButton.IsVisibleAsync())
            // Fallback to Get Started button or href-based selector
            loginButton = Page.Locator("a[href*='authentication/login'], a[href*='authentication/register']").First;

        if (await loginButton.IsVisibleAsync())
        {
            await loginButton.ClickAsync();

            await Page.WaitForURLAsync(url =>
                    url.Contains("ciamlogin.com") ||
                    url.Contains("login.microsoftonline.com") ||
                    url.Contains("b2clogin.com"),
                new PageWaitForURLOptions { Timeout = 15000 });

            await PlaywrightHooks.HandleEntraLoginPage(Page, email, password);

            await WaitForAuthenticatedState(Page);
        }
    }

    /// <summary>
    ///     Waits for the authenticated state to be resolved and any redirects to complete.
    /// </summary>
    private static async Task WaitForAuthenticatedState(IPage page)
    {
        // Wait for Blazor to process the auth callback and update state
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for either:
        // 1. Redirect to a dashboard (admin, advisor, or client)
        // 2. Log out button to appear (indicating auth is complete)
        // 3. Navigation sidebar to appear (authenticated layout)
        var maxAttempts = 30; // 15 seconds total
        for (var i = 0; i < maxAttempts; i++)
        {
            await page.WaitForTimeoutAsync(500);

            var url = page.Url;
            if (url.Contains("/dashboard") || url.Contains("/admin") || url.Contains("/advisor") ||
                url.Contains("/client")) break;

            // Check for authenticated UI elements
            var logoutVisible = await page.Locator("text=/Log out/i").First.IsVisibleAsync();
            var sidebarVisible =
                await page.Locator(".raj-sidebar, nav[aria-label*='navigation']").First.IsVisibleAsync();

            if (logoutVisible || sidebarVisible) break;
        }

        // Final wait for page to fully stabilize
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
    }

    [Given(@"I am not logged in")]
    public async Task GivenIAmNotLoggedIn()
    {
        // Navigate to the app without logging in
        // Clear any existing auth state by navigating to logout first
        await Page.GotoAsync(PlaywrightHooks.BaseUrl + "/authentication/logout");
        await Page.WaitForTimeoutAsync(1000);

        // Navigate to home
        await Page.GotoAsync(PlaywrightHooks.BaseUrl);
        await Page.WaitForTimeoutAsync(1000);
    }

    [When(@"I navigate to ""(.*)""")]
    public async Task WhenINavigateTo(string path)
    {
        var fullUrl = PlaywrightHooks.BaseUrl.TrimEnd('/') + path;
        await Page.GotoAsync(fullUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait for Blazor to fully render
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Additional wait for Blazor components to initialize
        await Page.WaitForTimeoutAsync(2000);

        // Wait for main content to be visible
        try
        {
            await Page.WaitForSelectorAsync("main, [role='main'], .raj-content",
                new PageWaitForSelectorOptions { Timeout = 5000, State = WaitForSelectorState.Visible });
        }
        catch
        {
            // Main content selector not found, continue anyway
        }
    }

    [Then(@"I should not see an access denied message")]
    public async Task ThenIShouldNotSeeAnAccessDeniedMessage()
    {
        var content = await Page.ContentAsync();
        Assert.DoesNotContain("Access Denied", content);
        Assert.DoesNotContain("not authorized", content.ToLower());
    }

    [Then(@"I should see an access denied message")]
    public async Task ThenIShouldSeeAnAccessDeniedMessage()
    {
        var content = await Page.ContentAsync();
        Assert.True(
            content.Contains("Access Denied") || content.Contains("not authorized", StringComparison.OrdinalIgnoreCase),
            "Should see an access denied message");
    }

    [When(@"I view the navigation menu")]
    public async Task WhenIViewTheNavigationMenu()
    {
        // Wait for the page to be fully loaded after login
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(1000);

        // Wait for sidebar/navigation to be present
        try
        {
            await Page.WaitForSelectorAsync(".raj-sidebar, .nav-menu, nav",
                new PageWaitForSelectorOptions { Timeout = 5000, State = WaitForSelectorState.Visible });
        }
        catch
        {
            // Navigation might not be visible yet, take a screenshot for debugging
            var screenshotPath = Path.Combine(Path.GetTempPath(), $"nav-debug-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
            Console.WriteLine($"Debug screenshot saved to: {screenshotPath}");
        }

        // On mobile, we might need to open the hamburger menu first
        var viewport = Page.ViewportSize;
        if (viewport != null && viewport.Width < 768)
        {
            var hamburger = Page.Locator(".raj-menu-toggle, [aria-label*='menu'], .hamburger, .menu-toggle").First;
            if (await hamburger.IsVisibleAsync())
            {
                await hamburger.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }
        }
    }

    [Then(@"I should see the ""(.*)"" link in admin section")]
    public async Task ThenIShouldSeeTheLinkInAdminSection(string linkText)
    {
        // Look for the link within the admin navigation section
        var adminSection = Page.Locator("nav").Filter(new LocatorFilterOptions { HasText = "Administration" });
        var link = adminSection.Locator($"text={linkText}");
        await Assertions.Expect(link).ToBeVisibleAsync();
    }

    [When(@"I press Tab multiple times")]
    public async Task WhenIPressTabMultipleTimes()
    {
        for (var i = 0; i < 5; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            await Page.WaitForTimeoutAsync(100);
        }
    }

    [Then(@"I should be able to navigate through menu items")]
    public async Task ThenIShouldBeAbleToNavigateThroughMenuItems()
    {
        var activeElement = await Page.EvaluateAsync<string>(
            "document.activeElement?.tagName");
        Assert.NotNull(activeElement);
        Assert.NotEqual("BODY", activeElement.ToUpper());
    }

    [Then(@"I should see a hamburger menu button")]
    public async Task ThenIShouldSeeAHamburgerMenuButton()
    {
        // Hamburger menu only visible on mobile for authenticated users
        var hamburger = Page.Locator(".raj-menu-toggle").First;
        await Assertions.Expect(hamburger).ToBeVisibleAsync();
    }

    [When(@"I click the hamburger menu button")]
    public async Task WhenIClickTheHamburgerMenuButton()
    {
        var hamburger = Page.Locator(".raj-menu-toggle").First;
        await hamburger.ClickAsync();
        // Wait for sidebar animation to complete
        await Page.WaitForTimeoutAsync(300);
    }

    [Then(@"the navigation menu should be visible")]
    public async Task ThenTheNavigationMenuShouldBeVisible()
    {
        // Check if the sidebar has the "open" class (mobile) or is visible (desktop)
        var sidebar = Page.Locator(".raj-sidebar");
        await Assertions.Expect(sidebar.First).ToBeVisibleAsync();
    }
}