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
            .WithErrorCode(EntityErrorCodes.RoleContactRequired)
            .WithMessage("ContactId is required");

        RuleFor(x => x.RoleType)
            .NotNull()
            .WithErrorCode(EntityErrorCodes.RoleTypeRequired)
            .WithMessage("Role type is required")
            .IsInEnum()
            .When(x => x.RoleType.HasValue)
            .WithErrorCode(EntityErrorCodes.RoleTypeRequired)
            .WithMessage("Invalid role type");

        RuleFor(x => x.OwnershipPercent)
            .InclusiveBetween(0, 100)
            .When(x => x.OwnershipPercent.HasValue)
            .WithErrorCode(EntityErrorCodes.RoleOwnershipOutOfRange)
            .WithMessage("Ownership percent must be between 0 and 100");

        RuleFor(x => x.BeneficialInterestPercent)
            .InclusiveBetween(0, 100)
            .When(x => x.BeneficialInterestPercent.HasValue)
            .WithErrorCode(EntityErrorCodes.RoleBeneficialInterestOutOfRange)
            .WithMessage("Beneficial interest percent must be between 0 and 100");

        RuleFor(x => x)
            .Must(x => !(x.EffectiveDate.HasValue && x.EndDate.HasValue)
                       || x.EndDate >= x.EffectiveDate)
            .WithErrorCode(EntityErrorCodes.RoleDateRangeInvalid)
            .WithMessage("End date cannot precede effective date");
    }
}
