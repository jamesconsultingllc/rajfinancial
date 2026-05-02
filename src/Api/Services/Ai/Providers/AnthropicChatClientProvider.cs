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

        var sdkClient = CreateSdkClient(apiKey, options.BaseUrl);
        var instrumented = BuildPipeline(sdkClient.Messages, options);

        LogProviderInitialized(options.Model, RedactBaseUrlForLog(options.BaseUrl));
        return instrumented;
    }

    /// <summary>
    /// Returns a log-safe representation of the configured BaseUrl. Strips the userinfo
    /// component (e.g., <c>https://user:pass@host/...</c>) and query string (which may
    /// contain signed-URL credentials) so the diagnostics contract — "never log secret
    /// values" — is preserved even if a proxy or signed URL is configured.
    /// </summary>
    internal static string RedactBaseUrlForLog(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "<sdk-default>";
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsed))
        {
            return "<invalid>";
        }

        var builder = new UriBuilder(parsed)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty,
        };

        return builder.Uri.GetLeftPart(UriPartial.Path);
    }

    /// <summary>
    /// Builds the Anthropic SDK client and applies the optional <see cref="AiProviderOptions.BaseUrl"/>
    /// override. Exposed as <c>internal</c> so unit tests can verify the override actually
    /// reaches <see cref="AnthropicClient.ApiUrlFormat"/> without making a live API call.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="baseUrl"/> is non-empty but is not a well-formed absolute
    /// http(s) URI. Prevents typos like <c>localhost:11434</c> or <c>/proxy</c> from passing
    /// startup and only failing on the first outgoing request.
    /// </exception>
    internal static AnthropicClient CreateSdkClient(string apiKey, string? baseUrl)
    {
        var sdk = new AnthropicClient(new APIAuthentication(apiKey));
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    $"Ai:Providers:Anthropic:BaseUrl must be an absolute http or https URI. Received: '{baseUrl}'.");
            }

            sdk.ApiUrlFormat = baseUrl;
        }

        return sdk;
    }

    /// <summary>
    /// Wraps an inner <see cref="IChatClient"/> with the production observability +
    /// option-defaulting middleware. Exposed as <c>internal</c> so unit tests can drive the
    /// exact same pipeline the production <see cref="CreateClient"/> path returns, with a
    /// capturing inner client.
    /// </summary>
    /// <remarks>
    /// Builder applies first-registered as outermost wrapper. We want:
    /// <c>caller -&gt; OpenTelemetry (observes full call) -&gt; ConfigureOptions (defaults
    /// ModelId before delegating to the SDK) -&gt; inner</c>.
    /// </remarks>
    internal static IChatClient BuildPipeline(IChatClient inner, AiProviderOptions options) =>
        new ChatClientBuilder(inner)
            .UseOpenTelemetry(
                loggerFactory: null,
                sourceName: ObservabilityDomains.Ai)
            .ConfigureOptions(o => o.ModelId ??= options.Model)
            .Build();
}
