using Microsoft.Extensions.AI;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Services.Ai.Abstractions;

/// <summary>
/// Resolves a <see cref="IChatClient"/> for a configured AI provider.
/// </summary>
/// <remarks>
/// <para>
/// This is the single entry point for every consumer of AI in the application. Vertical
/// Features (D1 Assets, D2 Accounts &amp; Transactions, etc.) call into the factory rather
/// than constructing provider-specific clients themselves. That isolation is what lets:
/// </para>
/// <list type="bullet">
///   <item><description>Providers be swapped without touching consumers.</description></item>
///   <item><description>B1's tool-calling host (Feature #648) wrap the returned client with
///   tool registration uniformly across providers.</description></item>
///   <item><description>B2's safety middleware (Feature #649 — citations, scope enforcement,
///   preview/confirm) decorate the pipeline once instead of being per-provider.</description></item>
/// </list>
/// <para>
/// Implementations are registered as singletons. The returned <see cref="IChatClient"/> may
/// itself be a singleton or transient depending on the provider's thread-safety; consumers
/// must not assume either.
/// </para>
/// <para>
/// <b>Foundation-PR scope (Feature #396):</b> the factory resolves clients only. It does
/// <i>not</i> attach AIFunctions, register MCP tools, or apply safety middleware. Those
/// concerns layer on top of <see cref="IChatClient"/> in subsequent Features and must not
/// leak into this contract.
/// </para>
/// </remarks>
public interface IChatClientFactory
{
    /// <summary>
    /// Returns a chat client for the application's default provider
    /// (<see cref="AiOptions.DefaultProvider"/>).
    /// </summary>
    /// <returns>A configured <see cref="IChatClient"/> ready for use.</returns>
    /// <exception cref="InvalidOperationException">
    /// The default provider is not present in <see cref="AiOptions.Providers"/>, or its
    /// configuration is incomplete (missing model, missing API key reference, etc.).
    /// </exception>
    IChatClient GetClient();

    /// <summary>
    /// Returns a chat client for an explicitly-named provider.
    /// </summary>
    /// <param name="providerId">The provider to resolve.</param>
    /// <returns>A configured <see cref="IChatClient"/> ready for use.</returns>
    /// <exception cref="InvalidOperationException">
    /// The named provider is not configured, or its configuration is incomplete.
    /// </exception>
    IChatClient GetClient(AiProviderId providerId);
}
