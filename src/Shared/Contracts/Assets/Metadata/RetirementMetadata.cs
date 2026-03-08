using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for retirement account assets (401k, IRA, Roth, etc.).
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record RetirementMetadata : IAssetMetadata
{
    /// <summary>Type of retirement account.</summary>
    [MemoryPackOrder(0)]
    public required RetirementAccountType AccountType { get; init; }

    /// <summary>Tiered employer match structure.</summary>
    [MemoryPackOrder(1)]
    public EmployerMatchTier[]? EmployerMatchTiers { get; init; }

    /// <summary>Current vesting percentage (0-100).</summary>
    [MemoryPackOrder(2)]
    public double? VestingPercent { get; init; }

    /// <summary>Total months to full vesting.</summary>
    [MemoryPackOrder(3)]
    public int? VestingScheduleMonths { get; init; }

    /// <summary>Expected yearly contribution amount.</summary>
    [MemoryPackOrder(4)]
    public double? ProjectedAnnualContribution { get; init; }

    /// <summary>Annual salary (used for match calculations).</summary>
    [MemoryPackOrder(5)]
    public double? Salary { get; init; }
}
