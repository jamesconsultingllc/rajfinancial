using MemoryPack;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Shared.Contracts.Assets;

/// <summary>
///     Data transfer object for asset list/summary views.
/// </summary>
/// <remarks>
///     Used by <c>GET /api/assets</c> to return a collection of assets.
///     Does not include beneficiary assignments or full depreciation details —
///     use <see cref="AssetDetailDto"/> for single-asset views.
/// </remarks>
[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AssetDto
{
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

    /// <summary>Whether this asset supports depreciation (DepreciableAsset subtype).</summary>
    [MemoryPackOrder(10)]
    public required bool IsDepreciable { get; init; }

    /// <summary>Whether the asset has been disposed of.</summary>
    [MemoryPackOrder(11)]
    public required bool IsDisposed { get; init; }

    /// <summary>Whether this asset has any beneficiary assignments.</summary>
    [MemoryPackOrder(12)]
    public required bool HasBeneficiaries { get; init; }

    /// <summary>Date and time the asset was created.</summary>
    [MemoryPackOrder(13)]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Date and time the asset was last updated.</summary>
    [MemoryPackOrder(14)]
    public DateTimeOffset? UpdatedAt { get; init; }
}
