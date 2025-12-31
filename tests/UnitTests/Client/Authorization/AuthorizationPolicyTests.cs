// ============================================================================
// RAJ Financial - Authorization Policy Unit Tests
// ============================================================================
// Tests for the implicit Client role authorization model.
//
// Authorization Model:
//   - All authenticated users are Clients by default (no explicit role needed)
//   - Only Administrators require explicit role assignment in Entra ID
//   - Data access is controlled via DataAccessGrant entities, not roles
// ============================================================================

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace RajFinancial.UnitTests.Client.Authorization;

/// <summary>
///     Unit tests for the authorization policies defined in Client/Program.cs.
/// </summary>
public class AuthorizationPolicyTests
{
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationPolicyTests()
    {
        var services = new ServiceCollection();

        // Configure authorization policies matching Client/Program.cs
        services.AddAuthorizationCore(options =>
        {
            // RequireAdministrator: Explicit Administrator role required
            options.AddPolicy("RequireAdministrator", policy =>
                policy.RequireRole("Administrator"));

            // RequireClient: Any authenticated user (implicit Client)
            options.AddPolicy("RequireClient", policy =>
                policy.RequireAssertion(context =>
                {
                    if (context.User.Identity?.IsAuthenticated != true)
                        return false;

                    if (context.User.IsInRole("Client") || context.User.IsInRole("Administrator"))
                        return true;

                    var hasAnyRole = context.User.Claims.Any(c =>
                        c.Type == "roles" ||
                        c.Type == "role" ||
                        c.Type == ClaimTypes.Role);

                    return !hasAnyRole;
                }));

            // RequireAuthenticated: Any authenticated user
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });

        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        _authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
    }

    #region RequireClient Policy Tests

    [Fact]
    public async Task RequireClient_WithExplicitClientRole_Succeeds()
    {
        // Arrange
        var user = CreateAuthenticatedUser("user@example.com", "Client");

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireClient");

        // Assert
        Assert.True(result.Succeeded, "User with explicit Client role should pass RequireClient policy");
    }

    [Fact]
    public async Task RequireClient_WithAdministratorRole_Succeeds()
    {
        // Arrange
        var user = CreateAuthenticatedUser("admin@example.com", "Administrator");

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireClient");

        // Assert
        Assert.True(result.Succeeded, "Administrator should have access to Client features");
    }

    [Fact]
    public async Task RequireClient_WithNoRoles_Succeeds()
    {
        // Arrange - Authenticated user with NO roles (new signup, no role assignment)
        var user = CreateAuthenticatedUserWithoutRoles("newuser@example.com");

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireClient");

        // Assert
        Assert.True(result.Succeeded, "Authenticated user without roles should be treated as implicit Client");
    }

    [Fact]
    public async Task RequireClient_Unauthenticated_Fails()
    {
        // Arrange
        var user = CreateUnauthenticatedUser();

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireClient");

        // Assert
        Assert.False(result.Succeeded, "Unauthenticated user should not pass RequireClient policy");
    }

    [Fact]
    public async Task RequireClient_WithUnknownRole_Fails()
    {
        // Arrange - User has a role, but it's not Client or Administrator
        var user = CreateAuthenticatedUser("user@example.com", "UnknownRole");

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireClient");

        // Assert
        Assert.False(result.Succeeded, 
            "User with unknown role should not pass RequireClient (they have roles, but not the right ones)");
    }

    #endregion

    #region RequireAdministrator Policy Tests

    [Fact]
    public async Task RequireAdministrator_WithAdministratorRole_Succeeds()
    {
        // Arrange
        var user = CreateAuthenticatedUser("admin@example.com", "Administrator");

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireAdministrator");

        // Assert
        Assert.True(result.Succeeded, "User with Administrator role should pass RequireAdministrator policy");
    }

    [Fact]
    public async Task RequireAdministrator_WithClientRole_Fails()
    {
        // Arrange
        var user = CreateAuthenticatedUser("user@example.com", "Client");

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireAdministrator");

        // Assert
        Assert.False(result.Succeeded, "Client role should not pass RequireAdministrator policy");
    }

    [Fact]
    public async Task RequireAdministrator_WithNoRoles_Fails()
    {
        // Arrange
        var user = CreateAuthenticatedUserWithoutRoles("newuser@example.com");

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireAdministrator");

        // Assert
        Assert.False(result.Succeeded, "User without roles should not pass RequireAdministrator policy");
    }

    [Fact]
    public async Task RequireAdministrator_Unauthenticated_Fails()
    {
        // Arrange
        var user = CreateUnauthenticatedUser();

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireAdministrator");

        // Assert
        Assert.False(result.Succeeded, "Unauthenticated user should not pass RequireAdministrator policy");
    }

    #endregion

    #region RequireAuthenticated Policy Tests

    [Fact]
    public async Task RequireAuthenticated_AuthenticatedUser_Succeeds()
    {
        // Arrange
        var user = CreateAuthenticatedUserWithoutRoles("user@example.com");

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireAuthenticated");

        // Assert
        Assert.True(result.Succeeded, "Authenticated user should pass RequireAuthenticated policy");
    }

    [Fact]
    public async Task RequireAuthenticated_Unauthenticated_Fails()
    {
        // Arrange
        var user = CreateUnauthenticatedUser();

        // Act
        var result = await _authorizationService.AuthorizeAsync(user, "RequireAuthenticated");

        // Assert
        Assert.False(result.Succeeded, "Unauthenticated user should not pass RequireAuthenticated policy");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    ///     Creates an authenticated ClaimsPrincipal with the specified role.
    ///     Uses ClaimTypes.Role so that IsInRole() works correctly.
    /// </summary>
    private static ClaimsPrincipal CreateAuthenticatedUser(string email, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new("sub", Guid.NewGuid().ToString()),
            // Use ClaimTypes.Role so IsInRole() works, AND "roles" for our custom check
            new(ClaimTypes.Role, role),
            new("roles", role) // Entra External ID uses "roles" claim
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    ///     Creates an authenticated ClaimsPrincipal with no role claims.
    ///     This simulates a new user who just signed up but hasn't been assigned a role.
    /// </summary>
    private static ClaimsPrincipal CreateAuthenticatedUserWithoutRoles(string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new("sub", Guid.NewGuid().ToString())
            // No "roles" claim - this is the key scenario
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    ///     Creates an unauthenticated ClaimsPrincipal.
    /// </summary>
    private static ClaimsPrincipal CreateUnauthenticatedUser()
    {
        var identity = new ClaimsIdentity(); // No authentication type = unauthenticated
        return new ClaimsPrincipal(identity);
    }

    #endregion
}
