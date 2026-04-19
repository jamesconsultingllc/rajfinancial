namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Classification of investment/brokerage account types.
/// </summary>
public enum InvestmentAccountType
{
    /// <summary>Individual brokerage account.</summary>
    Individual,

    /// <summary>Joint brokerage account.</summary>
    Joint,

    /// <summary>Custodial account (UGMA/UTMA).</summary>
    Custodial,

    /// <summary>Trust-held investment account.</summary>
    Trust,

    /// <summary>Restricted Stock Unit grant.</summary>
    Rsu,

    /// <summary>Other investment account type.</summary>
    Other
}
