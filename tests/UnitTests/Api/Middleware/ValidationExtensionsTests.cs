using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using RajFinancial.Api.Middleware;

namespace RajFinancial.UnitTests.Api.Middleware;

/// <summary>
/// Unit tests for <see cref="ValidationExtensions"/>.
/// Tests request body deserialization and validation logic.
/// </summary>
public class ValidationExtensionsTests
{
    [Fact]
    public void GetBody_WhenValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        var context = new TestFunctionContext();
        var json = """{"name":"Test","value":42}""";
        context.Items["RequestBody"] = json;

        // Act
        var result = context.GetBody<TestRequest>();

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GetBody_WhenNoBody_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();

        // Act
        var result = context.GetBody<TestRequest>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetBody_WhenInvalidJson_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["RequestBody"] = "not valid json";

        // Act
        var result = context.GetBody<TestRequest>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetBody_WhenEmptyString_ReturnsNull()
    {
        // Arrange
        var context = new TestFunctionContext();
        context.Items["RequestBody"] = "";

        // Act
        var result = context.GetBody<TestRequest>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetBody_WithCamelCaseJson_DeserializesCorrectly()
    {
        // Arrange
        var context = new TestFunctionContext();
        var json = """{"name":"CamelCase","value":100}""";
        context.Items["RequestBody"] = json;

        // Act
        var result = context.GetBody<TestRequest>();

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("CamelCase");
    }

    [Fact]
    public void GetBody_WithPascalCaseJson_DeserializesCorrectly()
    {
        // Arrange - JSON has PascalCase, C# properties also PascalCase
        var context = new TestFunctionContext();
        var json = """{"Name":"PascalCase","Value":200}""";
        context.Items["RequestBody"] = json;

        // Act
        var result = context.GetBody<TestRequest>();

        // Assert - PropertyNameCaseInsensitive = true handles this
        result.Should().NotBeNull();
        result!.Name.Should().Be("PascalCase");
    }

    [Fact]
    public void GetBodyAsDictionary_WhenValidJson_ReturnsDictionary()
    {
        // Arrange
        var context = new TestFunctionContext();
        var json = """{"key1":"value1","key2":"value2"}""";
        context.Items["RequestBody"] = json;

        // Act
        var result = context.GetBodyAsDictionary();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("key1");
        result.Should().ContainKey("key2");
    }

    public class TestRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
