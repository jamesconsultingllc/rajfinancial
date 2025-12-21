// ============================================================================
// RAJ Financial - Home Page Step Definitions
// ============================================================================
// Reqnroll step definitions for HomePage.feature
// ============================================================================

using Microsoft.Playwright;
using RajFinancial.AcceptanceTests.Hooks;
using Reqnroll;

namespace RajFinancial.AcceptanceTests.StepDefinitions;

/// <summary>
/// Step definitions for home page scenarios.
/// </summary>
[Binding]
public class HomePageSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage Page => _scenarioContext.GetPage();

    public HomePageSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"I am on the home page")]
    public async Task GivenIAmOnTheHomePage()
    {
        await Page.GotoAsync(PlaywrightHooks.BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        // Wait for Blazor to hydrate
        await Page.WaitForTimeoutAsync(2000);
    }

    [Given(@"I am not logged in")]
    public void GivenIAmNotLoggedIn()
    {
        // Default state - no authentication cookies
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

    [Then(@"I should see the brand name ""(.*)""")]
    public async Task ThenIShouldSeeTheBrandName(string brandName)
    {
        var content = await Page.ContentAsync();
        Assert.Contains(brandName, content);
    }

    [Then(@"I should see the tagline ""(.*)""")]
    public async Task ThenIShouldSeeTheTagline(string tagline)
    {
        var content = await Page.ContentAsync();
        Assert.Contains(tagline, content);
    }

    [Then(@"I should see the hero section")]
    public async Task ThenIShouldSeeTheHeroSection()
    {
        var heroSection = await Page.Locator(".hero-section").CountAsync();
        Assert.True(heroSection >= 1, "Hero section should be visible");
    }

    [Then(@"I should see a ""(.*)"" button")]
    public async Task ThenIShouldSeeAButton(string buttonText)
    {
        var button = Page.Locator($"text={buttonText}").First;
        await Assertions.Expect(button).ToBeVisibleAsync();
    }

    [Then(@"I should not see a ""(.*)"" button")]
    public async Task ThenIShouldNotSeeAButton(string buttonText)
    {
        var button = Page.Locator($"text={buttonText}").First;
        await Assertions.Expect(button).Not.ToBeVisibleAsync();
    }

    [Then(@"I should see at least (\d+) feature cards")]
    public async Task ThenIShouldSeeAtLeastFeatureCards(int count)
    {
        var featureCards = await Page.Locator(".feature-card").CountAsync();
        Assert.True(featureCards >= count, $"Expected at least {count} feature cards, found {featureCards}");
    }

    [Then(@"I should see features describing the platform benefits")]
    public async Task ThenIShouldSeeFeaturesDescribingThePlatformBenefits()
    {
        var featureCards = await Page.Locator(".feature-card").CountAsync();
        Assert.True(featureCards >= 1, "Should have feature cards describing benefits");
    }

    [Then(@"I should see the CTA section")]
    public async Task ThenIShouldSeeTheCTASection()
    {
        var ctaSection = await Page.Locator(".cta-section").CountAsync();
        Assert.True(ctaSection >= 1, "CTA section should be visible");
    }

    [Then(@"the CTA section should encourage users to sign up")]
    public async Task ThenTheCTASectionShouldEncourageUsersToSignUp()
    {
        var ctaContent = await Page.Locator(".cta-section").TextContentAsync();
        Assert.NotNull(ctaContent);
        // Should have some call to action text
        Assert.True(ctaContent.Length > 10, "CTA section should have content");
    }

    [Then(@"the page should not have horizontal scroll")]
    public async Task ThenThePageShouldNotHaveHorizontalScroll()
    {
        var hasHorizontalScroll = await Page.EvaluateAsync<bool>(
            "document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(hasHorizontalScroll, "Page should not have horizontal scroll");
    }

    [Then(@"the hero section should be visible")]
    public async Task ThenTheHeroSectionShouldBeVisible()
    {
        await ThenIShouldSeeTheHeroSection();
    }

    [Then(@"the navigation should be accessible")]
    public async Task ThenTheNavigationShouldBeAccessible()
    {
        // On mobile, navigation is accessible via hamburger menu
        var hamburger = await Page.Locator("[aria-label*='menu'], [aria-label*='Menu'], .hamburger, .menu-toggle").CountAsync();
        var nav = await Page.Locator("nav").CountAsync();
        Assert.True(hamburger >= 1 || nav >= 1, "Navigation should be accessible");
    }

    [Then(@"the feature cards should be visible")]
    public async Task ThenTheFeatureCardsShouldBeVisible()
    {
        var featureCards = await Page.Locator(".feature-card").CountAsync();
        Assert.True(featureCards >= 1, "Feature cards should be visible");
    }

    [Then(@"the page should have proper heading hierarchy")]
    public async Task ThenThePageShouldHaveProperHeadingHierarchy()
    {
        var h1Count = await Page.Locator("h1").CountAsync();
        Assert.True(h1Count >= 1, "Page should have at least one h1 heading");
    }

    [Then(@"all buttons should have accessible labels")]
    public async Task ThenAllButtonsShouldHaveAccessibleLabels()
    {
        var buttons = await Page.Locator("button, a[class*='btn']").AllAsync();
        foreach (var button in buttons)
        {
            var text = await button.TextContentAsync();
            var ariaLabel = await button.GetAttributeAsync("aria-label");
            var hasLabel = !string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(ariaLabel);
            Assert.True(hasLabel, "All buttons should have accessible labels");
        }
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

    [Then(@"focus indicators should be visible when tabbing")]
    public async Task ThenFocusIndicatorsShouldBeVisibleWhenTabbing()
    {
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        
        var hasFocusIndicator = await Page.EvaluateAsync<bool>(@"
            () => {
                const el = document.activeElement;
                if (!el) return false;
                const styles = window.getComputedStyle(el);
                const outline = styles.outline;
                const boxShadow = styles.boxShadow;
                return (outline && !outline.includes('none')) || (boxShadow && boxShadow !== 'none');
            }
        ");
        Assert.True(hasFocusIndicator, "Focus indicators should be visible");
    }
}
