using FluentAssertions;
using Microsoft.Extensions.Hosting;
using RajFinancial.Api.Services.Ai.Telemetry;

namespace RajFinancial.Api.Tests.Services.Ai.Telemetry;

public class AiTelemetryRedactorOptionsValidatorTests
{
    private sealed class StubEnvironment(string envName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = envName;
        public string ApplicationName { get; set; } = "RajFinancial.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public void Development_with_placeholder_secret_passes()
    {
        var validator = new AiTelemetryRedactorOptionsValidator(
            new StubEnvironment(Environments.Development));

        var result = validator.Validate(
            name: null,
            options: new AiTelemetryRedactorOptions
            {
                MerchantHashSecret = AiTelemetryRedactorOptions.DevPlaceholderSecret,
            });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Production_with_placeholder_secret_fails()
    {
        var validator = new AiTelemetryRedactorOptionsValidator(
            new StubEnvironment(Environments.Production));

        var result = validator.Validate(
            name: null,
            options: new AiTelemetryRedactorOptions
            {
                MerchantHashSecret = AiTelemetryRedactorOptions.DevPlaceholderSecret,
            });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MerchantHashSecret");
    }

    [Fact]
    public void Production_with_real_secret_passes()
    {
        var validator = new AiTelemetryRedactorOptionsValidator(
            new StubEnvironment(Environments.Production));

        var result = validator.Validate(
            name: null,
            options: new AiTelemetryRedactorOptions
            {
                MerchantHashSecret = new string('x', 32),
            });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Staging_with_placeholder_secret_fails()
    {
        var validator = new AiTelemetryRedactorOptionsValidator(
            new StubEnvironment("Staging"));

        var result = validator.Validate(
            name: null,
            options: new AiTelemetryRedactorOptions
            {
                MerchantHashSecret = AiTelemetryRedactorOptions.DevPlaceholderSecret,
            });

        result.Failed.Should().BeTrue();
    }
}
