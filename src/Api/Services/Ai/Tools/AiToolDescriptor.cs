using Microsoft.Extensions.AI;

namespace RajFinancial.Api.Services.Ai.Tools;

/// <summary>
/// DI-time descriptor for a single AI tool. Domain modules register descriptors via
/// <c>services.AddAiTool(scope, name, factory)</c>; <see cref="AiToolRegistry"/> resolves
/// every descriptor exactly once at construction time and holds the resulting
/// <see cref="AIFunction"/> instances in an immutable snapshot.
/// </summary>
/// <param name="Scope">Domain scope (use a <see cref="AiToolScopes"/> constant). Non-empty.</param>
/// <param name="Name">Globally unique tool name. Non-empty. Lowercased dotted form recommended (e.g., <c>assets.summary</c>).</param>
/// <param name="Factory">
/// Factory invoked once at registry construction with the <i>root</i>
/// <see cref="IServiceProvider"/>. The factory must NOT capture request-scoped services
/// directly; instead, the returned <see cref="AIFunction"/> delegate should resolve scoped
/// services per-invocation via <see cref="ServiceProviderServiceExtensions.CreateScope"/>
/// (or accept an <see cref="IServiceProvider"/> on <see cref="AIFunctionArguments.Services"/>
/// and call <c>CreateScope()</c> there). PR-2 enforces this only by convention/documentation;
/// architecture tests will sharpen the boundary in a follow-up.
/// </param>
internal sealed record AiToolDescriptor(
    string Scope,
    string Name,
    Func<IServiceProvider, AIFunction> Factory);
