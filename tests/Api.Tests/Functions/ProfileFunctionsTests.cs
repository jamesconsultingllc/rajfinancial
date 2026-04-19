using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Functions;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Content;
using RajFinancial.Api.Services.UserProfiles;
using RajFinancial.Api.Tests.Middleware;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Tests.Functions;

/// <summary>
/// TDD unit tests for <see cref="ProfileFunctions"/>.
/// Tests the <c>/api/profile/me</c> endpoint that returns the persisted
/// <see cref="UserProfile"/> for the authenticated user.
/// </summary>
public class ProfileFunctionsTests
{
    private readonly Mock<ILogger<ProfileFunctions>> loggerMock;
    private readonly Mock<IUserProfileService> userProfileServiceMock;
    private readonly Mock<ISerializationFactory> serializationFactoryMock;

    private static readonly Guid testUserId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid testTenantId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");

    public ProfileFunctionsTests()
    {
        loggerMock = new Mock<ILogger<ProfileFunctions>>();
        userProfileServiceMock = new Mock<IUserProfileService>();
        serializationFactoryMock = new Mock<ISerializationFactory>();

        // Default: serialize any object to JSON bytes (camelCase to match API output)
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        serializationFactoryMock
            .Setup(s => s.SerializeAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<object, string, CancellationToken>((data, _, _) =>
                Task.FromResult(Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(data, jsonOptions))));
    }

    private ProfileFunctions CreateFunctions()
        => new(loggerMock.Object, userProfileServiceMock.Object, serializationFactoryMock.Object);

    private static (HttpRequestData Request, TestFunctionContext Context) CreateAuthenticatedRequest(
        Guid userId)
    {
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = userId.ToString();
        context.Items["UserIdGuid"] = userId;

        var mockRequest = new Mock<HttpRequestData>(context);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri("https://localhost/api/profile/me"));
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

    // =========================================================================
    // GET /api/profile/me — success
    // =========================================================================

    [Fact]
    public async Task GetMyProfile_ExistingProfile_Returns200()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(testUserId);
        userProfileServiceMock
            .Setup(s => s.GetByIdAsync(testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestProfile());

        // Act
        var response = await functions.GetMyProfile(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyProfile_ExistingProfile_ReturnsProfileId()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(testUserId);
        userProfileServiceMock
            .Setup(s => s.GetByIdAsync(testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestProfile());

        // Act
        var response = await functions.GetMyProfile(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain(testUserId.ToString());
    }

    [Fact]
    public async Task GetMyProfile_ExistingProfile_ReturnsLocaleDefaults()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(testUserId);
        userProfileServiceMock
            .Setup(s => s.GetByIdAsync(testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestProfile());

        // Act
        var response = await functions.GetMyProfile(request, context);

        // Assert — defaults when no PreferencesJson
        var body = await ReadResponseBody(response);
        body.Should().Contain("\"locale\":\"en-US\"");
        body.Should().Contain("\"timezone\":\"America/New_York\"");
        body.Should().Contain("\"currency\":\"USD\"");
    }

    [Fact]
    public async Task GetMyProfile_ExistingProfile_ReturnsCreatedAt()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(testUserId);
        userProfileServiceMock
            .Setup(s => s.GetByIdAsync(testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestProfile());

        // Act
        var response = await functions.GetMyProfile(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("\"createdAt\":");
    }

    [Fact]
    public async Task GetMyProfile_ExistingProfile_ReturnsDisplayName()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(testUserId);
        var profile = CreateTestProfile();
        profile.DisplayName = "John Doe";
        userProfileServiceMock
            .Setup(s => s.GetByIdAsync(testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var response = await functions.GetMyProfile(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("John Doe");
    }

    // =========================================================================
    // GET /api/profile/me — not found
    // =========================================================================

    [Fact]
    public async Task GetMyProfile_NoProfile_Returns404()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(testUserId);
        userProfileServiceMock
            .Setup(s => s.GetByIdAsync(testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var response = await functions.GetMyProfile(request, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyProfile_NoProfile_ReturnsErrorCode()
    {
        // Arrange
        var functions = CreateFunctions();
        var (request, context) = CreateAuthenticatedRequest(testUserId);
        userProfileServiceMock
            .Setup(s => s.GetByIdAsync(testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var response = await functions.GetMyProfile(request, context);

        // Assert
        var body = await ReadResponseBody(response);
        body.Should().Contain("PROFILE_NOT_FOUND");
    }

    // =========================================================================
    // GET /api/profile/me — missing UserId
    // =========================================================================

    [Fact]
    public async Task GetMyProfile_NoUserIdGuid_Returns401()
    {
        // Arrange
        var functions = CreateFunctions();
        var context = new TestFunctionContext();
        context.Items["IsAuthenticated"] = true;
        context.Items["UserId"] = "not-a-guid";
        // No UserIdGuid

        var mockRequest = new Mock<HttpRequestData>(context);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri("https://localhost/api/profile/me"));
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
        var response = await functions.GetMyProfile(mockRequest.Object, context);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static UserProfile CreateTestProfile()
    {
        return new UserProfile
        {
            Id = testUserId,
            Email = "user@rajfinancial.com",
            DisplayName = "Test User",
            Role = UserRole.Client,
            TenantId = testTenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow
        };
    }

    private static async Task<string> ReadResponseBody(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
