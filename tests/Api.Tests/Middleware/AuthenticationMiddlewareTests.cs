using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Hosting;
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
    private readonly AuthenticationMiddleware middleware;
    private readonly Mock<ILogger<AuthenticationMiddleware>> loggerMock;
    private readonly Mock<IHostEnvironment> environmentMock;

    public AuthenticationMiddlewareTests()
    {
        loggerMock = new Mock<ILogger<AuthenticationMiddleware>>();
        loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        middleware = new AuthenticationMiddleware(loggerMock.Object, environmentMock.Object);
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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

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
        await middleware.Invoke(context, Next);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("user-for-log")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // =========================================================================
    // UserIdGuid population
    // =========================================================================

    [Fact]
    public async Task Invoke_WithValidGuidObjectId_SetsUserIdGuidInContext()
    {
        // Arrange
        var guid = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: guid.ToString());
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        context.Items["UserIdGuid"].Should().Be(guid);
    }

    [Fact]
    public async Task Invoke_WithNonGuidObjectId_DoesNotSetUserIdGuid()
    {
        // Arrange — "user-123" is not a valid Guid
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123");
        context.Items["ClaimsPrincipal"] = principal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        context.Items["UserId"].Should().Be("user-123");
        context.Items.Should().NotContainKey("UserIdGuid");
    }

    // =========================================================================
    // JWT parsing — environment gating (Finding #1 security fix)
    // =========================================================================

    [Fact]
    public async Task Invoke_ProductionEnvironment_RejectsUnvalidatedJwt()
    {
        // Arrange — create middleware with Production environment
        var prodEnv = new Mock<IHostEnvironment>();
        prodEnv.Setup(e => e.EnvironmentName).Returns("Production");
        var prodMiddleware = new AuthenticationMiddleware(loggerMock.Object, prodEnv.Object);

        // Build a minimal JWT token (unvalidated) to pass via Authorization header
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateEncodedJwt(
            issuer: "https://fake-issuer",
            audience: "fake-audience",
            subject: new ClaimsIdentity([
                new Claim("oid", Guid.NewGuid().ToString()),
                new Claim("roles", "Administrator")
            ]),
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: DateTime.UtcNow.AddHours(1),
            issuedAt: DateTime.UtcNow,
            signingCredentials: null);

        var context = CreateContextWithAuthorizationHeader($"Bearer {token}");
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await prodMiddleware.Invoke(context, Next);

        // Assert — token must be rejected; user must NOT be authenticated
        context.Items["IsAuthenticated"].Should().Be(false);
        context.Items.Should().NotContainKey("UserId");

        // Verify warning was logged
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Rejecting unvalidated token")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_DevelopmentEnvironment_ParsesUnvalidatedJwt()
    {
        // Arrange — default fixture middleware uses Development environment
        var handler = new JwtSecurityTokenHandler();
        var userId = Guid.NewGuid().ToString();
        var token = handler.CreateEncodedJwt(
            issuer: "https://fake-issuer",
            audience: "fake-audience",
            subject: new ClaimsIdentity([
                new Claim("oid", userId),
                new Claim("roles", "Client")
            ]),
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: DateTime.UtcNow.AddHours(1),
            issuedAt: DateTime.UtcNow,
            signingCredentials: null);

        var context = CreateContextWithAuthorizationHeader($"Bearer {token}");
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert — in Development, the unvalidated JWT should be trusted
        context.Items["IsAuthenticated"].Should().Be(true);
        context.Items["UserId"].Should().Be(userId);
    }

    // =========================================================================
    // EasyAuth via HttpContext.User (ConfigureFunctionsWebApplication)
    // =========================================================================

    [Fact]
    public async Task Invoke_WithEasyAuthHttpContextUser_SetsUserContext()
    {
        // Arrange — simulate EasyAuth populating HttpContext.User via ConfigureFunctionsWebApplication
        var userId = Guid.NewGuid().ToString();
        var easyAuthPrincipal = CreatePrincipal(objectId: userId, email: "easyauth@rajfinancial.com", roles: ["Client"]);
        var context = CreateContextWithHttpContextUser(easyAuthPrincipal);

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert — claims from HttpContext.User must be used
        context.Items["IsAuthenticated"].Should().Be(true);
        context.Items["UserId"].Should().Be(userId);
        context.Items["UserEmail"].Should().Be("easyauth@rajfinancial.com");
        var roles = context.Items["UserRoles"] as IReadOnlyList<string>;
        roles.Should().Contain("Client");
    }

    [Fact]
    public async Task Invoke_WithEasyAuthHttpContextUser_ProductionNoJwtBypass()
    {
        // Arrange — Production environment; EasyAuth sets HttpContext.User (no bearer token needed)
        var prodEnv = new Mock<IHostEnvironment>();
        prodEnv.Setup(e => e.EnvironmentName).Returns("Production");
        // Use a fresh logger mock so previous test runs don't pollute the Times.Never assertion
        var freshLogger = new Mock<ILogger<AuthenticationMiddleware>>();
        freshLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var prodMiddleware = new AuthenticationMiddleware(freshLogger.Object, prodEnv.Object);

        var userId = Guid.NewGuid().ToString();
        var easyAuthPrincipal = CreatePrincipal(objectId: userId);
        var context = CreateContextWithHttpContextUser(easyAuthPrincipal);

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await prodMiddleware.Invoke(context, Next);

        // Assert — EasyAuth-validated principal accepted; no "unvalidated token" warning logged
        context.Items["IsAuthenticated"].Should().Be(true);
        context.Items["UserId"].Should().Be(userId);
        freshLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Rejecting unvalidated token")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Invoke_WithUnauthenticatedHttpContextUser_FallsThroughToJwt()
    {
        // Arrange — HttpContext.User present but not authenticated; should fall through to JWT parsing
        var unauthenticatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // no auth type → not authenticated
        var context = CreateContextWithHttpContextUser(unauthenticatedPrincipal);

        // Also add an Authorization header so the JWT path is exercised
        var handler = new JwtSecurityTokenHandler();
        var userId = Guid.NewGuid().ToString();
        var token = handler.CreateEncodedJwt(
            issuer: "https://fake-issuer",
            audience: "fake-audience",
            subject: new ClaimsIdentity([new Claim("oid", userId)]),
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: DateTime.UtcNow.AddHours(1),
            issuedAt: DateTime.UtcNow,
            signingCredentials: null);

        var headers = new HttpHeadersCollection { { "Authorization", $"Bearer {token}" } };
        context.SetHttpRequestHeaders(headers);

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act — Development middleware used so JWT fallback is allowed
        await middleware.Invoke(context, Next);

        // Assert — JWT parsed because HttpContext.User was not authenticated
        context.Items["IsAuthenticated"].Should().Be(true);
        context.Items["UserId"].Should().Be(userId);
    }

    [Fact]
    public async Task Invoke_ItemsClaimsPrincipalTakesPriorityOverHttpContextUser()
    {
#pragma warning disable S125 // Descriptive prose comment, not commented-out code.
        // Arrange — both Items["ClaimsPrincipal"] and HttpContext.User are set;
        // Items["ClaimsPrincipal"] should win (existing behavior preserved)
#pragma warning restore S125
        var itemsUserId = Guid.NewGuid().ToString();
        var httpContextUserId = Guid.NewGuid().ToString();

        var itemsPrincipal = CreatePrincipal(objectId: itemsUserId);
        var httpContextPrincipal = CreatePrincipal(objectId: httpContextUserId);

        var context = CreateContextWithHttpContextUser(httpContextPrincipal);
        context.Items["ClaimsPrincipal"] = itemsPrincipal;

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert — the principal from Items takes precedence
        context.Items["UserId"].Should().Be(itemsUserId);
    }

    [Fact]
    public async Task Invoke_WithNullHttpContext_FallsThroughToJwtParsing()
    {
        // Arrange — no HttpContext set (non-HTTP trigger or missing context); falls through to JWT path
        var handler = new JwtSecurityTokenHandler();
        var userId = Guid.NewGuid().ToString();
        var token = handler.CreateEncodedJwt(
            issuer: "https://fake-issuer",
            audience: "fake-audience",
            subject: new ClaimsIdentity([new Claim("oid", userId)]),
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: DateTime.UtcNow.AddHours(1),
            issuedAt: DateTime.UtcNow,
            signingCredentials: null);

        var context = CreateContextWithAuthorizationHeader($"Bearer {token}");

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act — Development middleware; no Items["ClaimsPrincipal"], no HttpContext in Items
        await middleware.Invoke(context, Next);

        // Assert — fell through to JWT parsing
        context.Items["IsAuthenticated"].Should().Be(true);
        context.Items["UserId"].Should().Be(userId);
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

    /// <summary>
    /// Creates a <see cref="TestFunctionContext"/> with an HTTP request containing
    /// an Authorization header. Used to test the JWT parsing path (no EasyAuth principal).
    /// </summary>
    private static TestFunctionContext CreateContextWithAuthorizationHeader(string authorizationHeader)
    {
        var context = new TestFunctionContext();
        var headers = new HttpHeadersCollection { { "Authorization", authorizationHeader } };
        context.SetHttpRequestHeaders(headers);
        return context;
    }

    /// <summary>
    /// Creates a <see cref="TestFunctionContext"/> that simulates EasyAuth populating
    /// <see cref="HttpContext.User"/> via <c>ConfigureFunctionsWebApplication()</c>.
    /// </summary>
    private static TestFunctionContext CreateContextWithHttpContextUser(ClaimsPrincipal userPrincipal)
    {
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.SetupGet(h => h.User).Returns(userPrincipal);

        var context = new TestFunctionContext
        {
            Items =
            {
                // GetHttpContext() reads from Items["HttpRequestContext"] — the key used internally
                // by Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore (Constants.HttpContextKey).
                ["HttpRequestContext"] = mockHttpContext.Object
            }
        };
        return context;
    }
}
