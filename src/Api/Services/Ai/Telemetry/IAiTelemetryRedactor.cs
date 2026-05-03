namespace RajFinancial.Api.Services.Ai.Telemetry;

/// <summary>
/// Redacts AI tool-call arguments + previews for <i>telemetry</i> only — never modifies
/// what is sent to the model. The redactor's job is to keep secrets, PII, and
/// finance-sensitive identifiers out of Application Insights / OTel traces and
/// structured logs.
/// </summary>
/// <remarks>
/// <para>
/// This is explicitly NOT a model-context safety boundary (which is B2 scope: prompt
/// injection / tool-result safety). If a tool argument is sensitive enough that the
/// model itself should not see it raw, that decision lives elsewhere.
/// </para>
/// <para>
/// <b>Defaults (current PR):</b>
/// <list type="bullet">
///   <item>Argument names are matched case-insensitively against an internal allow-list
///   of known-safe metadata (e.g., <c>pageNumber</c>, <c>limit</c>, <c>scope</c>) — those
///   pass through verbatim.</item>
///   <item>Argument names matching <c>merchantName</c> / <c>payee</c> / <c>vendor</c> are
///   replaced with <c>[REDACTED:HMAC=&lt;prefix&gt;]</c>, where the prefix is the first
///   12 hex chars of an HMAC-SHA-256 of the value keyed by the configured
///   <c>MerchantHashSecret</c>. Repeated identical merchants emit identical prefixes
///   (cluster-friendly), but a raw dictionary attack on prefixes is computationally
///   infeasible without the secret.</item>
///   <item>Argument names matching <c>accountNumber</c> / <c>cardNumber</c> /
///   <c>routingNumber</c> are reduced to <c>****&lt;last4&gt;</c>.</item>
///   <item>All other arguments fall through to <c>[REDACTED]</c> by default — fail-closed
///   for telemetry. Domain modules may extend the allow-list in a future PR.</item>
/// </list>
/// </para>
/// </remarks>
public interface IAiTelemetryRedactor
{
    /// <summary>
    /// Returns a telemetry-safe representation of the supplied tool argument value.
    /// </summary>
    /// <param name="toolName">Name of the tool whose argument is being redacted.</param>
    /// <param name="argumentName">Argument key.</param>
    /// <param name="value">
    /// Raw argument value as supplied by the model. May be <c>null</c>. Implementations
    /// must be tolerant of any boxed CLR type the JSON deserializer can produce
    /// (string, number, bool, list, dict, null).
    /// </param>
    /// <returns>
    /// A short, low-cardinality, secret-free string suitable for activity events and
    /// structured logs. Never <c>null</c>.
    /// </returns>
    string Redact(string toolName, string argumentName, object? value);
}
