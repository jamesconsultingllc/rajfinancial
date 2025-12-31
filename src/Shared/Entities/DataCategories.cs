namespace RajFinancial.Shared.Entities;

/// <summary>
///     Data categories that can be granted access to.
/// </summary>
public static class DataCategories
{
    public const string ACCOUNTS = "accounts";
    public const string ASSETS = "assets";
    public const string LIABILITIES = "liabilities";
    public const string BENEFICIARIES = "beneficiaries";
    public const string DOCUMENTS = "documents";
    public const string ANALYSIS = "analysis";
    public const string ALL = "all";

    public static readonly string[] AllCategories =
    [
        ACCOUNTS,
        ASSETS,
        LIABILITIES,
        BENEFICIARIES,
        DOCUMENTS,
        ANALYSIS
    ];
}