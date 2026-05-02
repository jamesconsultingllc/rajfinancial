using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Services.Ai.Telemetry;

/// <summary>
/// Centralized OpenTelemetry primitives for AI tool-call observability.
/// </summary>
/// <remarks>
/// <para>
/// Per AGENTS.md "Centralized Interceptor Only": there is exactly one
/// <see cref="ActivitySource"/> + <see cref="Meter"/> for AI domain telemetry; per-tool
/// metric counters are NOT scattered across services. Cardinality is kept low —
/// metric tags are limited to <c>tool.name</c>, <c>tool.scope</c>, <c>tool.outcome</c>.
/// Schema fragments and redacted argument previews are emitted as structured
/// activity events, never as metric labels.
/// </para>
/// <para>
/// Activity span name is the stable string <c>Ai.ToolCall</c> with tags — never
/// <c>Ai.ToolCall.{toolName}</c>, which would explode trace aggregation cardinality.
/// </para>
/// </remarks>
internal static class AiToolTelemetry
{
    internal const string SpanName = "Ai.ToolCall";

    internal const string TagToolName = "tool.name";
    internal const string TagToolScope = "tool.scope";
    internal const string TagToolOutcome = "tool.outcome";
    internal const string TagErrorType = "error.type";

    internal const string OutcomeSuccess = "success";
    internal const string OutcomeError = "error";
    internal const string OutcomeCancelled = "cancelled";

    internal const string EventArgsRedacted = "tool.arguments.redacted";

    internal static readonly ActivitySource ActivitySource = new(ObservabilityDomains.Ai);
    internal static readonly Meter Meter = new(ObservabilityDomains.Ai);

    internal static readonly Counter<long> ToolInvocations =
        Meter.CreateCounter<long>("ai.tool.invocations.count", description: "AI tool invocations.");

    internal static readonly Histogram<double> ToolDurationMs =
        Meter.CreateHistogram<double>("ai.tool.duration.ms", unit: "ms", description: "AI tool invocation duration.");
}
