using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Middleware.Content;

namespace RajFinancial.UnitTests.Api.Middleware;

/// <summary>
/// Unit tests for <see cref="ContentNegotiationExtensions"/>.
/// Tests the extension methods used for content negotiation in functions.
/// </summary>
public class ContentNegotiationExtensionsTests
{
    [Fact]
    public async Task DeserializeBodyAsync_WhenNoBodyBytes_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();
        var factory = CreateSerializationFactory();

        // Act
        var result = await context.DeserializeBodyAsync<TestDto>(factory);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeBodyAsync_WhenEmptyBytes_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["RequestBodyBytes"] = Array.Empty<byte>();
        var factory = CreateSerializationFactory();

        // Act
        var result = await context.DeserializeBodyAsync<TestDto>(factory);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeBodyAsync_WithJsonBytes_DeserializesCorrectly()
    {
        // Arrange
        var context = new TestFunctionContext();
        var json = """{"name":"Test","value":42}""";
        context.Items["RequestBodyBytes"] = Encoding.UTF8.GetBytes(json);
        context.Items["RequestContentType"] = SerializationFactory.JsonContentType;
        var factory = CreateSerializationFactory();

        // Act
        var result = await context.DeserializeBodyAsync<TestDto>(factory);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task DeserializeBodyAsync_DefaultsToJsonContentType_WhenNotSet()
    {
        // Arrange
        var context = new TestFunctionContext();
        var json = """{"name":"Fallback","value":99}""";
        context.Items["RequestBodyBytes"] = Encoding.UTF8.GetBytes(json);
        // RequestContentType not set - should default to JSON
        var factory = CreateSerializationFactory();

        // Act
        var result = await context.DeserializeBodyAsync<TestDto>(factory);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Fallback");
    }

    [Fact]
    public void GetResponseContentType_WhenSet_ReturnsContentType()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["ResponseContentType"] = "application/x-memorypack";

        // Act
        var result = context.GetResponseContentType();

        // Assert
        result.Should().Be("application/x-memorypack");
    }

    [Fact]
    public void GetResponseContentType_WhenNotSet_ReturnsJson()
    {
        // Arrange
        var context = new TestFunctionContext();

        // Act
        var result = context.GetResponseContentType();

        // Assert
        result.Should().Be(SerializationFactory.JsonContentType);
    }

    private static ISerializationFactory CreateSerializationFactory()
    {
        var configData = new Dictionary<string, string?>
        {
            { "AZURE_FUNCTIONS_ENVIRONMENT", "Development" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var loggerMock = new Mock<ILogger<SerializationFactory>>();
        return new SerializationFactory(configuration, loggerMock.Object);
    }

    public class TestDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
