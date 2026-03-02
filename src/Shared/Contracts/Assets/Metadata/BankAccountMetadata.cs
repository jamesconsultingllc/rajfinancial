using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for bank/deposit account assets.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record BankAccountMetadata : IAssetMetadata
{
    /// <summary>Type of bank account.</summary>
    [MemoryPackOrder(0)]
    public required BankAccountType BankAccountType { get; init; }

    /// <summary>9-digit ABA routing number.</summary>
    [MemoryPackOrder(1)]
    public string? RoutingNumber { get; init; }

    /// <summary>Annual Percentage Yield as percentage (e.g. 4.5 for 4.5%).</summary>
    [MemoryPackOrder(2)]
    public double? Apy { get; init; }

    /// <summary>CD maturity date (only for CD accounts).</summary>
    [MemoryPackOrder(3)]
    public DateTime? MaturityDate { get; init; }

    /// <summary>CD term length in months (only for CD accounts).</summary>
    [MemoryPackOrder(4)]
    public int? Term { get; init; }

    /// <summary>Whether the account is jointly held.</summary>
    [MemoryPackOrder(5)]
    public bool? IsJointAccount { get; init; }
}
