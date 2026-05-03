namespace RajFinancial.Api.Services.RateLimit;

/// <summary>
///     Behavior when the rate-limit backing store is unreachable or returns an unrecoverable error.
/// </summary>
/// <remarks>
///     The project default for AI/tool endpoints is <see cref="FailClosed" /> per the
///     rubber-duck design review. Fail-open is opt-in only.
/// </remarks>
public enum RateLimitFailureMode
{
    /// <summary>Reject the request with <c>503 Service Unavailable</c> when the store is unavailable.</summary>
    FailClosed = 0,

    /// <summary>Allow the request to proceed when the store is unavailable.</summary>
    FailOpen = 1,
}
