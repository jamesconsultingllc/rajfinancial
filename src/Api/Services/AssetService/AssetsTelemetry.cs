// ============================================================================
// RAJ Financial - Assets Telemetry
// ============================================================================
// Centralized ActivitySource, Meter, instrument names, tag keys and activity
// names for the Assets domain. Extracted out of AssetService / AssetFunctions
// to satisfy the Services_ShouldNotHavePrivateStaticMethods architecture
// invariant and the AGENT.md "No Magic Strings or Numbers" rule.
//
// NOTE for future contributors (tracked by ADO #628):
// The per-call counter helpers (RecordCreated/Updated/Deleted) are STAGING
// for this domain only. They will be consolidated once #628 lands a
// SaveChangesInterceptor that emits business counters centrally. Do NOT add
// new per-domain counter helpers in other domains; let #628 do them centrally.
//
// Span naming follows the Phase 1c decision (Option b2, see
// docs/plans/phase-1c-span-validation.md §7):
//   - Function-layer span:  Assets.<Op>
//   - Service-layer span:   Assets.<Op>.Service
// ============================================================================

using System.Diagnostics;
using System.Diagnostics.Metrics;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Api.Services.AssetService;

/// <summary>
///     Owns all OpenTelemetry primitives for the Assets instrumentation domain.
/// </summary>
internal static class AssetsTelemetry
{
    // Activity names (function layer — Assets.<Op>)
    internal const string ActivityGetList = "Assets.GetList";
    internal const string ActivityGetById = "Assets.GetById";
    internal const string ActivityCreate = "Assets.Create";
    internal const string ActivityUpdate = "Assets.Update";
    internal const string ActivityDelete = "Assets.Delete";

    // Activity names (service layer — Assets.<Op>.Service)
    internal const string ActivityGetListService = "Assets.GetList.Service";
    internal const string ActivityGetByIdService = "Assets.GetById.Service";
    internal const string ActivityCreateService = "Assets.Create.Service";
    internal const string ActivityUpdateService = "Assets.Update.Service";
    internal const string ActivityDeleteService = "Assets.Delete.Service";

    // Tag keys
    internal const string TagUserId = "user.id";
    internal const string TagOwnerUserId = "owner.user.id";
    internal const string TagAssetId = "asset.id";
    internal const string TagAssetType = "asset.type";
    internal const string TagAssetsCount = "assets.count";
    internal const string TagTypeSwitch = "asset.type_switch";

    // Metric instrument names
    private const string METRIC_ASSETS_CREATED = "assets.created.count";
    private const string METRIC_ASSETS_UPDATED = "assets.updated.count";
    private const string METRIC_ASSETS_DELETED = "assets.deleted.count";

    internal static readonly ActivitySource ActivitySource = new(ObservabilityDomains.Assets);
    private static readonly Meter Meter = new(ObservabilityDomains.Assets);

    private static readonly Counter<long> AssetsCreated =
        Meter.CreateCounter<long>(METRIC_ASSETS_CREATED);

    private static readonly Counter<long> AssetsUpdated =
        Meter.CreateCounter<long>(METRIC_ASSETS_UPDATED);

    private static readonly Counter<long> AssetsDeleted =
        Meter.CreateCounter<long>(METRIC_ASSETS_DELETED);

    internal static void RecordCreated(string assetType)
    {
        var tags = new TagList { { TagAssetType, assetType } };
        AssetsCreated.Add(1, tags);
    }

    internal static void RecordUpdated(string assetType, bool typeSwitch = false)
    {
        var tags = new TagList
        {
            { TagAssetType, assetType },
            { TagTypeSwitch, typeSwitch }
        };
        AssetsUpdated.Add(1, tags);
    }

    internal static void RecordDeleted(string assetType)
    {
        var tags = new TagList { { TagAssetType, assetType } };
        AssetsDeleted.Add(1, tags);
    }
}
