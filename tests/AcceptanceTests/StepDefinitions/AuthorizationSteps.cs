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
///     Step definitions for authorization scenarios.
/// </summary>
[Binding]
public class AuthorizationSteps
{
    private readonly ScenarioContext scenarioContext;

    protected AuthorizationSteps(ScenarioContext scenarioContext)
    {
        this.scenarioContext = scenarioContext;
    }

    private IPage Page => scenarioContext.GetPage();

    [Then(@"I should see the portfolio page")]
    public async Task ThenIShouldSeeThePortfolioPage()
    {
        var content = await Page.ContentAsync();
        Assert.True(
            content.Contains("Portfolio") || content.Contains("portfolio"),
            "Should see portfolio page content");
    }

    [Then(@"I should see the admin dashboard")]
    public async Task ThenIShouldSeeTheAdminDashboard()
    {
        // Ensure page has settled
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(1000);

        var dashboard = Page.Locator("[data-testid='admin-dashboard']");
        var title = Page.Locator("[data-testid='admin-dashboard-title'], h1:has-text('Administrator Dashboard')");
        // Wait for either the dashboard container or the title to be visible
        var tasks = new List<Task>
        {
            Assertions.Expect(dashboard).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15000 }),
            Assertions.Expect(title).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15000 })
        };

        var completed = await Task.WhenAny(tasks);
        try
        {
            await completed; // propagate any exception if the first completed is a failure
        }
        catch
        {
            // Capture diagnostics to help understand failures
            var content = await Page.ContentAsync();
            var url = Page.Url;
            Console.WriteLine($"Admin dashboard not visible. URL: {url}");
            Console.WriteLine(
                $"Page contains 'Administrator Dashboard': {content.Contains("Administrator Dashboard")}");
            throw;
        }
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
        var editButtons = await Page.Locator("button:has-text('Edit'), button:has-text('Save'), [data-testid*='edit']")
            .AllAsync();

        foreach (var button in editButtons)
        {
            var isDisabled = await button.IsDisabledAsync();
            var isHidden = !await button.IsVisibleAsync();
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