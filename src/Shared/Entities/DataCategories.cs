namespace RajFinancial.Shared.Entities;

/// <summary>
///     Data categories that can be granted access to.
/// </summary>
public static class DataCategories
{
    public const string Accounts = "accounts";
    public const string Assets = "assets";
    public const string Liabilities = "liabilities";
    public const string Beneficiaries = "beneficiaries";
    public const string Documents = "documents";
    public const string Analysis = "analysis";
    public const string All = "all";

    public static readonly string[] AllCategories =
    [
        Accounts,
        Assets,
        Liabilities,
        Beneficiaries,
        Documents,
        Analysis
    ];
}