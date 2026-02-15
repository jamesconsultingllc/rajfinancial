using System.Reflection;
using FluentAssertions;
using RajFinancial.Api.Middleware.Authorization;

namespace RajFinancial.Api.Tests.Middleware;

/// <summary>
/// Unit tests for <see cref="RequireAuthenticationAttribute"/>.
/// Validates the attribute can be applied to methods and is discoverable via reflection.
/// </summary>
public class RequireAuthenticationAttributeTests
{
    [Fact]
    public void Attribute_CanBeInstantiated()
    {
        // Act
        var attribute = new RequireAuthenticationAttribute();

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void Attribute_InheritsFromAttribute()
    {
        // Act
        var attribute = new RequireAuthenticationAttribute();

        // Assert
        attribute.Should().BeAssignableTo<Attribute>();
    }

    [Fact]
    public void Attribute_CanBeAppliedToMethods()
    {
        // Arrange
        var usage = typeof(RequireAuthenticationAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.Should().NotBeNull();
        (usage!.ValidOn & AttributeTargets.Method).Should().Be(AttributeTargets.Method);
    }

    [Fact]
    public void Attribute_CanBeAppliedToClasses()
    {
        // Arrange
        var usage = typeof(RequireAuthenticationAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.Should().NotBeNull();
        (usage!.ValidOn & AttributeTargets.Class).Should().Be(AttributeTargets.Class);
    }

    [Fact]
    public void Attribute_IsNotInherited()
    {
        // Arrange — each function should explicitly opt in
        var usage = typeof(RequireAuthenticationAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.Should().NotBeNull();
        usage!.Inherited.Should().BeFalse();
    }

    [Fact]
    public void Attribute_DoesNotAllowMultiple()
    {
        // Arrange
        var usage = typeof(RequireAuthenticationAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.Should().NotBeNull();
        usage!.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void Attribute_IsDiscoverableOnDecoratedMethod()
    {
        // Arrange
        var method = typeof(SampleDecoratedClass)
            .GetMethod(nameof(SampleDecoratedClass.ProtectedMethod));

        // Act
        var attribute = method?.GetCustomAttribute<RequireAuthenticationAttribute>();

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void Attribute_IsNotPresentOnUndecoratedMethod()
    {
        // Arrange
        var method = typeof(SampleDecoratedClass)
            .GetMethod(nameof(SampleDecoratedClass.PublicMethod));

        // Act
        var attribute = method?.GetCustomAttribute<RequireAuthenticationAttribute>();

        // Assert
        attribute.Should().BeNull();
    }

    /// <summary>
    /// Helper class for reflection-based tests.
    /// </summary>
    private class SampleDecoratedClass
    {
        [RequireAuthentication]
        public void ProtectedMethod() { }

        public void PublicMethod() { }
    }
}
