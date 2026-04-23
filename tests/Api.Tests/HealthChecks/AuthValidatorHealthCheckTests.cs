using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.HealthChecks;
using RajFinancial.Api.Services.Auth;
using RajFinancial.Shared.HealthContract;

namespace RajFinancial.Api.Tests.HealthChecks;

public class AuthValidatorHealthCheckTests
{
    private static readonly Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration Registration = new(
        HealthCheckContract.AuthValidatorCheckName,
        Mock.Of<IHealthCheck>(),
        HealthStatus.Unhealthy,
        tags: null);

    private static HealthCheckContext CreateContext() => new() { Registration = Registration };

    private static IJwtBearerValidator CreateProductionValidator()
    {
        var options = Options.Create(new EntraExternalIdOptions
        {
            Instance = "https://example.ciamlogin.com/",
            TenantId = Guid.NewGuid().ToString(),
            ClientId = Guid.NewGuid().ToString(),
            ValidAudiences = new List<string> { "api://example" },
        });
        var configManager = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>().Object;
        return new JwtBearerValidator(configManager, options, new NullLogger<JwtBearerValidator>());
    }

    private static IJwtBearerValidator CreateLocalValidator() =>
        new LocalUnsignedJwtValidator(new NullLogger<LocalUnsignedJwtValidator>());

    [Fact]
    public async Task Healthy_When_Local_Validator_Selected_Regardless_Of_Audiences()
    {
        var sut = new AuthValidatorHealthCheck(
            CreateLocalValidator(),
            Options.Create(new EntraExternalIdOptions { ValidAudiences = new List<string>() }));

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data[AuthValidatorNames.DataKey].Should().Be(AuthValidatorNames.UnsignedLocal);
    }

    [Fact]
    public async Task Healthy_When_Production_Validator_With_Usable_Audience()
    {
        var sut = new AuthValidatorHealthCheck(
            CreateProductionValidator(),
            Options.Create(new EntraExternalIdOptions { ValidAudiences = new List<string> { "api://example" } }));

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data[AuthValidatorNames.DataKey].Should().Be(AuthValidatorNames.Jwt);
    }

    [Fact]
    public async Task Unhealthy_When_Production_Validator_With_Empty_Audiences()
    {
        var sut = new AuthValidatorHealthCheck(
            CreateProductionValidator(),
            Options.Create(new EntraExternalIdOptions { ValidAudiences = new List<string>() }));

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain(ConfigurationKeys.EntraValidAudiences);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(ConfigurationKeys.PlaceholderValue)]
    public async Task Unhealthy_When_Production_Validator_With_Placeholder_Or_Whitespace_Audience(string entry)
    {
        // Defensive: a single junk entry with Count==1 must NOT fool the probe into reporting Healthy,
        // because JwtBearerValidator would still reject every incoming token.
        var sut = new AuthValidatorHealthCheck(
            CreateProductionValidator(),
            Options.Create(new EntraExternalIdOptions { ValidAudiences = new List<string> { entry } }));

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain(ConfigurationKeys.EntraValidAudiences);
    }

    [Fact]
    public async Task Healthy_When_Production_Validator_Has_Mix_Of_Junk_And_Real_Audience()
    {
        var sut = new AuthValidatorHealthCheck(
            CreateProductionValidator(),
            Options.Create(new EntraExternalIdOptions
            {
                ValidAudiences = new List<string> { ConfigurationKeys.PlaceholderValue, "api://example" }
            }));

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
