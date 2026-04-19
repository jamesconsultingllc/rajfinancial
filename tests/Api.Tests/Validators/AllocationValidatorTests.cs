using FluentAssertions;
using RajFinancial.Shared.Contracts.Assets;
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
            MakeAssignment("Alice", 60, BeneficiaryType.Primary),
            MakeAssignment("Bob", 40, BeneficiaryType.Primary)
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
            MakeAssignment("Alice", 50, BeneficiaryType.Primary),
            MakeAssignment("Bob", 50, BeneficiaryType.Primary),
            MakeAssignment("Carol", 70, BeneficiaryType.Contingent),
            MakeAssignment("Dave", 30, BeneficiaryType.Contingent)
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
            MakeAssignment("Alice", 100, BeneficiaryType.Primary)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoContingent_ContingentIsValid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.Primary)
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
            MakeAssignment("Alice", 0.01, BeneficiaryType.Primary),
            MakeAssignment("Bob", 99.99, BeneficiaryType.Primary)
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
            MakeAssignment("Alice", 30, BeneficiaryType.Primary),
            MakeAssignment("Bob", 30, BeneficiaryType.Primary)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.IsPrimaryValid.Should().BeFalse();
        result.PrimaryTotal.Should().Be(60);
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.PrimaryTotalInvalid);
    }

    [Fact]
    public void Validate_PrimaryOver100_ReturnsInvalid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 60, BeneficiaryType.Primary),
            MakeAssignment("Bob", 60, BeneficiaryType.Primary)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.IsPrimaryValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.PrimaryTotalInvalid);
    }

    // =========================================================================
    // Contingent total validation
    // =========================================================================

    [Fact]
    public void Validate_ContingentUnder100_ReturnsInvalid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.Primary),
            MakeAssignment("Carol", 40, BeneficiaryType.Contingent)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.IsContingentValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.ContingentTotalInvalid);
    }

    [Fact]
    public void Validate_ContingentOver100_ReturnsInvalid()
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.Primary),
            MakeAssignment("Carol", 60, BeneficiaryType.Contingent),
            MakeAssignment("Dave", 60, BeneficiaryType.Contingent)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.IsContingentValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.ContingentTotalInvalid);
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
            MakeAssignment("Alice", percent, BeneficiaryType.Primary)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.PercentOutOfRange);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_AllocationInRange_NoRangeError(double percent)
    {
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", percent, BeneficiaryType.Primary)
        };

        var result = AllocationValidator.Validate(assignments);

        result.Errors.Should().NotContain(e => e.ErrorCode == AllocationErrorCodes.PercentOutOfRange);
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
            MakeAssignment("Alice", 50, BeneficiaryType.Primary, id),
            MakeAssignment("Alice", 50, BeneficiaryType.Primary, id)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.DuplicateBeneficiary);
    }

    [Fact]
    public void Validate_DuplicateContingentBeneficiary_ReturnsError()
    {
        var id = Guid.NewGuid();
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.Primary),
            MakeAssignment("Bob", 50, BeneficiaryType.Contingent, id),
            MakeAssignment("Bob", 50, BeneficiaryType.Contingent, id)
        };

        var result = AllocationValidator.Validate(assignments);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AllocationErrorCodes.DuplicateBeneficiary);
    }

    [Fact]
    public void Validate_SameBeneficiaryDifferentTypes_NoDuplicateError()
    {
        var id = Guid.NewGuid();
        var assignments = new List<BeneficiaryAssignmentDto>
        {
            MakeAssignment("Alice", 100, BeneficiaryType.Primary, id),
            MakeAssignment("Alice", 100, BeneficiaryType.Contingent, id)
        };

        var result = AllocationValidator.Validate(assignments);

        result.Errors.Should().NotContain(e => e.ErrorCode == AllocationErrorCodes.DuplicateBeneficiary);
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
            MakeAssignment("Alice", 33.33, BeneficiaryType.Primary),
            MakeAssignment("Bob", 33.33, BeneficiaryType.Primary),
            MakeAssignment("Carol", 33.34, BeneficiaryType.Primary)
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
            MakeAssignment("Alice", 0, BeneficiaryType.Primary, id),       // out of range
            MakeAssignment("Alice", 50, BeneficiaryType.Primary, id),       // duplicate + total != 100
            MakeAssignment("Bob", 150, BeneficiaryType.Contingent)          // out of range + contingent != 100
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
