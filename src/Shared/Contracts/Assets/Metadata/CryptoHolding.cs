using MemoryPack;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     A single cryptocurrency position within a wallet or exchange account.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record CryptoHolding
{
    /// <summary>Coin/token ticker symbol (e.g. BTC, ETH, SOL).</summary>
    [MemoryPackOrder(0)]
    public required string CoinSymbol { get; init; }

    /// <summary>Full coin/token name (e.g. Bitcoin, Ethereum).</summary>
    [MemoryPackOrder(1)]
    public string? CoinName { get; init; }

    /// <summary>Number of coins/tokens held.</summary>
    [MemoryPackOrder(2)]
    public required double Quantity { get; init; }

    /// <summary>Total cost basis for this position.</summary>
    [MemoryPackOrder(3)]
    public double? CostBasis { get; init; }

    /// <summary>Current price per coin/token.</summary>
    [MemoryPackOrder(4)]
    public double? CurrentPrice { get; init; }

    /// <summary>Whether this position is currently staked.</summary>
    [MemoryPackOrder(5)]
    public bool? IsStaking { get; init; }

    /// <summary>Staking APY percentage (only when staking).</summary>
    [MemoryPackOrder(6)]
    public double? StakingApy { get; init; }
}
