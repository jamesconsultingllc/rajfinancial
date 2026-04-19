using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Services.UserProfiles;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Tests.Middleware;

/// <summary>
/// TDD unit tests for <see cref="UserProfileProvisioningMiddleware"/>.
/// Tests that JIT provisioning is called for authenticated users, skipped for
/// unauthenticated requests, and that correct claim data is passed through.
/// </summary>
public class UserProfileProvisioningMiddlewareTests
{
    private readonly Mock<ILogger<UserProfileProvisioningMiddleware>> loggerMock;
    private readonly Mock<IUserProfileService> userProfileServiceMock;

    private static readonly Guid TestUserId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");

    public UserProfileProvisioningMiddlewareTests()
    {
        loggerMock = new Mock<ILogger<UserProfileProvisioningMiddleware>>();
        loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        userProfileServiceMock = new Mock<IUserProfileService>();
    }

    private UserProfileProvisioningMiddleware CreateMiddleware()
        => new(loggerMock.Object);

    /// <summary>
    /// Creates a TestFunctionContext with mock InstanceServices that can resolve
    /// <see cref="IUserProfileService"/>.
    /// </summary>
    private TestFunctionContext CreateAuthenticatedContext(
        Guid userId,
        string? email = "user@rajfinancial.com",
        string? name = "John Doe",
        IReadOnlyList<string>? roles = null)
    {
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = userId.ToString();
        context.Items["UserIdGuid"] = userId;
        if (email != null) context.Items["UserEmail"] = email;
        if (name != null) context.Items["UserName"] = name;
        context.Items["UserRoles"] = roles ?? (IReadOnlyList<string>)["Client"];

        // Set up InstanceServices to resolve IUserProfileService
        var services = new ServiceCollection();
        services.AddSingleton(userProfileServiceMock.Object);
        context.InstanceServices = services.BuildServiceProvider();

        return context;
    }

    private TestFunctionContext CreateUnauthenticatedContext()
    {
        var context = new TestFunctionContext();
        // IsAuthenticated not set or false
        return context;
    }

    // =========================================================================
    // Authenticated requests — calls EnsureProfileExistsAsync
    // =========================================================================

    [Fact]
    public async Task Invoke_AuthenticatedUser_CallsEnsureProfileExists()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateAuthenticatedContext(TestUserId);
        userProfileServiceMock
            .Setup(s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Id = TestUserId });

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        userProfileServiceMock.Verify(
            s => s.EnsureProfileExistsAsync(
                TestUserId,
                "user@rajfinancial.com",
                "John Doe",
                It.Is<IReadOnlyList<string>>(r => r.Contains("Client")),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_AuthenticatedUser_PassesCorrectEmail()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateAuthenticatedContext(TestUserId, email: "custom@example.com");
        userProfileServiceMock
            .Setup(s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Id = TestUserId });

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        userProfileServiceMock.Verify(
            s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(),
                "custom@example.com",
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_AuthenticatedUser_PassesCorrectRoles()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateAuthenticatedContext(
            TestUserId, roles: ["Administrator", "Advisor"]);
        userProfileServiceMock
            .Setup(s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Id = TestUserId });

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        userProfileServiceMock.Verify(
            s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.Is<IReadOnlyList<string>>(
                    r => r.Contains("Administrator") && r.Contains("Advisor")),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_AuthenticatedUser_AlwaysCallsNext()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateAuthenticatedContext(TestUserId);
        userProfileServiceMock
            .Setup(s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Id = TestUserId });

        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    // =========================================================================
    // Unauthenticated requests — skips provisioning
    // =========================================================================

    [Fact]
    public async Task Invoke_UnauthenticatedRequest_SkipsProvisioning()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateUnauthenticatedContext();

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        userProfileServiceMock.Verify(
            s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Invoke_UnauthenticatedRequest_AlwaysCallsNext()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateUnauthenticatedContext();

        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert
        nextCalled.Should().BeTrue();
    }

    // =========================================================================
    // Error handling — provisioning failure does not block request
    // =========================================================================

    [Fact]
    public async Task Invoke_ProvisioningThrowsException_StillCallsNext()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateAuthenticatedContext(TestUserId);
        userProfileServiceMock
            .Setup(s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        var nextCalled = false;
        Task Next(FunctionContext _) { nextCalled = true; return Task.CompletedTask; }

        // Act
        await middleware.Invoke(context, Next);

        // Assert — middleware should swallow the error and proceed
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_ProvisioningThrowsException_LogsWarning()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateAuthenticatedContext(TestUserId);
        userProfileServiceMock
            .Setup(s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert — should log a warning
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    // =========================================================================
    // Edge cases
    // =========================================================================

    [Fact]
    public async Task Invoke_AuthenticatedWithNoEmail_PassesEmptyEmail()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateAuthenticatedContext(TestUserId, email: null);
        userProfileServiceMock
            .Setup(s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Id = TestUserId });

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert — empty string is passed when email claim is missing
        userProfileServiceMock.Verify(
            s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(),
                string.Empty,
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_AuthenticatedWithNoUserIdGuid_SkipsProvisioning()
    {
        // Arrange — UserId set as string but UserIdGuid is missing/invalid
        var middleware = CreateMiddleware();
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = "not-a-guid";
        // No UserIdGuid set

        Task Next(FunctionContext _) => Task.CompletedTask;

        // Act
        await middleware.Invoke(context, Next);

        // Assert — cannot provision without a valid Guid
        userProfileServiceMock.Verify(
            s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
