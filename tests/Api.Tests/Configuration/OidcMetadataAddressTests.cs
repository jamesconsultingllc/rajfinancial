using FluentAssertions;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Tests.Configuration;

public class OidcMetadataAddressTests
{
    [Theory]
    [InlineData("https://login.microsoftonline.com/", "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000001/v2.0/.well-known/openid-configuration")]
    [InlineData("https://login.microsoftonline.com", "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000001/v2.0/.well-known/openid-configuration")]
    public void Build_NormalizesTrailingSlashOnInstance(string instance, string expected)
    {
        var options = new EntraExternalIdOptions
        {
            Instance = instance,
            TenantId = "00000000-0000-0000-0000-000000000001",
        };

        OidcMetadataAddress.Build(options).Should().Be(expected);
    }

    [Theory]
    [InlineData("", "tid")]
    [InlineData("   ", "tid")]
    [InlineData("https://login.microsoftonline.com/", "")]
    [InlineData("https://login.microsoftonline.com/", "   ")]
    public void Build_ThrowsWhenInstanceOrTenantMissing(string instance, string tenantId)
    {
        var options = new EntraExternalIdOptions { Instance = instance, TenantId = tenantId };

        var act = () => OidcMetadataAddress.Build(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Instance*TenantId*");
    }

    [Fact]
    public void Build_ThrowsWhenInstanceIsNotAbsoluteHttpsUrl()
    {
        var options = new EntraExternalIdOptions
        {
            Instance = "login.microsoftonline.com/",
            TenantId = "00000000-0000-0000-0000-000000000001",
        };

        var act = () => OidcMetadataAddress.Build(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a valid absolute https URL*");
    }

    [Fact]
    public void Build_RejectsHttpScheme()
    {
        var options = new EntraExternalIdOptions
        {
            Instance = "http://login.microsoftonline.com/",
            TenantId = "00000000-0000-0000-0000-000000000001",
        };

        var act = () => OidcMetadataAddress.Build(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a valid absolute https URL*");
    }

    [Fact]
    public void Build_ThrowsForNullOptions()
    {
        var act = () => OidcMetadataAddress.Build(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
