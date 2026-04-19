namespace RajFinancial.Shared.Contracts.Assets;

/// <summary>
///     Error codes returned by beneficiary allocation validation.
///     Referenced by both API validators and client-side localization.
/// </summary>
public static class AllocationErrorCodes
{
    // =========================================================================
    // Allocation totals
    // =========================================================================

    public const string PrimaryTotalInvalid = "ALLOCATION_PRIMARY_TOTAL_INVALID";
    public const string ContingentTotalInvalid = "ALLOCATION_CONTINGENT_TOTAL_INVALID";

    // =========================================================================
    // Individual allocation constraints
    // =========================================================================

    public const string PercentOutOfRange = "ALLOCATION_PERCENT_OUT_OF_RANGE";

    // =========================================================================
    // Duplicates
    // =========================================================================

    public const string DuplicateBeneficiary = "ALLOCATION_DUPLICATE_BENEFICIARY";
}
