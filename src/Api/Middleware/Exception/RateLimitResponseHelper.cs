using System.Globalization;

namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
///     Helpers used by <see cref="ExceptionMiddleware" /> to build rate-limit responses
///     without inflating the middleware's type-dependency count (Sonar S1200).
/// </summary>
internal static class RateLimitResponseHelper
{
    public static int RetryAfterSeconds(RateLimitedException ex)
    {
        var seconds = (int)Math.Ceiling(ex.RetryAfter.TotalSeconds);
        return seconds < 1 ? 1 : seconds;
    }

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
