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
/// <b>Concurrency:</b> The cache stores <see cref="Lazy{T}"/> entries with
/// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>, so
/// <see cref="IChatClientProvider.CreateClient"/> is invoked at most once per provider id
/// even under concurrent first access — matching the contract on
/// <see cref="IChatClientProvider.CreateClient"/>. A client created concurrently with
/// <see cref="Dispose"/> is detected post-publication and disposed immediately so no
/// <see cref="IChatClient"/> ever leaks past factory teardown.
/// </para>
/// <para>
/// <b>Security (OWASP A04 / A09):</b> The factory never resolves API keys, and never logs
/// secret or sensitive configuration values. It logs only non-secret diagnostic values —
/// provider id and model name — both already public configuration.
/// </para>
/// </remarks>
internal sealed partial class ChatClientFactory : IChatClientFactory, IDisposable
{
    private readonly AiOptions _options;
    private readonly IReadOnlyDictionary<AiProviderId, IChatClientProvider> _providers;
    private readonly ConcurrentDictionary<AiProviderId, Lazy<IChatClient>> _clients = new();
    private readonly object _gate = new();
    private readonly ILogger<ChatClientFactory> _logger;
    private int _disposedFlag;

    private bool IsDisposed => Volatile.Read(ref _disposedFlag) != 0;

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

        // Materialize providers into a dictionary once. Duplicate provider ids are rejected
        // (see ctor below) — there is no last-registration-wins fallback.
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
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (_options.Providers is null)
        {
            LogProviderConfigMissing(providerId);
            throw new InvalidOperationException(
                $"{AiOptions.SectionName}:Providers is not configured. " +
                $"Add an entry under {AiOptions.SectionName}:Providers:{providerId}.");
        }

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

        // Fast path: client already created and published, no synchronization needed.
        if (_clients.TryGetValue(providerId, out var existing) && existing.IsValueCreated)
        {
            return existing.Value;
        }

        // Slow path: creation is serialized against Dispose so a client can never be
        // published into _clients after Dispose has drained. Lazy<T>(ExecutionAndPublication)
        // additionally guarantees CreateClient is invoked at most once per provider id —
        // matching the contract on IChatClientProvider.CreateClient.
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            var lazy = _clients.GetOrAdd(
                providerId,
                id => new Lazy<IChatClient>(
                    () =>
                    {
                        var client = provider.CreateClient(providerOptions);
                        if (client is null)
                        {
                            throw new InvalidOperationException(
                                $"IChatClientProvider for '{id}' returned a null IChatClient.");
                        }

                        LogProviderClientCreated(id, providerOptions.Model);
                        return client;
                    },
                    LazyThreadSafetyMode.ExecutionAndPublication));

            return lazy.Value;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedFlag, 1) != 0)
        {
            return;
        }

        // Block until any in-flight creation finishes, then dispose every cached client.
        // Anything entering GetClient after the flag flip throws ObjectDisposedException
        // before publishing a Lazy entry, so the cache cannot grow after this point.
        lock (_gate)
        {
            foreach (var kvp in _clients)
            {
                if (!kvp.Value.IsValueCreated)
                {
                    continue;
                }

                try
                {
                    kvp.Value.Value.Dispose();
                }
                catch (Exception ex)
                {
                    LogProviderClientDisposeFailed(ex, kvp.Key);
                }
            }

            _clients.Clear();
        }
    }
}
