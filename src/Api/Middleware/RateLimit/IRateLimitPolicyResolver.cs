using Microsoft.Azure.Functions.Worker;
using RajFinancial.Api.Services.RateLimit;

namespace RajFinancial.Api.Middleware.RateLimit;

/// <summary>Resolves the <see cref="RateLimitPolicy" /> for a function invocation.</summary>
public interface IRateLimitPolicyResolver
{
    /// <summary>
    ///     Resolves the policy for the function defined in <paramref name="context" />.
    ///     Returns <see cref="RateLimitPolicy.None" /> for functions without a
    ///     <see cref="RateLimitPolicyAttribute" />.
    /// </summary>
    RateLimitPolicy Resolve(FunctionContext context);
}
