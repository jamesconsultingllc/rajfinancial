using System.Security.Claims;
using FluentAssertions;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Exception;

namespace RajFinancial.UnitTests.Api.Middleware;

/// <summary>
/// Unit tests for <see cref="FunctionContextExtensions"/>.
/// Tests the extension methods used to access user context from FunctionContext.
/// </summary>
public class FunctionContextExtensionsTests
{
    [Fact]
    public void GetUserId_WhenUserIdExists_ReturnsUserId()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["UserId"] = "user-123";

        // Act
        var result = context.GetUserId();

        // Assert
        result.Should().Be("user-123");
    }

    [Fact]
    public void GetUserId_WhenUserIdMissing_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();

        // Act
        var result = context.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserEmail_WhenEmailExists_ReturnsEmail()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["UserEmail"] = "test@example.com";

        // Act
        var result = context.GetUserEmail();

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void GetUserEmail_WhenEmailMissing_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();

        // Act
        var result = context.GetUserEmail();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserName_WhenNameExists_ReturnsName()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["UserName"] = "John Doe";

        // Act
        var result = context.GetUserName();

        // Assert
        result.Should().Be("John Doe");
    }

    [Fact]
    public void GetUserName_WhenNameMissing_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();

        // Act
        var result = context.GetUserName();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserRoles_WhenRolesExist_ReturnsRoles()
    {
        // Arrange
        var context = new TestFunctionContext();
        var roles = new List<string> { "Client", "Administrator" };
        context.Items["UserRoles"] = roles;

        // Act
        var result = context.GetUserRoles();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Client");
        result.Should().Contain("Administrator");
    }

    [Fact]
    public void GetUserRoles_WhenRolesMissing_ReturnsEmptyCollection()
    {
        // Arrange
        var context = new TestFunctionContext();

        // Act
        var result = context.GetUserRoles();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void IsAuthenticated_WhenTrue_ReturnsTrue()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;

        // Act
        var result = context.IsAuthenticated();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WhenFalse_ReturnsFalse()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = false;

        // Act
        var result = context.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenMissing_ReturnsFalse()
    {
        // Arrange
        var context = new TestFunctionContext();

        // Act
        var result = context.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("Administrator", true)]
    [InlineData("administrator", true)]
    [InlineData("ADMINISTRATOR", true)]
    [InlineData("Client", false)]
    [InlineData("Unknown", false)]
    public void HasRole_ReturnsExpectedResult(string role, bool expected)
    {
        // Arrange
        var context = new TestFunctionContext();
        var roles = new List<string> { "Administrator" };
        context.Items["UserRoles"] = roles;

        // Act
        var result = context.HasRole(role);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsAdministrator_WhenHasAdminRole_ReturnsTrue()
    {
        // Arrange
        var context = new TestFunctionContext();
        var roles = new List<string> { "Administrator" };
        context.Items["UserRoles"] = roles;

        // Act
        var result = context.IsAdministrator();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAdministrator_WhenNoAdminRole_ReturnsFalse()
    {
        // Arrange
        var context = new TestFunctionContext();
        var roles = new List<string> { "Client" };
        context.Items["UserRoles"] = roles;

        // Act
        var result = context.IsAdministrator();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetClaimsPrincipal_WhenExists_ReturnsPrincipal()
    {
        // Arrange
        var context = new TestFunctionContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser")
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        context.Items["ClaimsPrincipal"] = principal;

        // Act
        var result = context.GetClaimsPrincipal();

        // Assert
        result.Should().NotBeNull();
        result.Identity!.Name.Should().Be("testuser");
    }

    [Fact]
    public void GetClaimsPrincipal_WhenMissing_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();

        // Act
        var result = context.GetClaimsPrincipal();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void RequireAuthentication_WhenAuthenticated_DoesNotThrow()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;

        // Act & Assert
        var action = () => context.RequireAuthentication();
        action.Should().NotThrow();
    }

    [Fact]
    public void RequireAuthentication_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = false;

        // Act & Assert
        var action = () => context.RequireAuthentication();
        action.Should().Throw<UnauthorizedException>()
            .WithMessage("Authentication required");
    }

    [Fact]
    public void RequireRole_WhenHasRole_DoesNotThrow()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        var roles = new List<string> { "Administrator" };
        context.Items["UserRoles"] = roles;

        // Act & Assert
        var action = () => context.RequireRole("Administrator");
        action.Should().NotThrow();
    }

    [Fact]
    public void RequireRole_WhenMissingRole_ThrowsForbiddenException()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        var roles = new List<string> { "Client" };
        context.Items["UserRoles"] = roles;

        // Act & Assert
        var action = () => context.RequireRole("Administrator");
        action.Should().Throw<ForbiddenException>()
            .WithMessage("Role 'Administrator' is required");
    }

    [Fact]
    public void RequireRole_WhenNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = false;

        // Act & Assert
        var action = () => context.RequireRole("Administrator");
        action.Should().Throw<UnauthorizedException>();
    }

    [Fact]
    public void RequireAdministrator_WhenIsAdmin_DoesNotThrow()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        var roles = new List<string> { "Administrator" };
        context.Items["UserRoles"] = roles;

        // Act & Assert
        var action = () => context.RequireAdministrator();
        action.Should().NotThrow();
    }

    [Fact]
    public void RequireAdministrator_WhenNotAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        var roles = new List<string> { "Client" };
        context.Items["UserRoles"] = roles;

        // Act & Assert
        var action = () => context.RequireAdministrator();
        action.Should().Throw<ForbiddenException>();
    }
}