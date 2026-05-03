using System.Globalization;

namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
///     Helpers used by <see cref="ExceptionMiddleware" /> to build rate-limit responses
///     without inflating the middleware's type-dependency count (Sonar S1200).
/// </summary>
internal static class RateLimitResponseHelper
{
    /// <summary>
    ///     Canonical Retry-After computation: ceiling-to-seconds with a floor of 1.
    ///     Used by the response header, the response body, the exception message
    ///     (<see cref="RateLimitedException" />), and structured log/trace fields
    ///     so they all report the same value.
    /// </summary>
    public static int RetryAfterSeconds(TimeSpan retryAfter)
    {
        var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
        return seconds < 1 ? 1 : seconds;
    }

    public static int RetryAfterSeconds(RateLimitedException ex) =>
        RetryAfterSeconds(ex.RetryAfter);

    public static string ErrorCode(RateLimitedException ex) =>
        ex.StoreUnavailable
            ? MiddlewareErrorCodes.RateLimitStoreUnavailable
            : MiddlewareErrorCodes.RateLimited;

    public static Dictionary<string, string> BuildHeaders(RateLimitedException ex) =>
        new()
        {
            [HttpHeaderNames.RetryAfter] = RetryAfterSeconds(ex).ToString(CultureInfo.InvariantCulture),
        };
}
