namespace RajFinancial.Api.Services.RateLimit;

/// <summary>
///     Marks an Azure Function as subject to a specific rate-limit policy.
/// </summary>
/// <remarks>
///     Functions without this attribute resolve to <see cref="RateLimitPolicy.None" />.
///     The actual numeric limits live in configuration so they can be tuned without redeploy.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RateLimitPolicyAttribute(RateLimitPolicyKind kind) : Attribute
{
    /// <summary>The policy kind to apply to the decorated method or class.</summary>
    public RateLimitPolicyKind Kind { get; } = kind;
}
