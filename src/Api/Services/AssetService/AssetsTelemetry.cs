// ============================================================================
// RAJ Financial - Assets Telemetry
// ============================================================================
// Centralized ActivitySource, Meter, instrument names, tag keys and activity
// names for the Assets domain. Extracted out of AssetService / AssetFunctions
// to satisfy the Services_ShouldNotHavePrivateStaticMethods architecture
// invariant and the AGENT.md "No Magic Strings or Numbers" rule.
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
    // Activity names (service layer)
    internal const string ActivityGetList = "Assets.GetList";
    internal const string ActivityGetById = "Assets.GetById";
    internal const string ActivityCreate = "Assets.Create";
    internal const string ActivityUpdate = "Assets.Update";
    internal const string ActivityDelete = "Assets.Delete";

    // Activity names (HTTP layer)
    internal const string ActivityHttpGetList = "Assets.Http.GetList";
    internal const string ActivityHttpGetById = "Assets.Http.GetById";
    internal const string ActivityHttpCreate = "Assets.Http.Create";
    internal const string ActivityHttpUpdate = "Assets.Http.Update";
    internal const string ActivityHttpDelete = "Assets.Http.Delete";

    // Tag keys
    internal const string TagUserId = "user.id";
    internal const string TagOwnerUserId = "owner.user.id";
    internal const string TagAssetId = "asset.id";
    internal const string TagAssetType = "asset.type";
    internal const string TagAssetsCount = "assets.count";
    internal const string TagOperation = "operation";
    internal const string TagTypeSwitch = "type_switch";

    // Operation tag values for the query-duration histogram
    internal const string OperationList = "list";
    internal const string OperationGet = "get";

    // Metric instrument names
    private const string METRIC_ASSETS_CREATED = "assets.created.count";
    private const string METRIC_ASSETS_UPDATED = "assets.updated.count";
    private const string METRIC_ASSETS_DELETED = "assets.deleted.count";
    private const string METRIC_ASSETS_QUERY_DURATION_MS = "assets.query.duration.ms";

    internal static readonly ActivitySource ActivitySource = new(ObservabilityDomains.Assets);
    private static readonly Meter Meter = new(ObservabilityDomains.Assets);

    private static readonly Counter<long> AssetsCreated =
        Meter.CreateCounter<long>(METRIC_ASSETS_CREATED);

    private static readonly Counter<long> AssetsUpdated =
        Meter.CreateCounter<long>(METRIC_ASSETS_UPDATED);

    private static readonly Counter<long> AssetsDeleted =
        Meter.CreateCounter<long>(METRIC_ASSETS_DELETED);

    private static readonly Histogram<double> AssetsQueryDuration =
        Meter.CreateHistogram<double>(METRIC_ASSETS_QUERY_DURATION_MS);

    internal static void RecordCreated(string assetType)
    {
        var tags = new TagList { { TagAssetType, assetType } };
        AssetsCreated.Add(1, tags);
    }

    internal static void RecordUpdated(string assetType, bool typeSwitch = false)
    {
        var tags = new TagList { { TagAssetType, assetType } };
        if (typeSwitch)
            tags.Add(TagTypeSwitch, true);
        AssetsUpdated.Add(1, tags);
    }

    internal static void RecordDeleted(string assetType)
    {
        var tags = new TagList { { TagAssetType, assetType } };
        AssetsDeleted.Add(1, tags);
    }

    internal static void RecordQueryDuration(string operation, double milliseconds)
    {
        var tags = new TagList { { TagOperation, operation } };
        AssetsQueryDuration.Record(milliseconds, tags);
    }
}
