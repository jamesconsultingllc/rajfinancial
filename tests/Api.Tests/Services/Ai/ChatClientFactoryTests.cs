using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Services.Ai;
using RajFinancial.Api.Services.Ai.Abstractions;
using RajFinancial.Api.Tests.Services.Ai.Fakes;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Tests.Services.Ai;

public class ChatClientFactoryTests
{
    private static AiOptions OptionsWithAnthropic() => new()
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

    private static ChatClientFactory CreateFactory(
        AiOptions options,
        params IChatClientProvider[] providers) =>
        new(Options.Create(options), providers, NullLogger<ChatClientFactory>.Instance);

    [Fact]
    public void GetClient_returns_client_from_default_provider()
    {
        var fakeClient = new FakeChatClient("default");
        var provider = new FakeChatClientProvider(AiProviderId.Anthropic, _ => fakeClient);
        var factory = CreateFactory(OptionsWithAnthropic(), provider);

        var result = factory.GetClient();

        result.Should().BeSameAs(fakeClient);
        provider.CreateCallCount.Should().Be(1);
    }

    [Fact]
    public void GetClient_with_explicit_id_dispatches_to_matching_provider()
    {
        var fakeClient = new FakeChatClient("anthropic");
        var provider = new FakeChatClientProvider(AiProviderId.Anthropic, _ => fakeClient);
        var factory = CreateFactory(OptionsWithAnthropic(), provider);

        var result = factory.GetClient(AiProviderId.Anthropic);

        result.Should().BeSameAs(fakeClient);
    }

    [Fact]
    public void GetClient_passes_provider_specific_options_to_provider()
    {
        var provider = new FakeChatClientProvider(AiProviderId.Anthropic);
        var options = OptionsWithAnthropic();
        var factory = CreateFactory(options, provider);

        factory.GetClient();

        provider.ReceivedOptions.Should().ContainSingle()
            .Which.Should().BeSameAs(options.Providers[AiProviderId.Anthropic]);
    }

    [Fact]
    public void GetClient_caches_per_provider_id_across_calls()
    {
        var provider = new FakeChatClientProvider(AiProviderId.Anthropic);
        var factory = CreateFactory(OptionsWithAnthropic(), provider);

        var first = factory.GetClient();
        var second = factory.GetClient();
        var third = factory.GetClient(AiProviderId.Anthropic);

        first.Should().BeSameAs(second).And.BeSameAs(third);
        provider.CreateCallCount.Should().Be(1);
    }

    [Fact]
    public void GetClient_throws_when_provider_id_is_not_in_options()
    {
        // Options bind, but DefaultProvider points at an id that has no Providers entry.
        // The startup validator would catch this; the factory must also defend at
        // GetClient time so a bad config can't slip past lazy validation.
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
        var provider = new FakeChatClientProvider(AiProviderId.Anthropic);
        var factory = CreateFactory(options, provider);

        var act = () => factory.GetClient();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No configuration found for AI provider*");
    }

    [Fact]
    public void GetClient_throws_when_no_provider_implementation_is_registered()
    {
        var factory = CreateFactory(OptionsWithAnthropic());

        var act = () => factory.GetClient();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No IChatClientProvider implementation is registered*");
    }

    [Fact]
    public void Constructor_throws_when_two_providers_claim_the_same_id()
    {
        var first = new FakeChatClientProvider(AiProviderId.Anthropic);
        var second = new FakeChatClientProvider(AiProviderId.Anthropic);

        var act = () => CreateFactory(OptionsWithAnthropic(), first, second);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Multiple IChatClientProvider implementations*");
    }

    [Fact]
    public void GetClient_throws_when_provider_returns_null_client()
    {
        var provider = new FakeChatClientProvider(AiProviderId.Anthropic, _ => null!);
        var factory = CreateFactory(OptionsWithAnthropic(), provider);

        var act = () => factory.GetClient();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*returned a null IChatClient*");
    }

    [Fact]
    public void Dispose_disposes_all_cached_clients()
    {
        var fakeClient = new FakeChatClient();
        var provider = new FakeChatClientProvider(AiProviderId.Anthropic, _ => fakeClient);
        var factory = CreateFactory(OptionsWithAnthropic(), provider);

        _ = factory.GetClient();
        factory.Dispose();

        fakeClient.DisposeCallCount.Should().Be(1);
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        var fakeClient = new FakeChatClient();
        var provider = new FakeChatClientProvider(AiProviderId.Anthropic, _ => fakeClient);
        var factory = CreateFactory(OptionsWithAnthropic(), provider);

        _ = factory.GetClient();
        factory.Dispose();
        factory.Dispose();

        fakeClient.DisposeCallCount.Should().Be(1);
    }

    [Fact]
    public void GetClient_after_dispose_throws_object_disposed()
    {
        var provider = new FakeChatClientProvider(AiProviderId.Anthropic);
        var factory = CreateFactory(OptionsWithAnthropic(), provider);
        factory.Dispose();

        var act = () => factory.GetClient();

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_swallows_per_client_dispose_errors()
    {
        // A misbehaving client must not block disposal of the factory or sibling clients.
        var throwingClient = new ThrowingChatClient();
        var provider = new FakeChatClientProvider(AiProviderId.Anthropic, _ => throwingClient);
        var factory = CreateFactory(OptionsWithAnthropic(), provider);
        _ = factory.GetClient();

        var act = () => factory.Dispose();

        act.Should().NotThrow();
    }

    private sealed class ThrowingChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

#pragma warning disable S3877 // Throwing from Dispose is the intended behavior under test.
        public void Dispose()
        {
            throw new InvalidOperationException("dispose blew up");
        }
#pragma warning restore S3877
    }
}
