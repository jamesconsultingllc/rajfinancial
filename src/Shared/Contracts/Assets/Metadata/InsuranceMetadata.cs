using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for insurance policy assets (whole life, term, universal, annuity).
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record InsuranceMetadata : IAssetMetadata
{
    /// <summary>Type of insurance policy.</summary>
    [MemoryPackOrder(0)]
    public required InsurancePolicyType PolicyType { get; init; }

    /// <summary>Cash surrender value (not applicable for term life).</summary>
    [MemoryPackOrder(1)]
    public double? CashValue { get; init; }

    /// <summary>Base death benefit amount.</summary>
    [MemoryPackOrder(2)]
    public double? DeathBenefit { get; init; }

    /// <summary>Base premium payment amount.</summary>
    [MemoryPackOrder(3)]
    public double? PremiumAmount { get; init; }

    /// <summary>How frequently premiums are paid.</summary>
    [MemoryPackOrder(4)]
    public PremiumFrequency? PremiumFrequency { get; init; }

    /// <summary>Date when coverage began.</summary>
    [MemoryPackOrder(5)]
    public DateTime? PolicyStartDate { get; init; }

    /// <summary>Date when term expires (term life only).</summary>
    [MemoryPackOrder(6)]
    public DateTime? PolicyEndDate { get; init; }

    /// <summary>Policy riders (add-on benefits).</summary>
    [MemoryPackOrder(7)]
    public PolicyRider[]? Riders { get; init; }

    /// <summary>How dividends are applied (whole life only).</summary>
    [MemoryPackOrder(8)]
    public DividendOption? DividendOption { get; init; }

    /// <summary>Most recent annual dividend amount.</summary>
    [MemoryPackOrder(9)]
    public double? AnnualDividend { get; init; }
}
