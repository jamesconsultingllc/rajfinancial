using FluentValidation;
using RajFinancial.Shared.Contracts.Auth;

namespace RajFinancial.Api.Validators;

/// <summary>
///     Validator for <see cref="UpdateProfileRequest"/>.
/// </summary>
public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    private static readonly string[] VALID_LOCALES =
    [
        "en-US", "es-MX", "es-ES", "fr-FR", "pt-BR"
    ];

    private static readonly string[] VALID_TIMEZONES =
    [
        "America/New_York", "America/Chicago", "America/Denver", "America/Los_Angeles",
        "America/Anchorage", "Pacific/Honolulu", "Europe/London", "Europe/Paris", "Asia/Tokyo"
    ];

    private static readonly string[] VALID_CURRENCIES =
    [
        "USD", "EUR", "GBP", "MXN", "BRL", "CAD"
    ];

    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithErrorCode("DISPLAY_NAME_REQUIRED")
            .WithMessage("Display name is required")
            .MaximumLength(200)
            .WithErrorCode("DISPLAY_NAME_MAX_LENGTH")
            .WithMessage("Display name must not exceed 200 characters");

        RuleFor(x => x.Locale)
            .NotEmpty()
            .WithErrorCode("LOCALE_REQUIRED")
            .WithMessage("Locale is required")
            .Must(locale => VALID_LOCALES.Contains(locale))
            .WithErrorCode("LOCALE_INVALID")
            .WithMessage("Locale must be one of: " + string.Join(", ", VALID_LOCALES));

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .WithErrorCode("TIMEZONE_REQUIRED")
            .WithMessage("Timezone is required")
            .Must(tz => VALID_TIMEZONES.Contains(tz))
            .WithErrorCode("TIMEZONE_INVALID")
            .WithMessage("Timezone must be a valid IANA timezone");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithErrorCode("CURRENCY_REQUIRED")
            .WithMessage("Currency is required")
            .Must(c => VALID_CURRENCIES.Contains(c))
            .WithErrorCode("CURRENCY_INVALID")
            .WithMessage("Currency must be one of: " + string.Join(", ", VALID_CURRENCIES));
    }
}
