using FluentValidation;
using RajFinancial.Shared.Contracts.Assets;
using RajFinancial.Shared.Entities.Assets;

namespace RajFinancial.Api.Validators;

/// <summary>
///     Validator for <see cref="UpdateAssetRequest"/>.
/// </summary>
public class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(AssetErrorCodes.NameRequired)
            .WithMessage("Asset name is required")
            .MaximumLength(200)
            .WithErrorCode(AssetErrorCodes.NameMaxLength)
            .WithMessage("Asset name must not exceed 200 characters");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithErrorCode(AssetErrorCodes.TypeInvalid)
            .WithMessage("Invalid asset type");

        RuleFor(x => x.CurrentValue)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(AssetErrorCodes.ValueNegative)
            .WithMessage("Current value must be greater than or equal to zero");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PurchasePrice.HasValue)
            .WithErrorCode(AssetErrorCodes.PurchasePriceNegative)
            .WithMessage("Purchase price must be greater than or equal to zero");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null)
            .WithErrorCode(AssetErrorCodes.DescriptionMaxLength)
            .WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Location)
            .MaximumLength(500)
            .When(x => x.Location is not null)
            .WithErrorCode(AssetErrorCodes.LocationMaxLength)
            .WithMessage("Location must not exceed 500 characters");

        RuleFor(x => x.AccountNumber)
            .MaximumLength(100)
            .When(x => x.AccountNumber is not null)
            .WithErrorCode(AssetErrorCodes.AccountNumberMaxLength)
            .WithMessage("Account number must not exceed 100 characters");

        RuleFor(x => x.InstitutionName)
            .MaximumLength(200)
            .When(x => x.InstitutionName is not null)
            .WithErrorCode(AssetErrorCodes.InstitutionNameMaxLength)
            .WithMessage("Institution name must not exceed 200 characters");

        // Depreciation field validation
        RuleFor(x => x.DepreciationMethod)
            .IsInEnum()
            .When(x => x.DepreciationMethod.HasValue)
            .WithErrorCode(AssetErrorCodes.DepreciationMethodInvalid)
            .WithMessage("Invalid depreciation method");

        RuleFor(x => x.SalvageValue)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SalvageValue.HasValue)
            .WithErrorCode(AssetErrorCodes.SalvageValueNegative)
            .WithMessage("Salvage value must be greater than or equal to zero");

        RuleFor(x => x.UsefulLifeMonths)
            .GreaterThan(0)
            .When(x => x.UsefulLifeMonths.HasValue)
            .WithErrorCode(AssetErrorCodes.UsefulLifeInvalid)
            .WithMessage("Useful life must be greater than zero months");

        RuleFor(x => x.MarketValue)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MarketValue.HasValue)
            .WithErrorCode(AssetErrorCodes.MarketValueNegative)
            .WithMessage("Market value must be greater than or equal to zero");

        // Cross-field: if depreciation method is not None, require useful life
        RuleFor(x => x.UsefulLifeMonths)
            .NotNull()
            .When(x => x.DepreciationMethod.HasValue && x.DepreciationMethod != DepreciationMethod.None)
            .WithErrorCode(AssetErrorCodes.UsefulLifeRequired)
            .WithMessage("Useful life is required when a depreciation method is specified");
    }
}
