using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Middleware.Authorization;
using RajFinancial.Api.Middleware.Exception;

namespace RajFinancial.Api.Tests.Middleware;

/// <summary>
/// Unit tests for <see cref="AuthorizationMiddleware"/>.
/// Tests OWASP A01:2025 (Broken Access Control) compliance by verifying
/// attribute-based authorization enforcement.
/// </summary>
public class AuthorizationMiddlewareTests
{
    private readonly AuthorizationMiddleware middleware;
    private readonly Mock<ILogger<AuthorizationMiddleware>> loggerMock;

    public AuthorizationMiddlewareTests()
    {
        loggerMock = new Mock<ILogger<AuthorizationMiddleware>>();
        loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        middleware = new AuthorizationMiddleware(loggerMock.Object);
    }

    // =========================================================================
    // Public endpoints (no attributes)
    // =========================================================================

    [Fact]
    public async Task Invoke_NoAttributes_CallsNextWithoutThrowing()
    {
        // Arrange
        var context = CreateContext(EntryPoint<PublicFunctions>(nameof(PublicFunctions.PublicEndpoint)));
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_NullFunctionDefinition_CallsNextWithoutThrowing()
    {
        // Arrange — non-HTTP triggers or missing definition
        var context = new TestFunctionContext();
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_InvalidEntryPoint_TreatedAsPublic_CallsNext()
    {
        // Arrange — malformed entry point that ResolveMethod cannot parse
        var context = CreateContext("NoDotsHere");
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert — invalid entry point should be treated as public (no auth check)
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_NonExistentType_TreatedAsPublic_CallsNext()
    {
        // Arrange — valid format but type doesn't exist in any loaded assembly
        var context = CreateContext("Completely.Fake.Namespace.ClassName.MethodName");
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert — unresolvable type should be treated as public (no auth check)
        nextCalled.Should().BeTrue();
    }

    // =========================================================================
    // [RequireAuthentication] — method level
    // =========================================================================

    [Fact]
    public async Task Invoke_RequireAuthentication_Authenticated_CallsNext()
    {
        // Arrange
        var context = CreateContext(
            EntryPoint<AuthFunctions>(nameof(AuthFunctions.ProtectedEndpoint)),
            isAuthenticated: true);
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_RequireAuthentication_NotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var context = CreateContext(
            EntryPoint<AuthFunctions>(nameof(AuthFunctions.ProtectedEndpoint)),
            isAuthenticated: false);
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        var act = () => middleware.Invoke(context, Next);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // =========================================================================
    // [RequireAuthentication] — class level
    // =========================================================================

    [Fact]
    public async Task Invoke_RequireAuthentication_OnClass_Authenticated_CallsNext()
    {
        // Arrange
        var context = CreateContext(
            EntryPoint<ProtectedClass>(nameof(ProtectedClass.AnyEndpoint)),
            isAuthenticated: true);
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_RequireAuthentication_OnClass_NotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var context = CreateContext(
            EntryPoint<ProtectedClass>(nameof(ProtectedClass.AnyEndpoint)),
            isAuthenticated: false);
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        var act = () => middleware.Invoke(context, Next);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // =========================================================================
    // [RequireRole] — method level
    // =========================================================================

    [Fact]
    public async Task Invoke_RequireRole_UserHasRole_CallsNext()
    {
        // Arrange
        var context = CreateContext(
            EntryPoint<RoleFunctions>(nameof(RoleFunctions.AdminOnly)),
            isAuthenticated: true,
            roles: ["Administrator"]);
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_RequireRole_UserLacksRole_ThrowsForbiddenException()
    {
        // Arrange
        var context = CreateContext(
            EntryPoint<RoleFunctions>(nameof(RoleFunctions.AdminOnly)),
            isAuthenticated: true,
            roles: ["Client"]);
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        var act = () => middleware.Invoke(context, Next);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Access denied");
    }

    [Fact]
    public async Task Invoke_RequireRole_NotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange — auth is checked before role (401 before 403)
        var context = CreateContext(
            EntryPoint<RoleFunctions>(nameof(RoleFunctions.AdminOnly)),
            isAuthenticated: false);
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        var act = () => middleware.Invoke(context, Next);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // =========================================================================
    // [RequireRole] — multiple roles (OR logic)
    // =========================================================================

    [Fact]
    public async Task Invoke_RequireRole_MultipleRoles_UserHasOne_CallsNext()
    {
        // Arrange — OR logic: user has "Client", endpoint requires "Administrator" OR "Client"
        var context = CreateContext(
            EntryPoint<RoleFunctions>(nameof(RoleFunctions.MultiRoleEndpoint)),
            isAuthenticated: true,
            roles: ["Client"]);
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_RequireRole_MultipleRoles_UserHasNone_ThrowsForbiddenException()
    {
        // Arrange — user has "Professional", endpoint requires "Administrator" OR "Client"
        var context = CreateContext(
            EntryPoint<RoleFunctions>(nameof(RoleFunctions.MultiRoleEndpoint)),
            isAuthenticated: true,
            roles: ["Professional"]);
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        var act = () => middleware.Invoke(context, Next);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Access denied");
    }

    // =========================================================================
    // [RequireRole] — class level
    // =========================================================================

    [Fact]
    public async Task Invoke_RequireRole_OnClass_UserHasRole_CallsNext()
    {
        // Arrange
        var context = CreateContext(
            EntryPoint<AdminClass>(nameof(AdminClass.AnyEndpoint)),
            isAuthenticated: true,
            roles: ["Administrator"]);
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_RequireRole_OnClass_UserLacksRole_ThrowsForbiddenException()
    {
        // Arrange
        var context = CreateContext(
            EntryPoint<AdminClass>(nameof(AdminClass.AnyEndpoint)),
            isAuthenticated: true,
            roles: ["Client"]);
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        var act = () => middleware.Invoke(context, Next);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Access denied");
    }

    // =========================================================================
    // Security: ForbiddenException must not leak required role names (OWASP A01)
    // =========================================================================

    [Fact]
    public async Task Invoke_RequireRole_ForbiddenMessage_DoesNotLeakRoleNames()
    {
        // Arrange — endpoint requires "Administrator", user has "Client"
        var context = CreateContext(
            EntryPoint<RoleFunctions>(nameof(RoleFunctions.AdminOnly)),
            isAuthenticated: true,
            roles: ["Client"]);
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        var act = () => middleware.Invoke(context, Next);

        // Assert — message must be generic; must never contain required role names
        var ex = (await act.Should().ThrowAsync<ForbiddenException>()).Which;
        ex.Message.Should().NotContainAny("Administrator", "Client", "role");
    }

    // =========================================================================
    // Case-insensitive role matching
    // =========================================================================

    [Fact]
    public async Task Invoke_RequireRole_CaseInsensitiveMatch_CallsNext()
    {
        // Arrange — role in token is "administrator" (lowercase), attribute requires "Administrator"
        var context = CreateContext(
            EntryPoint<RoleFunctions>(nameof(RoleFunctions.AdminOnly)),
            isAuthenticated: true,
            roles: ["administrator"]);
        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    // =========================================================================
    // Logging
    // =========================================================================

    [Fact]
    public async Task Invoke_RequireAuthentication_Authenticated_LogsDebug()
    {
        // Arrange
        var context = CreateContext(
            EntryPoint<AuthFunctions>(nameof(AuthFunctions.ProtectedEndpoint)),
            isAuthenticated: true);
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Authentication verified")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_RequireRole_Authorized_LogsDebug()
    {
        // Arrange
        var context = CreateContext(
            EntryPoint<RoleFunctions>(nameof(RoleFunctions.AdminOnly)),
            isAuthenticated: true,
            roles: ["Administrator"]);
        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Authorization passed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Builds a fully qualified entry point string for a method on a given type.
    /// Handles nested classes correctly (uses '+' separator).
    /// </summary>
    private static string EntryPoint<T>(string methodName) =>
        $"{typeof(T).FullName}.{methodName}";

    /// <summary>
    /// Creates a <see cref="TestFunctionContext"/> with a mocked
    /// <see cref="FunctionDefinition"/> and pre-populated authentication context.
    /// </summary>
    private static TestFunctionContext CreateContext(
        string entryPoint,
        bool isAuthenticated = false,
        IReadOnlyList<string>? roles = null)
    {
        var funcDef = new Mock<FunctionDefinition>();
        funcDef.Setup(f => f.EntryPoint).Returns(entryPoint);
        funcDef.Setup(f => f.Name).Returns("TestFunction");

        var context = new TestFunctionContext
        {
            FunctionDefinitionValue = funcDef.Object
        };

        context.Items["IsAuthenticated"] = isAuthenticated;

        if (isAuthenticated && roles is not null)
            context.Items["UserRoles"] = roles;

        return context;
    }

    // =========================================================================
    // Sample function classes for reflection-based tests
    // =========================================================================

    /// <summary>Public endpoint — no authorization attributes.</summary>
    private class PublicFunctions
    {
        public void PublicEndpoint() { }
    }

    /// <summary>Method-level [RequireAuthentication].</summary>
    private class AuthFunctions
    {
        [RequireAuthentication]
        public void ProtectedEndpoint() { }
    }

    /// <summary>Class-level [RequireAuthentication].</summary>
    [RequireAuthentication]
    private class ProtectedClass
    {
        public void AnyEndpoint() { }
    }

    /// <summary>Method-level [RequireRole] with single and multiple roles.</summary>
    private class RoleFunctions
    {
        [RequireRole("Administrator")]
        public void AdminOnly() { }

        [RequireRole("Administrator", "Client")]
        public void MultiRoleEndpoint() { }
    }

    /// <summary>Class-level [RequireRole].</summary>
    [RequireRole("Administrator")]
    private class AdminClass
    {
        public void AnyEndpoint() { }
    }
}
