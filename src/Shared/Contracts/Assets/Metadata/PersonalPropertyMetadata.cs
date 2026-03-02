using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for personal property assets (jewelry, electronics, firearms, etc.).
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record PersonalPropertyMetadata : IAssetMetadata
{
    /// <summary>Category of personal property.</summary>
    [MemoryPackOrder(0)]
    public required PersonalPropertyCategory Category { get; init; }

    /// <summary>Free-text category when category is Other.</summary>
    [MemoryPackOrder(1)]
    public string? CustomCategory { get; init; }

    /// <summary>Physical condition of the item.</summary>
    [MemoryPackOrder(2)]
    public ItemCondition? Condition { get; init; }

    /// <summary>Manufacturer serial number.</summary>
    [MemoryPackOrder(3)]
    public string? SerialNumber { get; init; }

    /// <summary>Brand or manufacturer (e.g. Rolex, Gibson, Apple).</summary>
    [MemoryPackOrder(4)]
    public string? Brand { get; init; }

    /// <summary>Manufacturer model number.</summary>
    [MemoryPackOrder(5)]
    public string? ModelNumber { get; init; }

    /// <summary>Name of the appraiser.</summary>
    [MemoryPackOrder(6)]
    public string? AppraiserName { get; init; }

    /// <summary>Date of most recent appraisal.</summary>
    [MemoryPackOrder(7)]
    public DateTime? LastAppraisalDate { get; init; }

    /// <summary>Value listed on insurance rider/schedule.</summary>
    [MemoryPackOrder(8)]
    public double? InsuredValue { get; init; }
}
