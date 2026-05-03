using RajFinancial.Api.Services.RateLimit;

namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
///     Thrown by <c>RateLimitMiddleware</c> to short-circuit a request that has either
///     exceeded its window quota or hit a fail-closed store outage.
/// </summary>
/// <remarks>
///     Translated to:
///     <list type="bullet">
///         <item>
///             <c>429 Too Many Requests</c> with <c>Retry-After</c> when
///             <see cref="StoreUnavailable" /> is <c>false</c>.
///         </item>
///         <item>
///             <c>503 Service Unavailable</c> with <c>Retry-After</c> when
///             <see cref="StoreUnavailable" /> is <c>true</c> (fail-closed).
///         </item>
///     </list>
/// </remarks>
public sealed class RateLimitedException(TimeSpan retryAfter, bool storeUnavailable, RateLimitWindow window)
    : System.Exception(BuildMessage(retryAfter, storeUnavailable, window))
{
    public TimeSpan RetryAfter { get; } = retryAfter;
    public bool StoreUnavailable { get; } = storeUnavailable;
    public RateLimitWindow Window { get; } = window;

    private static string BuildMessage(TimeSpan retryAfter, bool storeUnavailable, RateLimitWindow window)
    {
        // Must match RateLimitResponseHelper.RetryAfterSeconds so the message agrees
        // with the Retry-After header (ceiling, minimum 1 second).
        var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        return storeUnavailable
            ? $"Rate-limit store unavailable; retry after {seconds}s."
            : $"{window} rate-limit exceeded; retry after {seconds}s.";
    }
}
