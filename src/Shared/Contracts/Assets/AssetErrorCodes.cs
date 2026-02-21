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

    public const string NAME_REQUIRED = "ASSET_NAME_REQUIRED";
    public const string USEFUL_LIFE_REQUIRED = "ASSET_USEFUL_LIFE_REQUIRED";

    // =========================================================================
    // Max length
    // =========================================================================

    public const string NAME_MAX_LENGTH = "ASSET_NAME_MAX_LENGTH";
    public const string DESCRIPTION_MAX_LENGTH = "ASSET_DESCRIPTION_MAX_LENGTH";
    public const string LOCATION_MAX_LENGTH = "ASSET_LOCATION_MAX_LENGTH";
    public const string ACCOUNT_NUMBER_MAX_LENGTH = "ASSET_ACCOUNT_NUMBER_MAX_LENGTH";
    public const string INSTITUTION_NAME_MAX_LENGTH = "ASSET_INSTITUTION_NAME_MAX_LENGTH";

    // =========================================================================
    // Invalid values
    // =========================================================================

    public const string TYPE_INVALID = "ASSET_TYPE_INVALID";
    public const string DEPRECIATION_METHOD_INVALID = "ASSET_DEPRECIATION_METHOD_INVALID";
    public const string USEFUL_LIFE_INVALID = "ASSET_USEFUL_LIFE_INVALID";

    // =========================================================================
    // Numeric constraints
    // =========================================================================

    public const string VALUE_NEGATIVE = "ASSET_VALUE_NEGATIVE";
    public const string PURCHASE_PRICE_NEGATIVE = "ASSET_PURCHASE_PRICE_NEGATIVE";
    public const string SALVAGE_VALUE_NEGATIVE = "ASSET_SALVAGE_VALUE_NEGATIVE";
    public const string MARKET_VALUE_NEGATIVE = "ASSET_MARKET_VALUE_NEGATIVE";

    // =========================================================================
    // Service-level errors
    // =========================================================================

    public const string NOT_FOUND = "ASSET_NOT_FOUND";
}
