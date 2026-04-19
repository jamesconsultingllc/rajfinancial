// ============================================================================
// RAJ Financial - Depreciable Asset Entity
// ============================================================================
// Extends Asset with depreciation calculation fields for physical assets
// that lose value over time (real estate, vehicles, equipment, etc.).
// ============================================================================

namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     An asset that loses value over time and supports depreciation calculations.
/// </summary>
/// <remarks>
///     <para>
///         Extends <see cref="Asset"/> with fields needed for depreciation:
///         method, salvage value, useful life, and in-service date.
///     </para>
///     <para>
///         Depreciation is computed at read time by the service layer, not stored.
///         This avoids stale computed columns and keeps the entity as the single
///         source of truth for raw financial data.
///     </para>
///     <para>
///         <b>Typical depreciable asset types:</b>
///         <see cref="AssetType.RealEstate"/>, <see cref="AssetType.Vehicle"/>,
///         <see cref="AssetType.Business"/>, <see cref="AssetType.PersonalProperty"/>,
///         <see cref="AssetType.IntellectualProperty"/>.
///     </para>
///     <para>
///         <b>Non-depreciable asset types</b> (use base <see cref="Asset"/>):
///         <see cref="AssetType.BankAccount"/>, <see cref="AssetType.Investment"/>,
///         <see cref="AssetType.Retirement"/>, <see cref="AssetType.Insurance"/>,
///         <see cref="AssetType.Cryptocurrency"/>.
///     </para>
/// </remarks>
public class DepreciableAsset : Asset
{
    /// <summary>
    ///     Method used to calculate depreciation for this asset.
    /// </summary>
    public DepreciationMethod DepreciationMethod { get; set; }

    /// <summary>
    ///     Estimated residual value at the end of the asset's useful life.
    ///     Used in depreciation calculations as the floor value.
    ///     Null if not specified (distinct from zero residual value).
    /// </summary>
    public decimal? SalvageValue { get; set; }

    /// <summary>
    ///     Expected useful life of the asset in months.
    ///     Must be greater than zero for depreciation calculations.
    ///     Null if not specified.
    /// </summary>
    public int? UsefulLifeMonths { get; set; }

    /// <summary>
    ///     Date the asset was placed in service for depreciation purposes.
    ///     Depreciation begins from this date. Falls back to <see cref="Asset.PurchaseDate"/> if not set.
    /// </summary>
    public DateTimeOffset? InServiceDate { get; set; }
}
