using Microsoft.Extensions.Logging;

namespace RajFinancial.Api.Functions.RateLimit;

public partial class RateLimitCleanupFunction
{
    [LoggerMessage(EventId = 5150, Level = LogLevel.Information,
        Message = "Rate-limit cleanup completed: deleted={Deleted} elapsedMs={ElapsedMs}")]
    private partial void LogCleanupCompleted(long deleted, double elapsedMs);

    [LoggerMessage(EventId = 5151, Level = LogLevel.Warning,
        Message = "Rate-limit cleanup skipped: subsystem disabled via configuration")]
    private partial void LogSkippedDisabled();

    [LoggerMessage(EventId = 5152, Level = LogLevel.Error,
        Message = "Rate-limit cleanup failed unexpectedly")]
    private partial void LogCleanupFailed(System.Exception ex);
}
