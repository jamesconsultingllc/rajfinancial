// ============================================================================
// RAJ Financial - NavMenu Component Unit Tests
// ============================================================================
// bUnit tests for the navigation menu with role-based authorization
//
// Role Model:
//   - Client: Standard user who owns their financial data (IMPLICIT)
//   - Administrator: Platform staff with system-wide access (EXPLICIT)
//
// Fine-grained access control is handled via DataAccessGrant entities,
// not through additional roles.
// ============================================================================

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using RajFinancial.Client.Shared;

namespace RajFinancial.UnitTests.Client.Shared;

/// <summary>
///     Unit tests for the NavMenu component.
/// </summary>
public class NavMenuTests : BunitContext
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
    ///     Returns localized values matching the resource file.
    /// </summary>
    private static string GetLocalizedValue(string key)
    {
        return key switch
        {
            "Brand.Name" => "RAJ Financial",
            "Brand.Home.AriaLabel" => "RAJ Financial - Home",
            "Nav.AriaLabel" => "Main navigation",
            "Nav.Home" => "Home",
            "Section.MyAccount" => "My Account",
            "Nav.Portfolio" => "My Portfolio",
            "Nav.Transactions" => "Transactions",
            "Nav.Statements" => "Statements",
            "Nav.Sharing" => "Sharing",
            "Section.Administration" => "Administration",
            "Nav.Dashboard" => "Dashboard",
            "Nav.UserManagement" => "User Management",
            "Nav.Settings" => "Settings",
            _ => key
        };
    }

    [Fact]
    public void NavMenu_Renders_BrandLogo()
    {
        // Arrange
        SetupAuthorization();

        // Act
        var cut = Render<NavMenu>();

        // Assert - Logo should be rendered with nav-brand-logo class
        var logo = cut.Find(".nav-brand-logo");
        Assert.NotNull(logo);
        Assert.Equal("img", logo.TagName.ToLower());
    }

    [Fact]
    public void NavMenu_BrandLogo_HasAltText()
    {
        // Arrange
        SetupAuthorization();

        // Act
        var cut = Render<NavMenu>();

        // Assert - Logo should have alt text for accessibility
        var logo = cut.Find(".nav-brand-logo");
        Assert.True(logo.HasAttribute("alt"), "Logo should have alt attribute");
        Assert.Equal("RAJ Financial", logo.GetAttribute("alt"));
    }

    [Fact]
    public void NavMenu_Renders_HomeLink_Always()
    {
        // Arrange
        SetupAuthorization();

        // Act
        var cut = Render<NavMenu>();

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
        SetupAuthorization();

        // Act
        var cut = Render<NavMenu>();

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
        var cut = Render<NavMenu>();

        // Assert - My Account section should be visible
        Assert.Contains("My Account", cut.Markup);
    }

    [Fact]
    public void NavMenu_HidesAdminSection_ForClientRole()
    {
        // Arrange - Client only has RequireClient
        SetupAuthorizationWithPolicies("RequireClient");

        // Act
        var cut = Render<NavMenu>();

        // Assert - Administration should NOT be visible
        Assert.DoesNotContain("Administration", cut.Markup);
    }

    [Fact]
    public void NavMenu_ShowsAdminSection_ForAdministratorRole()
    {
        // Arrange - Administrator has RequireAdministrator
        SetupAuthorizationWithPolicies("RequireAdministrator");

        // Act
        var cut = Render<NavMenu>();

        // Assert - Administration should be visible
        Assert.Contains("Administration", cut.Markup);
    }

    [Fact]
    public void NavMenu_ShowsAllSections_ForAdministratorWithClientAccess()
    {
        // Arrange - Administrator can also access client features
        SetupAuthorizationWithPolicies("RequireAdministrator", "RequireClient");

        // Act
        var cut = Render<NavMenu>();

        // Assert - All sections should be visible
        Assert.Contains("My Account", cut.Markup);
        Assert.Contains("Administration", cut.Markup);
    }

    [Fact]
    public void NavMenu_ShowsSharingLink_ForAuthenticatedUsers()
    {
        // Arrange
        SetupAuthorizationWithPolicies("RequireClient");

        // Act
        var cut = Render<NavMenu>();

        // Assert - Sharing link should be visible in My Account section
        Assert.Contains("Sharing", cut.Markup);
    }

    [Fact]
    public void NavMenu_HasAccessibleNavigation()
    {
        // Arrange
        SetupAuthorization();

        // Act
        var cut = Render<NavMenu>();

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
        var cut = Render<NavMenu>();

        // Assert - All nav links should have text content
        var links = cut.FindAll(".nav-link");
        foreach (var link in links)
            Assert.False(string.IsNullOrWhiteSpace(link.TextContent),
                "Nav links should have text content");
    }

    /// <summary>
    ///     Sets up authorization for unauthenticated or basic authenticated state.
    /// </summary>
    private void SetupAuthorization(bool isAuthenticated = false)
    {
        var authContext = AddAuthorization();

        if (isAuthenticated)
            authContext.SetAuthorized("testuser@example.com");
        else
            authContext.SetNotAuthorized();
    }

    /// <summary>
    ///     Sets up authorization with specific policies granted.
    /// </summary>
    private void SetupAuthorizationWithPolicies(params string[] policies)
    {
        var authContext = AddAuthorization();
        authContext.SetAuthorized("testuser@example.com");
        authContext.SetPolicies(policies);
    }
}