namespace RajFinancial.Api.Services.RateLimit;

/// <summary>Fully resolved rate-limit specification for a single function invocation.</summary>
/// <param name="Kind">Policy class.</param>
/// <param name="RequestsPerMinute">Allowed requests per minute.</param>
/// <param name="RequestsPerHour">Allowed requests per hour.</param>
/// <param name="FailureMode">Behavior when the backing store is unreachable.</param>
public sealed record RateLimitPolicy(
    RateLimitPolicyKind Kind,
    int RequestsPerMinute,
    int RequestsPerHour,
    RateLimitFailureMode FailureMode)
{
    /// <summary>Sentinel for functions with no enforcement.</summary>
    public static RateLimitPolicy None { get; } = new(
        RateLimitPolicyKind.None,
        RequestsPerMinute: int.MaxValue,
        RequestsPerHour: int.MaxValue,
        FailureMode: RateLimitFailureMode.FailOpen);

    /// <summary>Sentinel for explicit-bypass endpoints (e.g., health probes).</summary>
    public static RateLimitPolicy Bypass { get; } = new(
        RateLimitPolicyKind.Bypass,
        RequestsPerMinute: int.MaxValue,
        RequestsPerHour: int.MaxValue,
        FailureMode: RateLimitFailureMode.FailOpen);

    /// <summary><c>true</c> if this policy short-circuits the middleware.</summary>
    public bool IsNoOp => Kind is RateLimitPolicyKind.None or RateLimitPolicyKind.Bypass;
}
