using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Services.RateLimit;

namespace RajFinancial.Api.Functions.RateLimit;

/// <summary>
///     Weekly timer that sweeps expired rate-limit counter rows from Azure Table
///     Storage. Table Storage has no native TTL, so we rely on this scheduled
///     sweep to keep the table from accumulating dead rows indefinitely.
/// </summary>
public partial class RateLimitCleanupFunction(
    IRateLimitStore store,
    IOptionsMonitor<RateLimitOptions> optionsMonitor,
    TimeProvider timeProvider,
    ILogger<RateLimitCleanupFunction> logger)
{
    // Sundays 03:00 UTC. Off-peak; aligns with maintenance windows.
    private const string Schedule = "0 0 3 * * 0";

    [Function("RateLimitCleanup")]
    public async Task Run([TimerTrigger(Schedule)] TimerInfo timer, CancellationToken cancellationToken)
    {
        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled)
        {
            LogSkippedDisabled();
            return;
        }

        var olderThan = timeProvider.GetUtcNow() - options.CleanupRetention;
        var sw = Stopwatch.StartNew();
        using var activity = RateLimitTelemetry.StartActivity(RateLimitTelemetry.ActivityCleanup);
        activity?.SetTag("ratelimit.cleanup.older_than", olderThan.ToString("o"));

        long deleted;
        try
        {
            deleted = await store.CleanupExpiredAsync(olderThan, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (System.Exception ex)
        {
            sw.Stop();
            RateLimitTelemetry.RecordCleanupDuration(sw.Elapsed.TotalMilliseconds);
            LogCleanupFailed(ex);
            return;
        }

        sw.Stop();
        RateLimitTelemetry.RecordCleanupDuration(sw.Elapsed.TotalMilliseconds);
        RateLimitTelemetry.RecordCleanupRowsDeleted(deleted);
        activity?.SetTag("ratelimit.cleanup.rows_deleted", deleted);
        LogCleanupCompleted(deleted, sw.Elapsed.TotalMilliseconds);
    }
}
