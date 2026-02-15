using System.Text;
using FluentAssertions;
using MemoryPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RajFinancial.Api.Middleware.Content;

namespace RajFinancial.Api.Tests.Middleware;

/// <summary>
/// Unit tests for <see cref="SerializationFactory"/>.
/// Tests JSON/MemoryPack content negotiation logic.
/// </summary>
public partial class SerializationFactoryTests
{
    private readonly Mock<ILogger<SerializationFactory>> loggerMock = new();

    private SerializationFactory CreateFactory(string environment = "Development", bool useMemoryPackInProduction = true)
    {
        var configData = new Dictionary<string, string?>
        {
            { "AZURE_FUNCTIONS_ENVIRONMENT", environment },
            { "Serialization:UseMemoryPackInProduction", useMemoryPackInProduction.ToString() }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        return new SerializationFactory(configuration, loggerMock.Object);
    }

    [Fact]
    public void GetPreferredContentType_InDevelopment_AlwaysReturnsJson()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert - should return JSON regardless of Accept header
        factory.GetPreferredContentType(null).Should().Be(SerializationFactory.JsonContentType);
        factory.GetPreferredContentType("application/json").Should().Be(SerializationFactory.JsonContentType);
        factory.GetPreferredContentType("application/x-memorypack").Should().Be(SerializationFactory.JsonContentType);
        factory.GetPreferredContentType("*/*").Should().Be(SerializationFactory.JsonContentType);
    }

    [Fact]
    public void GetPreferredContentType_InProduction_DefaultsToMemoryPack()
    {
        // Arrange
        var factory = CreateFactory("Production", useMemoryPackInProduction: true);

        // Act
        var result = factory.GetPreferredContentType(null);

        // Assert
        result.Should().Be(SerializationFactory.MemoryPackContentType);
    }

    [Fact]
    public void GetPreferredContentType_InProduction_WhenClientRequestsOnlyJson_ReturnsJson()
    {
        // Arrange
        var factory = CreateFactory("Production", useMemoryPackInProduction: true);

        // Act
        var result = factory.GetPreferredContentType("application/json");

        // Assert
        result.Should().Be(SerializationFactory.JsonContentType);
    }

    [Fact]
    public void GetPreferredContentType_InProduction_WhenClientRequestsMemoryPack_ReturnsMemoryPack()
    {
        // Arrange
        var factory = CreateFactory("Production", useMemoryPackInProduction: true);

        // Act
        var result = factory.GetPreferredContentType("application/x-memorypack");

        // Assert
        result.Should().Be(SerializationFactory.MemoryPackContentType);
    }

    [Fact]
    public void GetPreferredContentType_InProduction_WhenMemoryPackDisabled_ReturnsJson()
    {
        // Arrange
        var factory = CreateFactory("Production", useMemoryPackInProduction: false);

        // Act
        var result = factory.GetPreferredContentType(null);

        // Assert
        result.Should().Be(SerializationFactory.JsonContentType);
    }

    [Fact]
    public async Task SerializeAsync_WithJsonContentType_ReturnsJsonBytes()
    {
        // Arrange
        var factory = CreateFactory();
        var testData = new TestDto { Name = "Test", Value = 42 };

        // Act
        var bytes = await factory.SerializeAsync(testData, SerializationFactory.JsonContentType);

        // Assert
        bytes.Should().NotBeEmpty();
        var json = Encoding.UTF8.GetString(bytes);
        json.Should().Contain("name");
        json.Should().Contain("Test");
    }

    [Fact]
    public async Task DeserializeAsync_WithJsonContentType_ReturnsObject()
    {
        // Arrange
        var factory = CreateFactory();
        var json = """{"name":"Test","value":42}""";
        var bytes = Encoding.UTF8.GetBytes(json);

        // Act
        var result = await factory.DeserializeAsync<TestDto>(bytes, SerializationFactory.JsonContentType);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task DeserializeAsync_WithEmptyBytes_ReturnsDefault()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var result = await factory.DeserializeAsync<TestDto>([], SerializationFactory.JsonContentType);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SerializeAsync_WithMemoryPackContentType_ReturnsMemoryPackBytes()
    {
        // Arrange
        var factory = CreateFactory();
        var testData = new TestDto { Name = "Test", Value = 42 };

        // Act
        var bytes = await factory.SerializeAsync(testData, SerializationFactory.MemoryPackContentType);

        // Assert
        bytes.Should().NotBeEmpty();
        // MemoryPack uses binary format, so we can't easily verify content
        // but we can verify it's different from JSON
        var jsonBytes = await factory.SerializeAsync(testData, SerializationFactory.JsonContentType);
        bytes.Should().NotBeEquivalentTo(jsonBytes);
    }

    [Fact]
    public async Task SerializeAndDeserializeAsync_WithMemoryPack_RoundTripsCorrectly()
    {
        // Arrange
        var factory = CreateFactory();
        var testData = new TestDto { Name = "RoundTrip", Value = 123 };

        // Act
        var bytes = await factory.SerializeAsync(testData, SerializationFactory.MemoryPackContentType);
        var result = await factory.DeserializeAsync<TestDto>(bytes, SerializationFactory.MemoryPackContentType);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("RoundTrip");
        result.Value.Should().Be(123);
    }

    [Fact]
    public void ContentTypeConstants_ShouldBeCorrect()
    {
        // Assert
        SerializationFactory.JsonContentType.Should().Be("application/json");
        SerializationFactory.MemoryPackContentType.Should().Be("application/x-memorypack");
    }

    /// <summary>
    /// Test DTO for serialization tests.
    /// </summary>
    [MemoryPackable]
    public partial class TestDto
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
