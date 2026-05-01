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
    private int _createCallCount;
    private readonly System.Collections.Concurrent.ConcurrentBag<AiProviderOptions> _receivedOptions = new();

    public FakeChatClientProvider(AiProviderId id, Func<AiProviderOptions, IChatClient>? factory = null)
    {
        Id = id;
        _factory = factory ?? (_ => new FakeChatClient(id.ToString()));
    }

    public AiProviderId Id { get; }

    public int CreateCallCount => Volatile.Read(ref _createCallCount);

    public IReadOnlyCollection<AiProviderOptions> ReceivedOptions => _receivedOptions;

    public IChatClient CreateClient(AiProviderOptions options)
    {
        Interlocked.Increment(ref _createCallCount);
        _receivedOptions.Add(options);
        return _factory(options);
    }
}
