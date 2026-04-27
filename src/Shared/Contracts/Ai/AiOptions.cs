namespace RajFinancial.Shared.Contracts.Ai;

/// <summary>
/// Root AI platform configuration. Bound from the <c>Ai</c> section of application configuration.
/// </summary>
/// <remarks>
/// <para>
/// Example <c>appsettings.Development.json</c> shape (illustrative; <b>not</b> added by Task #398):
/// </para>
/// <code language="json">
/// {
///   "Ai": {
///     "DefaultProvider": "Anthropic",
///     "Providers": {
///       "Anthropic": {
///         "Model": "claude-sonnet-4-5",
///         "ApiKeyEnvVar": "ANTHROPIC_API_KEY"
///       }
///     }
///   }
/// }
/// </code>
/// <para>
/// Validation (required <see cref="DefaultProvider"/> entry exists in <see cref="Providers"/>,
/// non-empty Model on each provider, etc.) happens in the factory (Task #397) at startup, not
/// here. Keeping POCOs validation-free lets us share the type without dragging in
/// <c>Microsoft.Extensions.Options.DataAnnotations</c> from <c>Shared</c>.
/// </para>
/// </remarks>
public sealed class AiOptions
{
    /// <summary>
    /// Configuration section name. Use with <c>configuration.GetSection(AiOptions.SectionName)</c>.
    /// </summary>
    public const string SectionName = "Ai";

    /// <summary>
    /// The provider used when no explicit provider is requested. Must match one of the keys
    /// in <see cref="Providers"/>. Validated by the factory at startup.
    /// </summary>
    public AiProviderId DefaultProvider { get; set; } = AiProviderId.Anthropic;

    /// <summary>
    /// Per-provider configuration, keyed by <see cref="AiProviderId"/>. Configuration binds
    /// string keys (e.g., <c>"Anthropic"</c>) to enum values via the standard case-insensitive
    /// converter.
    /// </summary>
    public Dictionary<AiProviderId, AiProviderOptions> Providers { get; set; } = new();
}
