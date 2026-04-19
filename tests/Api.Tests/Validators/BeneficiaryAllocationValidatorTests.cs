using FluentValidation.TestHelper;
using RajFinancial.Api.Validators;
using RajFinancial.Shared.Contracts.Assets;
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
            MakeAssignment("Alice", 60, BeneficiaryType.Primary),
            MakeAssignment("Bob", 40, BeneficiaryType.Primary)
        };

        var result = validator.TestValidate(list);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidPrimaryAndContingent_Passes()
    {
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.Primary),
            MakeAssignment("Bob", 60, BeneficiaryType.Contingent),
            MakeAssignment("Carol", 40, BeneficiaryType.Contingent)
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
            MakeAssignment("Alice", percent, BeneficiaryType.Primary)
        };

        var result = validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor("list[0].AllocationPercent")
            .WithErrorCode(AllocationErrorCodes.PercentOutOfRange);
    }

    // =========================================================================
    // Primary total
    // =========================================================================

    [Fact]
    public void Validate_PrimaryNot100_FailsWithErrorCode()
    {
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 40, BeneficiaryType.Primary),
            MakeAssignment("Bob", 40, BeneficiaryType.Primary)
        };

        var result = validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode(AllocationErrorCodes.PrimaryTotalInvalid);
    }

    // =========================================================================
    // Contingent total
    // =========================================================================

    [Fact]
    public void Validate_ContingentNot100_FailsWithErrorCode()
    {
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.Primary),
            MakeAssignment("Carol", 30, BeneficiaryType.Contingent)
        };

        var result = validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode(AllocationErrorCodes.ContingentTotalInvalid);
    }

    [Fact]
    public void Validate_NoContingent_Passes()
    {
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.Primary)
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
            MakeAssignment("Alice", 50, BeneficiaryType.Primary, id),
            MakeAssignment("Alice", 50, BeneficiaryType.Primary, id)
        };

        var result = validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode(AllocationErrorCodes.DuplicateBeneficiary);
    }

    [Fact]
    public void Validate_DuplicateContingent_FailsWithErrorCode()
    {
        var id = Guid.NewGuid();
        var list = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.Primary),
            MakeAssignment("Bob", 50, BeneficiaryType.Contingent, id),
            MakeAssignment("Bob", 50, BeneficiaryType.Contingent, id)
        };

        var result = validator.TestValidate(list);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorCode(AllocationErrorCodes.DuplicateBeneficiary);
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
