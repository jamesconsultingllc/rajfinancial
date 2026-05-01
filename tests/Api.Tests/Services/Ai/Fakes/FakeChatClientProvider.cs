using Microsoft.Extensions.AI;
using RajFinancial.Api.Services.Ai.Abstractions;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Tests.Services.Ai.Fakes;

/// <summary>
/// Hand-rolled <see cref="IChatClientProvider"/> stub. Records every call to
/// <see cref="CreateClient"/> so factory caching behavior is verifiable.
/// </summary>
internal sealed class FakeChatClientProvider : IChatClientProvider
{
    private readonly Func<AiProviderOptions, IChatClient> _factory;

    public FakeChatClientProvider(AiProviderId id, Func<AiProviderOptions, IChatClient>? factory = null)
    {
        Id = id;
        _factory = factory ?? (_ => new FakeChatClient(id.ToString()));
    }

    public AiProviderId Id { get; }

    public int CreateCallCount { get; private set; }

    public List<AiProviderOptions> ReceivedOptions { get; } = new();

    public IChatClient CreateClient(AiProviderOptions options)
    {
        CreateCallCount++;
        ReceivedOptions.Add(options);
        return _factory(options);
    }
}
