using MemoryPack;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     A state business registration (formation or foreign qualification).
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record StateRegistration
{
    /// <summary>2-letter state code (e.g. DE, TX).</summary>
    [MemoryPackOrder(0)]
    public required string State { get; init; }

    /// <summary>Secretary of State filing number.</summary>
    [MemoryPackOrder(1)]
    public string? SosNumber { get; init; }

    /// <summary>Date of filing.</summary>
    [MemoryPackOrder(2)]
    public DateTime? FilingDate { get; init; }

    /// <summary>True if this is the state where the entity was formed.</summary>
    [MemoryPackOrder(3)]
    public required bool IsFormationState { get; init; }
}
