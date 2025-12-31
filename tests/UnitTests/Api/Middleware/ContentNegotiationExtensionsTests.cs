using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Middleware;

namespace RajFinancial.UnitTests.Api.Middleware;

/// <summary>
/// Unit tests for <see cref="ContentNegotiationExtensions"/>.
/// Tests the extension methods used for content negotiation in functions.
/// </summary>
public class ContentNegotiationExtensionsTests
{
    [Fact]
    public void DeserializeBody_WhenNoBodyBytes_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();

        // Act
        var result = context.DeserializeBody<TestDto>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DeserializeBody_WhenEmptyBytes_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["RequestBodyBytes"] = Array.Empty<byte>();

        // Act
        var result = context.DeserializeBody<TestDto>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DeserializeBody_WithJsonBytes_DeserializesCorrectly()
    {
        // Arrange
        var context = new TestFunctionContext();
        var json = """{"name":"Test","value":42}""";
        context.Items["RequestBodyBytes"] = System.Text.Encoding.UTF8.GetBytes(json);
        context.Items["RequestContentType"] = SerializationFactory.JsonContentType;
        context.Items["SerializationFactory"] = CreateSerializationFactory();

        // Act
        var result = context.DeserializeBody<TestDto>();

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void DeserializeBody_WithoutFactory_FallsBackToJson()
    {
        // Arrange
        var context = new TestFunctionContext();
        var json = """{"name":"Fallback","value":99}""";
        context.Items["RequestBodyBytes"] = System.Text.Encoding.UTF8.GetBytes(json);
        // No SerializationFactory in context

        // Act
        var result = context.DeserializeBody<TestDto>();

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

    [Fact]
    public void GetSerializationFactory_WhenSet_ReturnsFactory()
    {
        // Arrange
        var context = new TestFunctionContext();
        var factory = CreateSerializationFactory();
        context.Items["SerializationFactory"] = factory;

        // Act
        var result = context.GetSerializationFactory();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(factory);
    }

    [Fact]
    public void GetSerializationFactory_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();

        // Act
        var result = context.GetSerializationFactory();

        // Assert
        result.Should().BeNull();
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
