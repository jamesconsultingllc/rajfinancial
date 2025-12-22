// ============================================================================
// RAJ Financial - Admin Users Step Definitions
// ============================================================================
// Reqnroll step definitions for AdminUsers.feature
// ============================================================================

using Microsoft.Playwright;
using RajFinancial.AcceptanceTests.Hooks;
using Reqnroll;

namespace RajFinancial.AcceptanceTests.StepDefinitions;

/// <summary>
/// Step definitions for admin user management scenarios.
/// </summary>
[Binding]
public class AdminUsersSteps(ScenarioContext scenarioContext)
{
    private IPage Page => scenarioContext.GetPage();

    [Then(@"I should see a table with user information")]
    public async Task ThenIShouldSeeATableWithUserInformation()
    {
        var table = Page.Locator("table");
        await Assertions.Expect(table).ToBeVisibleAsync();
    }

    [Then(@"the user table should have a ""(.*)"" column")]
    public async Task ThenTheUserTableShouldHaveAColumn(string columnName)
    {
        var column = Page.Locator($"th:has-text('{columnName}')");
        await Assertions.Expect(column).ToBeVisibleAsync();
    }

    [Then(@"the user table should be scrollable or use cards")]
    public async Task ThenTheUserTableShouldBeScrollableOrUseCards()
    {
        // On mobile, either table is scrollable or it uses a card layout
        var hasTable = await Page.Locator("table").CountAsync() > 0;
        var hasCards = await Page.Locator(".card").CountAsync() > 0;

        Assert.True(hasTable || hasCards,
            "User data should be displayed in table or card format");

        if (hasTable)
        {
            // If table exists, check if it's in a scrollable container
            var isScrollable = await Page.EvaluateAsync<bool>(@"
                () => {
                    const table = document.querySelector('table');
                    if (!table) return false;
                    const parent = table.parentElement;
                    const styles = window.getComputedStyle(parent);
                    return styles.overflowX === 'auto' || styles.overflowX === 'scroll';
                }
            ");

            // On mobile, we expect scrollable table
            var viewport = Page.ViewportSize;
            if (viewport != null && viewport.Width < 768)
            {
                Assert.True(isScrollable, "Table should be scrollable on mobile");
            }
        }
    }

    [Then(@"the user table should be properly structured")]
    public async Task ThenTheUserTableShouldBeProperlyStructured()
    {
        // Check for thead and tbody
        var thead = await Page.Locator("thead").CountAsync();
        var tbody = await Page.Locator("tbody").CountAsync();

        Assert.True(thead >= 1 && tbody >= 1,
            "Table should have proper structure with thead and tbody");
    }

    [Then(@"all action buttons should have accessible labels")]
    public async Task ThenAllActionButtonsShouldHaveAccessibleLabels()
    {
        var buttons = await Page.Locator("button, a[class*='btn']").AllAsync();

        foreach (var button in buttons)
        {
            var text = await button.TextContentAsync();
            var ariaLabel = await button.GetAttributeAsync("aria-label");
            var title = await button.GetAttributeAsync("title");

            var hasLabel = !string.IsNullOrWhiteSpace(text) ||
                          !string.IsNullOrWhiteSpace(ariaLabel) ||
                          !string.IsNullOrWhiteSpace(title);

            Assert.True(hasLabel, "All buttons should have accessible labels");
        }
    }
}
