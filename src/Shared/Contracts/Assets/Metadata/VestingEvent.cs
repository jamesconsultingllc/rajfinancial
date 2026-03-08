using MemoryPack;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     A single vesting event in an RSU vesting schedule.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record VestingEvent
{
    /// <summary>Date when shares vest.</summary>
    [MemoryPackOrder(0)]
    public required DateTime Date { get; init; }

    /// <summary>Number of shares vesting on this date (supports fractional).</summary>
    [MemoryPackOrder(1)]
    public required double Shares { get; init; }
}
