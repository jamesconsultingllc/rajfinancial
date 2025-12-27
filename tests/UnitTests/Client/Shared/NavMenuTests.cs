// ============================================================================
// RAJ Financial - NavMenu Component Unit Tests
// ============================================================================
// bUnit tests for the navigation menu with role-based authorization
// ============================================================================

using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using RajFinancial.Client.Shared;

namespace RajFinancial.UnitTests.Client.Shared;

/// <summary>
/// Unit tests for the NavMenu component.
/// </summary>
public class NavMenuTests : TestContext
{
    public NavMenuTests()
    {
        // Register the mock localizer
        var localizer = new Mock<IStringLocalizer<NavMenu>>();
        localizer.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, GetLocalizedValue(key)));
        Services.AddSingleton(localizer.Object);
    }

    /// <summary>
    /// Returns localized values matching the resource file.
    /// </summary>
    private static string GetLocalizedValue(string key) => key switch
    {
        "Brand.Name" => "RAJ Financial",
        "Brand.Home.AriaLabel" => "RAJ Financial - Home",
        "Nav.AriaLabel" => "Main navigation",
        "Nav.Home" => "Home",
        "Section.MyAccount" => "My Account",
        "Nav.Portfolio" => "My Portfolio",
        "Nav.Transactions" => "Transactions",
        "Nav.Statements" => "Statements",
        "Section.AdvisorTools" => "Advisor Tools",
        "Nav.MyClients" => "My Clients",
        "Nav.Reports" => "Reports",
        "Section.Administration" => "Administration",
        "Nav.Dashboard" => "Dashboard",
        "Nav.UserManagement" => "User Management",
        "Nav.Settings" => "Settings",
        _ => key
    };

    [Fact]
    public void NavMenu_Renders_BrandLogo()
    {
        // Arrange
        SetupAuthorization();

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var logo = cut.Find(".nav-brand-icon");
        Assert.NotNull(logo);
    }

    [Fact]
    public void NavMenu_Renders_BrandName()
    {
        // Arrange
        SetupAuthorization();

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var brandText = cut.Find(".nav-brand-text");
        Assert.Equal("RAJ Financial", brandText.TextContent);
    }

    [Fact]
    public void NavMenu_Renders_HomeLink_Always()
    {
        // Arrange
        SetupAuthorization();

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert - "Home" appears somewhere in the markup
        Assert.Contains("Home", cut.Markup);
        
        // Also verify a home link exists
        var homeLink = cut.Find("a[href='']");
        Assert.NotNull(homeLink);
    }

    [Fact]
    public void NavMenu_HidesClientSection_ForUnauthenticatedUsers()
    {
        // Arrange
        SetupAuthorization(isAuthenticated: false);

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert - My Account section should not be visible
        Assert.DoesNotContain("My Account", cut.Markup);
        Assert.DoesNotContain("My Portfolio", cut.Markup);
    }

    [Fact]
    public void NavMenu_ShowsClientSection_ForClientRole()
    {
        // Arrange
        SetupAuthorizationWithPolicies("RequireClient");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert - My Account section should be visible
        Assert.Contains("My Account", cut.Markup);
    }

    [Fact]
    public void NavMenu_HidesAdvisorSection_ForClientRole()
    {
        // Arrange - Client has RequireClient but NOT RequireAdvisor
        SetupAuthorizationWithPolicies("RequireClient");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert - Advisor Tools should NOT be visible
        Assert.DoesNotContain("Advisor Tools", cut.Markup);
    }

    [Fact]
    public void NavMenu_ShowsAdvisorSection_ForAdvisorRole()
    {
        // Arrange - Advisor has both RequireAdvisor and RequireClient
        SetupAuthorizationWithPolicies("RequireAdvisor", "RequireClient");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert - Advisor Tools should be visible
        Assert.Contains("Advisor Tools", cut.Markup);
    }

    [Fact]
    public void NavMenu_HidesAdminSection_ForNonAdminRoles()
    {
        // Arrange - Client only has RequireClient
        SetupAuthorizationWithPolicies("RequireClient");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert - Administration should NOT be visible
        Assert.DoesNotContain("Administration", cut.Markup);
    }

    [Fact]
    public void NavMenu_ShowsAdminSection_ForAdministratorRole()
    {
        // Arrange - Administrator only has RequireAdministrator
        SetupAuthorizationWithPolicies("RequireAdministrator");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert - Administration should be visible
        Assert.Contains("Administration", cut.Markup);
    }

    [Fact]
    public void NavMenu_ShowsAllSections_ForAdminAdvisorRole()
    {
        // Arrange - AdminAdvisor has all policies
        SetupAuthorizationWithPolicies("RequireAdministrator", "RequireAdvisor", "RequireClient");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert - All sections should be visible
        Assert.Contains("My Account", cut.Markup);
        Assert.Contains("Advisor Tools", cut.Markup);
        Assert.Contains("Administration", cut.Markup);
    }

    [Fact]
    public void NavMenu_HasAccessibleNavigation()
    {
        // Arrange
        SetupAuthorization();

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert - Should have nav element with aria-label
        var nav = cut.Find("nav");
        Assert.True(nav.HasAttribute("aria-label"), "Navigation should have aria-label");
    }

    [Fact]
    public void NavMenu_LinksHaveAccessibleLabels()
    {
        // Arrange
        SetupAuthorization();

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert - All nav links should have text content
        var links = cut.FindAll(".nav-link");
        foreach (var link in links)
        {
            Assert.False(string.IsNullOrWhiteSpace(link.TextContent), 
                "Nav links should have text content");
        }
    }

    /// <summary>
    /// Sets up authorization for unauthenticated or basic authenticated state.
    /// </summary>
    private void SetupAuthorization(bool isAuthenticated = false)
    {
        var authContext = this.AddTestAuthorization();
        
        if (isAuthenticated)
        {
            authContext.SetAuthorized("testuser@example.com");
        }
        else
        {
            authContext.SetNotAuthorized();
        }
    }

    /// <summary>
    /// Sets up authorization with specific policies granted.
    /// </summary>
    private void SetupAuthorizationWithPolicies(params string[] policies)
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("testuser@example.com");
        authContext.SetPolicies(policies);
    }
}
