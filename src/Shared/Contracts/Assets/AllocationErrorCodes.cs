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

    public const string PRIMARY_TOTAL_INVALID = "ALLOCATION_PRIMARY_TOTAL_INVALID";
    public const string CONTINGENT_TOTAL_INVALID = "ALLOCATION_CONTINGENT_TOTAL_INVALID";

    // =========================================================================
    // Individual allocation constraints
    // =========================================================================

    public const string PERCENT_OUT_OF_RANGE = "ALLOCATION_PERCENT_OUT_OF_RANGE";

    // =========================================================================
    // Duplicates
    // =========================================================================

    public const string DUPLICATE_BENEFICIARY = "ALLOCATION_DUPLICATE_BENEFICIARY";
}
