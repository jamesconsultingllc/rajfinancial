namespace RajFinancial.Api.Services.RateLimit;

/// <summary>Identifies which window triggered a rate-limit rejection.</summary>
public enum RateLimitWindow
{
    /// <summary>No window — the request was allowed (or the policy was None / Bypass).</summary>
    None = 0,

    /// <summary>Per-minute fixed window.</summary>
    Minute = 1,

    /// <summary>Per-hour fixed window.</summary>
    Hour = 2,
}
