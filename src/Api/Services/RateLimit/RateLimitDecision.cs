namespace RajFinancial.Api.Services.RateLimit;

/// <summary>Result of a single <see cref="IRateLimitStore.TryConsumeAsync" /> call.</summary>
public sealed record RateLimitDecision(
    bool Allowed,
    RateLimitWindow Window,
    TimeSpan RetryAfter,
    bool StoreUnavailable)
{
    /// <summary>Allowed within all windows.</summary>
    public static RateLimitDecision Allow() =>
        new(Allowed: true, RateLimitWindow.None, TimeSpan.Zero, StoreUnavailable: false);

    /// <summary>Allowed because the store was unreachable and the policy is fail-open.</summary>
    public static RateLimitDecision AllowFailOpen() =>
        new(Allowed: true, RateLimitWindow.None, TimeSpan.Zero, StoreUnavailable: true);

    /// <summary>Rejected because the store was unreachable and the policy is fail-closed.</summary>
    public static RateLimitDecision RejectFailClosed(TimeSpan retryAfter) =>
        new(Allowed: false, RateLimitWindow.None, retryAfter, StoreUnavailable: true);

    /// <summary>Rejected because the named window's limit was exceeded.</summary>
    public static RateLimitDecision RejectWindow(RateLimitWindow window, TimeSpan retryAfter) =>
        new(Allowed: false, window, retryAfter, StoreUnavailable: false);
}
