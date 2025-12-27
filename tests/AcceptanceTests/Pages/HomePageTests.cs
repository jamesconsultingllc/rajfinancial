// ============================================================================
// RAJ Financial - Home Page E2E Tests
// ============================================================================
// Playwright tests for home page functionality
// ============================================================================

using Microsoft.Playwright;

namespace RajFinancial.AcceptanceTests.Pages;

/// <summary>
/// End-to-end tests for the home page.
/// </summary>
public class HomePageTests : IAsyncLifetime
{
    private IPlaywright? playwright;
    private IBrowser? browser;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? TestConfiguration.Instance.BaseUrl;

    public async Task InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (browser != null)
        {
            await browser.CloseAsync();
            await browser.DisposeAsync();
        }
        playwright?.Dispose();
    }

    [Fact]
    public async Task HomePage_ShouldLoadSuccessfully()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Act
            var response = await page.GotoAsync(baseUrl);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Ok, $"Failed to load page: {response.Status}");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldHaveCorrectTitle()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000); // Wait for Blazor
            
            var title = await page.TitleAsync();

            // Assert
            Assert.Contains("RAJ Financial", title);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldDisplayHeroSection()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Assert
            var heroSection = await page.Locator(".hero-section").CountAsync();
            Assert.True(heroSection >= 1, "Hero section should be visible");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldDisplayBrandName()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Assert
            var content = await page.ContentAsync();
            Assert.Contains("RAJ Financial", content);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldDisplayTagline()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Assert
            var content = await page.ContentAsync();
            Assert.Contains("Your Financial Future", content);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldDisplayFeatureCards()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Assert
            var featureCards = await page.Locator(".feature-card").CountAsync();
            Assert.True(featureCards >= 3, $"Expected at least 3 feature cards, found {featureCards}");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldDisplayCtaSection()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Assert
            var ctaSection = await page.Locator(".cta-section").CountAsync();
            Assert.True(ctaSection >= 1, "CTA section should be visible");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_GetStartedButton_ShouldBeClickable()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Find Get Started button
            var getStartedButton = page.Locator("text=Get Started").First;
            
            // Assert it's visible and clickable
            await Assertions.Expect(getStartedButton).ToBeVisibleAsync();
            await Assertions.Expect(getStartedButton).ToBeEnabledAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldBeResponsive_Mobile()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Set mobile viewport
            await page.SetViewportSizeAsync(375, 667); // iPhone SE size
            
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Assert - content should still be visible
            var heroSection = await page.Locator(".hero-section").CountAsync();
            Assert.True(heroSection >= 1, "Hero section should be visible on mobile");
            
            // Verify no horizontal scroll
            var hasHorizontalScroll = await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth > document.documentElement.clientWidth");
            Assert.False(hasHorizontalScroll, "Page should not have horizontal scroll on mobile");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldBeResponsive_Tablet()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Set tablet viewport
            await page.SetViewportSizeAsync(768, 1024); // iPad size
            
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Assert - content should still be visible
            var heroSection = await page.Locator(".hero-section").CountAsync();
            Assert.True(heroSection >= 1, "Hero section should be visible on tablet");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldBeResponsive_Desktop()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Set desktop viewport
            await page.SetViewportSizeAsync(1920, 1080);
            
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Assert - content should still be visible
            var heroSection = await page.Locator(".hero-section").CountAsync();
            Assert.True(heroSection >= 1, "Hero section should be visible on desktop");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldDisplayLoginButton_WhenNotAuthenticated()
    {
        // Arrange
        var page = await browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Assert - Look for the Sign In button (solid gold) or Get Started button (dark gold)
            // Updated to match new separate button layout
            var loginButton = page.Locator("text=Sign In").Or(
                page.Locator("text=Get Started")).Or(
                page.Locator("a[href*='authentication/login']")).Or(
                page.Locator("a[href*='authentication/register']")).Or(
                page.Locator("[aria-label*='Sign in']"));
            
            await Assertions.Expect(loginButton.First).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
