using MemoryPack;
using RajFinancial.Shared.Contracts.Assets.Metadata;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets;

/// <summary>
///     Request body for creating a new asset.
/// </summary>
/// <remarks>
///     Used by <c>POST /api/assets</c>. The asset is automatically assigned
///     to the authenticated user. Depreciation fields are optional — if provided,
///     the service will compute depreciation values on subsequent reads.
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record CreateAssetRequest
{
    /// <summary>Display name of the asset (required, max 200 characters).</summary>
    [MemoryPackOrder(0)]
    public required string Name { get; init; }

    /// <summary>Classification of the asset (required).</summary>
    [MemoryPackOrder(1)]
    public required AssetType Type { get; init; }

    /// <summary>Current value of the asset (required, must be >= 0).</summary>
    [MemoryPackOrder(2)]
    public required double CurrentValue { get; init; }

    /// <summary>Original purchase price (cost basis).</summary>
    [MemoryPackOrder(3)]
    public double? PurchasePrice { get; init; }

    /// <summary>Date the asset was purchased.</summary>
    [MemoryPackOrder(4)]
    public DateTime? PurchaseDate { get; init; }

    /// <summary>Optional description or notes (max 2000 characters).</summary>
    [MemoryPackOrder(5)]
    public string? Description { get; init; }

    /// <summary>Physical location of the asset (max 500 characters).</summary>
    [MemoryPackOrder(6)]
    public string? Location { get; init; }

    /// <summary>Account or policy number (max 100 characters).</summary>
    [MemoryPackOrder(7)]
    public string? AccountNumber { get; init; }

    /// <summary>Financial institution holding the asset (max 200 characters).</summary>
    [MemoryPackOrder(8)]
    public string? InstitutionName { get; init; }

    // =========================================================================
    // Depreciation (optional)
    // =========================================================================

    /// <summary>Depreciation method to apply. Defaults to None if not specified.</summary>
    [MemoryPackOrder(9)]
    public DepreciationMethod? DepreciationMethod { get; init; }

    /// <summary>Estimated residual value at end of useful life (must be >= 0).</summary>
    [MemoryPackOrder(10)]
    public double? SalvageValue { get; init; }

    /// <summary>Expected useful life in months (must be > 0).</summary>
    [MemoryPackOrder(11)]
    public int? UsefulLifeMonths { get; init; }

    /// <summary>Date the asset was placed in service. Defaults to PurchaseDate if not set.</summary>
    [MemoryPackOrder(12)]
    public DateTime? InServiceDate { get; init; }

    // =========================================================================
    // Valuation (optional)
    // =========================================================================

    /// <summary>Current fair market value (appraisal or market data).</summary>
    [MemoryPackOrder(13)]
    public double? MarketValue { get; init; }

    /// <summary>Date when MarketValue was last updated.</summary>
    [MemoryPackOrder(14)]
    public DateTime? LastValuationDate { get; init; }

    /// <summary>Per-type metadata (vehicle details, real estate details, etc.).</summary>
    [MemoryPackOrder(15)]
    public IAssetMetadata? Metadata { get; init; }
}
