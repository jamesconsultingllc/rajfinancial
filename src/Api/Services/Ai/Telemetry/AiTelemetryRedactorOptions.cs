using System.ComponentModel.DataAnnotations;

namespace RajFinancial.Api.Services.Ai.Telemetry;

/// <summary>
/// Options bound from configuration section <c>Ai:Telemetry</c>. Loaded under
/// <c>Microsoft.Extensions.Options</c> with <c>ValidateOnStart()</c>.
/// </summary>
public sealed class AiTelemetryRedactorOptions
{
    public const string SectionName = "Ai:Telemetry";

    /// <summary>
    /// Sentinel placeholder used as the default <see cref="MerchantHashSecret"/> for
    /// dev/test ergonomics. <see cref="AiTelemetryRedactorOptionsValidator"/> rejects
    /// this value outside of the Development environment so a production deploy that
    /// forgets to override the setting fails fast at startup rather than silently
    /// emitting reversible HMAC prefixes.
    /// </summary>
    internal const string DevPlaceholderSecret =
        "rajfin-telemetry-dev-only-do-not-use-in-prod";

    /// <summary>
    /// HMAC key used by <see cref="DefaultAiTelemetryRedactor"/> for hashing
    /// merchant-style argument values. Must be at least 32 characters of high-entropy
    /// random material in production. In dev/test the <see cref="DevPlaceholderSecret"/>
    /// placeholder may be used — this only affects telemetry partitioning, never
    /// security boundaries. The placeholder is rejected in non-Development environments.
    /// </summary>
    [Required]
    [MinLength(16)]
    public string MerchantHashSecret { get; set; } = DevPlaceholderSecret;

    /// <summary>
    /// Number of hex characters from the HMAC prefix to include in the
    /// <c>[REDACTED:HMAC=...]</c> tag. 12 hex chars = 48 bits of clustering precision —
    /// enough to spot dominant merchants in dashboards, not enough to enumerate.
    /// </summary>
    [Range(8, 32)]
    public int HmacPrefixLength { get; set; } = 12;
}
