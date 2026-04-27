namespace RajFinancial.Shared.Contracts.Ai;

/// <summary>
/// Per-provider AI configuration. Bound from <c>Ai:Providers:&lt;ProviderId&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// This type is configuration data only — no runtime behavior, no key resolution. The factory
/// (<c>IChatClientFactory</c>, Task #397) is responsible for translating <see cref="ApiKeyEnvVar"/>
/// (or, in BYOK Feature #551, a user-scoped key reference) into the actual secret at the moment
/// a client is requested. Storing the resolved secret here would (a) keep it in memory for the
/// lifetime of the options snapshot, (b) make rotation harder, and (c) conflate config with
/// secrets management.
/// </para>
/// <para>
/// All properties are mutable to support <c>IOptions</c> binding. Treat instances as immutable
/// after construction.
/// </para>
/// </remarks>
public sealed class AiProviderOptions
{
    /// <summary>
    /// Default model identifier used when a caller does not specify one. Provider-specific
    /// (e.g., <c>"claude-sonnet-4-5"</c> for Anthropic). Required.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Name of the environment variable that holds this provider's API key. The factory
    /// (#397) resolves this to the actual secret at client construction time.
    /// </summary>
    /// <remarks>
    /// This is a deliberate <b>foundation-PR temporary</b>. Real BYOK / Key Vault / Azure App
    /// Configuration integration lives in Feature #551 (BYOK key management). Tracked under that
    /// Feature in ADO; this property exists so the foundation factory has <i>some</i> key path
    /// to bind to without blocking on #551.
    /// </remarks>
    public string ApiKeyEnvVar { get; set; } = string.Empty;

    /// <summary>
    /// Optional override for the provider's base URL. <c>null</c> uses the SDK default
    /// (e.g., <c>https://api.anthropic.com/v1</c> for Anthropic). Useful for self-hosted
    /// gateways, on-prem proxies, or testing against a stub.
    /// </summary>
    public string? BaseUrl { get; set; }
}
