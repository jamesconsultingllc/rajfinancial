// ============================================================================
// RAJ Financial - Admin Dashboard Step Definitions
// ============================================================================
// Reqnroll step definitions for AdminDashboard.feature
// ============================================================================

using Microsoft.Playwright;
using RajFinancial.AcceptanceTests.Hooks;
using Reqnroll;

namespace RajFinancial.AcceptanceTests.StepDefinitions;

/// <summary>
/// Step definitions for admin dashboard scenarios.
/// </summary>
[Binding]
public class AdminDashboardSteps
{
    private readonly ScenarioContext scenarioContext;
    private IPage Page => scenarioContext.GetPage();

    protected AdminDashboardSteps(ScenarioContext scenarioContext)
    {
        this.scenarioContext = scenarioContext;
    }

    [Then(@"I should see statistics cards with numeric values")]
    public async Task ThenIShouldSeeStatisticsCardsWithNumericValues()
    {
        // Wait for page to settle and cards to render
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        var cards = Page.Locator(".card h3");
        await Assertions.Expect(cards).ToHaveCountAsync(4, new() { Timeout = 15000 });

        var count = await cards.CountAsync();
        Assert.True(count >= 4, "Should have at least four statistic values");

        for (int i = 0; i < Math.Min(4, count); i++)
        {
            var text = (await cards.Nth(i).InnerTextAsync()).Trim();
            // Normalize currency symbols and commas
            var normalized = text.Replace(",", string.Empty)
                                  .Replace("$", string.Empty)
                                  .Replace("€", string.Empty)
                                  .Replace("£", string.Empty)
                                  .Trim();

            Assert.True(decimal.TryParse(normalized, out _), $"Card {i + 1} should display a numeric value but was '{text}'");
        }
    }

    [Then(@"all statistics should have labels")]
    public async Task ThenAllStatisticsShouldHaveLabels()
    {
        // Statistics should be in .card elements
        var cards = await Page.Locator(".card").AllAsync();
        Assert.True(cards.Count >= 4, "Should have at least 4 statistics cards");

        foreach (var card in cards.Take(4))
        {
            var text = await card.TextContentAsync();
            Assert.NotNull(text);
            Assert.True(text.Length > 5, "Each card should have descriptive text");
        }
    }

    [Then(@"all statistics should have icons")]
    public async Task ThenAllStatisticsShouldHaveIcons()
    {
        // Check for icons (using Open Iconic or other icon libraries)
        var icons = await Page.Locator(".oi, svg, i[class*='icon']").CountAsync();
        Assert.True(icons >= 4, "Should have at least 4 icons for statistics");
    }

    [Then(@"I should see activity items in the recent activity section")]
    public async Task ThenIShouldSeeActivityItemsInTheRecentActivitySection()
    {
        var activityItems = await Page.Locator("li").CountAsync();
        Assert.True(activityItems >= 1, "Recent activity should have items");
    }

    [Then(@"the statistics cards should be stacked vertically")]
    public async Task ThenTheStatisticsCardsShouldBeStackedVertically()
    {
        // On mobile, grid should be single column
        var isStacked = await Page.EvaluateAsync<bool>(@"
            () => {
                const grid = document.querySelector('.grid');
                if (!grid) return false;
                const styles = window.getComputedStyle(grid);
                // Check if grid-template-columns is set to 1 column
                return styles.gridTemplateColumns === '1fr' ||
                       styles.gridTemplateColumns.startsWith('repeat(1,');
            }
        ");

        // Alternative check: verify cards are stacked by checking their positions
        if (!isStacked)
        {
            var cards = await Page.Locator(".card").AllAsync();
            if (cards.Count >= 2)
            {
                var box1 = await cards[0].BoundingBoxAsync();
                var box2 = await cards[1].BoundingBoxAsync();

                // Cards should be vertically stacked (y positions different)
                Assert.NotNull(box1);
                Assert.NotNull(box2);
                Assert.True(box2.Y > box1.Y, "Cards should be stacked vertically on mobile");
            }
        }
    }

    [Then(@"the quick actions should be visible")]
    public async Task ThenTheQuickActionsShouldBeVisible()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        var quickActions = Page.Locator("[data-testid='quick-actions'], h5:has-text(\"Quick Actions\")");
        await quickActions.First.WaitForAsync(new() { Timeout = 20000, State = WaitForSelectorState.Attached });
        await quickActions.First.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(quickActions.First).ToBeVisibleAsync(new() { Timeout = 20000 });
    }

    [Then(@"the statistics cards should adapt to tablet layout")]
    public async Task ThenTheStatisticsCardsShouldAdaptToTabletLayout()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        var cards = Page.Locator(".grid .card");
        await Assertions.Expect(cards).ToBeVisibleAsync();
    }

    [Then(@"all statistics should have accessible labels")]
    public async Task ThenAllStatisticsShouldHaveAccessibleLabels()
    {
        // Check that statistics have proper labels (h6, labels, or aria-labels)
        var cards = await Page.Locator(".card").AllAsync();

        foreach (var card in cards.Take(4))
        {
            var hasLabel = await card.Locator("h1, h2, h3, h4, h5, h6, label, [aria-label]").CountAsync();
            Assert.True(hasLabel >= 1, "Each statistic card should have an accessible label");
        }
    }

    [Then(@"all icons should be hidden from screen readers or have labels")]
    public async Task ThenAllIconsShouldBeHiddenFromScreenReadersOrHaveLabels()
    {
        var icons = await Page.Locator(".oi, svg, i[class*='icon']").AllAsync();

        foreach (var icon in icons)
        {
            var ariaHidden = await icon.GetAttributeAsync("aria-hidden");
            var ariaLabel = await icon.GetAttributeAsync("aria-label");
            var title = await icon.GetAttributeAsync("title");

            var isAccessible = ariaHidden == "true" ||
                              !string.IsNullOrWhiteSpace(ariaLabel) ||
                              !string.IsNullOrWhiteSpace(title);

            Assert.True(isAccessible, "Icons should be hidden from screen readers or have labels");
        }
    }
}
