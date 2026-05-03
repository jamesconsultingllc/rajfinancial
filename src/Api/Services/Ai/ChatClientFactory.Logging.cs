using Microsoft.Extensions.Logging;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Services.Ai;

/// <summary>
/// Source-generated logging for <see cref="ChatClientFactory"/> (EventId 8000-8999).
/// </summary>
internal sealed partial class ChatClientFactory
{
    [LoggerMessage(
        EventId = 8001,
        Level = LogLevel.Information,
        Message = "AI chat client created for provider {ProviderId} (model {Model})")]
    private partial void LogProviderClientCreated(AiProviderId providerId, string model);

    [LoggerMessage(
        EventId = 8002,
        Level = LogLevel.Warning,
        Message = "AI chat client requested for provider {ProviderId} but no configuration is bound")]
    private partial void LogProviderConfigMissing(AiProviderId providerId);

    [LoggerMessage(
        EventId = 8003,
        Level = LogLevel.Warning,
        Message = "AI chat client requested for provider {ProviderId} but no IChatClientProvider is registered")]
    private partial void LogProviderImplMissing(AiProviderId providerId);

    [LoggerMessage(
        EventId = 8010,
        Level = LogLevel.Warning,
        Message = "Disposing cached AI chat client for provider {ProviderId} threw")]
    private partial void LogProviderClientDisposeFailed(Exception ex, AiProviderId providerId);

    [LoggerMessage(
        EventId = 8011,
        Level = LogLevel.Information,
        Message = "Tool-calling middleware enabled on AI chat client for provider {ProviderId} ({ToolCount} tools)")]
    private partial void LogToolCallingEnabled(AiProviderId providerId, int toolCount);
}
