using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     Metadata for investment/brokerage account assets.
///     When <see cref="AccountType"/> is <see cref="InvestmentAccountType.RSU"/>,
///     RSU-specific fields are populated instead of <see cref="Holdings"/>.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record InvestmentMetadata : IAssetMetadata
{
    /// <summary>Type of investment account.</summary>
    [MemoryPackOrder(0)]
    public required InvestmentAccountType AccountType { get; init; }

    /// <summary>Array of positions held in this account (not used for RSU).</summary>
    [MemoryPackOrder(1)]
    public Holding[]? Holdings { get; init; }

    // =========================================================================
    // RSU-specific fields (only when AccountType == RSU)
    // =========================================================================

    /// <summary>Date RSUs were granted.</summary>
    [MemoryPackOrder(2)]
    public DateTime? GrantDate { get; init; }

    /// <summary>Total shares in the RSU grant.</summary>
    [MemoryPackOrder(3)]
    public int? TotalSharesGranted { get; init; }

    /// <summary>Number of shares vested so far.</summary>
    [MemoryPackOrder(4)]
    public int? SharesVested { get; init; }

    /// <summary>Vesting schedule events.</summary>
    [MemoryPackOrder(5)]
    public VestingEvent[]? VestingSchedule { get; init; }

    /// <summary>Company stock ticker symbol.</summary>
    [MemoryPackOrder(6)]
    public string? Ticker { get; init; }

    /// <summary>Fair market value per share at grant date.</summary>
    [MemoryPackOrder(7)]
    public double? GrantPricePerShare { get; init; }
}
