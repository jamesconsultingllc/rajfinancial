using FluentValidation.TestHelper;
using RajFinancial.Api.Validators;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Tests.Validators;

/// <summary>
///     Unit tests for <see cref="CreateAssetRequestValidator"/>.
/// </summary>
public class CreateAssetRequestValidatorTests
{
    private readonly CreateAssetRequestValidator validator = new();

    // =========================================================================
    // Valid requests
    // =========================================================================

    [Fact]
    public void Validate_MinimalValidRequest_Passes()
    {
        var request = new CreateAssetRequest
        {
            Name = "Chase Checking",
            Type = AssetType.BankAccount,
            CurrentValue = 5000
        };

        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_FullValidRequest_Passes()
    {
        var request = new CreateAssetRequest
        {
            Name = "Investment Property",
            Type = AssetType.RealEstate,
            CurrentValue = 250000,
            PurchasePrice = 200000,
            PurchaseDate = DateTime.UtcNow.AddYears(-2),
            Description = "Rental property in Austin",
            Location = "123 Main St, Austin TX",
            AccountNumber = "PROP-001",
            InstitutionName = "First National Bank",
            DepreciationMethod = DepreciationMethod.StraightLine,
            SalvageValue = 50000,
            UsefulLifeMonths = 360,
            InServiceDate = DateTime.UtcNow.AddYears(-2),
            MarketValue = 275000,
            LastValuationDate = DateTime.UtcNow
        };

        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // =========================================================================
    // Required field validation
    // =========================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_Fails(string? name)
    {
        var request = new CreateAssetRequest { Name = name!, Type = AssetType.BankAccount, CurrentValue = 100 };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // =========================================================================
    // String length validation
    // =========================================================================

    [Fact]
    public void Validate_NameExceeds200_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = new string('A', 201),
            Type = AssetType.BankAccount,
            CurrentValue = 100
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_DescriptionExceeds2000_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.BankAccount,
            CurrentValue = 100,
            Description = new string('A', 2001)
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_LocationExceeds500_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.BankAccount,
            CurrentValue = 100,
            Location = new string('A', 501)
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Location);
    }

    [Fact]
    public void Validate_AccountNumberExceeds100_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.BankAccount,
            CurrentValue = 100,
            AccountNumber = new string('A', 101)
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.AccountNumber);
    }

    [Fact]
    public void Validate_InstitutionNameExceeds200_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.BankAccount,
            CurrentValue = 100,
            InstitutionName = new string('A', 201)
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.InstitutionName);
    }

    // =========================================================================
    // Numeric validation
    // =========================================================================

    [Fact]
    public void Validate_NegativeCurrentValue_Fails()
    {
        var request = new CreateAssetRequest { Name = "Test", Type = AssetType.BankAccount, CurrentValue = -1 };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CurrentValue);
    }

    [Fact]
    public void Validate_NegativePurchasePrice_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.BankAccount,
            CurrentValue = 100,
            PurchasePrice = -1
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.PurchasePrice);
    }

    [Fact]
    public void Validate_NegativeSalvageValue_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.Vehicle,
            CurrentValue = 100,
            SalvageValue = -1
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SalvageValue);
    }

    [Fact]
    public void Validate_ZeroUsefulLifeMonths_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.Vehicle,
            CurrentValue = 100,
            UsefulLifeMonths = 0
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UsefulLifeMonths);
    }

    [Fact]
    public void Validate_NegativeUsefulLifeMonths_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.Vehicle,
            CurrentValue = 100,
            UsefulLifeMonths = -12
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UsefulLifeMonths);
    }

    [Fact]
    public void Validate_NegativeMarketValue_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.BankAccount,
            CurrentValue = 100,
            MarketValue = -1
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MarketValue);
    }

    // =========================================================================
    // Cross-field depreciation validation
    // =========================================================================

    [Fact]
    public void Validate_DepreciationMethodWithoutUsefulLife_Fails()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.Vehicle,
            CurrentValue = 10000,
            DepreciationMethod = DepreciationMethod.StraightLine,
            UsefulLifeMonths = null
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UsefulLifeMonths);
    }

    [Fact]
    public void Validate_DepreciationMethodNone_UsefulLifeNotRequired()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.Vehicle,
            CurrentValue = 10000,
            DepreciationMethod = DepreciationMethod.None,
            UsefulLifeMonths = null
        };

        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.UsefulLifeMonths);
    }

    [Fact]
    public void Validate_ZeroCurrentValue_Passes()
    {
        var request = new CreateAssetRequest { Name = "Test", Type = AssetType.BankAccount, CurrentValue = 0 };
        var result = validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.CurrentValue);
    }

    // =========================================================================
    // Error code verification (contract with client localization)
    // =========================================================================

    [Fact]
    public void Validate_EmptyName_ReturnsCorrectErrorCode()
    {
        var request = new CreateAssetRequest { Name = "", Type = AssetType.BankAccount, CurrentValue = 100 };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(AssetErrorCodes.NameRequired);
    }

    [Fact]
    public void Validate_NameTooLong_ReturnsCorrectErrorCode()
    {
        var request = new CreateAssetRequest { Name = new string('A', 201), Type = AssetType.BankAccount, CurrentValue = 100 };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(AssetErrorCodes.NameMaxLength);
    }

    [Fact]
    public void Validate_NegativeValue_ReturnsCorrectErrorCode()
    {
        var request = new CreateAssetRequest { Name = "Test", Type = AssetType.BankAccount, CurrentValue = -1 };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CurrentValue)
            .WithErrorCode(AssetErrorCodes.ValueNegative);
    }

    [Fact]
    public void Validate_NegativeSalvage_ReturnsCorrectErrorCode()
    {
        var request = new CreateAssetRequest { Name = "Test", Type = AssetType.Vehicle, CurrentValue = 100, SalvageValue = -1 };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SalvageValue)
            .WithErrorCode(AssetErrorCodes.SalvageValueNegative);
    }

    [Fact]
    public void Validate_UsefulLifeRequired_ReturnsCorrectErrorCode()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.Vehicle,
            CurrentValue = 10000,
            DepreciationMethod = DepreciationMethod.StraightLine,
            UsefulLifeMonths = null
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UsefulLifeMonths)
            .WithErrorCode(AssetErrorCodes.UsefulLifeRequired);
    }

    [Fact]
    public void Validate_DescriptionTooLong_ReturnsCorrectErrorCode()
    {
        var request = new CreateAssetRequest
        {
            Name = "Test",
            Type = AssetType.BankAccount,
            CurrentValue = 100,
            Description = new string('A', 2001)
        };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorCode(AssetErrorCodes.DescriptionMaxLength);
    }
}
