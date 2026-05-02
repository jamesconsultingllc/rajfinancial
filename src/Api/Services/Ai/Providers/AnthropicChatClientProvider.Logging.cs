using Microsoft.Extensions.Logging;

namespace RajFinancial.Api.Services.Ai.Providers;

/// <summary>
/// Source-generated logging for <see cref="AnthropicChatClientProvider"/> (EventId 8020-8029).
/// </summary>
internal sealed partial class AnthropicChatClientProvider
{
    [LoggerMessage(
        EventId = 8020,
        Level = LogLevel.Information,
        Message = "Anthropic chat client initialized (model {Model}, baseUrl {BaseUrl})")]
    private partial void LogProviderInitialized(string model, string baseUrl);

    [LoggerMessage(
        EventId = 8021,
        Level = LogLevel.Error,
        Message = "Anthropic API key environment variable {ApiKeyEnvVar} is not set or empty")]
    private partial void LogApiKeyMissing(string apiKeyEnvVar);
}
