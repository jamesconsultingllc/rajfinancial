using Microsoft.Extensions.AI;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Services.Ai.Abstractions;

/// <summary>
/// Per-provider strategy that knows how to construct a <see cref="IChatClient"/> from
/// configuration. Factory implementations dispatch on <see cref="Id"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam by which new AI providers come online: each provider task adds a
/// concrete <c>IChatClientProvider</c> implementation that wraps the provider's SDK
/// (e.g., Anthropic in Task #545). The factory itself stays provider-agnostic.
/// </para>
/// <para>
/// <b>Implementation contract:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Implementations are registered as singletons in DI.</description></item>
///   <item><description><see cref="CreateClient"/> resolves any required secret material
///   (e.g., API keys via <see cref="AiProviderOptions.ApiKeyEnvVar"/>) at the moment of
///   construction. The factory never sees secret values; only the env-var <i>name</i> flows
///   through configuration. This keeps secret-touching code confined to the provider.</description></item>
///   <item><description>Implementations may throw <see cref="InvalidOperationException"/>
///   if the supplied options do not satisfy provider-specific requirements (e.g., model
///   not supported, env-var not set). The factory surfaces these to callers unchanged.</description></item>
///   <item><description>The returned <see cref="IChatClient"/> may be a cached instance
///   shared across factory consumers; do not assume per-call uniqueness.</description></item>
/// </list>
/// </remarks>
public interface IChatClientProvider
{
    /// <summary>
    /// The provider this strategy services. Used by <see cref="IChatClientFactory"/> to
    /// dispatch <see cref="IChatClientFactory.GetClient(AiProviderId)"/> requests.
    /// </summary>
    AiProviderId Id { get; }

    /// <summary>
    /// Constructs a <see cref="IChatClient"/> for this provider using the supplied
    /// configuration. Called at most once per provider id by the factory; the result is
    /// cached for the lifetime of the factory.
    /// </summary>
    /// <param name="options">Per-provider configuration bound from
    /// <c>Ai:Providers:&lt;ProviderId&gt;</c>. Will not be <see langword="null"/>.</param>
    /// <returns>A configured <see cref="IChatClient"/> ready for use. Must not be
    /// <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The supplied <paramref name="options"/> are incomplete or the provider's required
    /// secrets are unavailable (e.g., <see cref="AiProviderOptions.ApiKeyEnvVar"/> names
    /// an env var that is not set).
    /// </exception>
    IChatClient CreateClient(AiProviderOptions options);
}
