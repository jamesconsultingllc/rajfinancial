// ============================================================================
// RAJ Financial - Asset Entity
// ============================================================================
// Base class for all user-owned assets. Subclassed by DepreciableAsset for
// assets that lose value over time (real estate, vehicles, equipment).
// Uses EF Core TPH (Table Per Hierarchy) with a discriminator column.
// ============================================================================

using MemoryPack;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Base class for a manually tracked asset owned by a user.
/// </summary>
/// <remarks>
///     <para>
///         Assets are user-scoped resources protected by the three-tier authorization model
///         (Owner > DataAccessGrant > Administrator). All access is gated through
///         <see cref="DataCategories.Assets"/>.
///     </para>
///     <para>
///         Assets that lose value over time (real estate, vehicles, equipment) should use
///         <see cref="DepreciableAsset"/>, which adds depreciation calculation fields.
///         Financial assets (bank accounts, investments, retirement) use this base class directly.
///     </para>
///     <para>
///         <b>EF Core:</b> Uses TPH (Table Per Hierarchy) inheritance with a discriminator column.
///     </para>
/// </remarks>
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class Asset
{
    /// <summary>
    ///     Unique identifier for the asset.
    /// </summary>
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }

    /// <summary>
    ///     The user who owns this asset (Entra Object ID).
    /// </summary>
    [MemoryPackOrder(1)]
    public Guid UserId { get; set; }

    /// <summary>
    ///     Display name of the asset (e.g., "Chase Checking", "Fidelity 401k").
    /// </summary>
    [MemoryPackOrder(2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Classification of the asset for categorization and reporting.
    /// </summary>
    [MemoryPackOrder(3)]
    public AssetType Type { get; set; }

    /// <summary>
    ///     Current value of the asset as entered by the user.
    /// </summary>
    [MemoryPackOrder(4)]
    public decimal CurrentValue { get; set; }

    /// <summary>
    ///     Original purchase price or initial deposit amount.
    /// </summary>
    [MemoryPackOrder(5)]
    public decimal? PurchasePrice { get; set; }

    /// <summary>
    ///     Date the asset was purchased or account was opened.
    /// </summary>
    [MemoryPackOrder(6)]
    public DateTimeOffset? PurchaseDate { get; set; }

    /// <summary>
    ///     Optional description or notes about the asset.
    /// </summary>
    [MemoryPackOrder(7)]
    public string? Description { get; set; }

    /// <summary>
    ///     Physical location of the asset (address, branch, safe deposit box).
    /// </summary>
    [MemoryPackOrder(8)]
    public string? Location { get; set; }

    /// <summary>
    ///     Account or policy number associated with the asset.
    /// </summary>
    [MemoryPackOrder(9)]
    public string? AccountNumber { get; set; }

    /// <summary>
    ///     Financial institution holding the asset (bank, brokerage, insurer).
    /// </summary>
    [MemoryPackOrder(10)]
    public string? InstitutionName { get; set; }

    // =========================================================================
    // Disposal
    // =========================================================================

    /// <summary>
    ///     Whether the asset has been disposed of (sold, closed, transferred).
    ///     Disposed assets are excluded from net worth calculations.
    /// </summary>
    [MemoryPackOrder(11)]
    public bool IsDisposed { get; set; }

    /// <summary>
    ///     Date the asset was disposed of.
    /// </summary>
    [MemoryPackOrder(12)]
    public DateTimeOffset? DisposalDate { get; set; }

    /// <summary>
    ///     Sale price or fair market value at the time of disposal.
    /// </summary>
    [MemoryPackOrder(13)]
    public decimal? DisposalPrice { get; set; }

    /// <summary>
    ///     Notes about the disposal (e.g., "Account closed", "Sold to private buyer").
    /// </summary>
    [MemoryPackOrder(14)]
    public string? DisposalNotes { get; set; }

    // =========================================================================
    // Valuation
    // =========================================================================

    /// <summary>
    ///     Current fair market value as determined by appraisal or market data.
    ///     Separate from <see cref="CurrentValue"/> which is the user's own estimate.
    /// </summary>
    [MemoryPackOrder(15)]
    public decimal? MarketValue { get; set; }

    /// <summary>
    ///     Date when <see cref="MarketValue"/> was last updated or appraised.
    /// </summary>
    [MemoryPackOrder(16)]
    public DateTimeOffset? LastValuationDate { get; set; }

    // =========================================================================
    // Audit
    // =========================================================================

    /// <summary>
    ///     Date and time the asset record was created.
    ///     Set by the service layer at creation time.
    /// </summary>
    [MemoryPackOrder(17)]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    ///     Date and time the asset record was last updated.
    ///     Null if never updated after initial creation.
    /// </summary>
    [MemoryPackOrder(18)]
    public DateTimeOffset? UpdatedAt { get; set; }
}
