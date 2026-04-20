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

    public const string NameRequired = "ENTITY_NAME_REQUIRED";
    public const string TypeRequired = "ENTITY_TYPE_REQUIRED";

    // =========================================================================
    // Max length
    // =========================================================================

    public const string NameMaxLength = "ENTITY_NAME_MAX_LENGTH";
    public const string SlugMaxLength = "ENTITY_SLUG_MAX_LENGTH";

    // =========================================================================
    // Invalid values
    // =========================================================================

    public const string TypeInvalid = "ENTITY_TYPE_INVALID";
    public const string SlugInvalid = "ENTITY_SLUG_INVALID";

    // =========================================================================
    // Trust metadata (cross-field validation)
    // =========================================================================

    public const string TrustTestamentaryMustBeIrrevocable =
        "ENTITY_TRUST_TESTAMENTARY_MUST_BE_IRREVOCABLE";
    public const string TrustOtherTypeDescriptionRequired =
        "ENTITY_TRUST_OTHER_TYPE_DESCRIPTION_REQUIRED";
    public const string TrustGrantorCannotHaveIncomeTreatment =
        "ENTITY_TRUST_GRANTOR_CANNOT_HAVE_INCOME_TREATMENT";

    // =========================================================================
    // Business rules
    // =========================================================================

    public const string PersonalAlreadyExists = "ENTITY_PERSONAL_ALREADY_EXISTS";
    public const string PersonalCannotDelete = "ENTITY_PERSONAL_CANNOT_DELETE";
    public const string PersonalNameImmutable = "ENTITY_PERSONAL_NAME_IMMUTABLE";
    public const string PersonalTypeImmutable = "ENTITY_PERSONAL_TYPE_IMMUTABLE";
    public const string SlugDuplicate = "ENTITY_SLUG_DUPLICATE";
    public const string NotFound = "ENTITY_NOT_FOUND";
    public const string ParentNotFound = "ENTITY_PARENT_NOT_FOUND";
    public const string ParentCycle = "ENTITY_PARENT_CYCLE";
    public const string HasChildrenCannotDelete = "ENTITY_HAS_CHILDREN_CANNOT_DELETE";

    // =========================================================================
    // Role errors
    // =========================================================================

    public const string RoleNotFound = "ENTITY_ROLE_NOT_FOUND";
    public const string RoleInvalidForEntityType = "ENTITY_ROLE_INVALID_FOR_ENTITY_TYPE";
    public const string RoleOwnershipExceeds100 = "ENTITY_ROLE_OWNERSHIP_EXCEEDS_100";
    public const string RoleBeneficialInterestExceeds100 =
        "ENTITY_ROLE_BENEFICIAL_INTEREST_EXCEEDS_100";
    public const string RoleOwnershipOutOfRange = "ENTITY_ROLE_OWNERSHIP_OUT_OF_RANGE";
    public const string RoleBeneficialInterestOutOfRange =
        "ENTITY_ROLE_BENEFICIAL_INTEREST_OUT_OF_RANGE";
    public const string RoleContactRequired = "ENTITY_ROLE_CONTACT_REQUIRED";
    public const string RoleContactNotFound = "ENTITY_ROLE_CONTACT_NOT_FOUND";
    public const string RoleTypeRequired = "ENTITY_ROLE_TYPE_REQUIRED";
    public const string RoleDateRangeInvalid = "ENTITY_ROLE_DATE_RANGE_INVALID";
    public const string RoleOwnershipNotAllowed = "ENTITY_ROLE_OWNERSHIP_NOT_ALLOWED";
    public const string RoleBeneficialInterestNotAllowed =
        "ENTITY_ROLE_BENEFICIAL_INTEREST_NOT_ALLOWED";
}
