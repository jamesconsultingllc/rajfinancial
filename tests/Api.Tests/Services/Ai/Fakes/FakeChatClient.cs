using Microsoft.Extensions.AI;

namespace RajFinancial.Api.Tests.Services.Ai.Fakes;

/// <summary>
/// Minimal hand-rolled <see cref="IChatClient"/> stub for factory tests. We only need
/// reference identity (to verify the factory dispatched to the correct provider) and a
/// way to observe disposal — actual chat behavior is not exercised here.
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
    private int _disposeCallCount;

    public string Tag { get; }
    public int DisposeCallCount => Volatile.Read(ref _disposeCallCount);

    public FakeChatClient(string tag = "fake")
    {
        Tag = tag;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Factory tests do not exercise chat behavior.");

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Factory tests do not exercise chat behavior.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() => Interlocked.Increment(ref _disposeCallCount);
}
