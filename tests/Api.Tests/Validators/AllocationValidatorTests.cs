using FluentAssertions;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Assets;
using RajFinancial.Shared.Validators;

namespace RajFinancial.Api.Tests.Validators;

/// <summary>
///     Unit tests for <see cref="AllocationValidator"/> (shared, framework-agnostic).
/// </summary>
public class AllocationValidatorTests
{
    // =========================================================================
    // Valid scenarios
    // =========================================================================

    [Fact]
    public void Validate_PrimaryTotals100_ReturnsValid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 60, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 40, BeneficiaryType.PRIMARY)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeTrue();
        result.IsPrimaryValid.Should().BeTrue();
        result.PrimaryTotal.Should().Be(100);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_PrimaryAndContingentBothTotal100_ReturnsValid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 50, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 50, BeneficiaryType.PRIMARY),
            MakeAssignment("Carol", 70, BeneficiaryType.CONTINGENT),
            MakeAssignment("Dave", 30, BeneficiaryType.CONTINGENT)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeTrue();
        result.IsPrimaryValid.Should().BeTrue();
        result.IsContingentValid.Should().BeTrue();
        result.PrimaryTotal.Should().Be(100);
        result.ContingentTotal.Should().Be(100);
    }

    [Fact]
    public void Validate_SinglePrimary100Percent_ReturnsValid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.PRIMARY)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoContingent_ContingentIsValid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.PRIMARY)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsContingentValid.Should().BeTrue();
        result.ContingentTotal.Should().Be(0);
    }

    [Fact]
    public void Validate_EmptyList_ReturnsValid()
    {
        var result = AllocationValidator.Validate([]);

        result.IsValid.Should().BeTrue();
        result.PrimaryTotal.Should().Be(0);
        result.ContingentTotal.Should().Be(0);
    }

    [Fact]
    public void Validate_MinAllocationPercent_ReturnsValid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 0.01, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 99.99, BeneficiaryType.PRIMARY)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeTrue();
    }

    // =========================================================================
    // Primary total validation
    // =========================================================================

    [Fact]
    public void Validate_PrimaryUnder100_ReturnsInvalid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 30, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 30, BeneficiaryType.PRIMARY)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.IsPrimaryValid.Should().BeFalse();
        result.PrimaryTotal.Should().Be(60);
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.PRIMARY_TOTAL_INVALID);
    }

    [Fact]
    public void Validate_PrimaryOver100_ReturnsInvalid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 60, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 60, BeneficiaryType.PRIMARY)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.IsPrimaryValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.PRIMARY_TOTAL_INVALID);
    }

    // =========================================================================
    // Contingent total validation
    // =========================================================================

    [Fact]
    public void Validate_ContingentUnder100_ReturnsInvalid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
            MakeAssignment("Carol", 40, BeneficiaryType.CONTINGENT)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.IsContingentValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.CONTINGENT_TOTAL_INVALID);
    }

    [Fact]
    public void Validate_ContingentOver100_ReturnsInvalid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
            MakeAssignment("Carol", 60, BeneficiaryType.CONTINGENT),
            MakeAssignment("Dave", 60, BeneficiaryType.CONTINGENT)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.IsContingentValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.CONTINGENT_TOTAL_INVALID);
    }

    // =========================================================================
    // Individual allocation range
    // =========================================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(200)]
    public void Validate_AllocationOutOfRange_ReturnsError(double percent)
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", percent, BeneficiaryType.PRIMARY)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.PERCENT_OUT_OF_RANGE);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_AllocationInRange_NoRangeError(double percent)
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", percent, BeneficiaryType.PRIMARY)
        };

        var result = AllocationValidator.Validate(assignments);

        result.Errors.Should().NotContain(e => e.ErrorCode == AllocationErrorCodes.PERCENT_OUT_OF_RANGE);
    }

    // =========================================================================
    // Duplicate beneficiary detection
    // =========================================================================

    [Fact]
    public void Validate_DuplicatePrimaryBeneficiary_ReturnsError()
    {
        var id = Guid.NewGuid();
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 50, BeneficiaryType.PRIMARY, id),
            MakeAssignment("Alice", 50, BeneficiaryType.PRIMARY, id)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.DUPLICATE_BENEFICIARY);
    }

    [Fact]
    public void Validate_DuplicateContingentBeneficiary_ReturnsError()
    {
        var id = Guid.NewGuid();
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 50, BeneficiaryType.CONTINGENT, id),
            MakeAssignment("Bob", 50, BeneficiaryType.CONTINGENT, id)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.DUPLICATE_BENEFICIARY);
    }

    [Fact]
    public void Validate_SameBeneficiaryDifferentTypes_NoDuplicateError()
    {
        var id = Guid.NewGuid();
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.PRIMARY, id),
            MakeAssignment("Alice", 100, BeneficiaryType.CONTINGENT, id)
        };

        var result = AllocationValidator.Validate(assignments);

        result.Errors.Should().NotContain(e => e.ErrorCode == AllocationErrorCodes.DUPLICATE_BENEFICIARY);
    }

    // =========================================================================
    // Case insensitivity
    // =========================================================================

    [Theory]
    [InlineData("primary")]
    [InlineData("PRIMARY")]
    [InlineData("Primary")]
    public void Validate_TypeComparison_IsCaseInsensitive(string type)
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, type)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsPrimaryValid.Should().BeTrue();
        result.PrimaryTotal.Should().Be(100);
    }

    // =========================================================================
    // Floating-point tolerance
    // =========================================================================

    [Fact]
    public void Validate_FloatingPointRounding_Tolerates()
    {
        // 33.33 + 33.33 + 33.34 = 100.00 exactly, but test near-boundary
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 33.33, BeneficiaryType.PRIMARY),
            MakeAssignment("Bob", 33.33, BeneficiaryType.PRIMARY),
            MakeAssignment("Carol", 33.34, BeneficiaryType.PRIMARY)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsPrimaryValid.Should().BeTrue();
    }

    // =========================================================================
    // Multiple errors at once
    // =========================================================================

    [Fact]
    public void Validate_MultipleViolations_ReturnsAllErrors()
    {
        var id = Guid.NewGuid();
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 0, BeneficiaryType.PRIMARY, id),       // out of range
            MakeAssignment("Alice", 50, BeneficiaryType.PRIMARY, id),       // duplicate + total != 100
            MakeAssignment("Bob", 150, BeneficiaryType.CONTINGENT)          // out of range + contingent != 100
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
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
