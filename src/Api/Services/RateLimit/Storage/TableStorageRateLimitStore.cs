using System.Diagnostics;
using System.Globalization;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RajFinancial.Api.Services.RateLimit.Storage;

/// <summary>
///     Azure Table Storage implementation of <see cref="IRateLimitStore" />.
/// </summary>
/// <remarks>
///     <para>
///         <b>Atomicity:</b> Both the per-minute and per-hour counters live in the same Azure
///         partition (the hashed user id) so they are updated in a single entity-group
///         transaction. The ETag on each row gives optimistic concurrency: a concurrent
///         request that already advanced one of the counters will collide on
///         <c>SubmitTransactionAsync</c> with HTTP 409 (insert-on-existing) or 412 (precondition
///         failed) and we retry with full-jitter backoff up to <see cref="RateLimitOptions.RetryAttempts" />.
///     </para>
///     <para>
///         <b>Multi-window semantics (Option A):</b> If either the minute OR the hour window
///         would be exceeded by allowing this request, we reject WITHOUT writing. We do not
///         increment one window then reject on the other.
///     </para>
///     <para>
///         <b>Failure handling:</b> Transient/unavailable storage errors do NOT count as
///         "limit exceeded" — they are reported via <see cref="RateLimitDecision.StoreUnavailable" />
///         so the caller (middleware) can apply the policy's <see cref="RateLimitFailureMode" />.
///     </para>
/// </remarks>
internal sealed partial class TableStorageRateLimitStore : IRateLimitStore
{
    private readonly TableServiceClient tableServiceClient;
    private readonly IOptionsMonitor<RateLimitOptions> optionsMonitor;
    private readonly ILogger<TableStorageRateLimitStore> logger;
    private readonly TimeProvider timeProvider;
    private readonly Lazy<Task<TableClient>> tableClient;

    public TableStorageRateLimitStore(
        TableServiceClient tableServiceClient,
        IOptionsMonitor<RateLimitOptions> optionsMonitor,
        ILogger<TableStorageRateLimitStore> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(tableServiceClient);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.tableServiceClient = tableServiceClient;
        this.optionsMonitor = optionsMonitor;
        this.logger = logger;
        this.timeProvider = timeProvider;
        this.tableClient = new Lazy<Task<TableClient>>(InitializeTableClientAsync);
    }

    public async Task<RateLimitDecision> TryConsumeAsync(
        string userIdHash,
        RateLimitPolicy policy,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userIdHash);
        ArgumentNullException.ThrowIfNull(policy);

        if (policy.IsNoOp)
            return RateLimitDecision.Allow();

        var options = optionsMonitor.CurrentValue;
        var sw = Stopwatch.StartNew();
        using var activity = RateLimitTelemetry.StartActivity(RateLimitTelemetry.ActivityStoreTryConsume);

        try
        {
            TableClient client;
            try
            {
                client = await tableClient.Value.ConfigureAwait(false);
            }
            catch (RequestFailedException ex)
            {
                return HandleStoreFailure(activity, ex, policy, "table_init_failed");
            }

            var now = timeProvider.GetUtcNow();
            var (minRow, hourRow) = ComputeRowKeys(now);

            for (var attempt = 0; attempt < options.RetryAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var decision = await TryConsumeAttemptAsync(
                        client, userIdHash, minRow, hourRow, now, policy, cancellationToken).ConfigureAwait(false);
                    if (decision is not null)
                        return decision;

                    // Conflict — retry with full-jitter backoff.
                    await DelayJitterAsync(options.JitterMaxMs, cancellationToken).ConfigureAwait(false);
                }
                catch (RequestFailedException ex) when (IsTransient(ex))
                {
                    LogStoreTransientError(ex, attempt);
                    await DelayJitterAsync(options.JitterMaxMs, cancellationToken).ConfigureAwait(false);
                }
                catch (RequestFailedException ex)
                {
                    return HandleStoreFailure(activity, ex, policy, "request_failed");
                }
            }

            // Exhausted retry budget.
            RateLimitTelemetry.RecordStoreError("retry_exhausted");
            LogStoreRetryExhausted(options.RetryAttempts);
            return ApplyFailureMode(policy);
        }
        finally
        {
            sw.Stop();
            RateLimitTelemetry.RecordStoreDuration(sw.Elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    ///     Performs one read-decide-write cycle. Returns:
    ///     <list type="bullet">
    ///         <item><c>RateLimitDecision.Allow()</c> if both windows had room AND the write succeeded.</item>
    ///         <item><c>RateLimitDecision.RejectWindow(...)</c> if either window is exhausted (no write).</item>
    ///         <item><c>null</c> if the write hit a concurrency conflict — caller retries.</item>
    ///     </list>
    /// </summary>
    private static async Task<RateLimitDecision?> TryConsumeAttemptAsync(
        TableClient client,
        string userIdHash,
        string minRow,
        string hourRow,
        DateTimeOffset now,
        RateLimitPolicy policy,
        CancellationToken ct)
    {
        var minEntity = await GetEntityOrNullAsync(client, userIdHash, minRow, ct).ConfigureAwait(false);
        var hourEntity = await GetEntityOrNullAsync(client, userIdHash, hourRow, ct).ConfigureAwait(false);

        var nextMin = (minEntity?.Count ?? 0) + 1;
        var nextHour = (hourEntity?.Count ?? 0) + 1;

        if (nextHour > policy.RequestsPerHour)
        {
            var retry = NextHourBoundary(now) - now;
            return RateLimitDecision.RejectWindow(RateLimitWindow.Hour, retry);
        }

        if (nextMin > policy.RequestsPerMinute)
        {
            var retry = NextMinuteBoundary(now) - now;
            return RateLimitDecision.RejectWindow(RateLimitWindow.Minute, retry);
        }

        var minActions = BuildAction(userIdHash, minRow, nextMin, NextMinuteBoundary(now), minEntity);
        var hourActions = BuildAction(userIdHash, hourRow, nextHour, NextHourBoundary(now), hourEntity);

        try
        {
            await client.SubmitTransactionAsync([minActions, hourActions], ct).ConfigureAwait(false);
            return RateLimitDecision.Allow();
        }
        catch (TableTransactionFailedException ex) when (IsConflict(ex.Status))
        {
            return null;
        }
        catch (RequestFailedException ex) when (IsConflict(ex.Status))
        {
            return null;
        }
    }

    private static (string MinRow, string HourRow) ComputeRowKeys(DateTimeOffset now)
    {
        var min = "min:" + now.UtcDateTime.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
        var hour = "hour:" + now.UtcDateTime.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
        return (min, hour);
    }

    private static DateTimeOffset NextMinuteBoundary(DateTimeOffset now)
    {
        var truncated = new DateTimeOffset(
            now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, TimeSpan.Zero);
        return truncated.AddMinutes(1);
    }

    private static DateTimeOffset NextHourBoundary(DateTimeOffset now)
    {
        var truncated = new DateTimeOffset(
            now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
        return truncated.AddHours(1);
    }

    private static TableTransactionAction BuildAction(
        string partitionKey,
        string rowKey,
        long count,
        DateTimeOffset expiresAt,
        RateLimitCounterEntity? existing)
    {
        var entity = new RateLimitCounterEntity
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            Count = count,
            ExpiresAt = expiresAt,
        };

        if (existing is null)
        {
            return new TableTransactionAction(TableTransactionActionType.Add, entity);
        }

        entity.ETag = existing.ETag;
        return new TableTransactionAction(TableTransactionActionType.UpdateReplace, entity, existing.ETag);
    }

    private static async Task<RateLimitCounterEntity?> GetEntityOrNullAsync(
        TableClient client, string pk, string rk, CancellationToken ct)
    {
        var response = await client
            .GetEntityIfExistsAsync<RateLimitCounterEntity>(pk, rk, cancellationToken: ct)
            .ConfigureAwait(false);
        return response.HasValue ? response.Value : null;
    }

    private static bool IsConflict(int status) => status is 409 or 412;

    private static bool IsTransient(RequestFailedException ex) =>
        ex.Status is 408 or 429 or 500 or 502 or 503 or 504;

    private static Task DelayJitterAsync(int maxMs, CancellationToken ct)
    {
        if (maxMs <= 0) return Task.CompletedTask;
        // Full-jitter backoff in [0, maxMs] inclusive (AWS Architecture Blog recipe).
        // Random.Shared.Next(min, max) is exclusive on max, so pass maxMs + 1.
        var delay = Random.Shared.Next(0, maxMs + 1);
        return Task.Delay(delay, ct);
    }

    private RateLimitDecision HandleStoreFailure(
        Activity? activity, RequestFailedException ex, RateLimitPolicy policy, string errorType)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        RateLimitTelemetry.RecordStoreError(errorType);
        LogStoreUnavailable(ex);
        return ApplyFailureMode(policy);
    }

    private static RateLimitDecision ApplyFailureMode(RateLimitPolicy policy) =>
        policy.FailureMode == RateLimitFailureMode.FailClosed
            ? RateLimitDecision.RejectFailClosed(TimeSpan.FromSeconds(60))
            : RateLimitDecision.AllowFailOpen();

    private async Task<TableClient> InitializeTableClientAsync()
    {
        var options = optionsMonitor.CurrentValue;
        var client = tableServiceClient.GetTableClient(options.TableName);
        await client.CreateIfNotExistsAsync().ConfigureAwait(false);
        return client;
    }

    public async Task<long> CleanupExpiredAsync(DateTimeOffset olderThan, CancellationToken cancellationToken)
    {
        TableClient client;
        try
        {
            client = await tableClient.Value.ConfigureAwait(false);
        }
        catch (RequestFailedException ex)
        {
            RateLimitTelemetry.RecordStoreError("cleanup_table_init_failed");
            LogStoreUnavailable(ex);
            return 0;
        }

        long deleted = 0;
        var filter = $"ExpiresAt lt datetime'{olderThan:yyyy-MM-ddTHH:mm:ssZ}'";
        var pages = client.QueryAsync<RateLimitCounterEntity>(
            filter, maxPerPage: 100, cancellationToken: cancellationToken);

        var batch = new List<TableTransactionAction>();
        string? currentPartition = null;

        await foreach (var entity in pages.ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (currentPartition is null) currentPartition = entity.PartitionKey;

            if (entity.PartitionKey != currentPartition || batch.Count >= 100)
            {
                deleted += await SubmitDeleteBatchAsync(client, batch, cancellationToken).ConfigureAwait(false);
                batch.Clear();
                currentPartition = entity.PartitionKey;
            }

            batch.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity, entity.ETag));
        }

        if (batch.Count > 0)
            deleted += await SubmitDeleteBatchAsync(client, batch, cancellationToken).ConfigureAwait(false);

        return deleted;
    }

    private async Task<long> SubmitDeleteBatchAsync(
        TableClient client,
        List<TableTransactionAction> batch,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return 0;
        try
        {
            await client.SubmitTransactionAsync(batch, cancellationToken).ConfigureAwait(false);
            return batch.Count;
        }
        catch (RequestFailedException ex)
        {
            // Best-effort cleanup: drop the batch and continue.
            RateLimitTelemetry.RecordStoreError("cleanup_batch_failed");
            LogStoreUnavailable(ex);
            return 0;
        }
    }
}
