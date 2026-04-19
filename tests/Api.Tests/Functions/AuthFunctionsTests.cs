using System.Net;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Functions;
using RajFinancial.Api.Services.UserProfiles;
using RajFinancial.Api.Tests.Middleware;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Tests.Functions;

/// <summary>
/// TDD security-first unit tests for <see cref="AuthFunctions"/>.
/// Tests the <c>/api/auth/me</c> and <c>/api/auth/roles</c> endpoints.
/// </summary>
/// <remarks>
///     <para>
///         These tests follow AGENT.md step 3 (security unit tests) and are
///         written before the implementation (red-green-refactor TDD).
///     </para>
///     <para>
///         Covers OWASP A01:2025 (Broken Access Control) and
///         A07:2025 (Authentication Failures).
///     </para>
/// </remarks>
public class AuthFunctionsTests
{
    private readonly Mock<ILogger<AuthFunctions>> loggerMock;
    private readonly Mock<IUserProfileService> userProfileServiceMock;

    private static readonly Guid TestUserId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid TestTenantId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");

    public AuthFunctionsTests()
    {
        loggerMock = new Mock<ILogger<AuthFunctions>>();
        userProfileServiceMock = new Mock<IUserProfileService>();
    }

    private AuthFunctions CreateFunctions()
        => new(loggerMock.Object, userProfileServiceMock.Object);

    // =========================================================================
    // Test Infrastructure
    // =========================================================================

    /// <summary>
    /// Creates a fully-authenticated request context with all claims data
    /// required by auth endpoints (userId, email, displayName, roles).
    /// </summary>
    private static (HttpRequestData Request, TestFunctionContext Context) CreateAuthenticatedRequest(
        Guid userId,
        string email = "user@rajfinancial.com",
        string displayName = "Test User",
        IReadOnlyList<string>? roles = null,
        string route = "auth/me")
    {
        roles ??= new List<string> { "Client" };

        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = userId.ToString();
        context.Items["UserIdGuid"] = userId;
        context.Items["UserEmail"] = email;
        context.Items["UserName"] = displayName;
        context.Items["UserRoles"] = roles;

        var mockRequest = new Mock<HttpRequestData>(context);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri($"https://localhost/api/{route}"));
        mockRequest.SetupGet(r => r.Headers)
            .Returns(new HttpHeadersCollection());
        mockRequest.Setup(r => r.CreateResponse())
            .Returns(() =>
            {
                var mockResponse = new Mock<HttpResponseData>(context);
                mockResponse.SetupProperty(r => r.StatusCode);
                mockResponse.SetupProperty(r => r.Body, new MemoryStream());
                mockResponse.SetupGet(r => r.Headers)
                    .Returns(new HttpHeadersCollection());
                return mockResponse.Object;
            });

        return (mockRequest.Object, context);
    }

    /// <summary>
    /// Creates an unauthenticated request context (no claims data).
    /// </summary>
    private static (HttpRequestData Request, TestFunctionContext Context) CreateUnauthenticatedRequest(
        string route = "auth/me")
    {
        var context = new TestFunctionContext();
        // No IsAuthenticated, no UserId, no UserIdGuid

        var mockRequest = new Mock<HttpRequestData>(context);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri($"https://localhost/api/{route}"));
        mockRequest.SetupGet(r => r.Headers)
            .Returns(new HttpHeadersCollection());
        mockRequest.Setup(r => r.CreateResponse())
            .Returns(() =>
            {
                var mockResponse = new Mock<HttpResponseData>(context);
                mockResponse.SetupProperty(r => r.StatusCode);
                mockResponse.SetupProperty(r => r.Body, new MemoryStream());
                mockResponse.SetupGet(r => r.Headers)
                    .Returns(new HttpHeadersCollection());
                return mockResponse.Object;
            });

        return (mockRequest.Object, context);
    }

    private static async Task<string> ReadResponseBody(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static UserProfile CreateTestProfile(
        Guid? userId = null,
        string email = "user@rajfinancial.com",
        string displayName = "Test User",
        UserRole role = UserRole.Client,
        bool isProfileComplete = false,
        bool isActive = true)
    {
        return new UserProfile
        {
            Id = userId ?? TestUserId,
            Email = email,
            DisplayName = displayName,
            Role = role,
            TenantId = TestTenantId,
            IsProfileComplete = isProfileComplete,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // GET /api/auth/me — Security: 401 Unauthenticated (priority)
    // =========================================================================

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateUnauthenticatedRequest("auth/me");

        // Act
        var response = await functions.GetMe(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_Unauthenticated_ReturnsAuthRequiredCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateUnauthenticatedRequest("auth/me");

        // Act
        var response = await functions.GetMe(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("AUTH_REQUIRED");
    }

    [Fact]
    public async Task GetMe_NoUserIdGuid_Returns401()
    {
        // Arrange
        var functions = CreateFunctions();
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = "not-a-guid";
        // No UserIdGuid — malformed auth context

        var mockRequest = new Mock<HttpRequestData>(context);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri("https://localhost/api/auth/me"));
        mockRequest.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
        mockRequest.Setup(r => r.CreateResponse())
            .Returns(() =>
            {
                var mockResponse = new Mock<HttpResponseData>(context);
                mockResponse.SetupProperty(r => r.StatusCode);
                mockResponse.SetupProperty(r => r.Body, new MemoryStream());
                mockResponse.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
                return mockResponse.Object;
            });

        // Act
        var response = await functions.GetMe(mockRequest.Object, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // GET /api/auth/me — Success: 200 with UserProfileResponse fields
    // =========================================================================

    [Fact]
    public async Task GetMe_AuthenticatedUser_Returns200()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "advisor@rajfinancial.com",
            displayName: "Jane Advisor",
            roles: ["Advisor"]);

        SetupEnsureProfileExists("advisor@rajfinancial.com", "Jane Advisor", ["Advisor"],
            CreateTestProfile(TestUserId, "advisor@rajfinancial.com", "Jane Advisor", UserRole.Advisor));

        // Act
        var response = await functions.GetMe(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMe_AuthenticatedUser_ReturnsUserId()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(TestUserId);

        SetupEnsureProfileExists("user@rajfinancial.com", "Test User", ["Client"],
            CreateTestProfile());

        // Act
        var response = await functions.GetMe(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain(TestUserId.ToString());
    }

    [Fact]
    public async Task GetMe_AuthenticatedUser_DoesNotLeakEmail()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "advisor@rajfinancial.com",
            roles: ["Advisor"]);

        SetupEnsureProfileExists("advisor@rajfinancial.com", "Test User", ["Advisor"],
            CreateTestProfile(email: "advisor@rajfinancial.com", role: UserRole.Advisor));

        // Act
        var response = await functions.GetMe(request, context);

        // Assert — email is NOT in the profile response (comes from Entra claims instead)
        var body = await ReadResponseBody(response);
        body.Should().NotContain("advisor@rajfinancial.com");
        body.Should().Contain(TestUserId.ToString());
    }

    [Fact]
    public async Task GetMe_AuthenticatedUser_ReturnsDisplayName()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            displayName: "Jane Advisor");

        SetupEnsureProfileExists("user@rajfinancial.com", "Jane Advisor", ["Client"],
            CreateTestProfile(displayName: "Jane Advisor"));

        // Act
        var response = await functions.GetMe(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("Jane Advisor");
    }

    [Fact]
    public async Task GetMe_AuthenticatedUser_ReturnsLocaleDefaults()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(TestUserId);

        SetupEnsureProfileExists("user@rajfinancial.com", "Test User", ["Client"],
            CreateTestProfile());

        // Act
        var response = await functions.GetMe(request, context);

        // Assert — defaults when no PreferencesJson set
        var body = await ReadResponseBody(response);
        body.Should().Contain("\"locale\":\"en-US\"");
        body.Should().Contain("\"timezone\":\"America/New_York\"");
        body.Should().Contain("\"currency\":\"USD\"");
    }

    [Fact]
    public async Task GetMe_AuthenticatedUser_ReturnsCreatedAt()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(TestUserId);

        SetupEnsureProfileExists("user@rajfinancial.com", "Test User", ["Client"],
            CreateTestProfile());

        // Act
        var response = await functions.GetMe(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("\"createdAt\":");
    }

    // =========================================================================
    // GET /api/auth/me — JIT Provisioning (BDD: First-time user)
    // =========================================================================

    [Fact]
    public async Task GetMe_FirstTimeUser_CallsEnsureProfileExists()
    {
        // Arrange
        var newUserId = Guid.Parse("660e8400-e29b-41d4-a716-446655440001");
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            newUserId,
            email: "newuser@rajfinancial.com",
            displayName: "New User",
            roles: ["Client"]);

        SetupEnsureProfileExists("newuser@rajfinancial.com", "New User", ["Client"],
            CreateTestProfile(newUserId, "newuser@rajfinancial.com", "New User", UserRole.Client));

        // Act
        await functions.GetMe(request, context);

        // Assert
        userProfileServiceMock.Verify(
            s => s.EnsureProfileExistsAsync(
                newUserId,
                "newuser@rajfinancial.com",
                "New User",
                It.Is<IReadOnlyList<string>>(r => r.Contains("Client")),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMe_FirstTimeUser_Returns200WithNewProfile()
    {
        // Arrange
        var newUserId = Guid.Parse("660e8400-e29b-41d4-a716-446655440001");
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            newUserId,
            email: "newuser@rajfinancial.com",
            displayName: "New User",
            roles: ["Client"]);

        SetupEnsureProfileExists("newuser@rajfinancial.com", "New User", ["Client"],
            CreateTestProfile(newUserId, "newuser@rajfinancial.com", "New User", UserRole.Client));

        // Act
        var response = await functions.GetMe(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadResponseBody(response);
        body.Should().Contain(newUserId.ToString());
    }

    // =========================================================================
    // GET /api/auth/me — Claim Syncing (BDD: Returning user)
    // =========================================================================

    [Fact]
    public async Task GetMe_ReturningUser_PassesCurrentClaimsToEnsureProfile()
    {
        // Arrange — user's Entra email changed since last login
        var returningUserId = Guid.Parse("770e8400-e29b-41d4-a716-446655440002");
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            returningUserId,
            email: "returning@rajfinancial.com",
            displayName: "Returning User",
            roles: ["Advisor"]);

        // EnsureProfileExistsAsync handles syncing — it receives current claims
        SetupEnsureProfileExists("returning@rajfinancial.com", "Returning User", ["Advisor"],
            CreateTestProfile(returningUserId, "returning@rajfinancial.com", "Returning User",
                UserRole.Advisor));

        // Act
        await functions.GetMe(request, context);

        // Assert — verify the CURRENT claims (not stale ones) are passed
        userProfileServiceMock.Verify(
            s => s.EnsureProfileExistsAsync(
                returningUserId,
                "returning@rajfinancial.com",
                "Returning User",
                It.Is<IReadOnlyList<string>>(r => r.Contains("Advisor")),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // GET /api/auth/me — Multiple Roles (BDD: Highest-priority role mapped)
    // =========================================================================

    [Fact]
    public async Task GetMe_MultipleRolesIncludingAdmin_ReturnsProfile()
    {
        // Arrange — BDD: User with "Client,Administrator" => role = "Administrator"
        var multiRoleUserId = Guid.Parse("990e8400-e29b-41d4-a716-446655440004");
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            multiRoleUserId,
            roles: ["Client", "Administrator"]);

        // EnsureProfileExistsAsync maps highest-priority role internally
        SetupEnsureProfileExists("user@rajfinancial.com", "Test User", ["Client", "Administrator"],
            CreateTestProfile(multiRoleUserId, role: UserRole.Administrator));

        // Act
        var response = await functions.GetMe(request, context);

        // Assert — role is no longer in UserProfileResponse (available via /api/auth/roles),
        // verify profile response contains userId and display name
        var body = await ReadResponseBody(response);
        body.Should().Contain(multiRoleUserId.ToString());
        body.Should().Contain("Test User");
    }

    // =========================================================================
    // GET /api/auth/roles — Security: 401 Unauthenticated (priority)
    // =========================================================================

    [Fact]
    public async Task GetRoles_Unauthenticated_Returns401()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateUnauthenticatedRequest("auth/roles");

        // Act
        var response = await functions.GetRoles(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRoles_Unauthenticated_ReturnsAuthRequiredCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateUnauthenticatedRequest("auth/roles");

        // Act
        var response = await functions.GetRoles(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("AUTH_REQUIRED");
    }

    // =========================================================================
    // GET /api/auth/roles — Success: 200 with UserRolesResponse
    // =========================================================================

    [Fact]
    public async Task GetRoles_AuthenticatedAdvisor_Returns200()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: "auth/roles");

        // Act
        var response = await functions.GetRoles(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRoles_AuthenticatedAdvisor_ReturnsAdvisorRole()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: "auth/roles");

        // Act
        var response = await functions.GetRoles(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("Advisor");
    }

    [Fact]
    public async Task GetRoles_AuthenticatedAdvisor_ReturnsIsAdministratorFalse()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: "auth/roles");

        // Act
        var response = await functions.GetRoles(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("\"isAdministrator\":false");
    }

    // =========================================================================
    // GET /api/auth/roles — Administrator
    // =========================================================================

    [Fact]
    public async Task GetRoles_Administrator_ReturnsAdministratorRole()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Administrator"],
            route: "auth/roles");

        // Act
        var response = await functions.GetRoles(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("Administrator");
    }

    [Fact]
    public async Task GetRoles_Administrator_ReturnsIsAdministratorTrue()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Administrator"],
            route: "auth/roles");

        // Act
        var response = await functions.GetRoles(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("\"isAdministrator\":true");
    }

    // =========================================================================
    // GET /api/auth/roles — Multiple Roles
    // =========================================================================

    [Fact]
    public async Task GetRoles_MultipleRoles_ReturnsAllRoles()
    {
        // Arrange — BDD: "Client,Advisor" => both should appear
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Client", "Advisor"],
            route: "auth/roles");

        // Act
        var response = await functions.GetRoles(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("Client");
        body.Should().Contain("Advisor");
    }

    [Fact]
    public async Task GetRoles_MultipleRolesWithoutAdmin_ReturnsIsAdministratorFalse()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Client", "Advisor"],
            route: "auth/roles");

        // Act
        var response = await functions.GetRoles(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("\"isAdministrator\":false");
    }

    // =========================================================================
    // GET /api/auth/roles — No UserIdGuid (malformed auth)
    // =========================================================================

    [Fact]
    public async Task GetRoles_NoUserIdGuid_Returns401()
    {
        // Arrange
        var functions = CreateFunctions();
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = "not-a-guid";
        // No UserIdGuid, no UserRoles

        var mockRequest = new Mock<HttpRequestData>(context);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri("https://localhost/api/auth/roles"));
        mockRequest.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
        mockRequest.Setup(r => r.CreateResponse())
            .Returns(() =>
            {
                var mockResponse = new Mock<HttpResponseData>(context);
                mockResponse.SetupProperty(r => r.StatusCode);
                mockResponse.SetupProperty(r => r.Body, new MemoryStream());
                mockResponse.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
                return mockResponse.Object;
            });

        // Act
        var response = await functions.GetRoles(mockRequest.Object, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Service interaction — EnsureProfileExistsAsync
    // =========================================================================

    [Fact]
    public async Task GetMe_CallsEnsureProfileExistsWithCorrectParameters()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "advisor@rajfinancial.com",
            displayName: "Jane Advisor",
            roles: ["Advisor"]);

        SetupEnsureProfileExists("advisor@rajfinancial.com", "Jane Advisor", ["Advisor"],
            CreateTestProfile(TestUserId, "advisor@rajfinancial.com", "Jane Advisor",
                UserRole.Advisor));

        // Act
        await functions.GetMe(request, context);

        // Assert
        userProfileServiceMock.Verify(
            s => s.EnsureProfileExistsAsync(
                TestUserId,
                "advisor@rajfinancial.com",
                "Jane Advisor",
                It.Is<IReadOnlyList<string>>(
                    r => r.Count == 1 && r[0] == "Advisor"),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRoles_DoesNotCallEnsureProfileExists()
    {
        // Arrange — /api/auth/roles only reads claims, no JIT provisioning
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: "auth/roles");

        // Act
        await functions.GetRoles(request, context);

        // Assert
        userProfileServiceMock.Verify(
            s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Configures <see cref="IUserProfileService.EnsureProfileExistsAsync"/> to
    /// accept any matching call and return the specified profile.
    /// </summary>
    private void SetupEnsureProfileExists(
        string email,
        string displayName,
        IReadOnlyList<string> roles,
        UserProfile returnProfile)
    {
        userProfileServiceMock
            .Setup(s => s.EnsureProfileExistsAsync(
                It.IsAny<Guid>(),
                email,
                displayName,
                It.Is<IReadOnlyList<string>>(r => r.SequenceEqual(roles)),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnProfile);
    }
}
