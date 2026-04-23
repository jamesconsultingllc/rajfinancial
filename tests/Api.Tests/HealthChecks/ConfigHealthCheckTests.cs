using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.HealthChecks;

namespace RajFinancial.Api.Tests.HealthChecks;

/// <summary>
///     Unit tests for <see cref="ConfigHealthCheck"/>.
/// </summary>
/// <remarks>
///     The info-disclosure boundary (Dev vs non-Dev description shape) is security-sensitive:
///     <c>/health/ready</c> is publicly reachable and must not enumerate missing config keys
///     to anonymous callers outside Development. These tests pin that boundary.
/// </remarks>
public class ConfigHealthCheckTests
{
    private static readonly HealthCheckContext ReadyContext = new()
    {
        Registration = new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
            "config",
            instance: new NoopHealthCheck(),
            failureStatus: HealthStatus.Unhealthy,
            tags: null),
    };

    [Fact]
    public async Task Healthy_When_All_Required_Keys_Present_In_Dev()
    {
        var sut = CreateSut(AllPopulated(), isDevelopment: true);

        var result = await sut.CheckHealthAsync(ReadyContext);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Unhealthy_When_Required_Key_Missing_In_Dev_Description_Names_The_Key()
    {
        var settings = AllPopulated();
        settings[ConfigurationKeys.EntraDomain] = null;

        var sut = CreateSut(settings, isDevelopment: true);

        var result = await sut.CheckHealthAsync(ReadyContext);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain(ConfigurationKeys.EntraDomain,
            "Dev output is meant to help developers — name the missing key explicitly.");
    }

    [Fact]
    public async Task Unhealthy_When_Required_Key_Is_Placeholder_Value()
    {
        var settings = AllPopulated();
        settings[ConfigurationKeys.AppRoleClient] = "<SET-IN-ENVIRONMENT>";

        var sut = CreateSut(settings, isDevelopment: true);

        var result = await sut.CheckHealthAsync(ReadyContext);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain(ConfigurationKeys.AppRoleClient);
    }

    [Fact]
    public async Task Unhealthy_When_ApplicationInsights_Missing_In_NonDev()
    {
        var settings = AllPopulated();
        settings[ConfigurationKeys.ApplicationInsightsConnectionString] = null;

        var sut = CreateSut(settings, isDevelopment: false);

        var result = await sut.CheckHealthAsync(ReadyContext);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task NonDev_Description_Does_Not_Leak_Configuration_Key_Names()
    {
        // Security invariant: /health/ready is Anonymous; outside Dev we must NOT
        // enumerate which keys are missing. Only a generic count is allowed.
        var settings = AllPopulated();
        settings[ConfigurationKeys.EntraInstance] = null;
        settings[ConfigurationKeys.AppRoleAdvisor] = null;
        settings[ConfigurationKeys.ApplicationInsightsConnectionString] = null;

        var sut = CreateSut(settings, isDevelopment: false);

        var result = await sut.CheckHealthAsync(ReadyContext);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().NotContain(ConfigurationKeys.EntraInstance);
        result.Description.Should().NotContain(ConfigurationKeys.AppRoleAdvisor);
        result.Description.Should().NotContain(ConfigurationKeys.ApplicationInsightsConnectionString);
        result.Description.Should().Contain("3");
    }

    [Fact]
    public async Task Uses_FailureStatus_From_Registration()
    {
        var settings = AllPopulated();
        settings[ConfigurationKeys.AppRoleAdministrator] = null;

        var sut = CreateSut(settings, isDevelopment: true);

        var degradedContext = new HealthCheckContext
        {
            Registration = new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                "config",
                instance: new NoopHealthCheck(),
                failureStatus: HealthStatus.Degraded,
                tags: null),
        };

        var result = await sut.CheckHealthAsync(degradedContext);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    private static ConfigHealthCheck CreateSut(
        IDictionary<string, string?> settings,
        bool isDevelopment)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var environment = new TestHostEnvironment(
            isDevelopment ? Environments.Development : Environments.Production);

        return new ConfigHealthCheck(
            configuration,
            environment,
            NullLogger<ConfigHealthCheck>.Instance);
    }

    private static Dictionary<string, string?> AllPopulated() => new()
    {
        [ConfigurationKeys.EntraInstance] = "https://rajfinancial.ciamlogin.com/",
        [ConfigurationKeys.EntraDomain] = "rajfinancial.onmicrosoft.com",
        [ConfigurationKeys.EntraTenantId] = Guid.NewGuid().ToString(),
        [ConfigurationKeys.EntraClientId] = Guid.NewGuid().ToString(),
        [ConfigurationKeys.AppRoleClient] = Guid.NewGuid().ToString(),
        [ConfigurationKeys.AppRoleAdministrator] = Guid.NewGuid().ToString(),
        [ConfigurationKeys.AppRoleAdvisor] = Guid.NewGuid().ToString(),
        [ConfigurationKeys.ApplicationInsightsConnectionString] =
            "InstrumentationKey=00000000-0000-0000-0000-000000000000",
    };

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "RajFinancial.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class NoopHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }
}
