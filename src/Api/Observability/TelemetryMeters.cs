// ============================================================================
// RAJ Financial - Telemetry Meters
// ============================================================================
// Central registry of OpenTelemetry Meter instances and the business-event
// counters owned by BusinessEventsInterceptor. Single owner per Meter name
// avoids the duplicate-Meter emission bug noted historically in
// EntityTelemetry — two Meter instances with the same name double-emit.
//
// Domain telemetry helpers (AssetsTelemetry, EntityTelemetry, etc.) reuse the
// Meters declared here for surviving histograms instead of constructing their
// own. Counters live entirely on this type and are emitted by
// BusinessEventsInterceptor on successful SaveChangesAsync (with the single
// exception of userprofile.concurrent.conflicts.count, which fires from
// SaveChangesFailedAsync).
// ============================================================================

using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Observability;

/// <summary>
///     Central registry of <see cref="Meter"/> instances and business-event
///     <see cref="Counter{T}"/> instruments shared across the application.
/// </summary>
internal static class TelemetryMeters
{
    // Instrument names — kept as constants so dashboards depend on them.
    internal const string AssetsCreatedInstrument = "assets.created.count";
    internal const string AssetsUpdatedInstrument = "assets.updated.count";
    internal const string AssetsDeletedInstrument = "assets.deleted.count";

    internal const string EntitiesCreatedInstrument = "entities.created.count";
    internal const string EntityRolesAssignedInstrument = "entities.roles.assigned.count";

    internal const string GrantsCreatedInstrument = "clientmanagement.grants.created.count";
    internal const string GrantsRevokedInstrument = "clientmanagement.grants.revoked.count";

    internal const string UserProfileJitProvisionedInstrument = "userprofile.jit.provisioned.count";
    internal const string UserProfileSyncInstrument = "userprofile.sync.count";
    internal const string UserProfileConcurrentConflictsInstrument = "userprofile.concurrent.conflicts.count";

    // Meters — one per instrumentation domain. Domain helpers reuse these
    // for any surviving histogram so we never end up with two Meter instances
    // sharing a name.
    internal static readonly Meter AssetsMeter = new(ObservabilityDomains.Assets);
    internal static readonly Meter EntitiesMeter = new(ObservabilityDomains.Entities);
    internal static readonly Meter ClientManagementMeter = new(ObservabilityDomains.ClientManagement);
    internal static readonly Meter UserProfileMeter = new(ObservabilityDomains.UserProfile);

    // Counters — emitted by BusinessEventsInterceptor.
    internal static readonly Counter<long> AssetsCreated =
        AssetsMeter.CreateCounter<long>(AssetsCreatedInstrument);

    internal static readonly Counter<long> AssetsUpdated =
        AssetsMeter.CreateCounter<long>(AssetsUpdatedInstrument);

    internal static readonly Counter<long> AssetsDeleted =
        AssetsMeter.CreateCounter<long>(AssetsDeletedInstrument);

    internal static readonly Counter<long> EntitiesCreated =
        EntitiesMeter.CreateCounter<long>(EntitiesCreatedInstrument);

    internal static readonly Counter<long> EntityRolesAssigned =
        EntitiesMeter.CreateCounter<long>(EntityRolesAssignedInstrument);

    internal static readonly Counter<long> GrantsCreated =
        ClientManagementMeter.CreateCounter<long>(GrantsCreatedInstrument);

    internal static readonly Counter<long> GrantsRevoked =
        ClientManagementMeter.CreateCounter<long>(GrantsRevokedInstrument);

    internal static readonly Counter<long> UserProfileJitProvisioned =
        UserProfileMeter.CreateCounter<long>(UserProfileJitProvisionedInstrument);

    internal static readonly Counter<long> UserProfileSync =
        UserProfileMeter.CreateCounter<long>(UserProfileSyncInstrument);

    internal static readonly Counter<long> UserProfileConcurrentConflicts =
        UserProfileMeter.CreateCounter<long>(UserProfileConcurrentConflictsInstrument);
}
