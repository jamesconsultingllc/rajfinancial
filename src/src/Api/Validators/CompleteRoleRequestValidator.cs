using FluentValidation;
using RajFinancial.Shared.Contracts.Auth;

namespace RajFinancial.Api.Validators;

/// <summary>
/// Validator for <see cref="CompleteRoleRequest"/>.
/// Validates that both UserId and Role are provided and Role is a valid value.
/// </summary>
public class CompleteRoleRequestValidator : AbstractValidator<CompleteRoleRequest>
{
    /// <summary>
    /// Valid roles that can be assigned.
    /// </summary>
    private static readonly string[] ValidRoles = ["Client", "Advisor", "Administrator"];

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteRoleRequestValidator"/> class.
    /// </summary>
    public CompleteRoleRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId and Role are required")
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("UserId and Role are required")
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(x => x.Role)
            .Must(role => ValidRoles.Contains(role))
            .When(x => !string.IsNullOrEmpty(x.Role))
            .WithMessage("Invalid role")
            .WithErrorCode("INVALID_ROLE");
    }
}
