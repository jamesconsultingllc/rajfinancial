using FluentValidation;
using RajFinancial.Shared.Contracts.Entities;
using RajFinancial.Shared.Entities;

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
            .WithErrorCode(EntityErrorCodes.NAME_REQUIRED)
            .WithMessage("Entity name is required")
            .MaximumLength(200)
            .WithErrorCode(EntityErrorCodes.NAME_MAX_LENGTH)
            .WithMessage("Entity name must not exceed 200 characters");

        RuleFor(x => x.Type)
            .NotNull()
            .WithErrorCode(EntityErrorCodes.TYPE_REQUIRED)
            .WithMessage("Entity type is required")
            .IsInEnum()
            .When(x => x.Type.HasValue)
            .WithErrorCode(EntityErrorCodes.TYPE_INVALID)
            .WithMessage("Invalid entity type");

        RuleFor(x => x.Slug)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithErrorCode(EntityErrorCodes.SLUG_MAX_LENGTH)
            .WithMessage("Slug must not exceed 200 characters");

        RuleFor(x => x.Slug)
            .Matches("^[a-z0-9-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithErrorCode(EntityErrorCodes.SLUG_INVALID)
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens");

        // Trust cross-field invariants
        RuleFor(x => x.Trust!)
            .Must(t => !(t.CreationMethod == Shared.Entities.Trust.TrustCreationMethod.Testamentary
                         && t.Category != Shared.Entities.Trust.TrustCategory.Irrevocable))
            .When(x => x.Type == EntityType.Trust && x.Trust is not null)
            .WithErrorCode(EntityErrorCodes.TRUST_TESTAMENTARY_MUST_BE_IRREVOCABLE)
            .WithMessage("Testamentary trusts must be irrevocable");

        RuleFor(x => x.Trust!)
            .Must(t => t.Type != Shared.Entities.Trust.TrustType.Other
                       || !string.IsNullOrWhiteSpace(t.OtherTypeDescription))
            .When(x => x.Type == EntityType.Trust && x.Trust is not null)
            .WithErrorCode(EntityErrorCodes.TRUST_OTHER_TYPE_DESCRIPTION_REQUIRED)
            .WithMessage("OtherTypeDescription is required when Trust.Type is Other");

        RuleFor(x => x.Trust!)
            .Must(t => !(t.TaxStatus == Shared.Entities.Trust.TrustTaxStatus.Grantor
                         && t.IncomeTreatment.HasValue))
            .When(x => x.Type == EntityType.Trust && x.Trust is not null)
            .WithErrorCode(EntityErrorCodes.TRUST_GRANTOR_CANNOT_HAVE_INCOME_TREATMENT)
            .WithMessage("IncomeTreatment does not apply to grantor trusts");
    }
}
