namespace RajFinancial.Api.Services.RateLimit;

public enum RateLimitPolicyKind
{
    None = 0,
    Bypass = 1,
    AiToolCalling = 2,
}
