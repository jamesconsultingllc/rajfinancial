using FluentAssertions;
using RajFinancial.Api.Services.Ai;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Tests.Services.Ai;

public class AiOptionsValidatorTests
{
    private readonly AiOptionsValidator _validator = new();

    private static AiOptions ValidOptions() => new()
    {
        DefaultProvider = AiProviderId.Anthropic,
        Providers = new Dictionary<AiProviderId, AiProviderOptions>
        {
            [AiProviderId.Anthropic] = new()
            {
                Model = "claude-sonnet-4-5",
                ApiKeyEnvVar = "ANTHROPIC_API_KEY",
            },
        },
    };

    [Fact]
    public void Validate_passes_when_options_are_valid()
    {
        var result = _validator.Validate(name: null, ValidOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_fails_when_providers_dictionary_is_empty()
    {
        var options = new AiOptions
        {
            DefaultProvider = AiProviderId.Anthropic,
            Providers = new Dictionary<AiProviderId, AiProviderOptions>(),
        };

        var result = _validator.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Providers must contain at least one"));
    }

    [Fact]
    public void Validate_fails_when_default_provider_not_in_providers()
    {
        var options = new AiOptions
        {
            DefaultProvider = (AiProviderId)999,
            Providers = new Dictionary<AiProviderId, AiProviderOptions>
            {
                [AiProviderId.Anthropic] = new()
                {
                    Model = "claude-sonnet-4-5",
                    ApiKeyEnvVar = "ANTHROPIC_API_KEY",
                },
            },
        };

        var result = _validator.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DefaultProvider"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_fails_when_provider_model_is_blank(string? model)
    {
        var options = ValidOptions();
        options.Providers[AiProviderId.Anthropic].Model = model!;

        var result = _validator.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(":Model is required"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_fails_when_provider_apikeyenvvar_is_blank(string? envVar)
    {
        var options = ValidOptions();
        options.Providers[AiProviderId.Anthropic].ApiKeyEnvVar = envVar!;

        var result = _validator.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(":ApiKeyEnvVar is required"));
    }

    [Fact]
    public void Validate_collects_all_failures_in_one_pass()
    {
        var options = ValidOptions();
        options.Providers[AiProviderId.Anthropic].Model = "";
        options.Providers[AiProviderId.Anthropic].ApiKeyEnvVar = "";

        var result = _validator.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(2);
    }

    [Fact]
    public void Validate_throws_when_options_is_null()
    {
        var act = () => _validator.Validate(name: null, options: null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
