using Microsoft.Extensions.AI;

namespace RajFinancial.Api.Services.Ai.Tools;

/// <summary>
/// Read-only registry of <see cref="AIFunction"/> tools that can be exposed to a chat
/// model via Microsoft.Extensions.AI's tool-calling middleware.
/// </summary>
/// <remarks>
/// <para>
/// <b>Lifetime:</b> Singleton. The registry is built once from the
/// <see cref="AiToolDescriptor"/>s collected in DI; from that point forward it is an
/// immutable snapshot — there is no public <c>Register</c> or <c>Build</c> method.
/// </para>
/// <para>
/// <b>Consumers:</b> <see cref="ChatClientFactory"/> consults <see cref="IsEmpty"/> to
/// decide whether to apply <c>UseFunctionInvocation()</c> on the chat client pipeline.
/// Future per-scope chat-client factories will use <see cref="GetByScope"/> to expose only
/// the tools relevant to the calling domain.
/// </para>
/// <para>
/// <b>Telemetry:</b> Every <see cref="AIFunction"/> returned from this registry has been
/// pre-wrapped in a <c>TelemetryAIFunction</c> decorator that emits per-invocation
/// activity + counters + histograms on the <c>RajFinancial.Api.Ai</c> ActivitySource.
/// </para>
/// </remarks>
public interface IAiToolRegistry
{
    /// <summary>Returns <c>true</c> when no descriptors were registered.</summary>
    bool IsEmpty { get; }

    /// <summary>Total number of registered tools across all scopes.</summary>
    int Count { get; }

    /// <summary>All registered tools across every scope, in registration order.</summary>
    IReadOnlyList<AIFunction> GetAll();

    /// <summary>
    /// Tools registered under <paramref name="scope"/> in registration order.
    /// Returns an empty list for unknown scopes.
    /// </summary>
    IReadOnlyList<AIFunction> GetByScope(string scope);
}
