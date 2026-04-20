using FluentValidation;
using RajFinancial.Shared.Contracts.Entities;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Contracts.Entities.Trust;

namespace RajFinancial.Api.Validators.Entities;

/// <summary>
///     Validator for <see cref="CreateEntityRequest"/>.
/// </summary>
public class CreateEntityRequestValidator : AbstractValidator<CreateEntityRequest>
{
    public CreateEntityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(EntityErrorCodes.NameRequired)
            .WithMessage("Entity name is required")
            .MaximumLength(200)
            .WithErrorCode(EntityErrorCodes.NameMaxLength)
            .WithMessage("Entity name must not exceed 200 characters");

        RuleFor(x => x.Type)
            .NotNull()
            .WithErrorCode(EntityErrorCodes.TypeRequired)
            .WithMessage("Entity type is required")
            .IsInEnum()
            .When(x => x.Type.HasValue)
            .WithErrorCode(EntityErrorCodes.TypeInvalid)
            .WithMessage("Invalid entity type");

        RuleFor(x => x.Slug)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithErrorCode(EntityErrorCodes.SlugMaxLength)
            .WithMessage("Slug must not exceed 200 characters");

        RuleFor(x => x.Slug)
            .Matches("^[a-z0-9-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithErrorCode(EntityErrorCodes.SlugInvalid)
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens");

        // Trust cross-field invariants
        RuleFor(x => x.Trust!)
            .Must(t => !(t.CreationMethod == TrustCreationMethod.Testamentary
                         && t.Category != TrustCategory.Irrevocable))
            .When(x => x.Type == EntityType.Trust && x.Trust is not null)
            .WithErrorCode(EntityErrorCodes.TrustTestamentaryMustBeIrrevocable)
            .WithMessage("Testamentary trusts must be irrevocable");

        RuleFor(x => x.Trust!)
            .Must(t => t.Type != TrustType.Other
                       || !string.IsNullOrWhiteSpace(t.OtherTypeDescription))
            .When(x => x.Type == EntityType.Trust && x.Trust is not null)
            .WithErrorCode(EntityErrorCodes.TrustOtherTypeDescriptionRequired)
            .WithMessage("OtherTypeDescription is required when Trust.Type is Other");

        RuleFor(x => x.Trust!)
            .Must(t => !(t.TaxStatus == TrustTaxStatus.Grantor
                         && t.IncomeTreatment.HasValue))
            .When(x => x.Type == EntityType.Trust && x.Trust is not null)
            .WithErrorCode(EntityErrorCodes.TrustGrantorCannotHaveIncomeTreatment)
            .WithMessage("IncomeTreatment does not apply to grantor trusts");
    }
}
