using FluentAssertions;
using FluentValidation.TestHelper;
using RajFinancial.Api.Validators;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Tests.Validators;

/// <summary>
///     Unit tests for <see cref="BeneficiaryAllocationValidator"/> (FluentValidation wrapper).
/// </summary>
public class BeneficiaryAllocationValidatorTests
{
    private readonly BeneficiaryAllocationValidator validator = new();

    // =========================================================================
    // Valid scenarios
    // =========================================================================

    [Fact]
    public void Validate_ValidPrimaryAllocations_Passes()
    {
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 60, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 40, BeneficiaryType.PRIMARY)
        };

        var result = validator.TestValidate(list);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidPrimaryAndContingent_Passes()
    {
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 60, BeneficiaryType.CONTINGENT),
            MakeAssignment("Carol", 40, BeneficiaryType.CONTINGENT)
        };

        var result = validator.TestValidate(list);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // =========================================================================
    // Allocation percent range
    // =========================================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.01)]
    public void Validate_AllocationOutOfRange_FailsWithErrorCode(double percent)
    {
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", percent, BeneficiaryType.PRIMARY)
        };

        var result = validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor("list[0].AllocationPercent")
            .WithErrorCode(AllocationErrorCodes.PERCENT_OUT_OF_RANGE);
    }

    // =========================================================================
    // Primary total
    // =========================================================================

    [Fact]
    public void Validate_PrimaryNot100_FailsWithErrorCode()
    {
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 40, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 40, BeneficiaryType.PRIMARY)
        };

        var result = validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode(AllocationErrorCodes.PRIMARY_TOTAL_INVALID);
    }

    // =========================================================================
    // Contingent total
    // =========================================================================

    [Fact]
    public void Validate_ContingentNot100_FailsWithErrorCode()
    {
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
            MakeAssignment("Carol", 30, BeneficiaryType.CONTINGENT)
        };

        var result = validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode(AllocationErrorCodes.CONTINGENT_TOTAL_INVALID);
    }

    [Fact]
    public void Validate_NoContingent_Passes()
    {
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.PRIMARY)
        };

        var result = validator.TestValidate(list);
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    // =========================================================================
    // Duplicates
    // =========================================================================

    [Fact]
    public void Validate_DuplicatePrimary_FailsWithErrorCode()
    {
        var id = Guid.NewGuid();
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 50, BeneficiaryType.PRIMARY, id),
            MakeAssignment("Alice", 50, BeneficiaryType.PRIMARY, id)
        };

        var result = validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode(AllocationErrorCodes.DUPLICATE_BENEFICIARY);
    }

    [Fact]
    public void Validate_DuplicateContingent_FailsWithErrorCode()
    {
        var id = Guid.NewGuid();
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 50, BeneficiaryType.CONTINGENT, id),
            MakeAssignment("Bob", 50, BeneficiaryType.CONTINGENT, id)
        };

        var result = validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode(AllocationErrorCodes.DUPLICATE_BENEFICIARY);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static BeneficiaryAssignmentDto MakeAssignment(
        string name, double percent, string type, Guid? id = null)
    {
        return new BeneficiaryAssignmentDto
        {
            BeneficiaryId = id ?? Guid.NewGuid(),
            BeneficiaryName = name,
            Relationship = "Other",
            AllocationPercent = percent,
            Type = type
        };
    }
}
