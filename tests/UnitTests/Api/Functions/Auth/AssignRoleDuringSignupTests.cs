using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Moq;
using RajFinancial.Api.Functions.Auth;
using RajFinancial.Api.Services;

namespace RajFinancial.UnitTests.Api.Functions.Auth;

/// <summary>
/// Unit tests for AssignRoleDuringSignup API Connector function.
/// Tests role validation, security rules, and API Connector response format.
/// </summary>
public class AssignRoleDuringSignupTests
{
    private readonly Mock<ILogger<AssignRoleDuringSignup>> _loggerMock;
    private readonly Mock<IGraphClientWrapper> _graphClientMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AssignRoleDuringSignup _function;

    public AssignRoleDuringSignupTests()
    {
        _loggerMock = new Mock<ILogger<AssignRoleDuringSignup>>();
        _graphClientMock = new Mock<IGraphClientWrapper>();
        _configurationMock = new Mock<IConfiguration>();

        _function = new AssignRoleDuringSignup(
            _loggerMock.Object,
            _graphClientMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task Run_WithValidClientRole_ReturnsContinueWithRole()
    {
        // Arrange
        var request = CreateHttpRequestData(new
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            GivenName = "Test",
            Surname = "User",
            UiLocales = "Client"
        });

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await ReadResponseBodyAsync<ApiConnectorResponse>(response);
        responseBody.Should().NotBeNull();
        responseBody!.Version.Should().Be("1.0.0");
        responseBody.Action.Should().Be("Continue");
        responseBody.Extension_RequestedRole.Should().Be("Client");
    }

    [Fact]
    public async Task Run_WithValidAdvisorRole_ReturnsContinueWithRole()
    {
        // Arrange
        var request = CreateHttpRequestData(new
        {
            Email = "advisor@example.com",
            DisplayName = "Test Advisor",
            GivenName = "Test",
            Surname = "Advisor",
            UiLocales = "Advisor"
        });

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await ReadResponseBodyAsync<ApiConnectorResponse>(response);
        responseBody!.Extension_RequestedRole.Should().Be("Advisor");
    }

    [Fact]
    public async Task Run_WithAdministratorRole_DefaultsToClient()
    {
        // Arrange - Security test: Admin cannot self-signup
        var request = CreateHttpRequestData(new
        {
            Email = "hacker@example.com",
            DisplayName = "Hacker",
            GivenName = "Hacker",
            Surname = "User",
            UiLocales = "Administrator"
        });

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await ReadResponseBodyAsync<ApiConnectorResponse>(response);
        responseBody!.Extension_RequestedRole.Should().Be("Client",
            "Administrator role should be blocked and default to Client for security");

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid role requested")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithInvalidRole_DefaultsToClient()
    {
        // Arrange
        var request = CreateHttpRequestData(new
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            GivenName = "Test",
            Surname = "User",
            UiLocales = "SuperUser"
        });

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await ReadResponseBodyAsync<ApiConnectorResponse>(response);
        responseBody!.Extension_RequestedRole.Should().Be("Client",
            "Invalid roles should default to Client");
    }

    [Fact]
    public async Task Run_WithNoUiLocales_DefaultsToClient()
    {
        // Arrange
        var request = CreateHttpRequestData(new
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            GivenName = "Test",
            Surname = "User"
            // UiLocales not provided
        });

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await ReadResponseBodyAsync<ApiConnectorResponse>(response);
        responseBody!.Extension_RequestedRole.Should().Be("Client",
            "Should default to Client when role not specified");
    }

    [Fact]
    public async Task Run_WithMissingEmail_ReturnsBlockPage()
    {
        // Arrange - Invalid request without email
        var request = CreateHttpRequestData(new
        {
            DisplayName = "Test User",
            GivenName = "Test",
            Surname = "User"
            // Email missing
        });

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await ReadResponseBodyAsync<ApiConnectorResponse>(response);
        responseBody!.Action.Should().Be("ShowBlockPage",
            "Should block signup when email is missing");
        responseBody.UserMessage.Should().Contain("Invalid request");
    }

    [Fact]
    public async Task Run_WithEmptyRequestBody_ReturnsBlockPage()
    {
        // Arrange
        var request = CreateHttpRequestData("");

        // Act
        var response = await _function.Run(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await ReadResponseBodyAsync<ApiConnectorResponse>(response);
        responseBody!.Action.Should().Be("ShowBlockPage");
    }

    [Theory]
    [InlineData("Client")]
    [InlineData("Advisor")]
    public async Task Run_LogsUserSignup_ForValidRoles(string role)
    {
        // Arrange
        var request = CreateHttpRequestData(new
        {
            Email = $"{role.ToLower()}@example.com",
            DisplayName = $"Test {role}",
            GivenName = "Test",
            Surname = role,
            UiLocales = role
        });

        // Act
        await _function.Run(request);

        // Assert - Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User signing up")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithException_ReturnsContinueWithoutRole()
    {
        // Arrange - Simulate exception during processing
        var request = CreateInvalidHttpRequestData();

        // Act
        var response = await _function.Run(request);

        // Assert - Should not block signup on errors
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await ReadResponseBodyAsync<ApiConnectorResponse>(response);
        responseBody!.Action.Should().Be("Continue",
            "Should allow signup to continue even if role assignment fails");

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in AssignRoleDuringSignup")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #region Helper Methods

    /// <summary>
    /// Creates a mock HttpRequestData with the specified body content.
    /// </summary>
    private static HttpRequestData CreateHttpRequestData(object body)
    {
        var json = JsonSerializer.Serialize(body);
        return CreateHttpRequestData(json);
    }

    /// <summary>
    /// Creates a mock HttpRequestData with the specified JSON string.
    /// </summary>
    private static HttpRequestData CreateHttpRequestData(string jsonBody)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddOptions();
        serviceCollection.AddFunctionsWorkerDefaults();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var context = new Mock<FunctionContext>();
        context.Setup(c => c.InstanceServices).Returns(serviceProvider);

        var request = new Mock<HttpRequestData>(context.Object);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
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
    /// Creates an invalid HttpRequestData that will throw an exception when reading the body.
    /// Still provides valid InstanceServices so the error response can be written.
    /// </summary>
    private static HttpRequestData CreateInvalidHttpRequestData()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddOptions();
        serviceCollection.AddFunctionsWorkerDefaults();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var context = new Mock<FunctionContext>();
        context.Setup(c => c.InstanceServices).Returns(serviceProvider);

        var request = new Mock<HttpRequestData>(context.Object);

        // Setup Body to throw exception when accessed
        request.Setup(r => r.Body).Throws(new InvalidOperationException("Test exception"));

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
        // Reset stream position to beginning before reading
        response.Body.Position = 0;

        return await JsonSerializer.DeserializeAsync<T>(response.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    #endregion
}
