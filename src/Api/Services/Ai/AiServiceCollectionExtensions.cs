using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Services.Ai.Abstractions;
using RajFinancial.Api.Services.Ai.Telemetry;
using RajFinancial.Api.Services.Ai.Tools;
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
/// <see cref="IChatClientProvider"/> exists. The registered <c>ValidateOnStart</c> hook
/// validates the bound <see cref="AiOptions"/> shape (e.g. missing or invalid AI
/// configuration); it does <b>not</b> verify that matching <see cref="IChatClientProvider"/>
/// implementations are registered for each configured provider id — that check happens
/// later, lazily, inside <see cref="ChatClientFactory.GetClient(AiProviderId)"/>.
/// </para>
/// </remarks>
internal static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AI platform foundation services: options binding + validation +
    /// <see cref="IChatClientFactory"/>. Intended to be called once per host during
    /// startup; calling it more than once will register duplicate
    /// <see cref="IValidateOptions{TOptions}"/> and <see cref="IChatClientFactory"/>
    /// services.
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

        // Tool calling host (PR-2): registry + telemetry redactor.
        services.AddOptions<AiTelemetryRedactorOptions>()
            .Bind(configuration.GetSection(AiTelemetryRedactorOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IAiTelemetryRedactor, DefaultAiTelemetryRedactor>();
        services.AddSingleton<IAiToolRegistry, AiToolRegistry>();

        return services;
    }
}
