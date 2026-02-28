using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for collectible assets (coins, art, trading cards, etc.).
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record CollectibleMetadata : IAssetMetadata
{
    /// <summary>Category of collectible.</summary>
    [MemoryPackOrder(0)]
    public required CollectibleCategory Category { get; init; }

    /// <summary>Free-text category when category is Other.</summary>
    [MemoryPackOrder(1)]
    public string? CustomCategory { get; init; }

    /// <summary>Physical condition of the item.</summary>
    [MemoryPackOrder(2)]
    public ItemCondition? Condition { get; init; }

    /// <summary>Ownership history or documented origin.</summary>
    [MemoryPackOrder(3)]
    public string? Provenance { get; init; }

    /// <summary>Serial number (for graded coins, authenticated cards, etc.).</summary>
    [MemoryPackOrder(4)]
    public string? SerialNumber { get; init; }

    /// <summary>Grading/authentication body (e.g. PSA, NGC, CGC, BGS).</summary>
    [MemoryPackOrder(5)]
    public string? CertificationBody { get; init; }

    /// <summary>Grading/authentication certificate number.</summary>
    [MemoryPackOrder(6)]
    public string? CertificationNumber { get; init; }

    /// <summary>Grade assigned (e.g. PSA 10, MS-70, CGC 9.8).</summary>
    [MemoryPackOrder(7)]
    public string? Grade { get; init; }

    /// <summary>Edition info (e.g. "1st Edition", "Limited 50/500").</summary>
    [MemoryPackOrder(8)]
    public string? Edition { get; init; }

    /// <summary>Creator or maker (for art, crafts).</summary>
    [MemoryPackOrder(9)]
    public string? Artist { get; init; }

    /// <summary>Name of the appraiser.</summary>
    [MemoryPackOrder(10)]
    public string? AppraiserName { get; init; }

    /// <summary>Date of most recent appraisal.</summary>
    [MemoryPackOrder(11)]
    public DateTime? LastAppraisalDate { get; init; }

    /// <summary>Value listed on insurance rider/schedule.</summary>
    [MemoryPackOrder(12)]
    public double? InsuredValue { get; init; }
}
