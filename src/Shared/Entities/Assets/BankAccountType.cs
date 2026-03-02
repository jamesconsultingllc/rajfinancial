namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Classification of bank/deposit account types.
/// </summary>
public enum BankAccountType
{
    /// <summary>Checking/current account.</summary>
    Checking,

    /// <summary>Standard savings account.</summary>
    Savings,

    /// <summary>Money market account.</summary>
    MoneyMarket,

    /// <summary>Certificate of Deposit.</summary>
    CD,

    /// <summary>High-Yield Savings Account.</summary>
    HYSA,

    /// <summary>Other bank account type.</summary>
    Other
}
