using Azure;
using Microsoft.Extensions.Logging;

namespace RajFinancial.Api.Services.RateLimit.Storage;

internal sealed partial class TableStorageRateLimitStore
{
    [LoggerMessage(
        EventId = 5110,
        Level = LogLevel.Warning,
        Message = "Rate-limit Table Storage transient error on attempt {Attempt}; retrying.")]
    private partial void LogStoreTransientError(RequestFailedException ex, int attempt);

    [LoggerMessage(
        EventId = 5111,
        Level = LogLevel.Warning,
        Message = "Rate-limit Table Storage exhausted retry budget after {RetryAttempts} attempts; applying failure mode.")]
    private partial void LogStoreRetryExhausted(int retryAttempts);

    [LoggerMessage(
        EventId = 5112,
        Level = LogLevel.Error,
        Message = "Rate-limit Table Storage unavailable; applying failure mode.")]
    private partial void LogStoreUnavailable(RequestFailedException ex);
}
