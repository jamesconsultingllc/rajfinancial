using FluentValidation;
using RajFinancial.Shared.Contracts.Entities;

namespace RajFinancial.Api.Validators.Entities;

/// <summary>
///     Validator for <see cref="CreateEntityRoleRequest"/>.
/// </summary>
public class CreateEntityRoleRequestValidator : AbstractValidator<CreateEntityRoleRequest>
{
    public CreateEntityRoleRequestValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .WithErrorCode(EntityErrorCodes.ROLE_CONTACT_REQUIRED)
            .WithMessage("ContactId is required");

        RuleFor(x => x.RoleType)
            .NotNull()
            .WithErrorCode(EntityErrorCodes.ROLE_TYPE_REQUIRED)
            .WithMessage("Role type is required")
            .IsInEnum()
            .When(x => x.RoleType.HasValue)
            .WithErrorCode(EntityErrorCodes.ROLE_TYPE_REQUIRED)
            .WithMessage("Invalid role type");

        RuleFor(x => x.OwnershipPercent)
            .InclusiveBetween(0, 100)
            .When(x => x.OwnershipPercent.HasValue)
            .WithErrorCode(EntityErrorCodes.ROLE_OWNERSHIP_OUT_OF_RANGE)
            .WithMessage("Ownership percent must be between 0 and 100");

        RuleFor(x => x.BeneficialInterestPercent)
            .InclusiveBetween(0, 100)
            .When(x => x.BeneficialInterestPercent.HasValue)
            .WithErrorCode(EntityErrorCodes.ROLE_BENEFICIAL_INTEREST_OUT_OF_RANGE)
            .WithMessage("Beneficial interest percent must be between 0 and 100");

        RuleFor(x => x)
            .Must(x => !(x.EffectiveDate.HasValue && x.EndDate.HasValue)
                       || x.EndDate >= x.EffectiveDate)
            .WithErrorCode(EntityErrorCodes.ROLE_DATE_RANGE_INVALID)
            .WithMessage("End date cannot precede effective date");
    }
}
