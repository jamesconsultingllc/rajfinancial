using System.Security.Claims;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Services.Auth;

namespace RajFinancial.Api.Tests.Middleware;

/// <summary>
/// Unit tests for <see cref="AuthenticationMiddleware"/>.
/// Tests OWASP A01:2025 and A07:2025 compliance.
/// </summary>
public class AuthenticationMiddlewareTests
{
    private readonly AuthenticationMiddleware middleware;
    private readonly Mock<ILogger<AuthenticationMiddleware>> loggerMock;
    private readonly Mock<IJwtBearerValidator> validatorMock;

    public AuthenticationMiddlewareTests()
    {
        loggerMock = new Mock<ILogger<AuthenticationMiddleware>>();
        loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        validatorMock = new Mock<IJwtBearerValidator>();
        middleware = new AuthenticationMiddleware(loggerMock.Object, validatorMock.Object);
    }

    // =========================================================================
    // Authenticated user context population (Items[ClaimsPrincipal] path)
    // =========================================================================

    [Fact]
    public async Task Invoke_WithAuthenticatedPrincipal_SetsUserIdInContext()
    {
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123");
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        await middleware.Invoke(context, Next);

        nextCalled.Should().BeTrue();
        context.Items[FunctionContextKeys.UserId].Should().Be("user-123");
        context.Items[FunctionContextKeys.IsAuthenticated].Should().Be(true);
    }

    [Fact]
    public async Task Invoke_WithAuthenticatedPrincipal_SetsEmailInContext()
    {
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123", email: "test@rajfinancial.com");
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.UserEmail].Should().Be("test@rajfinancial.com");
    }

    [Fact]
    public async Task Invoke_WithAuthenticatedPrincipal_SetsNameInContext()
    {
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123", name: "John Doe");
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.UserName].Should().Be("John Doe");
    }

    [Fact]
    public async Task Invoke_WithAuthenticatedPrincipal_SetsRolesInContext()
    {
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123", roles: ["Client", "Administrator"]);
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        var roles = context.Items[FunctionContextKeys.UserRoles] as IReadOnlyList<string>;
        roles.Should().NotBeNull();
        roles.Should().Contain("Client");
        roles.Should().Contain("Administrator");
    }

    [Fact]
    public async Task Invoke_WithAuthenticatedPrincipal_SetsClaimsPrincipalInContext()
    {
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123");
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.ClaimsPrincipal].Should().Be(principal);
    }

    // =========================================================================
    // Unauthenticated requests
    // =========================================================================

    [Fact]
    public async Task Invoke_WithUnauthenticatedPrincipal_SetsIsAuthenticatedFalse()
    {
        var context = new TestFunctionContext();
        var identity = new ClaimsIdentity(); // Not authenticated (no auth type)
        var principal = new ClaimsPrincipal(identity);
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.IsAuthenticated].Should().Be(false);
    }

    [Fact]
    public async Task Invoke_WithNoPrincipal_SetsIsAuthenticatedFalse()
    {
        var context = new TestFunctionContext();

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.IsAuthenticated].Should().Be(false);
    }

    [Fact]
    public async Task Invoke_AlwaysCallsNext()
    {
        var context = new TestFunctionContext();
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        await middleware.Invoke(context, Next);

        nextCalled.Should().BeTrue();
    }

    // =========================================================================
    // Alternative claim types (Entra ID compatibility)
    // =========================================================================

    [Fact]
    public async Task Invoke_WithOidClaim_ExtractsUserId()
    {
        var context = new TestFunctionContext();
        var claims = new List<Claim> { new("oid", "user-alt-123") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.UserId].Should().Be("user-alt-123");
    }

    [Fact]
    public async Task Invoke_WithEmailsClaim_ExtractsEmail()
    {
        var context = new TestFunctionContext();
        var claims = new List<Claim>
        {
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-123"),
            new("emails", "external@rajfinancial.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.UserEmail].Should().Be("external@rajfinancial.com");
    }

    [Fact]
    public async Task Invoke_WithStandardEmailClaim_ExtractsEmail()
    {
        var context = new TestFunctionContext();
        var claims = new List<Claim>
        {
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-123"),
            new("email", "standard@rajfinancial.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.UserEmail].Should().Be("standard@rajfinancial.com");
    }

    // =========================================================================
    // Role deduplication
    // =========================================================================

    [Fact]
    public async Task Invoke_WithDuplicateRoles_DeduplicatesRoles()
    {
        var context = new TestFunctionContext();
        var claims = new List<Claim>
        {
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-123"),
            new("roles", "Administrator"),
            new(ClaimTypes.Role, "Administrator")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        var roles = context.Items[FunctionContextKeys.UserRoles] as IReadOnlyList<string>;
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
        var context = new TestFunctionContext();
        var claims = new List<Claim> { new("email", "noid@rajfinancial.com") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items.Should().NotContainKey("UserId");
    }

    // =========================================================================
    // Logging
    // =========================================================================

    [Fact]
    public async Task Invoke_WithAuthenticatedUser_LogsDebugMessage()
    {
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-for-log", roles: ["Client"]);
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

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
        var guid = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: guid.ToString());
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.UserIdGuid].Should().Be(guid);
    }

    [Fact]
    public async Task Invoke_WithNonGuidObjectId_DoesNotSetUserIdGuid()
    {
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: "user-123");
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.UserId].Should().Be("user-123");
        context.Items.Should().NotContainKey("UserIdGuid");
    }

    // =========================================================================
    // tid (tenant id) claim extraction
    // =========================================================================

    [Fact]
    public async Task Invoke_WithValidTidClaim_SetsTenantIdInContext()
    {
        var tenantId = Guid.Parse("496527a2-41f8-4297-a979-c916e7255a22");
        var context = new TestFunctionContext();
        var claims = new List<Claim>
        {
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", Guid.NewGuid().ToString()),
            new("tid", tenantId.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.TenantId].Should().Be(tenantId);
    }

    [Fact]
    public async Task Invoke_WithNonGuidTidClaim_DoesNotSetTenantId()
    {
        var context = new TestFunctionContext();
        var claims = new List<Claim>
        {
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", Guid.NewGuid().ToString()),
            new("tid", "not-a-guid")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items.Should().NotContainKey("TenantId");
    }

    [Fact]
    public async Task Invoke_WithMissingTidClaim_DoesNotSetTenantId()
    {
        var context = new TestFunctionContext();
        var principal = CreatePrincipal(objectId: Guid.NewGuid().ToString());
        context.Items[FunctionContextKeys.ClaimsPrincipal] = principal;

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items.Should().NotContainKey("TenantId");
    }

    // =========================================================================
    // Validator-based Bearer token path
    // =========================================================================

    [Fact]
    public async Task Invoke_WithBearerToken_InvokesValidatorAndPopulatesContext()
    {
        var userId = Guid.NewGuid().ToString();
        var principal = CreatePrincipal(objectId: userId);
        validatorMock
            .Setup(v => v.ValidateAsync("opaque-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        var context = CreateContextWithAuthorizationHeader("Bearer opaque-token");

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.IsAuthenticated].Should().Be(true);
        context.Items[FunctionContextKeys.UserId].Should().Be(userId);
        validatorMock.Verify(v => v.ValidateAsync("opaque-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Invoke_WithBearerToken_ValidatorReturnsNull_IsUnauthenticated()
    {
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClaimsPrincipal?)null);

        var context = CreateContextWithAuthorizationHeader("Bearer forged-or-expired");

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.IsAuthenticated].Should().Be(false);
        context.Items.Should().NotContainKey("UserId");
    }

    [Fact]
    public async Task Invoke_WithoutBearerPrefix_DoesNotInvokeValidator()
    {
        var context = CreateContextWithAuthorizationHeader("Basic abc123");

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.IsAuthenticated].Should().Be(false);
        validatorMock.Verify(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_WithNoHttpRequestData_DoesNotInvokeValidator()
    {
        var context = new TestFunctionContext();

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.IsAuthenticated].Should().Be(false);
        validatorMock.Verify(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_ItemsClaimsPrincipal_TakesPriorityOverAuthorizationHeader()
    {
        var itemsUserId = Guid.NewGuid().ToString();
        var context = CreateContextWithAuthorizationHeader("Bearer should-not-be-used");
        context.Items[FunctionContextKeys.ClaimsPrincipal] = CreatePrincipal(objectId: itemsUserId);

        await middleware.Invoke(context, _ => Task.CompletedTask);

        context.Items[FunctionContextKeys.UserId].Should().Be(itemsUserId);
        validatorMock.Verify(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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

        if (email is not null) claims.Add(new Claim("emails", email));
        if (name is not null) claims.Add(new Claim("name", name));
        if (roles is not null)
        {
            foreach (var role in roles)
                claims.Add(new Claim("roles", role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    /// <summary>
    /// Creates a <see cref="TestFunctionContext"/> with an HTTP request containing
    /// an Authorization header. Used to drive the validator path.
    /// </summary>
    private static TestFunctionContext CreateContextWithAuthorizationHeader(string authorizationHeader)
    {
        var context = new TestFunctionContext();
        var headers = new HttpHeadersCollection { { "Authorization", authorizationHeader } };
        context.SetHttpRequestHeaders(headers);
        return context;
    }
}
