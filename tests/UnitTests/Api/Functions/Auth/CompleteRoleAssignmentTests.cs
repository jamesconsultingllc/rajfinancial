using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Moq;
using RajFinancial.Api.Functions.Auth;
using RajFinancial.Api.Services;
using RajFinancial.Api.Validators;
using RajFinancial.Shared.Contracts.Auth;

namespace RajFinancial.UnitTests.Api.Functions.Auth;

/// <summary>
/// Unit tests for CompleteRoleAssignment function.
/// Tests role assignment via Microsoft Graph API with proper error handling.
/// </summary>
public class CompleteRoleAssignmentTests
{
    private readonly Mock<ILogger<CompleteRoleAssignment>> _loggerMock;
    private readonly Mock<IGraphClientWrapper> _graphClientMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly IValidator<CompleteRoleRequest> _validator;
    private readonly CompleteRoleAssignment _function;
    private const string TestServicePrincipalId = "12345678-1234-1234-1234-123456789012";

    public CompleteRoleAssignmentTests()
    {
        _loggerMock = new Mock<ILogger<CompleteRoleAssignment>>();
        _graphClientMock = new Mock<IGraphClientWrapper>();
        _configurationMock = new Mock<IConfiguration>();
        _validator = new CompleteRoleRequestValidator();

        // Setup configuration
        _configurationMock
            .Setup(c => c["EntraExternalId:ServicePrincipalId"])
            .Returns(TestServicePrincipalId);

        _function = new CompleteRoleAssignment(
            _loggerMock.Object,
            _graphClientMock.Object,
            _configurationMock.Object,
            _validator);
    }

    [Theory]
    [InlineData("Client")]
    [InlineData("Advisor")]
    [InlineData("Administrator")]
    public async Task Run_WithValidRequest_AssignsRole(string role)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = CreateHttpRequestData(new CompleteRoleRequest
        {
            UserId = userId,
            Role = role
        });

        SetupGraphClientForNewAssignment(userId);

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await ReadResponseBodyAsync<CompleteRoleResponse>(response);
        responseBody.Should().NotBeNull();
        responseBody!.Success.Should().BeTrue();
        responseBody.Role.Should().Be(role);
        responseBody.Message.Should().BeNull();

        // Verify Graph API was called
        _graphClientMock.Verify(
            x => x.AssignAppRoleToUserAsync(
                userId,
                It.IsAny<AppRoleAssignment>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithMissingUserId_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateHttpRequestData(new
        {
            Role = "Client"
            // UserId missing
        });

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseBody = await ReadResponseBodyAsync<CompleteRoleErrorResponse>(response);
        responseBody.Should().NotBeNull();
        responseBody!.Code.Should().Be("VALIDATION_FAILED");
        responseBody.Error.Should().Contain("UserId and Role are required");

        // Verify Graph API was NOT called
        _graphClientMock.Verify(
            x => x.AssignAppRoleToUserAsync(
                It.IsAny<string>(),
                It.IsAny<AppRoleAssignment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_WithMissingRole_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateHttpRequestData(new
        {
            UserId = Guid.NewGuid().ToString()
            // Role missing
        });

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseBody = await ReadResponseBodyAsync<CompleteRoleErrorResponse>(response);
        responseBody.Should().NotBeNull();
        responseBody!.Code.Should().Be("VALIDATION_FAILED");
        responseBody.Error.Should().Contain("UserId and Role are required");
    }

    [Fact]
    public async Task Run_WithInvalidRole_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateHttpRequestData(new CompleteRoleRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Role = "SuperUser" // Invalid role
        });

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseBody = await ReadResponseBodyAsync<CompleteRoleErrorResponse>(response);
        responseBody.Should().NotBeNull();
        responseBody!.Code.Should().Be("INVALID_ROLE");
        responseBody.Error.Should().Contain("Invalid role");
    }

    [Fact]
    public async Task Run_WithMissingServicePrincipalConfig_ReturnsInternalServerError()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock
            .Setup(c => c["EntraExternalId:ServicePrincipalId"])
            .Returns((string?)null); // Config missing

        var functionWithBadConfig = new CompleteRoleAssignment(
            _loggerMock.Object,
            _graphClientMock.Object,
            configMock.Object,
            _validator);

        var request = CreateHttpRequestData(new CompleteRoleRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Role = "Client"
        });

        // Act
        var response = await functionWithBadConfig.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var responseBody = await ReadResponseBodyAsync<CompleteRoleErrorResponse>(response);
        responseBody.Should().NotBeNull();
        responseBody!.Code.Should().Be("CONFIGURATION_ERROR");
        responseBody.Error.Should().Contain("Service principal configuration missing");
    }

    [Fact]
    public async Task Run_WhenRoleAlreadyAssigned_ReturnsSuccessWithMessage()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = CreateHttpRequestData(new CompleteRoleRequest
        {
            UserId = userId,
            Role = "Client"
        });

        SetupGraphClientForExistingAssignment(userId, "Client");

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await ReadResponseBodyAsync<CompleteRoleResponse>(response);
        responseBody.Should().NotBeNull();
        responseBody!.Success.Should().BeTrue();
        responseBody.Role.Should().Be("Client");
        responseBody.Message.Should().Contain("already assigned");

        // Verify Graph API was called to GET but NOT to POST
        _graphClientMock.Verify(
            x => x.AssignAppRoleToUserAsync(
                It.IsAny<string>(),
                It.IsAny<AppRoleAssignment>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not assign role if already assigned");
    }

    [Fact]
    public async Task Run_WithInsufficientPermissions_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = CreateHttpRequestData(new CompleteRoleRequest
        {
            UserId = userId,
            Role = "Client"
        });

        SetupGraphClientForNewAssignment(userId);

        // Setup Graph to throw permission error
        _graphClientMock
            .Setup(x => x.AssignAppRoleToUserAsync(
                userId,
                It.IsAny<AppRoleAssignment>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceException("Permission grants"));

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var responseBody = await ReadResponseBodyAsync<CompleteRoleErrorResponse>(response);
        responseBody.Should().NotBeNull();
        responseBody!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
        responseBody.Error.Should().Contain("Insufficient permissions");
        responseBody.Error.Should().Contain("AppRoleAssignment.ReadWrite.All");
    }

    [Fact]
    public async Task Run_WithGraphApiException_ReturnsInternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = CreateHttpRequestData(new CompleteRoleRequest
        {
            UserId = userId,
            Role = "Client"
        });

        SetupGraphClientForNewAssignment(userId);

        // Setup Graph to throw generic exception
        _graphClientMock
            .Setup(x => x.AssignAppRoleToUserAsync(
                userId,
                It.IsAny<AppRoleAssignment>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Graph API error"));

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var responseBody = await ReadResponseBodyAsync<CompleteRoleErrorResponse>(response);
        responseBody.Should().NotBeNull();
        responseBody!.Code.Should().Be("INTERNAL_ERROR");
        responseBody.Error.Should().Contain("Internal server error");

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error completing role assignment")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_LogsSuccessfulAssignment()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = CreateHttpRequestData(new CompleteRoleRequest
        {
            UserId = userId,
            Role = "Advisor"
        });

        SetupGraphClientForNewAssignment(userId);

        // Act
        await _function.Run(request);

        // Assert - Verify success logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully assigned role")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #region Helper Methods

    /// <summary>
    /// Sets up Graph client mock for a user with no existing role assignments.
    /// </summary>
    private void SetupGraphClientForNewAssignment(string userId)
    {
        var emptyAssignments = new AppRoleAssignmentCollectionResponse
        {
            Value = []
        };

        _graphClientMock
            .Setup(x => x.GetUserAppRoleAssignmentsAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyAssignments);

        _graphClientMock
            .Setup(x => x.AssignAppRoleToUserAsync(
                userId,
                It.IsAny<AppRoleAssignment>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppRoleAssignment());
    }

    /// <summary>
    /// Sets up Graph client mock for a user with an existing role assignment.
    /// </summary>
    private void SetupGraphClientForExistingAssignment(string userId, string role)
    {
        // Map role to GUID from CompleteRoleAssignment.cs
        var roleGuid = role switch
        {
            "Client" => Guid.Parse("00000000-0000-0000-0000-000000000003"),
            "Advisor" => Guid.Parse("00000000-0000-0000-0000-000000000002"),
            "Administrator" => Guid.Parse("00000000-0000-0000-0000-000000000001"),
            _ => throw new ArgumentException($"Unknown role: {role}")
        };

        var existingAssignments = new AppRoleAssignmentCollectionResponse
        {
            Value =
            [
                new AppRoleAssignment
                {
                    AppRoleId = roleGuid,
                    ResourceId = Guid.Parse(TestServicePrincipalId)
                }
            ]
        };

        _graphClientMock
            .Setup(x => x.GetUserAppRoleAssignmentsAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssignments);
    }

    /// <summary>
    /// Creates a mock HttpRequestData with the specified body content.
    /// </summary>
    private static HttpRequestData CreateHttpRequestData(object body)
    {
        var json = JsonSerializer.Serialize(body);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddOptions();
        serviceCollection.AddFunctionsWorkerDefaults();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var context = new Mock<FunctionContext>();
        context.Setup(c => c.InstanceServices).Returns(serviceProvider);

        var request = new Mock<HttpRequestData>(context.Object);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        request.Setup(r => r.Body).Returns(stream);

        var response = new Mock<HttpResponseData>(context.Object);
        response.SetupProperty(r => r.StatusCode);
        response.SetupProperty(r => r.Headers, new HttpHeadersCollection());

        var responseStream = new MemoryStream();
        response.Setup(r => r.Body).Returns(responseStream);

        request.Setup(r => r.CreateResponse()).Returns(response.Object);

        return request.Object;
    }

    /// <summary>
    /// Reads and deserializes the response body from HttpResponseData.
    /// </summary>
    private static async Task<T?> ReadResponseBodyAsync<T>(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    #endregion
}
