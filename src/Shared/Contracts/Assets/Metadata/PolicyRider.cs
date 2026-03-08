using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     An insurance policy rider (add-on benefit).
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record PolicyRider
{
    /// <summary>Type of rider.</summary>
    [MemoryPackOrder(0)]
    public required PolicyRiderType RiderType { get; init; }

    /// <summary>Custom name when rider type is Other.</summary>
    [MemoryPackOrder(1)]
    public string? Name { get; init; }

    /// <summary>Rider benefit amount or paid-up additions total value.</summary>
    [MemoryPackOrder(2)]
    public double? Value { get; init; }

    /// <summary>Annual rider premium/cost.</summary>
    [MemoryPackOrder(3)]
    public double? AnnualCost { get; init; }
}
