using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace RajFinancial.Api.Services.Ai.Telemetry;

/// <summary>
/// Rejects the well-known <see cref="AiTelemetryRedactorOptions.DevPlaceholderSecret"/>
/// in any environment other than <see cref="Environments.Development"/>. A deploy that
/// ships with the placeholder secret would emit reversible HMAC prefixes — the merchant
/// hash becomes vulnerable to offline dictionary attack because the key is public source.
/// Failing fast at startup turns the misconfiguration into a deploy failure rather than
/// a silent data-leak.
/// </summary>
internal sealed class AiTelemetryRedactorOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<AiTelemetryRedactorOptions>
{
    public ValidateOptionsResult Validate(string? name, AiTelemetryRedactorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        if (string.Equals(
                options.MerchantHashSecret,
                AiTelemetryRedactorOptions.DevPlaceholderSecret,
                StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail(
                $"{AiTelemetryRedactorOptions.SectionName}:MerchantHashSecret must be " +
                "explicitly configured outside of Development. The default placeholder " +
                "value is committed to source and is therefore reversible. Set the " +
                "environment variable Ai__Telemetry__MerchantHashSecret to at least 32 " +
                "characters of high-entropy random material.");
        }

        return ValidateOptionsResult.Success;
    }
}
