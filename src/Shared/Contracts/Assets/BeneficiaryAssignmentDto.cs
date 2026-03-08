using MemoryPack;

namespace RajFinancial.Shared.Contracts.Assets;

/// <summary>
///     Beneficiary assignment details for an asset.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
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
    public required double AllocationPercent { get; init; }

    /// <summary>Type of beneficiary (Primary or Contingent).</summary>
    [MemoryPackOrder(4)]
    public required string Type { get; init; }
}