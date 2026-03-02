using MemoryPack;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Contracts.Assets.Metadata;

/// <summary>
///     A single position/holding within an investment account.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record Holding
{
    /// <summary>Ticker symbol (e.g. AAPL, VTSAX).</summary>
    [MemoryPackOrder(0)]
    public required string Ticker { get; init; }

    /// <summary>Full name of the security (e.g. "Apple Inc.").</summary>
    [MemoryPackOrder(1)]
    public string? Name { get; init; }

    /// <summary>Type of holding (stock, bond, ETF, etc.).</summary>
    [MemoryPackOrder(2)]
    public required HoldingType HoldingType { get; init; }

    /// <summary>Number of shares or units held.</summary>
    [MemoryPackOrder(3)]
    public required double Shares { get; init; }

    /// <summary>Total cost basis for this position.</summary>
    [MemoryPackOrder(4)]
    public double? CostBasis { get; init; }

    /// <summary>Current price per share.</summary>
    [MemoryPackOrder(5)]
    public double? CurrentPrice { get; init; }
}
