// ============================================================================
// RAJ Financial - Navigation Step Definitions
// ============================================================================
// Reqnroll step definitions for Navigation.feature
// ============================================================================

using Microsoft.Playwright;
using RajFinancial.AcceptanceTests.Hooks;
using Reqnroll;

namespace RajFinancial.AcceptanceTests.StepDefinitions;

/// <summary>
/// Step definitions for navigation scenarios.
/// </summary>
[Binding]
public class NavigationSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage Page => _scenarioContext.GetPage();

    /// <summary>
    /// Test user emails by role (not sensitive - can be hardcoded).
    /// </summary>
    private static readonly Dictionary<string, string> TestUserEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Client"] = "test-client@rajfinancialdev.onmicrosoft.com",
        ["Advisor"] = "test-advisor@rajfinancialdev.onmicrosoft.com",
        ["Administrator"] = "test-admin@rajfinancialdev.onmicrosoft.com",
        ["Viewer"] = "test-viewer@rajfinancialdev.onmicrosoft.com"
    };

    public NavigationSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"I am logged in as a ""(.*)""")]
    [Given(@"I am logged in as an ""(.*)""")]
    public async Task GivenIAmLoggedInAs(string role)
    {
        // Store the role for later use
        _scenarioContext.Set(role, "UserRole");
        
        // Get email from hardcoded map
        if (!TestUserEmails.TryGetValue(role, out var email))
        {
            throw new ArgumentException($"Unknown test role: '{role}'. Valid roles: {string.Join(", ", TestUserEmails.Keys)}");
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
        
        // Click login button
        var loginButton = Page.Locator("text=Log in").First;
        if (await loginButton.IsVisibleAsync())
        {
            await loginButton.ClickAsync();
            
            // Wait for Entra login page
            await Page.WaitForURLAsync(url => 
                url.Contains("login") || 
                url.Contains("microsoftonline") || 
                url.Contains("ciamlogin"));
            
            // Fill email
            await Page.FillAsync("input[type='email'], input[name='loginfmt']", email);
            await Page.ClickAsync("input[type='submit'], button[type='submit']");
            
            // Wait for password page
            await Page.WaitForTimeoutAsync(1000);
            
            // Fill password
            await Page.FillAsync("input[type='password'], input[name='passwd']", password);
            await Page.ClickAsync("input[type='submit'], button[type='submit']");
            
            // Handle "Stay signed in?" prompt if it appears
            try
            {
                var noButton = Page.Locator("text=No");
                await noButton.WaitForAsync(new() { Timeout = 3000 });
                if (await noButton.IsVisibleAsync())
                {
                    await noButton.ClickAsync();
                }
            }
            catch (TimeoutException)
            {
                // No prompt, continue
            }
            
            // Wait for redirect back to app
            await Page.WaitForURLAsync(url => url.StartsWith(PlaywrightHooks.BaseUrl), 
                new() { Timeout = 30000 });
            await Page.WaitForTimeoutAsync(2000); // Wait for Blazor to process auth
        }
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

    [Then(@"I should see the ""(.*)"" link")]
    public async Task ThenIShouldSeeTheLink(string linkText)
    {
        var link = Page.Locator($"text={linkText}").First;
        await Assertions.Expect(link).ToBeVisibleAsync();
    }

    [Then(@"I should see the ""(.*)"" section")]
    public async Task ThenIShouldSeeTheSection(string sectionName)
    {
        var content = await Page.ContentAsync();
        Assert.Contains(sectionName, content);
    }

    [Then(@"I should not see the ""(.*)"" section")]
    public async Task ThenIShouldNotSeeTheSection(string sectionName)
    {
        var content = await Page.ContentAsync();
        Assert.DoesNotContain(sectionName, content);
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

    [Then(@"each focused item should have a visible focus indicator")]
    public async Task ThenEachFocusedItemShouldHaveAVisibleFocusIndicator()
    {
        var hasFocusIndicator = await Page.EvaluateAsync<bool>(@"
            () => {
                const el = document.activeElement;
                if (!el) return false;
                const styles = window.getComputedStyle(el);
                const outline = styles.outline;
                const boxShadow = styles.boxShadow;
                return (outline && !outline.includes('none') && !outline.includes('0px')) 
                    || (boxShadow && boxShadow !== 'none');
            }
        ");
        Assert.True(hasFocusIndicator, "Focused items should have visible focus indicator");
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
public class InconclusiveException : Exception
{
    public InconclusiveException(string message) : base(message) { }
}
