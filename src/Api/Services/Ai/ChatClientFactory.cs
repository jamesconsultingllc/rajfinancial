using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Services.Ai.Abstractions;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Services.Ai;

/// <summary>
/// Default <see cref="IChatClientFactory"/> implementation. Dispatches to a registered
/// <see cref="IChatClientProvider"/> for the requested <see cref="AiProviderId"/> and
/// caches the resulting <see cref="IChatClient"/> for the lifetime of the factory.
/// </summary>
/// <remarks>
/// <para>
/// <b>Lifetime:</b> Singleton. Cached <see cref="IChatClient"/> instances are disposed
/// when the factory is disposed (i.e., on host shutdown).
/// </para>
/// <para>
/// <b>Concurrency:</b> The cache is a <see cref="ConcurrentDictionary{TKey, TValue}"/>;
/// concurrent <c>GetClient</c> calls for the same provider id return the same instance
/// (modulo <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd"/> semantics: the value
/// factory may be invoked more than once under contention, but only one result is cached).
/// </para>
/// <para>
/// <b>Security (OWASP A04 / A09):</b> The factory never resolves API keys, never logs
/// configuration values, and never logs secret material. It logs only provider id and
/// model name — both already public configuration.
/// </para>
/// </remarks>
internal sealed partial class ChatClientFactory : IChatClientFactory, IDisposable
{
    private readonly AiOptions _options;
    private readonly IReadOnlyDictionary<AiProviderId, IChatClientProvider> _providers;
    private readonly ConcurrentDictionary<AiProviderId, IChatClient> _clients = new();
    private readonly ILogger<ChatClientFactory> _logger;
    private bool _disposed;

    public ChatClientFactory(
        IOptions<AiOptions> options,
        IEnumerable<IChatClientProvider> providers,
        ILogger<ChatClientFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value
            ?? throw new InvalidOperationException("AiOptions is null. Ensure AddRajFinancialAi(...) is called during startup.");
        _logger = logger;

        // Materialize providers into a dictionary once. Last-registration-wins semantics if
        // multiple providers claim the same id (DI registration order); validated below.
        var byId = new Dictionary<AiProviderId, IChatClientProvider>();
        foreach (var provider in providers)
        {
            if (byId.ContainsKey(provider.Id))
            {
                throw new InvalidOperationException(
                    $"Multiple IChatClientProvider implementations registered for {provider.Id}. " +
                    "Only one provider per AiProviderId is allowed.");
            }

            byId[provider.Id] = provider;
        }

        _providers = byId;
    }

    public IChatClient GetClient() => GetClient(_options.DefaultProvider);

    public IChatClient GetClient(AiProviderId providerId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_options.Providers.TryGetValue(providerId, out var providerOptions) || providerOptions is null)
        {
            LogProviderConfigMissing(providerId);
            throw new InvalidOperationException(
                $"No configuration found for AI provider '{providerId}'. " +
                $"Add an entry under {AiOptions.SectionName}:Providers:{providerId}.");
        }

        if (!_providers.TryGetValue(providerId, out var provider))
        {
            LogProviderImplMissing(providerId);
            throw new InvalidOperationException(
                $"No IChatClientProvider implementation is registered for AI provider '{providerId}'. " +
                "Ensure the provider's adapter package is wired in DI.");
        }

        return _clients.GetOrAdd(providerId, id =>
        {
            var client = provider.CreateClient(providerOptions);
            if (client is null)
            {
                throw new InvalidOperationException(
                    $"IChatClientProvider for '{id}' returned a null IChatClient.");
            }

            LogProviderClientCreated(id, providerOptions.Model);
            return client;
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Drain the cache and dispose each client. Swallow per-client dispose errors so a
        // single bad provider doesn't block disposal of the others; surface via logs.
        foreach (var kvp in _clients)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch (Exception ex)
            {
                LogProviderClientDisposeFailed(ex, kvp.Key);
            }
        }

        _clients.Clear();
    }
}
