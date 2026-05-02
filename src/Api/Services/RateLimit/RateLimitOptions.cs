namespace RajFinancial.Api.Services.RateLimit;

/// <summary>Strongly-typed binding for the <c>RateLimit</c> configuration section.</summary>
public sealed class RateLimitOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RateLimit";

    /// <summary>Master kill-switch. When <c>false</c>, the middleware is a no-op.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Name of the connection string the rate-limit Table Storage client should resolve.
    ///     Defaults to <c>AzureWebJobsStorage</c> for friction-free local dev (Azurite).
    ///     Production MUST point at a separate Storage Account.
    /// </summary>
    public string StorageConnectionName { get; set; } = "AzureWebJobsStorage";

    /// <summary>Azure Table name. 3-63 alphanumerics per Azure naming rules.</summary>
    public string TableName { get; set; } = "RateLimitCounters";

    /// <summary>Maximum optimistic-concurrency retry attempts on 412 conflicts.</summary>
    public int RetryAttempts { get; set; } = 5;

    /// <summary>Maximum jitter (ms) added to each retry. Full-jitter strategy.</summary>
    public int JitterMaxMs { get; set; } = 50;

    /// <summary>Retention for cleanup of expired counter rows. Defaults to 7 days.</summary>
    public TimeSpan CleanupRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Limits for <see cref="RateLimitPolicyKind.AiToolCalling" /> endpoints.</summary>
    public AiPolicyOptions AiPolicy { get; set; } = new();
}

/// <summary>Per-policy numeric limits and failure mode.</summary>
public sealed class AiPolicyOptions
{
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public RateLimitFailureMode FailureMode { get; set; } = RateLimitFailureMode.FailClosed;
}
