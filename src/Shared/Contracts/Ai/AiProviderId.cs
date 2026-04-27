namespace RajFinancial.Shared.Contracts.Ai;

/// <summary>
/// Identifies a built-in AI provider.
/// </summary>
/// <remarks>
/// <para>
/// V1 of the AI platform supports a single provider (<see cref="Anthropic"/>). Additional members
/// are added as providers are wired through the factory. The enum is the authoritative dispatch
/// key for <c>IChatClientFactory</c> implementations; bringing a new provider online is a
/// three-step change: add the enum value, add a provider package, add a factory branch.
/// </para>
/// <para>
/// Configuration binds string values (e.g. <c>"Anthropic"</c>) to this enum via the standard
/// <c>Microsoft.Extensions.Configuration</c> case-insensitive enum converter. See
/// <see cref="AiOptions"/> and <see cref="AiProviderOptions"/>.
/// </para>
/// <para>
/// BYOK (Feature #551) may layer a string-keyed registry on top of this enum to support
/// user-configured providers; the enum does not preclude that. If/when the BYOK Feature shows
/// the enum has become a bottleneck, it can be deprecated then. For v1, enum is the simpler
/// default — compile-time switch completeness, IDE autocomplete, no magic strings in factory
/// dispatch.
/// </para>
/// </remarks>
public enum AiProviderId
{
    /// <summary>Anthropic Claude — default and only provider in the foundation PR (Task #545).</summary>
    Anthropic = 1,
}
