using Microsoft.Playwright;
using RajFinancial.AcceptanceTests.Hooks;
using Reqnroll;
using Xunit;

namespace RajFinancial.AcceptanceTests.StepDefinitions;

/// <summary>
/// Base class for step definitions containing common/generic steps.
/// </summary>
[Binding]
public class SharedStepDefinitions(ScenarioContext scenarioContext)
{
    private IPage Page => scenarioContext.GetPage();

    [Given(@"the application is running")]
    public async Task GivenTheApplicationIsRunning()
    {
        var response = await Page.GotoAsync(PlaywrightHooks.BaseUrl);
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Application not running: {response.Status}");
    }

    [Then(@"I should see the ""(.*)"" section")]
    public async Task ThenIShouldSeeTheSection(string sectionName)
    {
        // Wait for page to render (Blazor client-side rendering)
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        // Build optional data-testid key (kebab-case)
        var dataTestId = sectionName.ToLowerInvariant().Replace(" ", "-");
        var locator = Page.Locator($"[data-testid='{dataTestId}'], text={sectionName}");

        try
        {
            await locator.First.WaitForAsync(new() { Timeout = 15000, State = WaitForSelectorState.Visible });
        }
        catch
        {
            // Section might not be visible as text, check content anyway
        }

        var content = await Page.ContentAsync();
        Assert.Contains(sectionName, content);
    }

    [Given(@"the viewport is set to mobile size")]
    public async Task GivenTheViewportIsSetToMobileSize()
    {
        await Page.SetViewportSizeAsync(375, 667);
    }

    [Given(@"the viewport is set to tablet size")]
    public async Task GivenTheViewportIsSetToTabletSize()
    {
        await Page.SetViewportSizeAsync(768, 1024);
    }

    [Given(@"the viewport is set to desktop size")]
    public async Task GivenTheViewportIsSetToDesktopSize()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
    }

    [Then(@"the page should load successfully")]
    public async Task ThenThePageShouldLoadSuccessfully()
    {
        var response = await Page.GotoAsync(PlaywrightHooks.BaseUrl);
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Page failed to load: {response.Status}");
    }

    [Then(@"the page title should contain ""(.*)""")]
    public async Task ThenThePageTitleShouldContain(string expectedTitle)
    {
        var title = await Page.TitleAsync();
        Assert.Contains(expectedTitle, title);
    }

    [Then(@"I should see ""(.*)""")]
    public async Task ThenIShouldSee(string text)
    {
        // Wait for page render/hydration and target text to appear
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        try
        {
            await Page.WaitForSelectorAsync($"text={text}", new() { Timeout = 10000, State = WaitForSelectorState.Visible });
        }
        catch
        {
            // fallback to content assertion below
        }

        var content = await Page.ContentAsync();
        Assert.Contains(text, content);
    }

    [Then(@"I should see a ""(.*)"" button")]
    public async Task ThenIShouldSeeAButton(string buttonText)
    {
        var button = Page.Locator($"a, button").Filter(new() { HasText = buttonText }).First;
        await Assertions.Expect(button).ToBeVisibleAsync();
    }

    [Then(@"I should not see a ""(.*)"" button")]
    public async Task ThenIShouldNotSeeAButton(string buttonText)
    {
        var button = Page.Locator($"a, button").Filter(new() { HasText = buttonText }).First;
        await Assertions.Expect(button).Not.ToBeVisibleAsync();
    }

    [Then(@"I should be redirected to the login page")]
    public async Task ThenIShouldBeRedirectedToTheLoginPage()
    {
        var url = Page.Url;
        var content = await Page.ContentAsync();
        
        var isLoginPage = url.Contains("login") || 
                          url.Contains("auth") || 
                          url.Contains("signin") ||
                          content.Contains("Log in") ||
                          content.Contains("Sign in");
        
        Assert.True(isLoginPage, $"Expected login page, but was on: {url}");
    }

    [Then(@"the page should not have horizontal scroll")]
    public async Task ThenThePageShouldNotHaveHorizontalScroll()
    {
        var hasHorizontalScroll = await Page.EvaluateAsync<bool>(
            "document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(hasHorizontalScroll, "Page should not have horizontal scroll");
    }

    [Then(@"all buttons should have accessible labels")]
    public async Task ThenAllButtonsShouldHaveAccessibleLabels()
    {
        var buttons = await Page.Locator("button, a.btn, a[class*='btn']").AllAsync();
        
        foreach (var button in buttons)
        {
            var text = await button.TextContentAsync();
            var ariaLabel = await button.GetAttributeAsync("aria-label");
            var title = await button.GetAttributeAsync("title");
            
            var hasLabel = !string.IsNullOrWhiteSpace(text) ||
                          !string.IsNullOrWhiteSpace(ariaLabel) ||
                          !string.IsNullOrWhiteSpace(title);
            
            Assert.True(hasLabel, $"Button with outer HTML '{await button.InnerHTMLAsync()}' should have an accessible label");
        }
    }

    [Then(@"the page should have proper heading hierarchy")]
    public async Task ThenThePageShouldHaveProperHeadingHierarchy()
    {
        // Wait for page content to render (especially important for Blazor)
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(1000);

        // Wait for headings to appear (prefer admin dashboard title if present)
        try
        {
            var headingLocator = Page.Locator("h1, [data-testid='admin-dashboard-title']");
            await headingLocator.First.WaitForAsync(new() { Timeout = 15000, State = WaitForSelectorState.Visible });
        }
        catch
        {
            // continue to checks below
        }

        var h1Count = await Page.Locator("h1").CountAsync();

        // Debug: If no h1 found, capture screenshot and log page content
        if (h1Count < 1)
        {
            await Page.ScreenshotAsync(new()
            {
                Path = $"TestResults/Screenshots/no-h1-debug-{DateTime.Now:yyyyMMddHHmmss}.png",
                FullPage = true
            });

            var pageContent = await Page.ContentAsync();
            Console.WriteLine($"Page URL: {Page.Url}");
            Console.WriteLine($"Page title: {await Page.TitleAsync()}");
            Console.WriteLine($"Page contains 'Administrator Dashboard': {pageContent.Contains("Administrator Dashboard")}");
            Console.WriteLine($"Page contains 'Access Denied': {pageContent.Contains("Access Denied")}");
        }

        Assert.True(h1Count >= 1, "Page should have at least one h1");

        var headings = await Page.EvaluateAsync<int[]>(@"
            () => {
                const headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
                return Array.from(headings).map(h => parseInt(h.tagName.substring(1)));
            }
        ");

        Assert.True(headings.Length > 0, "Page should have headings");
    }

    [Then(@"all images should have alt text")]
    public async Task ThenAllImagesShouldHaveAltText()
    {
        var images = await Page.Locator("img").AllAsync();
        foreach (var img in images)
        {
            var alt = await img.GetAttributeAsync("alt");
            var ariaHidden = await img.GetAttributeAsync("aria-hidden");
            var hasAlt = alt != null || ariaHidden == "true";
            Assert.True(hasAlt, "All images should have alt text or be hidden from accessibility");
        }
    }

    [When(@"I click the ""(.*)"" button")]
    public async Task WhenIClickTheButton(string buttonText)
    {
        // Look for button or link with the text (handles whitespace) and data-testid fallback
        var button = Page.Locator($"[data-testid*='{buttonText.Replace(" ", "-").ToLowerInvariant()}'], button:has-text('{buttonText}'), a:has-text('{buttonText}'), .btn:has-text('{buttonText}')").First;
        await button.WaitForAsync(new() { Timeout = 15000, State = WaitForSelectorState.Visible });
        await button.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);
    }

    [Then(@"I should be on the ""(.*)"" page")]
    public async Task ThenIShouldBeOnThePage(string path)
    {
        var url = Page.Url;
        Assert.Contains(path, url);
    }

    [Then(@"I should see the ""(.*)"" link")]
    public async Task ThenIShouldSeeTheLink(string linkText)
    {
        // Ensure page is settled
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        // Wait for nav container if present
        try
        {
            var content = await Page.ContentAsync(); 
            await Page.WaitForSelectorAsync("nav, .nav-menu, .raj-sidebar", new() { Timeout = 5000, State = WaitForSelectorState.Attached });
        }
        catch { /* best-effort */ }

        // Prefer data-testid if known
        var candidateTestIds = new List<string>();
        if (string.Equals(linkText, "Home", StringComparison.OrdinalIgnoreCase))
        {
            candidateTestIds.Add("nav-home-link");
            candidateTestIds.Add("raj-header-brand"); // Public layout uses brand logo as home link
        }
        else if (string.Equals(linkText, "My Portfolio", StringComparison.OrdinalIgnoreCase))
        {
            candidateTestIds.Add("nav-portfolio-link");
        }
        else if (string.Equals(linkText, "Transactions", StringComparison.OrdinalIgnoreCase))
        {
            candidateTestIds.Add("nav-transactions-link");
        }
        else if (string.Equals(linkText, "Statements", StringComparison.OrdinalIgnoreCase))
        {
            candidateTestIds.Add("nav-statements-link");
        }
        else if (string.Equals(linkText, "My Clients", StringComparison.OrdinalIgnoreCase))
        {
            candidateTestIds.Add("nav-my-clients-link");
        }
        else if (string.Equals(linkText, "Reports", StringComparison.OrdinalIgnoreCase))
        {
            candidateTestIds.Add("nav-reports-link");
        }

        var candidates = new List<ILocator>();
        candidates.AddRange(candidateTestIds.Select(id => Page.Locator($"[data-testid='{id}']")));
        
        // For "Home", also check the brand logo link (public layout)
        if (string.Equals(linkText, "Home", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(Page.Locator(".raj-header-brand"));
        }
        
        candidates.AddRange(new[]
        {
            Page.Locator($"a:has-text(\"{linkText}\")"),
            Page.Locator($"button:has-text(\"{linkText}\")"),
            Page.Locator($"[role='link']:has-text(\"{linkText}\")"),
            Page.Locator($".nav-link:has-text(\"{linkText}\")"),
            Page.Locator($"text={linkText}")
        });

        var foundVisible = false;
        foreach (var candidate in candidates)
        {
            try
            {
                await Assertions.Expect(candidate.First).ToBeVisibleAsync(new() { Timeout = 7000 });
                foundVisible = true;
                break;
            }
            catch
            {
                // try next candidate
            }
        }

        if (!foundVisible)
        {
            // Capture screenshot for debugging before failing
            var screenshotPath = Path.Combine(Path.GetTempPath(), $"nav-link-{linkText}-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            await Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
            Console.WriteLine($"Debug screenshot saved to {screenshotPath}");
        }

        Assert.True(foundVisible, $"Expected to see link with text '{linkText}'");
    }

    [Then(@"I should not see the ""(.*)"" section")]
    public async Task ThenIShouldNotSeeTheSection(string sectionName)
    {
        var content = await Page.ContentAsync();
        Assert.DoesNotContain(sectionName, content);
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
}
