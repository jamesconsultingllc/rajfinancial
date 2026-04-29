// ============================================================================
// RAJ Financial - Assets Telemetry
// ============================================================================
// Centralized ActivitySource, instrument names, tag keys and activity names
// for the Assets domain. Extracted out of AssetService / AssetFunctions to
// satisfy the Services_ShouldNotHavePrivateStaticMethods architecture
// invariant and the AGENT.md "No Magic Strings or Numbers" rule.
//
// Business counters (assets.created.count / updated / deleted) are owned by
// `RajFinancial.Api.Data.Interceptors.BusinessEventsInterceptor` and emitted
// centrally on successful SaveChangesAsync. The instrument names live on
// `RajFinancial.Api.Observability.TelemetryMeters`. Do NOT add per-domain
// `Record*` helpers — extend the interceptor's entity→counter mapping instead.
//
// Span naming follows the Phase 1c decision (Option b2, see
// docs/plans/phase-1c-span-validation.md §7):
//   - Function-layer span:  Assets.<Op>
//   - Service-layer span:   Assets.<Op>.Service
// ============================================================================

using System.Diagnostics;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Observability;

namespace RajFinancial.Api.Services.AssetService;

/// <summary>
///     Owns the Assets-domain ActivitySource and shared activity / tag names.
///     Counters live on <see cref="TelemetryMeters"/> and are emitted by
///     <see cref="RajFinancial.Api.Data.Interceptors.BusinessEventsInterceptor"/>.
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

    internal static readonly ActivitySource ActivitySource = new(ObservabilityDomains.Assets);
}
