using FluentValidation.TestHelper;
using RajFinancial.Api.Validators;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Tests.Validators;

/// <summary>
///     Unit tests for <see cref="UpdateAssetRequestValidator"/>.
///     Mirrors CreateAssetRequestValidatorTests since both validators
///     enforce the same rules.
/// </summary>
public class UpdateAssetRequestValidatorTests
{
    private readonly UpdateAssetRequestValidator validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new UpdateAssetRequest
        {
            Name = "Updated Asset",
            Type = AssetType.Investment,
            CurrentValue = 15000
        };

        var result = validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_Fails(string? name)
    {
        var request = new UpdateAssetRequest { Name = name!, Type = AssetType.BankAccount, CurrentValue = 100 };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds200_Fails()
    {
        var request = new UpdateAssetRequest
        {
            Name = new string('A', 201),
            Type = AssetType.BankAccount,
            CurrentValue = 100
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativeCurrentValue_Fails()
    {
        var request = new UpdateAssetRequest { Name = "Test", Type = AssetType.BankAccount, CurrentValue = -1 };
        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CurrentValue);
    }

    [Fact]
    public void Validate_NegativeSalvageValue_Fails()
    {
        var request = new UpdateAssetRequest
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
        var request = new UpdateAssetRequest
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
    public void Validate_DepreciationMethodWithoutUsefulLife_Fails()
    {
        var request = new UpdateAssetRequest
        {
            Name = "Test",
            Type = AssetType.Vehicle,
            CurrentValue = 10000,
            DepreciationMethod = DepreciationMethod.DecliningBalance,
            UsefulLifeMonths = null
        };

        var result = validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UsefulLifeMonths);
    }

    [Fact]
    public void Validate_DepreciationMethodNone_UsefulLifeNotRequired()
    {
        var request = new UpdateAssetRequest
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
}
