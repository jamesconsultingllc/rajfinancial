using System.Reflection;
using FluentAssertions;
using RajFinancial.Api.Middleware.Authorization;

namespace RajFinancial.Api.Tests.Middleware;

/// <summary>
/// Unit tests for <see cref="RequireRoleAttribute"/>.
/// Validates the attribute stores roles, enforces at least one role,
/// can target methods and classes, and is discoverable via reflection.
/// </summary>
public class RequireRoleAttributeTests
{
    [Fact]
    public void Constructor_WithSingleRole_StoresRole()
    {
        // Act
        var attribute = new RequireRoleAttribute("Administrator");

        // Assert
        attribute.Roles.Should().ContainSingle().Which.Should().Be("Administrator");
    }

    [Fact]
    public void Constructor_WithMultipleRoles_StoresAllRoles()
    {
        // Act
        var attribute = new RequireRoleAttribute("Administrator", "Client");

        // Assert
        attribute.Roles.Should().HaveCount(2);
        attribute.Roles.Should().Contain("Administrator");
        attribute.Roles.Should().Contain("Client");
    }

    [Fact]
    public void Constructor_WithNoRoles_ThrowsArgumentException()
    {
        // Act
        var act = () => new RequireRoleAttribute();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("roles")
            .WithMessage("*At least one role must be specified*");
    }

    [Fact]
    public void Constructor_WithNullRoles_ThrowsArgumentException()
    {
        // Act
        var act = () => new RequireRoleAttribute(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("roles");
    }

    [Fact]
    public void Attribute_InheritsFromAttribute()
    {
        // Act
        var attribute = new RequireRoleAttribute("Client");

        // Assert
        attribute.Should().BeAssignableTo<Attribute>();
    }

    [Fact]
    public void Attribute_CanBeAppliedToMethods()
    {
        // Arrange
        var usage = typeof(RequireRoleAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.Should().NotBeNull();
        (usage!.ValidOn & AttributeTargets.Method).Should().Be(AttributeTargets.Method);
    }

    [Fact]
    public void Attribute_CanBeAppliedToClasses()
    {
        // Arrange
        var usage = typeof(RequireRoleAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.Should().NotBeNull();
        (usage!.ValidOn & AttributeTargets.Class).Should().Be(AttributeTargets.Class);
    }

    [Fact]
    public void Attribute_IsNotInherited()
    {
        // Arrange — each function should explicitly opt in
        var usage = typeof(RequireRoleAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.Should().NotBeNull();
        usage!.Inherited.Should().BeFalse();
    }

    [Fact]
    public void Attribute_DoesNotAllowMultiple()
    {
        // Arrange
        var usage = typeof(RequireRoleAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.Should().NotBeNull();
        usage!.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void Attribute_IsDiscoverableOnDecoratedMethod_WithSingleRole()
    {
        // Arrange
        var method = typeof(SampleDecoratedClass)
            .GetMethod(nameof(SampleDecoratedClass.AdminOnlyMethod));

        // Act
        var attribute = method?.GetCustomAttribute<RequireRoleAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Roles.Should().ContainSingle().Which.Should().Be("Administrator");
    }

    [Fact]
    public void Attribute_IsDiscoverableOnDecoratedMethod_WithMultipleRoles()
    {
        // Arrange
        var method = typeof(SampleDecoratedClass)
            .GetMethod(nameof(SampleDecoratedClass.MultiRoleMethod));

        // Act
        var attribute = method?.GetCustomAttribute<RequireRoleAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Roles.Should().HaveCount(2);
        attribute.Roles.Should().Contain("Administrator");
        attribute.Roles.Should().Contain("Client");
    }

    [Fact]
    public void Attribute_IsNotPresentOnUndecoratedMethod()
    {
        // Arrange
        var method = typeof(SampleDecoratedClass)
            .GetMethod(nameof(SampleDecoratedClass.PublicMethod));

        // Act
        var attribute = method?.GetCustomAttribute<RequireRoleAttribute>();

        // Assert
        attribute.Should().BeNull();
    }

    [Fact]
    public void Attribute_IsDiscoverableOnDecoratedClass()
    {
        // Act
        var attribute = typeof(SampleAdminClass)
            .GetCustomAttribute<RequireRoleAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Roles.Should().ContainSingle().Which.Should().Be("Administrator");
    }

    [Fact]
    public void Roles_PreservesOriginalOrder()
    {
        // Act
        var attribute = new RequireRoleAttribute("Client", "Administrator", "Professional");

        // Assert
        attribute.Roles.Should().ContainInOrder("Client", "Administrator", "Professional");
    }

    /// <summary>
    /// Helper class for reflection-based tests.
    /// </summary>
    private class SampleDecoratedClass
    {
        [RequireRole("Administrator")]
        public void AdminOnlyMethod() { }

        [RequireRole("Administrator", "Client")]
        public void MultiRoleMethod() { }

        public void PublicMethod() { }
    }

    /// <summary>
    /// Helper class for class-level attribute tests.
    /// </summary>
    [RequireRole("Administrator")]
    private class SampleAdminClass;
}
