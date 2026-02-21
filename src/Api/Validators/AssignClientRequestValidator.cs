// ============================================================================
// RAJ Financial - Assign Client Request Validator
// ============================================================================
// FluentValidation rules for the POST /api/auth/clients endpoint.
// Validates email format, access type, and category requirements.
// ============================================================================

using FluentValidation;

using RajFinancial.Shared.Contracts.Auth;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Validators;

/// <summary>
///     Validates the <see cref="AssignClientRequest" /> for the POST /api/auth/clients endpoint.
/// </summary>
/// <remarks>
///     <para>Validation rules:</para>
///     <list type="bullet">
///         <item>ClientEmail: Required, valid email format</item>
///         <item>AccessType: Required, must be a valid <see cref="Entities.AccessType" /> value (excluding Owner)</item>
///         <item>Categories: Required, at least one item</item>
///     </list>
/// </remarks>
public class AssignClientRequestValidator : AbstractValidator<AssignClientRequest>
{
    /// <summary>
    ///     Valid access types that can be assigned (Owner is excluded — it is implicit for the data owner).
    /// </summary>
    private static readonly string[] AssignableAccessTypes =
        Enum.GetValues<AccessType>()
            .Where(at => at != AccessType.Owner)
            .Select(at => at.ToString())
            .ToArray();

    /// <summary>
    ///     Initializes a new instance of the <see cref="AssignClientRequestValidator" /> class.
    /// </summary>
    public AssignClientRequestValidator()
    {
        RuleFor(x => x.ClientEmail)
            .NotEmpty()
            .WithMessage("Client email is required")
            .WithErrorCode("VALIDATION_FAILED")
            .EmailAddress()
            .WithMessage("Client email must be a valid email address")
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(x => x.AccessType)
            .NotEmpty()
            .WithMessage("Access type is required")
            .WithErrorCode("VALIDATION_FAILED")
            .Must(at => AssignableAccessTypes.Contains(at))
            .When(x => !string.IsNullOrEmpty(x.AccessType))
            .WithMessage($"Access type must be one of: {string.Join(", ", AssignableAccessTypes)}")
            .WithErrorCode("VALIDATION_FAILED");

        RuleFor(x => x.Categories)
            .NotEmpty()
            .WithMessage("At least one data category is required")
            .WithErrorCode("VALIDATION_FAILED");
    }
}
