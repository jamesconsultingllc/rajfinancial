using Microsoft.Extensions.Logging;
using RajFinancial.Api.Services.RateLimit;

namespace RajFinancial.Api.Middleware.RateLimit;

public partial class RateLimitMiddleware
{
    [LoggerMessage(EventId = 5100, Level = LogLevel.Warning,
        Message = "Rate-limit rejected: function={Function} userIdHash={UserIdHash} window={Window} retryAfterSeconds={RetryAfterSeconds}")]
    private partial void LogRejected(string function, string userIdHash, RateLimitWindow window, int retryAfterSeconds);

    [LoggerMessage(EventId = 5101, Level = LogLevel.Error,
        Message = "Rate-limit fail-closed: store unavailable for function={Function} userIdHash={UserIdHash} retryAfterSeconds={RetryAfterSeconds}")]
    private partial void LogFailedClosed(string function, string userIdHash, int retryAfterSeconds);

    [LoggerMessage(EventId = 5102, Level = LogLevel.Warning,
        Message = "Rate-limit fail-open: store unavailable, request allowed for function={Function} userIdHash={UserIdHash}")]
    private partial void LogFailedOpen(string function, string userIdHash);

    [LoggerMessage(EventId = 5103, Level = LogLevel.Debug,
        Message = "Rate-limit skipped: no authenticated user id for function={Function} policyKind={PolicyKind}")]
    private partial void LogMissingUserId(string function, RateLimitPolicyKind policyKind);

    [LoggerMessage(EventId = 5104, Level = LogLevel.Error,
        Message = "Rate-limit store threw unexpectedly for function={Function}; applying failureMode={FailureMode}")]
    private partial void LogStoreUnhandled(System.Exception ex, string function, RateLimitFailureMode failureMode);
}
