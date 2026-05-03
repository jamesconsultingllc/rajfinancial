using Azure;
using Azure.Data.Tables;

namespace RajFinancial.Api.Services.RateLimit.Storage;

/// <summary>
///     Azure Table entity backing one fixed-window counter for one user.
/// </summary>
/// <remarks>
///     <para>
///         <b>PartitionKey</b> = the hashed user id (32 hex chars). All windows for one user
///         live in the same partition so the minute and hour counters can be atomically
///         updated in a single entity-group transaction.
///     </para>
///     <para>
///         <b>RowKey</b> shapes:
///         <list type="bullet">
///             <item><c>min:{yyyyMMddHHmm}</c> — minute bucket (UTC).</item>
///             <item><c>hour:{yyyyMMddHH}</c> — hour bucket (UTC).</item>
///         </list>
///     </para>
///     <para>
///         <b>ExpiresAt</b> is the wall-clock time after which a cleanup sweep may remove
///         the row. Azure Table Storage has no native TTL, so cleanup is performed by a
///         scheduled function trigger.
///     </para>
/// </remarks>
internal sealed class RateLimitCounterEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>Number of requests counted within this window.</summary>
    public long Count { get; set; }

    /// <summary>Wall-clock time after which the cleanup sweep may delete this row.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
