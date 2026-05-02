using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Services.Ai.Abstractions;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Services.Ai.Providers;

/// <summary>
/// <see cref="IChatClientProvider"/> for Anthropic Claude (Task #545).
/// </summary>
/// <remarks>
/// <para>
/// Resolves the API key from the env var named by
/// <see cref="AiProviderOptions.ApiKeyEnvVar"/> at the moment <see cref="CreateClient"/> is
/// called — the factory caches the resulting <see cref="IChatClient"/>, so this happens
/// once per host. The key is never stored on this instance.
/// </para>
/// <para>
/// The returned <see cref="IChatClient"/> wraps Anthropic.SDK's
/// <see cref="AnthropicClient.Messages"/> endpoint (which already implements
/// <see cref="IChatClient"/>) and is decorated with
/// <see cref="OpenTelemetryChatClient"/> so chat traffic emits spans / metrics on the
/// <c>RajFinancial.Api.Ai</c> ActivitySource registered via
/// <see cref="ObservabilityDomains.Ai"/>.
/// </para>
/// </remarks>
internal sealed partial class AnthropicChatClientProvider(
    ILogger<AnthropicChatClientProvider> logger) : IChatClientProvider
{
    public AiProviderId Id => AiProviderId.Anthropic;

    public IChatClient CreateClient(AiProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiKeyEnvVar))
        {
            throw new InvalidOperationException(
                "Ai:Providers:Anthropic:ApiKeyEnvVar must be a non-empty environment variable name.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new InvalidOperationException(
                "Ai:Providers:Anthropic:Model must be a non-empty model id (e.g., 'claude-sonnet-4-5').");
        }

        var apiKey = Environment.GetEnvironmentVariable(options.ApiKeyEnvVar);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            LogApiKeyMissing(options.ApiKeyEnvVar);
            throw new InvalidOperationException(
                $"Anthropic API key not found. Set the '{options.ApiKeyEnvVar}' environment " +
                "variable to a valid Anthropic API key before requesting an AI chat client.");
        }

        var auth = new APIAuthentication(apiKey);
        var sdkClient = new AnthropicClient(auth);
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            sdkClient.ApiUrlFormat = options.BaseUrl;
        }

        IChatClient inner = sdkClient.Messages;

        var instrumented = new ChatClientBuilder(inner)
            .UseOpenTelemetry(
                loggerFactory: null,
                sourceName: ObservabilityDomains.Ai)
            .Build();

        LogProviderInitialized(options.Model, options.BaseUrl ?? "<sdk-default>");
        return instrumented;
    }
}
