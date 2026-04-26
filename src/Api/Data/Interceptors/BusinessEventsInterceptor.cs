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
//   - SavedChangesAsync        — atomically swap the snapshot out (so a nested
//                                SaveChanges doesn't get our pending events)
//                                and emit one counter increment per event.
//   - SaveChangesFailedAsync   — emit userprofile.concurrent.conflicts.count
//                                only. All other domains do nothing on
//                                failure. Two arms — DbUpdateConcurrencyException
//                                tested first because it derives from
//                                DbUpdateException.
//
// Lifetime: registered scoped to the DbContext so each DbContext gets its
// own interceptor instance (and therefore its own snapshot list).
// ============================================================================

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

    private List<PendingBusinessEvent> pending = [];

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
        pending.Clear();

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
                    pending.Add(new EntityCreated(entity.Type.ToString()));
                    break;
                case EntityRole role when entry.State == EntityState.Added:
                    pending.Add(new EntityRoleAssigned(role.RoleType.ToString()));
                    break;
                case DataAccessGrant grant:
                    SnapshotGrant(grant, entry);
                    break;
                case UserProfile when entry.State == EntityState.Added:
                    pending.Add(new UserProfileAdded());
                    break;
                case UserProfile when entry.State == EntityState.Modified:
                    pending.Add(new UserProfileModified());
                    break;
            }
        }
    }

    private void SnapshotAsset(Asset asset, EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                pending.Add(new AssetCreated(asset.Type.ToString()));
                break;
            case EntityState.Modified:
                var typeSwitch = !Equals(
                    entry.OriginalValues[TYPE_PROPERTY],
                    entry.CurrentValues[TYPE_PROPERTY]);
                pending.Add(new AssetUpdated(asset.Type.ToString(), typeSwitch));
                break;
            case EntityState.Deleted:
                // OriginalValues is the correct semantic source for the
                // pre-delete type. Fall back to the live property if EF
                // somehow returns null (defensive).
                var deletedType = entry.OriginalValues[TYPE_PROPERTY];
                pending.Add(new AssetDeleted(deletedType?.ToString() ?? asset.Type.ToString()));
                break;
        }
    }

    private void SnapshotGrant(DataAccessGrant grant, EntityEntry entry)
    {
        _ = grant;

        switch (entry.State)
        {
            case EntityState.Added:
                pending.Add(new GrantCreated());
                break;
            case EntityState.Modified:
                // Revoke is a soft-delete (Status flips to Revoked). Detect by
                // value comparison rather than EntityState.Deleted.
                var originalStatus = entry.OriginalValues[STATUS_PROPERTY];
                var currentStatus = entry.CurrentValues[STATUS_PROPERTY];
                if (!Equals(originalStatus, GrantStatus.Revoked)
                    && Equals(currentStatus, GrantStatus.Revoked))
                {
                    pending.Add(new GrantRevoked());
                }
                break;
        }
    }

    private void DrainAndEmit()
    {
        // Atomic swap so a nested SaveChanges (e.g., triggered by a downstream
        // SavedChanges handler) starts with a fresh snapshot instead of
        // re-emitting our events.
        var snapshot = Interlocked.Exchange(ref pending, []);

        foreach (var ev in snapshot)
            Emit(ev);
    }

    private static void Emit(PendingBusinessEvent ev)
    {
        switch (ev)
        {
            case AssetCreated created:
                TelemetryMeters.AssetsCreated.Add(
                    1,
                    new KeyValuePair<string, object?>(ASSET_TYPE_TAG, created.AssetType));
                break;
            case AssetUpdated updated:
                TelemetryMeters.AssetsUpdated.Add(
                    1,
                    new KeyValuePair<string, object?>(ASSET_TYPE_TAG, updated.AssetType),
                    new KeyValuePair<string, object?>(ASSET_TYPE_SWITCH_TAG, updated.TypeSwitch));
                break;
            case AssetDeleted deleted:
                TelemetryMeters.AssetsDeleted.Add(
                    1,
                    new KeyValuePair<string, object?>(ASSET_TYPE_TAG, deleted.AssetType));
                break;
            case EntityCreated entity:
                TelemetryMeters.EntitiesCreated.Add(
                    1,
                    new KeyValuePair<string, object?>(ENTITY_TYPE_TAG, entity.EntityType));
                break;
            case EntityRoleAssigned role:
                TelemetryMeters.EntityRolesAssigned.Add(
                    1,
                    new KeyValuePair<string, object?>(ENTITY_ROLE_TYPE_TAG, role.RoleType));
                break;
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
        // distinction is that the subtype matches Modified profiles (optimistic
        // concurrency) while the base type matches Added profiles (PK conflict
        // from a concurrent JIT insert) — see plan AB#628 §two-arm rule.
        var isUserProfileConcurrencyConflict =
            (exception is DbUpdateConcurrencyException
             && pending.Any(e => e is UserProfileModified))
            || (exception is DbUpdateException
                && exception is not DbUpdateConcurrencyException
                && pending.Any(e => e is UserProfileAdded));

        if (isUserProfileConcurrencyConflict)
        {
            TelemetryMeters.UserProfileConcurrentConflicts.Add(1);
        }

        // Always clear so a retry on the same DbContext doesn't double-emit.
        pending.Clear();
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
