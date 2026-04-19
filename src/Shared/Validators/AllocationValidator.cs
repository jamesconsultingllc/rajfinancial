using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Shared.Validators;

/// <summary>
///     Validates beneficiary allocation assignments.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>Primary beneficiaries must total exactly 100%.</item>
///         <item>Contingent beneficiaries must total exactly 100% (if any are assigned).</item>
///         <item>Individual allocations must be between 0.01% and 100%.</item>
///         <item>No duplicate beneficiaries per type.</item>
///     </list>
///     This validator is framework-agnostic and can be used on both server (API) and
///     client (via the TypeScript port in <c>src/Client/src/lib/allocationValidator.ts</c>).
/// </remarks>
public static class AllocationValidator
{
    private const double MIN_PERCENT = 0.01;
    private const double MAX_PERCENT = 100.0;
    private const double REQUIRED_TOTAL = 100.0;
    private const double TOLERANCE = 0.0001;

    /// <summary>
    ///     Validates a list of beneficiary assignments against the allocation rules.
    /// </summary>
    public static AllocationValidationResult Validate(IReadOnlyList<BeneficiaryAssignmentDto> assignments)
    {
        var errors = new List<AllocationValidationError>();

        var primary = assignments
            .Where(a => string.Equals(a.Type, BeneficiaryType.Primary, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var contingent = assignments
            .Where(a => string.Equals(a.Type, BeneficiaryType.Contingent, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Individual allocation range checks
        foreach (var assignment in assignments)
        {
            if (assignment.AllocationPercent is < MIN_PERCENT or > MAX_PERCENT)
            {
                errors.Add(new AllocationValidationError(
                    AllocationErrorCodes.PercentOutOfRange,
                    $"Allocation for '{assignment.BeneficiaryName}' must be between {MIN_PERCENT}% and {MAX_PERCENT}%"));
            }
        }

        // Duplicate checks per type
        AddDuplicateErrors(primary, BeneficiaryType.Primary, errors);
        AddDuplicateErrors(contingent, BeneficiaryType.Contingent, errors);

        // Primary total must be exactly 100%
        var primaryTotal = primary.Sum(a => a.AllocationPercent);
        var isPrimaryValid = Math.Abs(primaryTotal - REQUIRED_TOTAL) < TOLERANCE;

        if (!isPrimaryValid && primary.Count > 0)
        {
            errors.Add(new AllocationValidationError(
                AllocationErrorCodes.PrimaryTotalInvalid,
                $"Primary beneficiary allocations must total exactly 100% (currently {primaryTotal:F2}%)"));
        }

        // Contingent total must be exactly 100% if any exist
        var contingentTotal = contingent.Sum(a => a.AllocationPercent);
        var isContingentValid = contingent.Count == 0
                                || Math.Abs(contingentTotal - REQUIRED_TOTAL) < TOLERANCE;

        if (!isContingentValid)
        {
            errors.Add(new AllocationValidationError(
                AllocationErrorCodes.ContingentTotalInvalid,
                $"Contingent beneficiary allocations must total exactly 100% (currently {contingentTotal:F2}%)"));
        }

        return new AllocationValidationResult
        {
            PrimaryTotal = primaryTotal,
            ContingentTotal = contingentTotal,
            IsPrimaryValid = isPrimaryValid,
            IsContingentValid = isContingentValid,
            Errors = errors
        };
    }

    private static void AddDuplicateErrors(
        List<BeneficiaryAssignmentDto> assignments,
        string type,
        List<AllocationValidationError> errors)
    {
        var duplicates = assignments
            .GroupBy(a => a.BeneficiaryId)
            .Where(g => g.Count() > 1)
            .Select(g => g.First().BeneficiaryName);

        foreach (var name in duplicates)
        {
            errors.Add(new AllocationValidationError(
                AllocationErrorCodes.DuplicateBeneficiary,
                $"'{name}' is assigned more than once as a {type} beneficiary"));
        }
    }
}
