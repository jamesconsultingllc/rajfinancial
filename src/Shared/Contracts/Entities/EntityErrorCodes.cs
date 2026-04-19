namespace RajFinancial.Shared.Contracts.Entities;

/// <summary>
///     Error codes returned by entity validation and service operations.
///     Referenced by both API validators and client-side localization.
/// </summary>
public static class EntityErrorCodes
{
    // =========================================================================
    // Required fields
    // =========================================================================

    public const string NAME_REQUIRED = "ENTITY_NAME_REQUIRED";
    public const string TYPE_REQUIRED = "ENTITY_TYPE_REQUIRED";

    // =========================================================================
    // Max length
    // =========================================================================

    public const string NAME_MAX_LENGTH = "ENTITY_NAME_MAX_LENGTH";
    public const string SLUG_MAX_LENGTH = "ENTITY_SLUG_MAX_LENGTH";

    // =========================================================================
    // Invalid values
    // =========================================================================

    public const string TYPE_INVALID = "ENTITY_TYPE_INVALID";
    public const string SLUG_INVALID = "ENTITY_SLUG_INVALID";

    // =========================================================================
    // Trust metadata (cross-field validation)
    // =========================================================================

    public const string TRUST_TESTAMENTARY_MUST_BE_IRREVOCABLE =
        "ENTITY_TRUST_TESTAMENTARY_MUST_BE_IRREVOCABLE";
    public const string TRUST_OTHER_TYPE_DESCRIPTION_REQUIRED =
        "ENTITY_TRUST_OTHER_TYPE_DESCRIPTION_REQUIRED";
    public const string TRUST_GRANTOR_CANNOT_HAVE_INCOME_TREATMENT =
        "ENTITY_TRUST_GRANTOR_CANNOT_HAVE_INCOME_TREATMENT";

    // =========================================================================
    // Business rules
    // =========================================================================

    public const string PERSONAL_ALREADY_EXISTS = "ENTITY_PERSONAL_ALREADY_EXISTS";
    public const string PERSONAL_CANNOT_DELETE = "ENTITY_PERSONAL_CANNOT_DELETE";
    public const string PERSONAL_NAME_IMMUTABLE = "ENTITY_PERSONAL_NAME_IMMUTABLE";
    public const string PERSONAL_TYPE_IMMUTABLE = "ENTITY_PERSONAL_TYPE_IMMUTABLE";
    public const string SLUG_DUPLICATE = "ENTITY_SLUG_DUPLICATE";
    public const string NOT_FOUND = "ENTITY_NOT_FOUND";
    public const string PARENT_NOT_FOUND = "ENTITY_PARENT_NOT_FOUND";
    public const string PARENT_CYCLE = "ENTITY_PARENT_CYCLE";
    public const string HAS_CHILDREN_CANNOT_DELETE = "ENTITY_HAS_CHILDREN_CANNOT_DELETE";

    // =========================================================================
    // Role errors
    // =========================================================================

    public const string ROLE_NOT_FOUND = "ENTITY_ROLE_NOT_FOUND";
    public const string ROLE_INVALID_FOR_ENTITY_TYPE = "ENTITY_ROLE_INVALID_FOR_ENTITY_TYPE";
    public const string ROLE_OWNERSHIP_EXCEEDS_100 = "ENTITY_ROLE_OWNERSHIP_EXCEEDS_100";
    public const string ROLE_BENEFICIAL_INTEREST_EXCEEDS_100 =
        "ENTITY_ROLE_BENEFICIAL_INTEREST_EXCEEDS_100";
    public const string ROLE_OWNERSHIP_OUT_OF_RANGE = "ENTITY_ROLE_OWNERSHIP_OUT_OF_RANGE";
    public const string ROLE_BENEFICIAL_INTEREST_OUT_OF_RANGE =
        "ENTITY_ROLE_BENEFICIAL_INTEREST_OUT_OF_RANGE";
    public const string ROLE_CONTACT_REQUIRED = "ENTITY_ROLE_CONTACT_REQUIRED";
    public const string ROLE_CONTACT_NOT_FOUND = "ENTITY_ROLE_CONTACT_NOT_FOUND";
    public const string ROLE_TYPE_REQUIRED = "ENTITY_ROLE_TYPE_REQUIRED";
    public const string ROLE_DATE_RANGE_INVALID = "ENTITY_ROLE_DATE_RANGE_INVALID";
    public const string ROLE_OWNERSHIP_NOT_ALLOWED = "ENTITY_ROLE_OWNERSHIP_NOT_ALLOWED";
    public const string ROLE_BENEFICIAL_INTEREST_NOT_ALLOWED =
        "ENTITY_ROLE_BENEFICIAL_INTEREST_NOT_ALLOWED";
}
