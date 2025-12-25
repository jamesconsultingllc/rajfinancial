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
public class HomePageSteps(ScenarioContext scenarioContext)
{
    private IPage Page => scenarioContext.GetPage();

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
    public async Task ThenIShouldSeeTheCtaSection()
    {
        var ctaSection = await Page.Locator(".cta-section").CountAsync();
        Assert.True(ctaSection >= 1, "CTA section should be visible");
    }

    [Then(@"the CTA section should encourage users to sign up")]
    public async Task ThenTheCtaSectionShouldEncourageUsersToSignUp()
    {
        var ctaContent = await Page.Locator(".cta-section").TextContentAsync();
        Assert.NotNull(ctaContent);
        // Should have some call to action text
        Assert.True(ctaContent.Length > 10, "CTA section should have content");
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
