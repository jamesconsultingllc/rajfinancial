// ============================================================================
// RAJ Financial — Client Management Functions Tests
// ============================================================================
// TDD security-first unit tests for ClientManagementFunctions.
// Tests POST /api/auth/clients, GET /api/auth/clients, and
// DELETE /api/auth/clients/{id} endpoints.
//
// Aligned with BDD scenarios in:
//   tests/AcceptanceTests/Features/ClientManagement.feature (18 scenarios)
// ============================================================================

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using FluentValidation;
using RajFinancial.Api.Functions;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Services.ClientManagement;
using RajFinancial.Api.Tests.Middleware;
using RajFinancial.Api.Validators;
using RajFinancial.Shared.Contracts.Auth;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Tests.Functions;

/// <summary>
/// TDD security-first unit tests for <see cref="ClientManagementFunctions"/>.
/// Tests the <c>/api/auth/clients</c> endpoints (POST, GET, DELETE).
/// </summary>
/// <remarks>
///     <para>
///         These tests follow AGENT.md step 3 (security unit tests) and are
///         written before the implementation (red-green-refactor TDD).
///     </para>
///     <para>
///         Covers OWASP A01:2025 (Broken Access Control) —
///         role-based access, ownership checks, and soft-delete.
///     </para>
/// </remarks>
public class ClientManagementFunctionsTests
{
    private readonly Mock<ILogger<ClientManagementFunctions>> loggerMock;
    private readonly Mock<IClientManagementService> clientManagementServiceMock;

    private static readonly Guid TestUserId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid TestGrantId = Guid.Parse("cccc0000-0000-0000-0000-000000000001");
    private static readonly Guid OtherUserId = Guid.Parse("dddd0000-0000-0000-0000-000000000002");
    private static readonly Guid AdminUserId = Guid.Parse("eeee0000-0000-0000-0000-000000000003");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ClientManagementFunctionsTests()
    {
        loggerMock = new Mock<ILogger<ClientManagementFunctions>>();
        clientManagementServiceMock = new Mock<IClientManagementService>();
    }

    private ClientManagementFunctions CreateFunctions()
        => new(loggerMock.Object, clientManagementServiceMock.Object);

    // =========================================================================
    // Test Infrastructure
    // =========================================================================

    /// <summary>
    /// Creates a fully-authenticated request context with role claims
    /// required by client management endpoints (userId, email, displayName, roles).
    /// </summary>
    private static (HttpRequestData Request, TestFunctionContext Context) CreateAuthenticatedRequest(
        Guid userId,
        string email = "advisor@rajfinancial.com",
        string displayName = "Test Advisor",
        IReadOnlyList<string>? roles = null,
        string route = "auth/clients",
        string method = "get",
        string? requestBody = null)
    {
        roles ??= new List<string> { "Advisor" };

        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = userId.ToString();
        context.Items["UserIdGuid"] = userId;
        context.Items["UserEmail"] = email;
        context.Items["UserName"] = displayName;
        context.Items["UserRoles"] = roles;

        // Store body and configure InstanceServices for GetValidatedBodyAsync
        if (requestBody is not null)
        {
            context.Items["RequestBody"] = requestBody;
            context.WithServices(configure =>
            {
                configure.AddScoped<IValidator<AssignClientRequest>, AssignClientRequestValidator>();
            });
        }

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

        // Setup Body stream for POST requests
        if (requestBody is not null)
        {
            var bodyStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBody));
            mockRequest.SetupGet(r => r.Body).Returns(bodyStream);
        }

        return (mockRequest.Object, context);
    }

    /// <summary>
    /// Creates an unauthenticated request context (no claims data).
    /// </summary>
    private static (HttpRequestData Request, TestFunctionContext Context) CreateUnauthenticatedRequest(
        string route = "auth/clients")
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

    /// <summary>
    /// Reads the response body as a string, resetting the stream position.
    /// </summary>
    private static async Task<string> ReadResponseBody(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Creates a test <see cref="DataAccessGrant"/> entity with configurable defaults.
    /// </summary>
    private static DataAccessGrant CreateTestGrant(
        Guid? grantId = null,
        Guid? grantorUserId = null,
        string granteeEmail = "client@example.com",
        AccessType accessType = AccessType.Read,
        List<string>? categories = null,
        string? relationshipLabel = "Primary Advisor",
        GrantStatus status = GrantStatus.Pending,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? revokedAt = null)
    {
        return new DataAccessGrant
        {
            Id = grantId ?? TestGrantId,
            GrantorUserId = grantorUserId ?? TestUserId,
            GranteeEmail = granteeEmail,
            AccessType = accessType,
            Categories = categories ?? ["accounts", "investments"],
            RelationshipLabel = relationshipLabel,
            Status = status,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            RevokedAt = revokedAt
        };
    }

    /// <summary>
    /// Serializes an <see cref="AssignClientRequest"/> to JSON string.
    /// </summary>
    private static string SerializeRequest(AssignClientRequest request)
        => JsonSerializer.Serialize(request, JsonOptions);

    /// <summary>
    /// Creates a valid <see cref="AssignClientRequest"/> with sensible defaults.
    /// </summary>
    private static AssignClientRequest CreateValidAssignRequest(
        string clientEmail = "client@example.com",
        string accessType = "Read",
        string[]? categories = null,
        string? relationshipLabel = "Primary Advisor")
    {
        return new AssignClientRequest
        {
            ClientEmail = clientEmail,
            AccessType = accessType,
            Categories = categories ?? ["accounts", "investments"],
            RelationshipLabel = relationshipLabel
        };
    }

    // =========================================================================
    // POST /api/auth/clients — Security: 401 Unauthenticated
    // =========================================================================

    [Fact]
    public async Task AssignClient_Unauthenticated_Returns401()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateUnauthenticatedRequest("auth/clients");

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignClient_Unauthenticated_ReturnsAuthRequiredCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateUnauthenticatedRequest("auth/clients");

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("AUTH_REQUIRED");
    }

    // =========================================================================
    // POST /api/auth/clients — Security: 403 Client role forbidden
    // =========================================================================

    [Fact]
    public async Task AssignClient_ClientRole_Returns403()
    {
        // Arrange — BDD: Client role should be forbidden from assigning
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "client@rajfinancial.com",
            roles: ["Client"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignClient_ClientRole_ReturnsAuthForbiddenCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "client@rajfinancial.com",
            roles: ["Client"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("AUTH_FORBIDDEN");
    }

    // =========================================================================
    // POST /api/auth/clients — Self-assignment prevention
    // =========================================================================

    [Fact]
    public async Task AssignClient_SelfAssignment_Returns400()
    {
        // Arrange — BDD: Advisor cannot assign themselves
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest(clientEmail: "advisor@rajfinancial.com");
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "advisor@rajfinancial.com",
            roles: ["Advisor"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignClient_SelfAssignment_ReturnsSelfAssignmentCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest(clientEmail: "advisor@rajfinancial.com");
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "advisor@rajfinancial.com",
            roles: ["Advisor"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("SELF_ASSIGNMENT_NOT_ALLOWED");
    }

    [Fact]
    public async Task AssignClient_SelfAssignmentCaseInsensitive_Returns400()
    {
        // Arrange — email comparison should be case-insensitive
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest(clientEmail: "ADVISOR@RajFinancial.com");
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "advisor@rajfinancial.com",
            roles: ["Advisor"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // POST /api/auth/clients — Success: Advisor assigns client (201)
    // =========================================================================

    [Fact]
    public async Task AssignClient_ValidRequest_Returns201()
    {
        // Arrange — BDD: Advisor assigns a client with valid data
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        var grant = CreateTestGrant();
        clientManagementServiceMock
            .Setup(s => s.AssignClientAsync(
                TestUserId,
                It.IsAny<AssignClientRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AssignClient_ValidRequest_ReturnsGrantId()
    {
        // Arrange
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        var grant = CreateTestGrant();
        clientManagementServiceMock
            .Setup(s => s.AssignClientAsync(
                TestUserId,
                It.IsAny<AssignClientRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain(TestGrantId.ToString());
    }

    [Fact]
    public async Task AssignClient_ValidRequest_ReturnsClientEmail()
    {
        // Arrange
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        var grant = CreateTestGrant();
        clientManagementServiceMock
            .Setup(s => s.AssignClientAsync(
                TestUserId,
                It.IsAny<AssignClientRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("client@example.com");
    }

    [Fact]
    public async Task AssignClient_ValidRequest_ReturnsPendingStatus()
    {
        // Arrange — new assignments always start as Pending
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        var grant = CreateTestGrant(status: GrantStatus.Pending);
        clientManagementServiceMock
            .Setup(s => s.AssignClientAsync(
                TestUserId,
                It.IsAny<AssignClientRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("\"status\":\"Pending\"");
    }

    [Fact]
    public async Task AssignClient_ValidRequest_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest(
            clientEmail: "newclient@example.com",
            accessType: "Full",
            categories: ["accounts", "documents"],
            relationshipLabel: "Estate Attorney");
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        var grant = CreateTestGrant(granteeEmail: "newclient@example.com", accessType: AccessType.Full);
        clientManagementServiceMock
            .Setup(s => s.AssignClientAsync(
                TestUserId,
                It.IsAny<AssignClientRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        // Act
        await functions.AssignClient(request, context);

        // Assert
        clientManagementServiceMock.Verify(
            s => s.AssignClientAsync(
                TestUserId,
                It.Is<AssignClientRequest>(r =>
                    r.ClientEmail == "newclient@example.com" &&
                    r.AccessType == "Full" &&
                    r.Categories.Contains("accounts") &&
                    r.Categories.Contains("documents") &&
                    r.RelationshipLabel == "Estate Attorney"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // POST /api/auth/clients — Admin can assign (201)
    // =========================================================================

    [Fact]
    public async Task AssignClient_AdminRole_Returns201()
    {
        // Arrange — BDD: Administrator can also assign clients
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest();
        var (request, context) = CreateAuthenticatedRequest(
            AdminUserId,
            email: "admin@rajfinancial.com",
            roles: ["Administrator"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        var grant = CreateTestGrant(grantorUserId: AdminUserId);
        clientManagementServiceMock
            .Setup(s => s.AssignClientAsync(
                AdminUserId,
                It.IsAny<AssignClientRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        // Act
        var response = await functions.AssignClient(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // =========================================================================
    // POST /api/auth/clients — Duplicate grant: same client email twice (201)
    // =========================================================================

    /// <summary>
    /// Verifies that assigning the same client email twice to the same advisor
    /// results in two separate grants (no duplicate prevention at the service level).
    /// </summary>
    [Fact]
    public async Task AssignClient_DuplicateClientEmail_Returns201BothTimes()
    {
        // Arrange — BDD: Same advisor assigns same client email a second time
        var functions = CreateFunctions();
        var assignRequest = CreateValidAssignRequest(clientEmail: "duplicate@example.com");
        var (request1, context1) = CreateAuthenticatedRequest(
            TestUserId,
            email: "advisor@rajfinancial.com",
            roles: ["Advisor"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        var grant1 = CreateTestGrant(
            grantId: Guid.Parse("cccc0000-0000-0000-0000-000000000010"),
            granteeEmail: "duplicate@example.com");
        var grant2 = CreateTestGrant(
            grantId: Guid.Parse("cccc0000-0000-0000-0000-000000000011"),
            granteeEmail: "duplicate@example.com");

        clientManagementServiceMock
            .SetupSequence(s => s.AssignClientAsync(
                TestUserId,
                It.IsAny<AssignClientRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant1)
            .ReturnsAsync(grant2);

        // Act — first assignment
        var response1 = await functions.AssignClient(request1, context1);

        // Second request (fresh context)
        var (request2, context2) = CreateAuthenticatedRequest(
            TestUserId,
            email: "advisor@rajfinancial.com",
            roles: ["Advisor"],
            route: "auth/clients",
            method: "post",
            requestBody: SerializeRequest(assignRequest));

        var response2 = await functions.AssignClient(request2, context2);

        // Assert — both return 201 Created
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify the service was called twice
        clientManagementServiceMock.Verify(
            s => s.AssignClientAsync(
                TestUserId,
                It.IsAny<AssignClientRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // =========================================================================
    // POST /api/auth/clients — No UserIdGuid (malformed auth context)
    // =========================================================================

    [Fact]
    public async Task AssignClient_NoUserIdGuid_Returns401()
    {
        // Arrange — authenticated but malformed context (no Guid)
        var functions = CreateFunctions();
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = "not-a-guid";
        context.Items["UserRoles"] = new List<string> { "Advisor" };
        // No UserIdGuid — malformed auth context

        var mockRequest = new Mock<HttpRequestData>(context);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri("https://localhost/api/auth/clients"));
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
        var response = await functions.AssignClient(mockRequest.Object, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // GET /api/auth/clients — Security: 401 Unauthenticated
    // =========================================================================

    [Fact]
    public async Task GetClients_Unauthenticated_Returns401()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateUnauthenticatedRequest("auth/clients");

        // Act
        var response = await functions.GetClients(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClients_Unauthenticated_ReturnsAuthRequiredCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateUnauthenticatedRequest("auth/clients");

        // Act
        var response = await functions.GetClients(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("AUTH_REQUIRED");
    }

    // =========================================================================
    // GET /api/auth/clients — Security: 403 Client role forbidden
    // =========================================================================

    [Fact]
    public async Task GetClients_ClientRole_Returns403()
    {
        // Arrange — BDD: Client role should not access client management
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "client@rajfinancial.com",
            roles: ["Client"]);

        // Act
        var response = await functions.GetClients(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetClients_ClientRole_ReturnsAuthForbiddenCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "client@rajfinancial.com",
            roles: ["Client"]);

        // Act
        var response = await functions.GetClients(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("AUTH_FORBIDDEN");
    }

    // =========================================================================
    // GET /api/auth/clients — Advisor retrieves own assignments (200)
    // =========================================================================

    [Fact]
    public async Task GetClients_AdvisorWithAssignments_Returns200()
    {
        // Arrange — BDD: Advisor retrieves their own client list
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"]);

        var grants = new List<DataAccessGrant>
        {
            CreateTestGrant(granteeEmail: "client1@example.com"),
            CreateTestGrant(
                grantId: Guid.Parse("cccc0000-0000-0000-0000-000000000002"),
                granteeEmail: "client2@example.com",
                accessType: AccessType.Full)
        };

        clientManagementServiceMock
            .Setup(s => s.GetClientAssignmentsAsync(
                TestUserId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grants);

        // Act
        var response = await functions.GetClients(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClients_AdvisorWithAssignments_ReturnsBothClients()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"]);

        var grants = new List<DataAccessGrant>
        {
            CreateTestGrant(granteeEmail: "client1@example.com"),
            CreateTestGrant(
                grantId: Guid.Parse("cccc0000-0000-0000-0000-000000000002"),
                granteeEmail: "client2@example.com")
        };

        clientManagementServiceMock
            .Setup(s => s.GetClientAssignmentsAsync(
                TestUserId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(grants);

        // Act
        var response = await functions.GetClients(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("client1@example.com");
        body.Should().Contain("client2@example.com");
    }

    [Fact]
    public async Task GetClients_AdvisorNoAssignments_ReturnsEmptyArray()
    {
        // Arrange — advisor with no clients yet
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"]);

        clientManagementServiceMock
            .Setup(s => s.GetClientAssignmentsAsync(
                TestUserId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataAccessGrant>());

        // Act
        var response = await functions.GetClients(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Be("[]");
    }

    [Fact]
    public async Task GetClients_Advisor_CallsServiceWithIsAdminFalse()
    {
        // Arrange — BDD: Advisor can only see their own assignments
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"]);

        clientManagementServiceMock
            .Setup(s => s.GetClientAssignmentsAsync(
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataAccessGrant>());

        // Act
        await functions.GetClients(request, context);

        // Assert — isAdmin = false for Advisor, so only own assignments returned
        clientManagementServiceMock.Verify(
            s => s.GetClientAssignmentsAsync(
                TestUserId,
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // GET /api/auth/clients — Admin sees all assignments (200)
    // =========================================================================

    [Fact]
    public async Task GetClients_AdminRole_Returns200()
    {
        // Arrange — BDD: Admin sees all client assignments across advisors
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            AdminUserId,
            email: "admin@rajfinancial.com",
            roles: ["Administrator"]);

        clientManagementServiceMock
            .Setup(s => s.GetClientAssignmentsAsync(
                AdminUserId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataAccessGrant>
            {
                CreateTestGrant(grantorUserId: TestUserId, granteeEmail: "client1@example.com"),
                CreateTestGrant(
                    grantId: Guid.Parse("cccc0000-0000-0000-0000-000000000002"),
                    grantorUserId: OtherUserId,
                    granteeEmail: "client2@example.com")
            });

        // Act
        var response = await functions.GetClients(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClients_AdminRole_CallsServiceWithIsAdminTrue()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            AdminUserId,
            email: "admin@rajfinancial.com",
            roles: ["Administrator"]);

        clientManagementServiceMock
            .Setup(s => s.GetClientAssignmentsAsync(
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataAccessGrant>());

        // Act
        await functions.GetClients(request, context);

        // Assert — isAdmin = true for Administrator
        clientManagementServiceMock.Verify(
            s => s.GetClientAssignmentsAsync(
                AdminUserId,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // GET /api/auth/clients — No UserIdGuid (malformed auth)
    // =========================================================================

    [Fact]
    public async Task GetClients_NoUserIdGuid_Returns401()
    {
        // Arrange
        var functions = CreateFunctions();
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = "not-a-guid";
        context.Items["UserRoles"] = new List<string> { "Advisor" };

        var mockRequest = new Mock<HttpRequestData>(context);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri("https://localhost/api/auth/clients"));
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
        var response = await functions.GetClients(mockRequest.Object, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // DELETE /api/auth/clients/{id} — Security: 401 Unauthenticated
    // =========================================================================

    [Fact]
    public async Task RemoveClient_Unauthenticated_Returns401()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateUnauthenticatedRequest($"auth/clients/{TestGrantId}");

        // Act
        var response = await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveClient_Unauthenticated_ReturnsAuthRequiredCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateUnauthenticatedRequest($"auth/clients/{TestGrantId}");

        // Act
        var response = await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("AUTH_REQUIRED");
    }

    // =========================================================================
    // DELETE /api/auth/clients/{id} — Security: 403 Client role forbidden
    // =========================================================================

    [Fact]
    public async Task RemoveClient_ClientRole_Returns403()
    {
        // Arrange — BDD: Client role cannot remove assignments
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "client@rajfinancial.com",
            roles: ["Client"],
            route: $"auth/clients/{TestGrantId}");

        // Act
        var response = await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveClient_ClientRole_ReturnsAuthForbiddenCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            email: "client@rajfinancial.com",
            roles: ["Client"],
            route: $"auth/clients/{TestGrantId}");

        // Act
        var response = await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("AUTH_FORBIDDEN");
    }

    // =========================================================================
    // DELETE /api/auth/clients/{id} — Advisor removes own assignment (204)
    // =========================================================================

    [Fact]
    public async Task RemoveClient_AdvisorOwnsGrant_Returns204()
    {
        // Arrange — BDD: Advisor removes their own client assignment
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: $"auth/clients/{TestGrantId}");

        var grant = CreateTestGrant(grantorUserId: TestUserId);
        clientManagementServiceMock
            .Setup(s => s.GetGrantByIdAsync(TestGrantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        clientManagementServiceMock
            .Setup(s => s.RemoveClientAccessAsync(
                TestGrantId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveClient_AdvisorOwnsGrant_CallsRemoveService()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: $"auth/clients/{TestGrantId}");

        var grant = CreateTestGrant(grantorUserId: TestUserId);
        clientManagementServiceMock
            .Setup(s => s.GetGrantByIdAsync(TestGrantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        clientManagementServiceMock
            .Setup(s => s.RemoveClientAccessAsync(
                TestGrantId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert — verify soft-delete service was called
        clientManagementServiceMock.Verify(
            s => s.RemoveClientAccessAsync(TestGrantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // DELETE /api/auth/clients/{id} — Advisor cannot remove other's (403)
    // =========================================================================

    [Fact]
    public async Task RemoveClient_AdvisorDoesNotOwnGrant_Returns403()
    {
        // Arrange — BDD: Advisor cannot remove another advisor's assignment
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: $"auth/clients/{TestGrantId}");

        // Grant belongs to OtherUserId, not TestUserId
        var grant = CreateTestGrant(grantorUserId: OtherUserId);
        clientManagementServiceMock
            .Setup(s => s.GetGrantByIdAsync(TestGrantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        // Act
        var response = await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveClient_AdvisorDoesNotOwnGrant_ReturnsAuthForbiddenCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: $"auth/clients/{TestGrantId}");

        var grant = CreateTestGrant(grantorUserId: OtherUserId);
        clientManagementServiceMock
            .Setup(s => s.GetGrantByIdAsync(TestGrantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        // Act
        var response = await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("AUTH_FORBIDDEN");
    }

    [Fact]
    public async Task RemoveClient_AdvisorDoesNotOwnGrant_DoesNotCallRemove()
    {
        // Arrange — service should NOT be called for unauthorized removal
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: $"auth/clients/{TestGrantId}");

        var grant = CreateTestGrant(grantorUserId: OtherUserId);
        clientManagementServiceMock
            .Setup(s => s.GetGrantByIdAsync(TestGrantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        // Act
        await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert
        clientManagementServiceMock.Verify(
            s => s.RemoveClientAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // DELETE /api/auth/clients/{id} — Non-existent grant (404)
    // =========================================================================

    [Fact]
    public async Task RemoveClient_GrantNotFound_Returns404()
    {
        // Arrange — BDD: Non-existent grant ID returns 404
        var nonExistentId = Guid.Parse("ffff0000-0000-0000-0000-000000000099");
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: $"auth/clients/{nonExistentId}");

        clientManagementServiceMock
            .Setup(s => s.GetGrantByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataAccessGrant?)null);

        // Act
        var response = await functions.RemoveClient(request, nonExistentId.ToString(), context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveClient_GrantNotFound_ReturnsResourceNotFoundCode()
    {
        // Arrange
        var nonExistentId = Guid.Parse("ffff0000-0000-0000-0000-000000000099");
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: $"auth/clients/{nonExistentId}");

        clientManagementServiceMock
            .Setup(s => s.GetGrantByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataAccessGrant?)null);

        // Act
        var response = await functions.RemoveClient(request, nonExistentId.ToString(), context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("RESOURCE_NOT_FOUND");
    }

    // =========================================================================
    // DELETE /api/auth/clients/{id} — Admin can remove any (204)
    // =========================================================================

    [Fact]
    public async Task RemoveClient_AdminRemovesOtherAdvisorsGrant_Returns204()
    {
        // Arrange — BDD: Admin can remove any advisor's assignment
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            AdminUserId,
            email: "admin@rajfinancial.com",
            roles: ["Administrator"],
            route: $"auth/clients/{TestGrantId}");

        // Grant belongs to TestUserId (not AdminUserId), but Admin can still remove
        var grant = CreateTestGrant(grantorUserId: TestUserId);
        clientManagementServiceMock
            .Setup(s => s.GetGrantByIdAsync(TestGrantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        clientManagementServiceMock
            .Setup(s => s.RemoveClientAccessAsync(
                TestGrantId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveClient_AdminRemovesAnyGrant_CallsRemoveService()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            AdminUserId,
            email: "admin@rajfinancial.com",
            roles: ["Administrator"],
            route: $"auth/clients/{TestGrantId}");

        var grant = CreateTestGrant(grantorUserId: OtherUserId);
        clientManagementServiceMock
            .Setup(s => s.GetGrantByIdAsync(TestGrantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        clientManagementServiceMock
            .Setup(s => s.RemoveClientAccessAsync(
                TestGrantId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await functions.RemoveClient(request, TestGrantId.ToString(), context);

        // Assert
        clientManagementServiceMock.Verify(
            s => s.RemoveClientAccessAsync(TestGrantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // DELETE /api/auth/clients/{id} — Invalid GUID format
    // =========================================================================

    [Fact]
    public async Task RemoveClient_InvalidGuidFormat_Returns400()
    {
        // Arrange — route parameter is not a valid GUID
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(
            TestUserId,
            roles: ["Advisor"],
            route: "auth/clients/not-a-guid");

        // Act
        var response = await functions.RemoveClient(request, "not-a-guid", context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // DELETE /api/auth/clients/{id} — No UserIdGuid (malformed auth)
    // =========================================================================

    [Fact]
    public async Task RemoveClient_NoUserIdGuid_Returns401()
    {
        // Arrange
        var functions = CreateFunctions();
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = "not-a-guid";
        context.Items["UserRoles"] = new List<string> { "Advisor" };

        var mockRequest = new Mock<HttpRequestData>(context);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri($"https://localhost/api/auth/clients/{TestGrantId}"));
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
        var response = await functions.RemoveClient(mockRequest.Object, TestGrantId.ToString(), context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
