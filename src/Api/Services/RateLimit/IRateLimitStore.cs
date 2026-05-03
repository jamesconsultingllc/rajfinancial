namespace RajFinancial.Api.Services.RateLimit;

/// <summary>Distributed counter store backing the rate-limit middleware.</summary>
public interface IRateLimitStore
{
    /// <summary>
    ///     Atomically increments the minute and hour counters for <paramref name="userIdHash" />
    ///     and returns whether the request is allowed under <paramref name="policy" />.
    /// </summary>
    /// <param name="userIdHash">
    ///     Stable, opaque identifier for the user. Callers MUST hash the raw user id (e.g.,
    ///     SHA-256 truncated to 32 hex chars) so raw identity is never persisted in storage.
    /// </param>
    /// <param name="policy">Resolved policy carrying per-window limits and failure mode.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    Task<RateLimitDecision> TryConsumeAsync(
        string userIdHash,
        RateLimitPolicy policy,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Sweeps expired counter rows older than <paramref name="olderThan" />.
    /// </summary>
    /// <returns>The number of rows deleted.</returns>
    Task<long> CleanupExpiredAsync(DateTimeOffset olderThan, CancellationToken cancellationToken);
}
