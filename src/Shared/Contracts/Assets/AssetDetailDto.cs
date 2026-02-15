using MemoryPack;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Shared.Contracts.Assets;

/// <summary>
///     Detailed data transfer object for a single asset view.
/// </summary>
/// <remarks>
///     Used by <c>GET /api/assets/{id}</c>. Includes all fields from <see cref="AssetDto"/>
///     plus depreciation details (computed at read time), disposal info, valuation,
///     and beneficiary assignments.
/// </remarks>
[MemoryPackable(GenerateType.VersionTolerant)]
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
    public required decimal CurrentValue { get; init; }

    /// <summary>Original purchase price (cost basis).</summary>
    [MemoryPackOrder(4)]
    public decimal? PurchasePrice { get; init; }

    /// <summary>Date the asset was purchased.</summary>
    [MemoryPackOrder(5)]
    public DateTimeOffset? PurchaseDate { get; init; }

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
    public decimal? SalvageValue { get; init; }

    /// <summary>Expected useful life in months.</summary>
    [MemoryPackOrder(13)]
    public int? UsefulLifeMonths { get; init; }

    /// <summary>Date the asset was placed in service.</summary>
    [MemoryPackOrder(14)]
    public DateTimeOffset? InServiceDate { get; init; }

    // =========================================================================
    // Depreciation (computed at read time — not stored)
    // =========================================================================

    /// <summary>Total depreciation accumulated from in-service date to now.</summary>
    [MemoryPackOrder(15)]
    public decimal? AccumulatedDepreciation { get; init; }

    /// <summary>Current book value: PurchasePrice - AccumulatedDepreciation.</summary>
    [MemoryPackOrder(16)]
    public decimal? BookValue { get; init; }

    /// <summary>Depreciation expense per month under the current method.</summary>
    [MemoryPackOrder(17)]
    public decimal? MonthlyDepreciation { get; init; }

    /// <summary>Percentage of useful life elapsed (0.0 to 1.0).</summary>
    [MemoryPackOrder(18)]
    public decimal? DepreciationPercentComplete { get; init; }

    // =========================================================================
    // Disposal
    // =========================================================================

    /// <summary>Whether the asset has been disposed of.</summary>
    [MemoryPackOrder(19)]
    public required bool IsDisposed { get; init; }

    /// <summary>Date the asset was disposed of.</summary>
    [MemoryPackOrder(20)]
    public DateTimeOffset? DisposalDate { get; init; }

    /// <summary>Sale price or fair market value at disposal.</summary>
    [MemoryPackOrder(21)]
    public decimal? DisposalPrice { get; init; }

    /// <summary>Notes about the disposal.</summary>
    [MemoryPackOrder(22)]
    public string? DisposalNotes { get; init; }

    // =========================================================================
    // Valuation
    // =========================================================================

    /// <summary>Current fair market value (appraisal or market data).</summary>
    [MemoryPackOrder(23)]
    public decimal? MarketValue { get; init; }

    /// <summary>Date the market value was last updated.</summary>
    [MemoryPackOrder(24)]
    public DateTimeOffset? LastValuationDate { get; init; }

    // =========================================================================
    // Beneficiaries
    // =========================================================================

    /// <summary>Whether this asset has any beneficiary assignments.</summary>
    [MemoryPackOrder(25)]
    public required bool HasBeneficiaries { get; init; }

    /// <summary>Beneficiary assignments for this asset.</summary>
    [MemoryPackOrder(26)]
    public IReadOnlyList<BeneficiaryAssignmentDto> Beneficiaries { get; init; } = [];

    // =========================================================================
    // Audit
    // =========================================================================

    /// <summary>Date and time the asset was created.</summary>
    [MemoryPackOrder(27)]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Date and time the asset was last updated. Null if never updated after creation.</summary>
    [MemoryPackOrder(28)]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
///     Beneficiary assignment details for an asset.
/// </summary>
[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record BeneficiaryAssignmentDto
{
    /// <summary>Unique identifier of the beneficiary.</summary>
    [MemoryPackOrder(0)]
    public required Guid BeneficiaryId { get; init; }

    /// <summary>Display name of the beneficiary.</summary>
    [MemoryPackOrder(1)]
    public required string BeneficiaryName { get; init; }

    /// <summary>Relationship to the asset owner (e.g., "Spouse", "Child").</summary>
    [MemoryPackOrder(2)]
    public required string Relationship { get; init; }

    /// <summary>Percentage of the asset allocated to this beneficiary.</summary>
    [MemoryPackOrder(3)]
    public required decimal AllocationPercent { get; init; }

    /// <summary>Type of beneficiary (Primary or Contingent).</summary>
    [MemoryPackOrder(4)]
    public required string Type { get; init; }
}
