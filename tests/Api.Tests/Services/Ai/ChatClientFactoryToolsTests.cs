using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Services.Ai;
using RajFinancial.Api.Services.Ai.Abstractions;
using RajFinancial.Api.Services.Ai.Telemetry;
using RajFinancial.Api.Services.Ai.Tools;
using RajFinancial.Api.Tests.Services.Ai.Fakes;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Tests.Services.Ai;

public class ChatClientFactoryToolsTests
{
    private static AiOptions Options() => new()
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

    private static IAiTelemetryRedactor Redactor() => new DefaultAiTelemetryRedactor(
        Microsoft.Extensions.Options.Options.Create(new AiTelemetryRedactorOptions
        {
            MerchantHashSecret = new string('p', 32),
            HmacPrefixLength = 12,
        }));

    [Fact]
    public void Empty_registry_returns_provider_client_unwrapped()
    {
        var fake = new FakeChatClient("default");
        var factory = new ChatClientFactory(
            Microsoft.Extensions.Options.Options.Create(Options()),
            [new FakeChatClientProvider(AiProviderId.Anthropic, _ => fake)],
            EmptyAiToolRegistry.Instance,
            NullLogger<ChatClientFactory>.Instance);

        var client = factory.GetClient();

        // FakeChatClient is what the provider returned; with empty registry no wrapping occurs.
        client.Should().BeSameAs(fake);
    }

    [Fact]
    public void Non_empty_registry_wraps_client_with_function_invocation()
    {
        var fake = new FakeChatClient("default");
        var d = new AiToolDescriptor(
            AiToolScopes.Diagnostics,
            "diag.ping",
            _ => AIFunctionFactory.Create(() => "pong", name: "diag.ping"));
        var registry = new AiToolRegistry(
            [d],
            new ServiceCollection().BuildServiceProvider(),
            Redactor());

        var factory = new ChatClientFactory(
            Microsoft.Extensions.Options.Options.Create(Options()),
            [new FakeChatClientProvider(AiProviderId.Anthropic, _ => fake)],
            registry,
            NullLogger<ChatClientFactory>.Instance);

        var client = factory.GetClient();

        // UseFunctionInvocation wraps the inner client — the returned reference is no longer the fake.
        client.Should().NotBeSameAs(fake);
    }
}
