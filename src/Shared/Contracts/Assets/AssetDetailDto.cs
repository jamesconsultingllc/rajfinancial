using MemoryPack;
using RajFinancial.Shared.Contracts.Assets.Metadata;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets;

/// <summary>
///     Detailed data transfer object for a single asset view.
/// </summary>
/// <remarks>
///     Used by <c>GET /api/assets/{id}</c>. Includes all fields from <see cref="AssetDto" />
///     plus depreciation details (computed at read time), disposal info, valuation,
///     and beneficiary assignments.
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record AssetDetailDto
{
    // =========================================================================
    // Core
    // =========================================================================

    /// <summary>Unique identifier for the asset.</summary>
    [MemoryPackOrder(0)]
    public required Guid Id { get; init; }

    /// <summary>Display name of the asset.</summary>
    [MemoryPackOrder(1)]
    public required string Name { get; init; }

    /// <summary>Classification of the asset.</summary>
    [MemoryPackOrder(2)]
    public required AssetType Type { get; init; }

    /// <summary>Current value as entered by the user.</summary>
    [MemoryPackOrder(3)]
    public required double CurrentValue { get; init; }

    /// <summary>Original purchase price (cost basis).</summary>
    [MemoryPackOrder(4)]
    public double? PurchasePrice { get; init; }

    /// <summary>Date the asset was purchased.</summary>
    [MemoryPackOrder(5)]
    public DateTime? PurchaseDate { get; init; }

    /// <summary>Optional description or notes.</summary>
    [MemoryPackOrder(6)]
    public string? Description { get; init; }

    /// <summary>Physical location of the asset.</summary>
    [MemoryPackOrder(7)]
    public string? Location { get; init; }

    /// <summary>Account or policy number.</summary>
    [MemoryPackOrder(8)]
    public string? AccountNumber { get; init; }

    /// <summary>Financial institution holding the asset.</summary>
    [MemoryPackOrder(9)]
    public string? InstitutionName { get; init; }

    // =========================================================================
    // Depreciation (input fields — populated only for DepreciableAsset)
    // =========================================================================

    /// <summary>Whether this asset supports depreciation (DepreciableAsset subtype).</summary>
    [MemoryPackOrder(10)]
    public required bool IsDepreciable { get; init; }

    /// <summary>Depreciation method applied to this asset.</summary>
    [MemoryPackOrder(11)]
    public DepreciationMethod? DepreciationMethod { get; init; }

    /// <summary>Estimated residual value at end of useful life.</summary>
    [MemoryPackOrder(12)]
    public double? SalvageValue { get; init; }

    /// <summary>Expected useful life in months.</summary>
    [MemoryPackOrder(13)]
    public int? UsefulLifeMonths { get; init; }

    /// <summary>Date the asset was placed in service.</summary>
    [MemoryPackOrder(14)]
    public DateTime? InServiceDate { get; init; }

    // =========================================================================
    // Depreciation (computed at read time — not stored)
    // =========================================================================

    /// <summary>Total depreciation accumulated from in-service date to now.</summary>
    [MemoryPackOrder(15)]
    public double? AccumulatedDepreciation { get; init; }

    /// <summary>Current book value: PurchasePrice - AccumulatedDepreciation.</summary>
    [MemoryPackOrder(16)]
    public double? BookValue { get; init; }

    /// <summary>Depreciation expense per month under the current method.</summary>
    [MemoryPackOrder(17)]
    public double? MonthlyDepreciation { get; init; }

    /// <summary>Percentage of useful life elapsed (0.0 to 1.0).</summary>
    [MemoryPackOrder(18)]
    public double? DepreciationPercentComplete { get; init; }

    // =========================================================================
    // Disposal
    // =========================================================================

    /// <summary>Whether the asset has been disposed of.</summary>
    [MemoryPackOrder(19)]
    public required bool IsDisposed { get; init; }

    /// <summary>Date the asset was disposed of.</summary>
    [MemoryPackOrder(20)]
    public DateTime? DisposalDate { get; init; }

    /// <summary>Sale price or fair market value at disposal.</summary>
    [MemoryPackOrder(21)]
    public double? DisposalPrice { get; init; }

    /// <summary>Notes about the disposal.</summary>
    [MemoryPackOrder(22)]
    public string? DisposalNotes { get; init; }

    // =========================================================================
    // Valuation
    // =========================================================================

    /// <summary>Current fair market value (appraisal or market data).</summary>
    [MemoryPackOrder(23)]
    public double? MarketValue { get; init; }

    /// <summary>Date the market value was last updated.</summary>
    [MemoryPackOrder(24)]
    public DateTime? LastValuationDate { get; init; }

    // =========================================================================
    // Beneficiaries
    // =========================================================================

    /// <summary>Whether this asset has any beneficiary assignments.</summary>
    [MemoryPackOrder(25)]
    public required bool HasBeneficiaries { get; init; }

    /// <summary>Beneficiary assignments for this asset.</summary>
    [MemoryPackOrder(26)]
    public BeneficiaryAssignmentDto[] Beneficiaries { get; init; } = [];

    // =========================================================================
    // Audit
    // =========================================================================

    /// <summary>Date and time the asset was created.</summary>
    [MemoryPackOrder(27)]
    public required DateTime CreatedAt { get; init; }

    /// <summary>Date and time the asset was last updated. Null if never updated after creation.</summary>
    [MemoryPackOrder(28)]
    public DateTime? UpdatedAt { get; init; }

    /// <summary>Per-type metadata (vehicle details, real estate details, etc.).</summary>
    [MemoryPackOrder(29)]
    public IAssetMetadata? Metadata { get; init; }
}