// ============================================================================
// RAJ Financial - Navigation Step Definitions
// ============================================================================
// Reqnroll step definitions for Navigation.feature
// ============================================================================

using Microsoft.Playwright;
using RajFinancial.AcceptanceTests.Hooks;
using Reqnroll;
using System.IO;

namespace RajFinancial.AcceptanceTests.StepDefinitions;

/// <summary>
/// Step definitions for navigation scenarios.
/// </summary>
[Binding]
public class NavigationSteps(ScenarioContext scenarioContext)
{
    /// <summary>
    /// Test user emails by role (not sensitive - can be hardcoded).
    /// </summary>
    private static readonly Dictionary<string, string> testUserEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Client"] = "test-client@rajfinancialdev.onmicrosoft.com",
        ["Advisor"] = "test-advisor@rajfinancialdev.onmicrosoft.com",
        ["Administrator"] = "test-admin@rajfinancialdev.onmicrosoft.com",
        ["Viewer"] = "test-viewer@rajfinancialdev.onmicrosoft.com"
    };

    private IPage Page => scenarioContext.GetPage();

    [Given(@"I am logged in as a ""(.*)""")]
    [Given(@"I am logged in as an ""(.*)""")]
    public async Task GivenIAmLoggedInAs(string role)
    {
        // Store the role for later use
        scenarioContext.Set(role, "UserRole");
        
        // Get email from hardcoded map
        if (!testUserEmails.TryGetValue(role, out var email))
        {
            throw new ArgumentException($"Unknown test role: '{role}'. Valid roles: {string.Join(", ", testUserEmails.Keys)}");
        }
        
        // Attempt storage state login first
        var storageStatePath = TestConfiguration.Instance.GetStorageStatePath(role);
        if (!string.IsNullOrWhiteSpace(storageStatePath))
        {
            var storageValid = await PlaywrightHooks.ValidateOrRegenerateStorageStateAsync(role, email, storageStatePath);
            if (storageValid)
            {
                // Dispose previous context/page created in hooks
                if (scenarioContext.TryGetValue<IPage>("Page", out var existingPage))
                {
                    await existingPage.CloseAsync();
                }

                if (scenarioContext.TryGetValue<IBrowserContext>("BrowserContext", out var existingContext))
                {
                    await existingContext.CloseAsync();
                }

                var context = await PlaywrightHooks.Browser.NewContextAsync(new BrowserNewContextOptions
                {
                    StorageStatePath = storageStatePath,
                    ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
                });
                var page = await context.NewPageAsync();

                scenarioContext.Set(context, "BrowserContext");
                scenarioContext.Set(page, "Page");

                await page.GotoAsync(PlaywrightHooks.BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForTimeoutAsync(1000);
                return;
            }
        }
        
        // Get password from configuration (appsettings.local.json or environment)
        var password = TestConfiguration.Instance.GetPassword(role) 
            ?? Environment.GetEnvironmentVariable($"TEST_{role.ToUpper()}_PASSWORD");
        
        if (string.IsNullOrEmpty(password))
        {
            // Skip authentication tests if password not configured
            throw new InconclusiveException(
                $"Test password not configured for role '{role}'. " +
                $"Set password in appsettings.local.json or TEST_{role.ToUpper()}_PASSWORD environment variable.");
        }
        
        // Navigate to app and trigger login
        await Page.GotoAsync(PlaywrightHooks.BaseUrl);
        await Page.WaitForTimeoutAsync(2000); // Wait for Blazor

        // Click login button - Entra External ID uses popup flow
        var loginButton = Page.Locator("text=Log in").First;
        if (await loginButton.IsVisibleAsync())
        {
            // Click login and wait for popup
            var loginPage = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await loginButton.ClickAsync();
            });

            // Handle the Entra ID login page in the popup
            await PlaywrightHooks.HandleEntraLoginPage(loginPage, email, password);

            // Wait for popup to close and Blazor to process auth
            await Page.WaitForTimeoutAsync(3000);

            // Wait for authentication to complete by checking for authenticated UI elements
            try
            {
                // Wait for either logout button or user profile indicator to appear
                await Page.WaitForSelectorAsync("text=Log out, a[href*='logout'], .oi-account-logout", new() { Timeout = 10000 });
            }
            catch
            {
                // If specific elements not found, wait for network to settle
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 10000 });
            }
        }
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

    [Then(@"I should be redirected to the login page")]
    public async Task ThenIShouldBeRedirectedToTheLoginPage()
    {
        // Wait for potential redirect
        await Page.WaitForTimeoutAsync(2000);
        
        var url = Page.Url;
        var content = await Page.ContentAsync();
        
        // Check if redirected to login or if login prompt is shown
        var isOnLoginPage = url.Contains("authentication/login") ||
                           url.Contains("login.microsoftonline.com") ||
                           url.Contains("ciamlogin") ||
                           content.Contains("Log in") ||
                           content.Contains("Sign in");
        
        Assert.True(isOnLoginPage, $"Should be redirected to login page. Current URL: {url}");
    }

    [When(@"I navigate to ""(.*)""")]
    public async Task WhenINavigateTo(string path)
    {
        var fullUrl = PlaywrightHooks.BaseUrl.TrimEnd('/') + path;
        await Page.GotoAsync(fullUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait for Blazor to fully render
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Additional wait for Blazor components to initialize
        await Page.WaitForTimeoutAsync(2000);

        // Wait for main content to be visible
        try
        {
            await Page.WaitForSelectorAsync("main, [role='main'], .raj-content", new() { Timeout = 5000, State = WaitForSelectorState.Visible });
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
        // Navigation is already visible in sidebar on desktop
        // On mobile, we might need to open the hamburger menu first
        var viewport = Page.ViewportSize;
        if (viewport != null && viewport.Width < 768)
        {
            var hamburger = Page.Locator("[aria-label*='menu'], .hamburger, .menu-toggle").First;
            if (await hamburger.IsVisibleAsync())
            {
                await hamburger.ClickAsync();
                await Page.WaitForTimeoutAsync(300);
            }
        }
    }

    [Then(@"I should see the ""(.*)"" link in admin section")]
    public async Task ThenIShouldSeeTheLinkInAdminSection(string linkText)
    {
        // Look for the link within the admin navigation section
        var adminSection = Page.Locator("nav").Filter(new() { HasText = "Administration" });
        var link = adminSection.Locator($"text={linkText}");
        await Assertions.Expect(link).ToBeVisibleAsync();
    }

    [When(@"I press Tab multiple times")]
    public async Task WhenIPressTabMultipleTimes()
    {
        for (int i = 0; i < 5; i++)
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

/// <summary>
/// Custom exception for tests that cannot run due to missing configuration.
/// </summary>
public class InconclusiveException(string message) : Exception(message);
