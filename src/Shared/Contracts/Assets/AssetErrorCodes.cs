namespace RajFinancial.Shared.Contracts.Assets;

/// <summary>
///     Error codes returned by asset validation and service operations.
///     Referenced by both API validators and client-side localization.
/// </summary>
public static class AssetErrorCodes
{
    // =========================================================================
    // Required fields
    // =========================================================================

    public const string NameRequired = "ASSET_NAME_REQUIRED";
    public const string UsefulLifeRequired = "ASSET_USEFUL_LIFE_REQUIRED";

    // =========================================================================
    // Max length
    // =========================================================================

    public const string NameMaxLength = "ASSET_NAME_MAX_LENGTH";
    public const string DescriptionMaxLength = "ASSET_DESCRIPTION_MAX_LENGTH";
    public const string LocationMaxLength = "ASSET_LOCATION_MAX_LENGTH";
    public const string AccountNumberMaxLength = "ASSET_ACCOUNT_NUMBER_MAX_LENGTH";
    public const string InstitutionNameMaxLength = "ASSET_INSTITUTION_NAME_MAX_LENGTH";

    // =========================================================================
    // Invalid values
    // =========================================================================

    public const string TypeInvalid = "ASSET_TYPE_INVALID";
    public const string DepreciationMethodInvalid = "ASSET_DEPRECIATION_METHOD_INVALID";
    public const string UsefulLifeInvalid = "ASSET_USEFUL_LIFE_INVALID";

    // =========================================================================
    // Numeric constraints
    // =========================================================================

    public const string ValueNegative = "ASSET_VALUE_NEGATIVE";
    public const string PurchasePriceNegative = "ASSET_PURCHASE_PRICE_NEGATIVE";
    public const string SalvageValueNegative = "ASSET_SALVAGE_VALUE_NEGATIVE";
    public const string MarketValueNegative = "ASSET_MARKET_VALUE_NEGATIVE";

    // =========================================================================
    // Service-level errors
    // =========================================================================

    public const string NotFound = "ASSET_NOT_FOUND";
}
