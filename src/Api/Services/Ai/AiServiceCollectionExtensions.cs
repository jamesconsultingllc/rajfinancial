using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Services.Ai.Abstractions;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Services.Ai;

/// <summary>
/// DI registration for the AI platform foundation (Feature #396).
/// </summary>
/// <remarks>
/// <para>
/// Registers <see cref="IChatClientFactory"/> + <see cref="AiOptions"/> binding +
/// <see cref="AiOptionsValidator"/>. Provider implementations
/// (<see cref="IChatClientProvider"/>) are added by separate provider-adapter modules
/// (e.g., the Anthropic adapter in Task #545).
/// </para>
/// <para>
/// <b>Foundation-PR scope:</b> this method is <i>defined</i> in #397 but is not yet called
/// from <c>Program.cs</c>. Wiring is enabled in #545 once at least one
/// <see cref="IChatClientProvider"/> exists, so a foundation-only deploy never starts a
/// host with <c>ValidateOnStart</c> failing for missing providers.
/// </para>
/// </remarks>
internal static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AI platform foundation services: options binding + validation +
    /// <see cref="IChatClientFactory"/>. Idempotent; safe to call once per host.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">Application configuration; the <c>Ai</c> section is bound
    /// to <see cref="AiOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddRajFinancialAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AiOptions>, AiOptionsValidator>();
        services.AddSingleton<IChatClientFactory, ChatClientFactory>();

        return services;
    }
}
