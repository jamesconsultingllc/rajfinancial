namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Classification of investment holdings within a brokerage account.
/// </summary>
public enum HoldingType
{
    /// <summary>Individual stocks / equities.</summary>
    Stocks,

    /// <summary>Bonds (corporate, municipal, treasury).</summary>
    Bonds,

    /// <summary>Mutual funds.</summary>
    MutualFunds,

    /// <summary>Exchange-traded funds.</summary>
    Etf,

    /// <summary>Options contracts.</summary>
    Options,

    /// <summary>Other holding type not listed.</summary>
    Other
}
