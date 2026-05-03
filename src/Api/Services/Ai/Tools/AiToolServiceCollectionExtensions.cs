using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace RajFinancial.Api.Services.Ai.Tools;

/// <summary>
/// Public registration entry point for AI tools.
/// </summary>
public static class AiToolServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="AIFunction"/> tool with the AI tool registry under
    /// <paramref name="scope"/> and <paramref name="name"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <paramref name="factory"/> is invoked exactly once at registry construction time
    /// (i.e., the first time <see cref="IAiToolRegistry"/> is resolved) with the
    /// <i>root</i> <see cref="IServiceProvider"/>. The factory must return an
    /// <see cref="AIFunction"/> whose <see cref="AIFunction.Name"/> exactly matches
    /// <paramref name="name"/>; otherwise registry construction will throw.
    /// </para>
    /// <para>
    /// If the returned <see cref="AIFunction"/> needs request-scoped services, the function
    /// implementation itself must call <see cref="ServiceProviderServiceExtensions.CreateScope"/>
    /// per invocation — capturing scoped services in the factory closure leaks across
    /// invocations.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAiTool(
        this IServiceCollection services,
        string scope,
        string name,
        Func<IServiceProvider, AIFunction> factory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);

        services.Add(
            ServiceDescriptor.Singleton(new AiToolDescriptor(scope, name, factory)));
        return services;
    }
}
