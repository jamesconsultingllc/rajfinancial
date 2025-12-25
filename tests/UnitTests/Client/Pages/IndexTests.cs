// ============================================================================
// RAJ Financial - Index Page Unit Tests
// ============================================================================
// bUnit tests for the public landing page (Index.razor)
// ============================================================================

using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;

namespace RajFinancial.UnitTests.Client.Pages;

/// <summary>
/// Unit tests for the Index (home) page component.
/// </summary>
public class IndexTests : TestContext
{
    public IndexTests()
    {
        // Setup authorization services for unauthenticated state by default
        var authContext = this.AddTestAuthorization();
        authContext.SetNotAuthorized();

        // Setup localization services with mock that returns the key as value
        var mockLocalizer = new Mock<IStringLocalizer<RajFinancial.Client.Pages.Index>>();
        mockLocalizer.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, GetLocalizedValue(key)));
        mockLocalizer.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(GetLocalizedValue(key), args)));
        Services.AddSingleton(mockLocalizer.Object);
    }

    /// <summary>
    /// Returns localized values for test assertions.
    /// </summary>
    private static string GetLocalizedValue(string key) => key switch
    {
        "PageTitle" => "RAJ Financial - Your Financial Future",
        "Hero.Title.Line1" => "Take Control of Your",
        "Hero.Title.Line2" => "Financial Future",
        "Hero.CTA.GetStarted" => "Get Started Free",
        "Hero.CTA.ExploreFeatures" => "Explore Features",
        "Action.Portfolio.Title" => "View Portfolio",
        "CTA.Title" => "Ready to Take Control?",
        _ => key
    };

    [Fact]
    public void Index_Renders_HeroSection()
    {
        // Act
        var cut = RenderComponent<RajFinancial.Client.Pages.Index>();

        // Assert
        Assert.NotNull(cut.Find(".hero-section"));
    }

    [Fact]
    public void Index_Renders_BrandName()
    {
        // Act
        var cut = RenderComponent<RajFinancial.Client.Pages.Index>();

        // Assert - Footer contains brand name
        var markup = cut.Markup;
        Assert.Contains("RAJ Financial", markup);
    }

    [Fact]
    public void Index_Renders_Tagline()
    {
        // Act
        var cut = RenderComponent<RajFinancial.Client.Pages.Index>();

        // Assert - Hero title includes "Financial Future"
        var markup = cut.Markup;
        Assert.Contains("Financial Future", markup);
    }

    [Fact]
    public void Index_Renders_GetStartedButton()
    {
        // Act
        var cut = RenderComponent<RajFinancial.Client.Pages.Index>();

        // Assert
        var button = cut.Find(".hero-actions a.btn-gold-solid, .hero-actions button.btn-gold-solid");
        Assert.NotNull(button);
    }

    [Fact]
    public void Index_Renders_ExploreFeatures_ForUnauthenticated()
    {
        // Act
        var cut = RenderComponent<RajFinancial.Client.Pages.Index>();

        // Assert - Unauthenticated view shows Explore Features button
        var markup = cut.Markup;
        Assert.Contains("Explore Features", markup);
    }

    [Fact]
    public void Index_Renders_FeatureCards()
    {
        // Act
        var cut = RenderComponent<RajFinancial.Client.Pages.Index>();

        // Assert
        var featureCards = cut.FindAll(".feature-card");
        Assert.True(featureCards.Count >= 3, "Expected at least 3 feature cards");
    }

    [Fact]
    public void Index_Renders_CtaSection()
    {
        // Act
        var cut = RenderComponent<RajFinancial.Client.Pages.Index>();

        // Assert
        Assert.NotNull(cut.Find(".cta-section"));
    }

    [Fact]
    public void Index_Has_AccessibleStructure()
    {
        // Act
        var cut = RenderComponent<RajFinancial.Client.Pages.Index>();

        // Assert - Check for proper heading hierarchy
        var h1 = cut.FindAll("h1");
        Assert.True(h1.Count >= 1, "Page should have at least one h1 heading");
    }

    [Fact]
    public void Index_ButtonsHaveAccessibleLabels()
    {
        // Act
        var cut = RenderComponent<RajFinancial.Client.Pages.Index>();

        // Assert - All buttons/links should have text or aria-label
        var buttons = cut.FindAll("button, a.btn, a.btn-gold-solid, a.btn-gold-outline-dark");
        foreach (var button in buttons)
        {
            var hasText = !string.IsNullOrWhiteSpace(button.TextContent);
            var hasAriaLabel = button.HasAttribute("aria-label");
            Assert.True(hasText || hasAriaLabel, 
                $"Button should have text or aria-label: {button.OuterHtml}");
        }
    }
}
