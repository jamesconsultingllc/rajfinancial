using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for business ownership assets.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record BusinessMetadata : IAssetMetadata
{
    /// <summary>Legal entity structure.</summary>
    [MemoryPackOrder(0)]
    public required BusinessEntityType EntityType { get; init; }

    /// <summary>Ownership stake percentage (greater than 0, up to 100).</summary>
    [MemoryPackOrder(1)]
    public required double OwnershipPercent { get; init; }

    /// <summary>Employer Identification Number (XX-XXXXXXX).</summary>
    [MemoryPackOrder(2)]
    public string? Ein { get; init; }

    /// <summary>6-digit NAICS industry code.</summary>
    [MemoryPackOrder(3)]
    public string? NaicsCode { get; init; }

    /// <summary>9-digit D-U-N-S number.</summary>
    [MemoryPackOrder(4)]
    public string? DunsNumber { get; init; }

    /// <summary>Free-text industry description (supplements NAICS).</summary>
    [MemoryPackOrder(5)]
    public string? Industry { get; init; }

    /// <summary>Most recent annual revenue.</summary>
    [MemoryPackOrder(6)]
    public double? AnnualRevenue { get; init; }

    /// <summary>Employee headcount.</summary>
    [MemoryPackOrder(7)]
    public int? NumberOfEmployees { get; init; }

    /// <summary>Date the business was established.</summary>
    [MemoryPackOrder(8)]
    public DateTime? FoundedDate { get; init; }

    /// <summary>State formation and foreign qualification registrations.</summary>
    [MemoryPackOrder(9)]
    public StateRegistration[]? Registrations { get; init; }
}
