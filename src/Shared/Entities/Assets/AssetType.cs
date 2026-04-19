namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Classification of asset types for categorization and reporting.
/// </summary>
public enum AssetType
{
    /// <summary>
    ///     Real estate properties (home, rental, land, commercial).
    /// </summary>
    RealEstate = 0,

    /// <summary>
    ///     Motor vehicles (cars, trucks, motorcycles, boats).
    /// </summary>
    Vehicle = 1,

    /// <summary>
    ///     Investment accounts (brokerage, stocks, bonds, mutual funds).
    /// </summary>
    Investment = 2,

    /// <summary>
    ///     Retirement accounts (401k, IRA, Roth IRA, pension).
    /// </summary>
    Retirement = 3,

    /// <summary>
    ///     Bank accounts (checking, savings, money market, CDs).
    /// </summary>
    BankAccount = 4,

    /// <summary>
    ///     Insurance policies with cash value (whole life, universal life).
    /// </summary>
    Insurance = 5,

    /// <summary>
    ///     Business interests (ownership stakes, partnerships, LLCs).
    /// </summary>
    Business = 6,

    /// <summary>
    ///     Personal property (jewelry, furniture, electronics, art).
    /// </summary>
    PersonalProperty = 7,

    /// <summary>
    ///     Collectible items (coins, stamps, wine, sports memorabilia).
    /// </summary>
    Collectible = 8,

    /// <summary>
    ///     Cryptocurrency holdings (Bitcoin, Ethereum, etc.).
    /// </summary>
    Cryptocurrency = 9,

    /// <summary>
    ///     Intellectual property (patents, trademarks, copyrights).
    /// </summary>
    IntellectualProperty = 10,

    /// <summary>
    ///     Assets that do not fit other categories.
    /// </summary>
    Other = 99
}
