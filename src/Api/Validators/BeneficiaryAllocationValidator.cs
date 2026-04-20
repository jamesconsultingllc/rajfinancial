using FluentValidation;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Validators;

/// <summary>
///     FluentValidation validator for a collection of <see cref="BeneficiaryAssignmentDto"/>.
///     Wraps the shared <see cref="Shared.Validators.AllocationValidator"/> rules
///     so they surface through the standard API validation pipeline.
/// </summary>
public class BeneficiaryAllocationValidator : AbstractValidator<IList<BeneficiaryAssignmentDto>>
{
    public BeneficiaryAllocationValidator()
    {
        RuleForEach(list => list)
            .ChildRules(assignment =>
            {
                assignment.RuleFor(a => a.AllocationPercent)
                    .InclusiveBetween(0.01, 100)
                    .WithErrorCode(AllocationErrorCodes.PercentOutOfRange)
                    .WithMessage("Allocation must be between 0.01% and 100%");
            });

        RuleFor(list => list)
            .Must(NoDuplicatePrimaryBeneficiaries)
            .WithErrorCode(AllocationErrorCodes.DuplicateBeneficiary)
            .WithMessage("Duplicate primary beneficiaries are not allowed");

        RuleFor(list => list)
            .Must(NoDuplicateContingentBeneficiaries)
            .WithErrorCode(AllocationErrorCodes.DuplicateBeneficiary)
            .WithMessage("Duplicate contingent beneficiaries are not allowed");

        RuleFor(list => list)
            .Must(PrimaryTotals100)
            .When(list => list.Any(a =>
                string.Equals(a.Type, BeneficiaryType.Primary, StringComparison.OrdinalIgnoreCase)))
            .WithErrorCode(AllocationErrorCodes.PrimaryTotalInvalid)
            .WithMessage("Primary beneficiary allocations must total exactly 100%");

        RuleFor(list => list)
            .Must(ContingentTotals100)
            .When(list => list.Any(a =>
                string.Equals(a.Type, BeneficiaryType.Contingent, StringComparison.OrdinalIgnoreCase)))
            .WithErrorCode(AllocationErrorCodes.ContingentTotalInvalid)
            .WithMessage("Contingent beneficiary allocations must total exactly 100%");
    }

    private static bool PrimaryTotals100(IList<BeneficiaryAssignmentDto> assignments)
    {
        var total = assignments
            .Where(a => string.Equals(a.Type, BeneficiaryType.Primary, StringComparison.OrdinalIgnoreCase))
            .Sum(a => a.AllocationPercent);
        return Math.Abs(total - 100) < 0.0001;
    }

    private static bool ContingentTotals100(IList<BeneficiaryAssignmentDto> assignments)
    {
        var contingent = assignments
            .Where(a => string.Equals(a.Type, BeneficiaryType.Contingent, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (contingent.Count == 0) return true;
        return Math.Abs(contingent.Sum(a => a.AllocationPercent) - 100) < 0.0001;
    }

    private static bool NoDuplicatePrimaryBeneficiaries(IList<BeneficiaryAssignmentDto> assignments)
    {
        return NoDuplicates(assignments, BeneficiaryType.Primary);
    }

    private static bool NoDuplicateContingentBeneficiaries(IList<BeneficiaryAssignmentDto> assignments)
    {
        return NoDuplicates(assignments, BeneficiaryType.Contingent);
    }

    private static bool NoDuplicates(IList<BeneficiaryAssignmentDto> assignments, string type)
    {
        var ofType = assignments
            .Where(a => string.Equals(a.Type, type, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return ofType.Select(a => a.BeneficiaryId).Distinct().Count() == ofType.Count;
    }
}
