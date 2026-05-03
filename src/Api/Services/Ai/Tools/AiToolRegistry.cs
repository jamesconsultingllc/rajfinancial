using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.Extensions.AI;
using RajFinancial.Api.Services.Ai.Telemetry;

namespace RajFinancial.Api.Services.Ai.Tools;

/// <summary>
/// Default <see cref="IAiToolRegistry"/>. Built once at DI construction from every
/// <see cref="AiToolDescriptor"/> registered with <see cref="AiToolServiceCollectionExtensions.AddAiTool"/>.
/// </summary>
/// <remarks>
/// <para>
/// At construction:
/// <list type="number">
///   <item>Validates non-empty scope/name on each descriptor (defense-in-depth — the record
///   constructor already throws if the original registration uses null/whitespace).</item>
///   <item>Invokes each descriptor's <c>Factory</c> exactly once with the root
///   <see cref="IServiceProvider"/>. A factory returning <c>null</c> is a fatal
///   configuration bug and throws.</item>
///   <item>Wraps the resulting <see cref="AIFunction"/> in a <see cref="TelemetryAIFunction"/>
///   so per-invocation telemetry is automatic for every registered tool.</item>
///   <item>Detects duplicate <c>(scope, name)</c> pairs and throws —
///   <see cref="InvalidOperationException"/>. The chat model would silently pick one of the
///   duplicates otherwise.</item>
///   <item>Snapshots into <see cref="ImmutableArray{T}"/> + a <see cref="FrozenDictionary{TKey,TValue}"/>
///   keyed by scope. From this point forward the registry is immutable.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class AiToolRegistry : IAiToolRegistry
{
    private readonly ImmutableArray<AIFunction> _all;
    private readonly FrozenDictionary<string, ImmutableArray<AIFunction>> _byScope;

    public AiToolRegistry(
        IEnumerable<AiToolDescriptor> descriptors,
        IServiceProvider rootServices,
        IAiTelemetryRedactor redactor)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        ArgumentNullException.ThrowIfNull(rootServices);
        ArgumentNullException.ThrowIfNull(redactor);

        var allBuilder = ImmutableArray.CreateBuilder<AIFunction>();
        var scopeBuilders = new Dictionary<string, ImmutableArray<AIFunction>.Builder>(StringComparer.Ordinal);
        var seen = new HashSet<(string Scope, string Name)>();

        foreach (var descriptor in descriptors)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.Scope);
            ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.Name);

            if (!seen.Add((descriptor.Scope, descriptor.Name)))
            {
                throw new InvalidOperationException(
                    $"Duplicate AI tool registration: scope='{descriptor.Scope}', name='{descriptor.Name}'.");
            }

            var raw = descriptor.Factory(rootServices)
                ?? throw new InvalidOperationException(
                    $"AI tool factory returned null for scope='{descriptor.Scope}', name='{descriptor.Name}'.");

            // Defensive: factory must produce a tool whose Name matches the descriptor.
            // Mismatch would cause the model to select a tool by one name and the host to
            // dispatch by another — silent breakage. Easier to throw at boot.
            if (!string.Equals(raw.Name, descriptor.Name, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"AI tool factory produced AIFunction.Name='{raw.Name}' but was registered as '{descriptor.Name}'.");
            }

            var wrapped = new TelemetryAIFunction(raw, redactor, descriptor.Scope);
            allBuilder.Add(wrapped);

            if (!scopeBuilders.TryGetValue(descriptor.Scope, out var scopeList))
            {
                scopeList = ImmutableArray.CreateBuilder<AIFunction>();
                scopeBuilders[descriptor.Scope] = scopeList;
            }
            scopeList.Add(wrapped);
        }

        _all = allBuilder.ToImmutable();
        _byScope = scopeBuilders.ToFrozenDictionary(
            kv => kv.Key,
            kv => kv.Value.ToImmutable(),
            StringComparer.Ordinal);
    }

    public bool IsEmpty => _all.IsEmpty;

    public int Count => _all.Length;

    public IReadOnlyList<AIFunction> GetAll() => _all;

    public IReadOnlyList<AIFunction> GetByScope(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        return _byScope.TryGetValue(scope, out var list) ? list : ImmutableArray<AIFunction>.Empty;
    }
}
