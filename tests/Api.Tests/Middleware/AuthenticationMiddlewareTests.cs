using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Middleware;

namespace RajFinancial.Api.Tests.Middleware;

/// <summary>
/// Unit tests for <see cref="AuthenticationMiddleware"/>.
/// Tests OWASP A01:2025 and A07:2025 compliance.
/// </summary>
public class AuthenticationMiddlewareTests
{
    private readonly AuthenticationMiddleware _middleware;
    private readonly Mock<ILogger<AuthenticationMiddleware>> _loggerMock;

    public AuthenticationMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<AuthenticationMiddleware>>();
        _middleware = new AuthenticationMiddleware(_loggerMock.Object);
    }

    // =========================================================================
    // Authenticated user context population
    // =========================================================================

    [Fact]
    public async Task Invoke_WithAuthenticatedPrincipal_SetsUserIdInContext()
    {
        // Arrange
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123");
        context.Items["ClaimsPrincipal"] = principal;

        var nextCalled = false;
        Task Next(FunctionContext _)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
        context.Items["UserId"].Should().Be("user-123");
        context.Items["IsAuthenticated"].Should().Be(true);
    }

    [Fact]
    public async Task Invoke_WithAuthenticatedPrincipal_SetsEmailInContext()
    {
        // Arrange
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123", email: "test@rajfinancial.com");
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        context.Items["UserEmail"].Should().Be("test@rajfinancial.com");
    }

    [Fact]
    public async Task Invoke_WithAuthenticatedPrincipal_SetsNameInContext()
    {
        // Arrange
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123", name: "John Doe");
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        context.Items["UserName"].Should().Be("John Doe");
    }

    [Fact]
    public async Task Invoke_WithAuthenticatedPrincipal_SetsRolesInContext()
    {
        // Arrange
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123", roles: ["Client", "Administrator"]);
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        var roles = context.Items["UserRoles"] as IReadOnlyList<string>;
        roles.Should().NotBeNull();
        roles.Should().Contain("Client");
        roles.Should().Contain("Administrator");
    }

    [Fact]
    public async Task Invoke_WithAuthenticatedPrincipal_SetsClaimsPrincipalInContext()
    {
        // Arrange
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123");
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        context.Items["ClaimsPrincipal"].Should().Be(principal);
    }

    // =========================================================================
    // Unauthenticated requests
    // =========================================================================

    [Fact]
    public async Task Invoke_WithUnauthenticatedPrincipal_SetsIsAuthenticatedFalse()
    {
        // Arrange
        var context = new TestFunctionContext();
        var identity = new ClaimsIdentity(); // Not authenticated (no auth type)
        var principal = new ClaimsPrincipal(identity);
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        context.Items["IsAuthenticated"].Should().Be(false);
    }

    [Fact]
    public async Task Invoke_WithNoPrincipal_SetsIsAuthenticatedFalse()
    {
        // Arrange
        var context = new TestFunctionContext();

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        context.Items["IsAuthenticated"].Should().Be(false);
    }

    [Fact]
    public async Task Invoke_AlwaysCallsNext()
    {
        // Arrange
        var context = new TestFunctionContext();
        var nextCalled = false;

        Task Next(FunctionContext _)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    // =========================================================================
    // Alternative claim types (Entra ID compatibility)
    // =========================================================================

    [Fact]
    public async Task Invoke_WithOidClaim_ExtractsUserId()
    {
        // Arrange - Entra uses 'oid' as alternative claim for object ID
        var context = new TestFunctionContext();
        var claims = new List<Claim>
        {
            new("oid", "user-alt-123")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        context.Items["UserId"].Should().Be("user-alt-123");
    }

    [Fact]
    public async Task Invoke_WithEmailsClaim_ExtractsEmail()
    {
        // Arrange - Entra External ID uses 'emails' claim
        var context = new TestFunctionContext();
        var claims = new List<Claim>
        {
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-123"),
            new("emails", "external@rajfinancial.com")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        context.Items["UserEmail"].Should().Be("external@rajfinancial.com");
    }

    [Fact]
    public async Task Invoke_WithStandardEmailClaim_ExtractsEmail()
    {
        // Arrange - fallback to standard email claim
        var context = new TestFunctionContext();
        var claims = new List<Claim>
        {
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-123"),
            new("email", "standard@rajfinancial.com")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        context.Items["UserEmail"].Should().Be("standard@rajfinancial.com");
    }

    // =========================================================================
    // Role deduplication
    // =========================================================================

    [Fact]
    public async Task Invoke_WithDuplicateRoles_DeduplicatesRoles()
    {
        // Arrange - roles and ClaimTypes.Role may overlap
        var context = new TestFunctionContext();
        var claims = new List<Claim>
        {
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-123"),
            new("roles", "Administrator"),
            new(ClaimTypes.Role, "Administrator")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        var roles = context.Items["UserRoles"] as IReadOnlyList<string>;
        roles.Should().NotBeNull();
        roles.Should().HaveCount(1);
        roles.Should().Contain("Administrator");
    }

    // =========================================================================
    // Missing user ID handling
    // =========================================================================

    [Fact]
    public async Task Invoke_WithAuthenticatedButNoObjectId_DoesNotSetUserId()
    {
        // Arrange - authenticated but missing object ID claim
        var context = new TestFunctionContext();
        var claims = new List<Claim>
        {
            new("email", "noid@rajfinancial.com")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        context.Items.Should().NotContainKey("UserId");
    }

    // =========================================================================
    // Logging
    // =========================================================================

    [Fact]
    public async Task Invoke_WithAuthenticatedUser_LogsDebugMessage()
    {
        // Arrange
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-for-log", roles: ["Client"]);
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await _middleware.Invoke(context, Next);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("user-for-log")),
                null,
                It.IsAny<Func<It.IsAnyType, System.Exception?, string>>()),
            Times.Once);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a ClaimsPrincipal simulating an Entra ID authenticated user.
    /// </summary>
    private static ClaimsPrincipal CreatePrincipal(
        string objectId,
        string? email = null,
        string? name = null,
        string[]? roles = null)
    {
        var claims = new List<Claim>
        {
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", objectId)
        };

        if (email is not null)
            claims.Add(new Claim("emails", email));

        if (name is not null)
            claims.Add(new Claim("name", name));

        if (roles is not null)
        {
            foreach (var role in roles)
                claims.Add(new Claim("roles", role));
        }

        var identity = new ClaimsIdentity(claims, "Bearer");
        return new ClaimsPrincipal(identity);
    }
}
