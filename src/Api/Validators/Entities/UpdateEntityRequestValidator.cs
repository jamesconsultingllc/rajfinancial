using FluentValidation;
using RajFinancial.Shared.Contracts.Entities;

namespace RajFinancial.Api.Validators.Entities;

/// <summary>
///     Validator for <see cref="UpdateEntityRequest"/>.
/// </summary>
public class UpdateEntityRequestValidator : AbstractValidator<UpdateEntityRequest>
{
    public UpdateEntityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(EntityErrorCodes.NAME_REQUIRED)
            .WithMessage("Entity name is required")
            .MaximumLength(200)
            .WithErrorCode(EntityErrorCodes.NAME_MAX_LENGTH)
            .WithMessage("Entity name must not exceed 200 characters");
    }
}
