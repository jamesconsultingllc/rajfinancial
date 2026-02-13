using FluentAssertions;
using RajFinancial.Api.Configuration;

namespace RajFinancial.UnitTests.Api.Configuration;

/// <summary>
/// Unit tests for <see cref="AppRoleOptions"/>.
/// </summary>
public class AppRoleOptionsTests
{
    private readonly AppRoleOptions sut;
    private readonly Guid clientRoleId = Guid.NewGuid();
    private readonly Guid administratorRoleId = Guid.NewGuid();

    public AppRoleOptionsTests()
    {
        sut = new AppRoleOptions
        {
            Client = clientRoleId,
            Administrator = administratorRoleId
        };
    }

    [Fact]
    public void SectionName_ShouldBeAppRoles()
    {
        // Assert
        AppRoleOptions.SectionName.Should().Be("AppRoles");
    }

    [Theory]
    [InlineData("Client")]
    [InlineData("CLIENT")]
    [InlineData("client")]
    [InlineData("ClIeNt")]
    public void GetRoleId_WithClientRole_ReturnsClientGuid(string roleName)
    {
        // Act
        var result = sut.GetRoleId(roleName);

        // Assert
        result.Should().Be(clientRoleId);
    }

    [Theory]
    [InlineData("Administrator")]
    [InlineData("ADMINISTRATOR")]
    [InlineData("administrator")]
    [InlineData("AdMiNiStRaToR")]
    public void GetRoleId_WithAdministratorRole_ReturnsAdministratorGuid(string roleName)
    {
        // Act
        var result = sut.GetRoleId(roleName);

        // Assert
        result.Should().Be(administratorRoleId);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData("SuperAdmin")]
    [InlineData("User")]
    public void GetRoleId_WithUnknownRole_ReturnsNull(string roleName)
    {
        // Act
        var result = sut.GetRoleId(roleName);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Client", true)]
    [InlineData("Administrator", true)]
    [InlineData("Unknown", false)]
    [InlineData("", false)]
    public void IsValidRole_ReturnsExpectedResult(string roleName, bool expected)
    {
        // Act
        var result = sut.IsValidRole(roleName);

        // Assert
        result.Should().Be(expected);
    }
}
