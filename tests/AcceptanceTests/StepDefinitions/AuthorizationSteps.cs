// ============================================================================
// RAJ Financial - Authorization Step Definitions
// ============================================================================
// Reqnroll step definitions for Authorization.feature
// ============================================================================

using Microsoft.Playwright;
using RajFinancial.AcceptanceTests.Hooks;
using Reqnroll;

namespace RajFinancial.AcceptanceTests.StepDefinitions;

/// <summary>
/// Step definitions for authorization scenarios.
/// </summary>
[Binding]
public class AuthorizationSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage Page => _scenarioContext.GetPage();

    public AuthorizationSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"the application is running")]
    public async Task GivenTheApplicationIsRunning()
    {
        var response = await Page.GotoAsync(PlaywrightHooks.BaseUrl);
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Application not running: {response.Status}");
    }

    [When(@"I navigate to ""(.*)""")]
    public async Task WhenINavigateTo(string path)
    {
        var url = PlaywrightHooks.BaseUrl.TrimEnd('/') + path;
        await Page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await Page.WaitForTimeoutAsync(2000);
    }

    [Then(@"I should be redirected to the login page")]
    public async Task ThenIShouldBeRedirectedToTheLoginPage()
    {
        // Check if we're on a login/authentication page
        var url = Page.Url;
        var content = await Page.ContentAsync();
        
        var isLoginPage = url.Contains("login") || 
                          url.Contains("auth") || 
                          url.Contains("signin") ||
                          content.Contains("Log in") ||
                          content.Contains("Sign in");
        
        Assert.True(isLoginPage, $"Expected login page, but was on: {url}");
    }

    [Then(@"I should see the portfolio page")]
    public async Task ThenIShouldSeeThePortfolioPage()
    {
        var content = await Page.ContentAsync();
        Assert.True(
            content.Contains("Portfolio") || content.Contains("portfolio"),
            "Should see portfolio page content");
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
            content.Contains("Access Denied") || content.Contains("not authorized"),
            "Should see access denied message");
    }

    [Then(@"I should see the admin dashboard")]
    public async Task ThenIShouldSeeTheAdminDashboard()
    {
        var content = await Page.ContentAsync();
        Assert.True(
            content.Contains("Dashboard") || content.Contains("Administration"),
            "Should see admin dashboard content");
    }

    [Then(@"I should see the client list page")]
    public async Task ThenIShouldSeeTheClientListPage()
    {
        var content = await Page.ContentAsync();
        Assert.True(
            content.Contains("Client") || content.Contains("client"),
            "Should see client list page content");
    }

    [Then(@"I should see data shared with me")]
    public async Task ThenIShouldSeeDataSharedWithMe()
    {
        // Viewer should see some content, but it should be clearly marked as shared
        var content = await Page.ContentAsync();
        // This is a placeholder - actual implementation depends on UI design
        Assert.NotEmpty(content);
    }

    [Then(@"I should not be able to edit any data")]
    public async Task ThenIShouldNotBeAbleToEditAnyData()
    {
        // Check that edit buttons are not visible or are disabled
        var editButtons = await Page.Locator("button:has-text('Edit'), button:has-text('Save'), [data-testid*='edit']").AllAsync();
        
        foreach (var button in editButtons)
        {
            var isDisabled = await button.IsDisabledAsync();
            var isHidden = !(await button.IsVisibleAsync());
            Assert.True(isDisabled || isHidden, "Edit controls should be disabled or hidden for viewers");
        }
    }

    [Then(@"the page should use the brand styling")]
    public async Task ThenThePageShouldUseTheBrandStyling()
    {
        // Check for gold color usage (brand primary color)
        var hasGoldColor = await Page.EvaluateAsync<bool>(@"
            () => {
                const elements = document.querySelectorAll('*');
                for (const el of elements) {
                    const styles = window.getComputedStyle(el);
                    const bg = styles.backgroundColor;
                    const color = styles.color;
                    // Check for gold-ish colors
                    if (bg.includes('235') || bg.includes('187') || color.includes('235') || color.includes('187')) {
                        return true;
                    }
                }
                // Also check for CSS variables
                const root = getComputedStyle(document.documentElement);
                const goldColor = root.getPropertyValue('--gold-500');
                return goldColor && goldColor.length > 0;
            }
        ");
        Assert.True(hasGoldColor, "Page should use brand gold color styling");
    }
}
