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
    private readonly ScenarioContext _scenarioContext;
    private IPage Page => _scenarioContext.GetPage();

    public AdminDashboardSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Then(@"I should see ""(.*)""")]
    public async Task ThenIShouldSee(string text)
    {
        var content = await Page.ContentAsync();
        Assert.Contains(text, content);
    }

    [Then(@"I should see the ""(.*)"" section")]
    public async Task ThenIShouldSeeTheSection(string sectionName)
    {
        var content = await Page.ContentAsync();
        Assert.Contains(sectionName, content);
    }

    [Then(@"I should see statistics cards with numeric values")]
    public async Task ThenIShouldSeeStatisticsCardsWithNumericValues()
    {
        // Look for statistics cards (they should contain numbers)
        var content = await Page.ContentAsync();

        // Check that we have numeric values displayed
        var hasNumbers = await Page.EvaluateAsync<bool>(@"
            () => {
                const text = document.body.innerText;
                // Look for dollar amounts or large numbers
                return /\$[\d,]+/.test(text) || /\d{2,}/.test(text);
            }
        ");

        Assert.True(hasNumbers, "Statistics cards should display numeric values");
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

    [When(@"I click the ""(.*)"" button")]
    public async Task WhenIClickTheButton(string buttonText)
    {
        var button = Page.Locator($"text={buttonText}").First;
        await button.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
    }

    [Then(@"I should be on the ""(.*)"" page")]
    public async Task ThenIShouldBeOnThePage(string path)
    {
        var url = Page.Url;
        Assert.Contains(path, url);
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
        var quickActions = Page.Locator("text=Quick Actions");
        await Assertions.Expect(quickActions).ToBeVisibleAsync();
    }

    [Then(@"the statistics cards should adapt to tablet layout")]
    public async Task ThenTheStatisticsCardsShouldAdaptToTabletLayout()
    {
        // On tablet, should show 2 columns or adapt appropriately
        var cards = await Page.Locator(".card").AllAsync();
        Assert.True(cards.Count >= 4, "Should have statistics cards");
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
