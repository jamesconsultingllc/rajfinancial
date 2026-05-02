using Microsoft.Extensions.Options;

namespace RajFinancial.Api.Services.RateLimit;

/// <summary>Shape-validation for <see cref="RateLimitOptions" />. Fails host boot on misconfiguration.</summary>
internal sealed class RateLimitOptionsValidator : IValidateOptions<RateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Disabled is a valid configuration; skip remaining checks so a clean
        // "RateLimit:Enabled=false" toggles off all enforcement without forcing
        // operators to also supply valid AiPolicy values.
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        ValidateConnection(options, failures);
        ValidateRetryAndCleanup(options, failures);
        ValidateAiPolicy(options, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateConnection(RateLimitOptions options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.StorageConnectionName))
            failures.Add($"{RateLimitOptions.SectionName}:StorageConnectionName is required when Enabled=true.");

        if (string.IsNullOrWhiteSpace(options.TableName))
            failures.Add($"{RateLimitOptions.SectionName}:TableName is required when Enabled=true.");
        else if (options.TableName.Length is < 3 or > 63)
            failures.Add($"{RateLimitOptions.SectionName}:TableName must be 3-63 characters.");
    }

    private static void ValidateRetryAndCleanup(RateLimitOptions options, List<string> failures)
    {
        if (options.RetryAttempts < 1)
            failures.Add($"{RateLimitOptions.SectionName}:RetryAttempts must be >= 1.");
        if (options.RetryAttempts > 10)
            failures.Add($"{RateLimitOptions.SectionName}:RetryAttempts must be <= 10 (bounded retry budget).");

        if (options.JitterMaxMs < 0)
            failures.Add($"{RateLimitOptions.SectionName}:JitterMaxMs must be >= 0.");

        if (options.CleanupRetention <= TimeSpan.Zero)
            failures.Add($"{RateLimitOptions.SectionName}:CleanupRetention must be > 0.");
    }

    private static void ValidateAiPolicy(RateLimitOptions options, List<string> failures)
    {
        if (options.AiPolicy is null)
        {
            failures.Add($"{RateLimitOptions.SectionName}:AiPolicy is required when Enabled=true.");
            return;
        }

        if (options.AiPolicy.RequestsPerMinute <= 0)
            failures.Add($"{RateLimitOptions.SectionName}:AiPolicy:RequestsPerMinute must be > 0.");
        if (options.AiPolicy.RequestsPerHour <= 0)
            failures.Add($"{RateLimitOptions.SectionName}:AiPolicy:RequestsPerHour must be > 0.");
        if (options.AiPolicy.RequestsPerHour < options.AiPolicy.RequestsPerMinute)
            failures.Add($"{RateLimitOptions.SectionName}:AiPolicy:RequestsPerHour must be >= RequestsPerMinute.");
    }
}
