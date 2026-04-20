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
    Cd,

    /// <summary>High-Yield Savings Account.</summary>
    Hysa,

    /// <summary>Other bank account type.</summary>
    Other
}
