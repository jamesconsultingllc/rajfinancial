namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Options for how whole life insurance dividends are applied.
/// </summary>
public enum DividendOption
{
    /// <summary>Use dividends to purchase paid-up additional insurance.</summary>
    PaidUpAdditions,

    /// <summary>Receive dividends as a cash payment.</summary>
    CashPayment,

    /// <summary>Apply dividends to reduce premium payments.</summary>
    PremiumReduction,

    /// <summary>Leave dividends on deposit to accumulate at interest.</summary>
    AccumulateAtInterest,

    /// <summary>Other dividend option.</summary>
    Other
}
