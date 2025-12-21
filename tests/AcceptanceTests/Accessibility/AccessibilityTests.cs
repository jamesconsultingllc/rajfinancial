// ============================================================================
// RAJ Financial - Accessibility Tests
// ============================================================================
// Playwright + axe-core tests for WCAG 2.1 AA compliance
// ============================================================================

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace RajFinancial.AcceptanceTests.Accessibility;

/// <summary>
/// Accessibility tests using axe-core to verify WCAG 2.1 AA compliance.
/// </summary>
public class AccessibilityTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl;

    public AccessibilityTests()
    {
        _baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? TestConfiguration.Instance.BaseUrl;
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
    }

    [Fact]
    public async Task HomePage_ShouldHaveNoAccessibilityViolations()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(_baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            
            // Wait for Blazor to fully load
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await page.WaitForTimeoutAsync(2000); // Wait for Blazor hydration
            
            // Run axe accessibility scan
            var results = await page.RunAxe(new AxeRunOptions
            {
                RunOnly = new RunOnlyOptions
                {
                    Type = "tag",
                    Values = new List<string> { "wcag2a", "wcag2aa" }
                }
            });

            // Assert
            var violations = results.Violations;
            
            if (violations.Any())
            {
                var violationMessages = string.Join("\n\n", violations.Select(v => 
                    $"Rule: {v.Id}\n" +
                    $"Impact: {v.Impact}\n" +
                    $"Description: {v.Description}\n" +
                    $"Help: {v.Help}\n" +
                    $"Help URL: {v.HelpUrl}\n" +
                    $"Nodes: {string.Join(", ", v.Nodes.Select(n => n.Html))}"));
                
                Assert.Fail($"Accessibility violations found:\n\n{violationMessages}");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldHaveProperHeadingHierarchy()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(_baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Check for h1
            var h1Count = await page.Locator("h1").CountAsync();
            Assert.True(h1Count >= 1, "Page should have at least one h1 heading");
            
            // Check that h1 comes before h2
            var headings = await page.Locator("h1, h2, h3, h4, h5, h6").AllAsync();
            if (headings.Count > 0)
            {
                var firstHeading = await headings[0].EvaluateAsync<string>("el => el.tagName");
                Assert.Equal("H1", firstHeading.ToUpper());
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldHaveAccessibleButtons()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(_baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Get all buttons and links styled as buttons
            var buttons = await page.Locator("button, a[class*='btn']").AllAsync();
            
            foreach (var button in buttons)
            {
                var text = await button.TextContentAsync();
                var ariaLabel = await button.GetAttributeAsync("aria-label");
                var title = await button.GetAttributeAsync("title");
                
                var hasAccessibleName = !string.IsNullOrWhiteSpace(text) || 
                                        !string.IsNullOrWhiteSpace(ariaLabel) ||
                                        !string.IsNullOrWhiteSpace(title);
                
                Assert.True(hasAccessibleName, 
                    $"Button should have accessible name: {await button.EvaluateAsync<string>("el => el.outerHTML")}");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldHaveAccessibleImages()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(_baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Get all images
            var images = await page.Locator("img").AllAsync();
            
            foreach (var img in images)
            {
                var alt = await img.GetAttributeAsync("alt");
                var role = await img.GetAttributeAsync("role");
                var ariaHidden = await img.GetAttributeAsync("aria-hidden");
                
                // Image should have alt text OR be marked as decorative
                var hasAlt = alt != null; // Empty alt is valid for decorative images
                var isDecorativeByRole = role == "presentation" || role == "none";
                var isHiddenFromA11y = ariaHidden == "true";
                
                Assert.True(hasAlt || isDecorativeByRole || isHiddenFromA11y,
                    $"Image should have alt attribute or be marked as decorative: {await img.GetAttributeAsync("src")}");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldHaveGoodColorContrast()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(_baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Run axe with only color contrast rule
            var results = await page.RunAxe(new AxeRunOptions
            {
                RunOnly = new RunOnlyOptions
                {
                    Type = "rule",
                    Values = new List<string> { "color-contrast" }
                }
            });

            // Assert
            var violations = results.Violations.Where(v => v.Id == "color-contrast").ToList();
            
            if (violations.Any())
            {
                var nodes = violations.SelectMany(v => v.Nodes).Take(5); // First 5 issues
                var issues = string.Join("\n", nodes.Select(n => 
                    $"Element: {n.Html}\nMessage: {n.Any}"));
                
                Assert.Fail($"Color contrast violations found:\n\n{issues}");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldBeFocusableWithKeyboard()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(_baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Press Tab to move through focusable elements
            var focusableElements = new List<string>();
            
            for (int i = 0; i < 10; i++)
            {
                await page.Keyboard.PressAsync("Tab");
                
                var activeElement = await page.EvaluateAsync<string>(
                    "document.activeElement?.tagName + (document.activeElement?.className ? '.' + document.activeElement.className : '')");
                
                if (!string.IsNullOrEmpty(activeElement) && activeElement != "BODY")
                {
                    focusableElements.Add(activeElement);
                }
            }
            
            // Assert - should be able to tab through at least a few elements
            Assert.True(focusableElements.Count >= 3, 
                $"Should have at least 3 focusable elements, found: {focusableElements.Count}");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_FocusIndicatorsShouldBeVisible()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(_baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Tab to first focusable element
            await page.Keyboard.PressAsync("Tab");
            await page.Keyboard.PressAsync("Tab");
            
            // Check if the focused element has visible focus indicator
            var hasFocusIndicator = await page.EvaluateAsync<bool>(@"
                () => {
                    const el = document.activeElement;
                    if (!el) return false;
                    
                    const styles = window.getComputedStyle(el);
                    const outline = styles.outline;
                    const boxShadow = styles.boxShadow;
                    
                    // Check if outline or box-shadow is set (common focus indicators)
                    const hasOutline = outline && !outline.includes('none') && !outline.includes('0px');
                    const hasBoxShadow = boxShadow && boxShadow !== 'none';
                    
                    return hasOutline || hasBoxShadow;
                }
            ");
            
            Assert.True(hasFocusIndicator, 
                "Focused elements should have visible focus indicators");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldHaveAccessibleLandmarks()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        
        try
        {
            // Act
            await page.GotoAsync(_baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);
            
            // Check for main landmark
            var mainCount = await page.Locator("main, [role='main']").CountAsync();
            Assert.True(mainCount >= 1, "Page should have a main landmark");
            
            // Check for navigation landmark
            var navCount = await page.Locator("nav, [role='navigation']").CountAsync();
            Assert.True(navCount >= 1, "Page should have a navigation landmark");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
