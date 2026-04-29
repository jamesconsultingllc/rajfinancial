// ============================================================================
// RAJ Financial - Business Events Interceptor
// ============================================================================
// EF Core SaveChangesInterceptor that emits domain business counters
// (assets.created.count, entities.created.count, etc.) centrally. Replaces
// the per-call `Record*` helpers previously sprinkled across services.
//
// Lifecycle:
//   - SavingChangesAsync       — snapshot ChangeTracker entries into an
//                                immutable list of pending events. Snapshotting
//                                is required because EF detaches/changes the
//                                state of entries after a successful save.
//   - SavedChangesAsync        — swap the active and draining list references
//                                (so a nested SaveChanges during emission lands
//                                in a fresh empty list instead of getting our
//                                pending events) and emit one counter increment
//                                per drained event. The drained list is then
//                                cleared in place so its backing array is
//                                reused on the next snapshot — no per-emit
//                                List<> allocation on the hot path.
//   - SaveChangesFailedAsync   — emit userprofile.concurrent.conflicts.count
//                                only. All other domains do nothing on
//                                failure. Two arms — DbUpdateConcurrencyException
//                                tested first because it derives from
//                                DbUpdateException.
//
// Lifetime: registered scoped to the DbContext so each DbContext gets its
// own interceptor instance (and therefore its own snapshot list).
// ============================================================================

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RajFinancial.Api.Observability;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Access;
using RajFinancial.Shared.Entities.Assets;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Data.Interceptors;

/// <summary>
///     Emits business-event counters from EF Core's <c>SaveChanges</c>
///     pipeline. See file header for the lifecycle and design rationale.
/// </summary>
public sealed class BusinessEventsInterceptor : SaveChangesInterceptor
{
    private const string TYPE_PROPERTY = "Type";
    private const string STATUS_PROPERTY = "Status";

    private const string ASSET_TYPE_TAG = "asset.type";
    private const string ASSET_TYPE_SWITCH_TAG = "asset.type_switch";
    private const string ENTITY_TYPE_TAG = "entity.type";
    private const string ENTITY_ROLE_TYPE_TAG = "entity.role.type";
    private const string TagConflictType = "conflict.type";
    private const string ConflictTypeJitRace = "jit_race";
    private const string ConflictTypeModifyRace = "modify_race";

    // Two-list swap eliminates per-emit List<> allocation. 'active' receives
    // snapshotted events; 'draining' is the spare slot. On DrainAndEmit we
    // swap the references, iterate the local capture, then Clear() the
    // drained list in place so its backing array is reused. EF Core does not
    // invoke interceptors concurrently on the same DbContext, so plain field
    // swap is sufficient — the only reentrancy we guard against is a nested
    // SaveChanges triggered from within our own emission callback, which
    // lands in the now-empty 'active' list.
    private List<PendingBusinessEvent> active = [];
    private List<PendingBusinessEvent> draining = [];

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Snapshot(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Snapshot(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DrainAndEmit();
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        DrainAndEmit();
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        EmitFailureAndClear(eventData);
        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc/>
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        EmitFailureAndClear(eventData);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <summary>
    ///     Test hook: snapshot the ChangeTracker without going through the full
    ///     EF Core interception pipeline. Production code paths should never
    ///     call this directly — they go through <c>SavingChanges[Async]</c>.
    /// </summary>
    internal void SnapshotForTesting(DbContext? context) => Snapshot(context);

    /// <summary>
    ///     Test hook: drive the failure emission path directly. Production code
    ///     paths should never call this — they go through <c>SaveChangesFailed[Async]</c>.
    /// </summary>
    internal void EmitFailureAndClearForTesting(Exception? exception) =>
        EmitFailureAndClearInternal(exception);

    private void Snapshot(DbContext? context)
    {
        active.Clear();

        if (context is null)
            return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.Entity)
            {
                case Asset asset:
                    SnapshotAsset(asset, entry);
                    break;
                case Entity entity when entry.State == EntityState.Added:
                    active.Add(new EntityCreated(entity.Type.ToString()));
                    break;
                case EntityRole role when entry.State == EntityState.Added:
                    active.Add(new EntityRoleAssigned(role.RoleType.ToString()));
                    break;
                case DataAccessGrant grant:
                    SnapshotGrant(grant, entry);
                    break;
                case UserProfile when entry.State == EntityState.Added:
                    active.Add(new UserProfileAdded());
                    break;
                case UserProfile when entry.State == EntityState.Modified:
                    active.Add(new UserProfileModified());
                    break;
            }
        }
    }

    private void SnapshotAsset(Asset asset, EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                active.Add(new AssetCreated(asset.Type.ToString()));
                break;
            case EntityState.Modified:
                var typeSwitch = !Equals(
                    entry.OriginalValues[TYPE_PROPERTY],
                    entry.CurrentValues[TYPE_PROPERTY]);
                active.Add(new AssetUpdated(asset.Type.ToString(), typeSwitch));
                break;
            case EntityState.Deleted:
                // OriginalValues is the correct semantic source for the
                // pre-delete type. Fall back to the live property if EF
                // somehow returns null (defensive).
                var deletedType = entry.OriginalValues[TYPE_PROPERTY];
                active.Add(new AssetDeleted(deletedType?.ToString() ?? asset.Type.ToString()));
                break;
        }
    }

    private void SnapshotGrant(DataAccessGrant grant, EntityEntry entry)
    {
        _ = grant;

        switch (entry.State)
        {
            case EntityState.Added:
                active.Add(new GrantCreated());
                break;
            case EntityState.Modified:
                // Revoke is a soft-delete (Status flips to Revoked). Detect by
                // value comparison rather than EntityState.Deleted.
                var originalStatus = entry.OriginalValues[STATUS_PROPERTY];
                var currentStatus = entry.CurrentValues[STATUS_PROPERTY];
                if (!Equals(originalStatus, GrantStatus.Revoked)
                    && Equals(currentStatus, GrantStatus.Revoked))
                {
                    active.Add(new GrantRevoked());
                }
                break;
        }
    }

    private void DrainAndEmit()
    {
        // Two-list swap: move 'active' aside into 'draining' (which was empty
        // from the previous drain) and use the freed list as the new 'active'.
        // A reentrant Snapshot triggered from inside Emit lands in the new
        // empty 'active'; the outer drain iterates its local capture so a
        // nested DrainAndEmit reassigning the fields does not disturb it.
        // After emission we Clear() the captured list in place — backing
        // array is reused, no per-emit allocation.
        (active, draining) = (draining, active);
        var snapshot = draining;

        foreach (var ev in snapshot)
            Emit(ev);

        snapshot.Clear();
    }

    // Uses TagList (struct) to avoid per-call KeyValuePair array allocation on
    // the SaveChanges hot path. Tag values that are enum .ToString() results are
    // strings (no boxing); bool values box into TagList's inline storage but no
    // heap array is allocated.
    private static void Emit(PendingBusinessEvent ev)
    {
        switch (ev)
        {
            case AssetCreated created:
            {
                var tags = new TagList { { ASSET_TYPE_TAG, created.AssetType } };
                TelemetryMeters.AssetsCreated.Add(1, tags);
                break;
            }
            case AssetUpdated updated:
            {
                var tags = new TagList
                {
                    { ASSET_TYPE_TAG, updated.AssetType },
                    { ASSET_TYPE_SWITCH_TAG, updated.TypeSwitch },
                };
                TelemetryMeters.AssetsUpdated.Add(1, tags);
                break;
            }
            case AssetDeleted deleted:
            {
                var tags = new TagList { { ASSET_TYPE_TAG, deleted.AssetType } };
                TelemetryMeters.AssetsDeleted.Add(1, tags);
                break;
            }
            case EntityCreated entity:
            {
                var tags = new TagList { { ENTITY_TYPE_TAG, entity.EntityType } };
                TelemetryMeters.EntitiesCreated.Add(1, tags);
                break;
            }
            case EntityRoleAssigned role:
            {
                var tags = new TagList { { ENTITY_ROLE_TYPE_TAG, role.RoleType } };
                TelemetryMeters.EntityRolesAssigned.Add(1, tags);
                break;
            }
            case GrantCreated:
                TelemetryMeters.GrantsCreated.Add(1);
                break;
            case GrantRevoked:
                TelemetryMeters.GrantsRevoked.Add(1);
                break;
            case UserProfileAdded:
                TelemetryMeters.UserProfileJitProvisioned.Add(1);
                break;
            case UserProfileModified:
                TelemetryMeters.UserProfileSync.Add(1);
                break;
        }
    }

    private void EmitFailureAndClear(DbContextErrorEventData eventData) =>
        EmitFailureAndClearInternal(eventData.Exception);

    private void EmitFailureAndClearInternal(Exception? exception)
    {
        // Order matters: DbUpdateConcurrencyException : DbUpdateException.
        // Test the subtype first so a concurrency exception with a Modified
        // UserProfile only emits once. Both arms emit the same counter — the
        // distinction is encoded in the conflict.type tag: modify_race for
        // optimistic-concurrency mismatches on Modified profiles, jit_race for
        // PK conflicts from a concurrent JIT insert (Added profiles) — see
        // plan AB#628 §two-arm rule.
        string? conflictType = null;
        if (exception is DbUpdateConcurrencyException
            && active.Any(e => e is UserProfileModified))
        {
            conflictType = ConflictTypeModifyRace;
        }
        else if (exception is DbUpdateException
                 && exception is not DbUpdateConcurrencyException
                 && active.Any(e => e is UserProfileAdded))
        {
            conflictType = ConflictTypeJitRace;
        }

        if (conflictType is not null)
        {
            var tags = new TagList { { TagConflictType, conflictType } };
            TelemetryMeters.UserProfileConcurrentConflicts.Add(1, tags);
        }

        // Always clear so a retry on the same DbContext doesn't double-emit.
        active.Clear();
    }

    // -------------------------------------------------------------------------
    // Snapshot record types — discriminated union of pending business events.
    // Internal so tests can assert against them.
    // -------------------------------------------------------------------------
    internal abstract record PendingBusinessEvent;

    internal sealed record AssetCreated(string AssetType) : PendingBusinessEvent;

    internal sealed record AssetUpdated(string AssetType, bool TypeSwitch) : PendingBusinessEvent;

    internal sealed record AssetDeleted(string AssetType) : PendingBusinessEvent;

    internal sealed record EntityCreated(string EntityType) : PendingBusinessEvent;

    internal sealed record EntityRoleAssigned(string RoleType) : PendingBusinessEvent;

    internal sealed record GrantCreated : PendingBusinessEvent;

    internal sealed record GrantRevoked : PendingBusinessEvent;

    internal sealed record UserProfileAdded : PendingBusinessEvent;

    internal sealed record UserProfileModified : PendingBusinessEvent;
}
