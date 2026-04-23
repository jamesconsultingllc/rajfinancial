using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Services.Auth;

namespace RajFinancial.Api.HealthChecks;

/// <summary>
///     Readiness probe that reports which <see cref="IJwtBearerValidator"/> implementation
///     is wired into DI. The result tags the <c>auth.validator</c> data field so integration
///     tests and dashboards can confirm whether the host is running with the production
///     validator or the unsigned local-only variant.
/// </summary>
/// <remarks>
///     The check also fails (Unhealthy) when the production validator is selected but
///     <see cref="EntraExternalIdOptions.ValidAudiences"/> is empty — that configuration
///     would always reject every token, so surfacing it at /health/ready catches
///     mis-provisioned environments before the first request arrives.
/// </remarks>
internal sealed class AuthValidatorHealthCheck(
    IJwtBearerValidator validator,
    IOptions<EntraExternalIdOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var validatorName = validator switch
        {
            JwtBearerValidator => AuthValidatorNames.Jwt,
            LocalUnsignedJwtValidator => AuthValidatorNames.UnsignedLocal,
            _ => validator.GetType().Name,
        };

        var data = new Dictionary<string, object> { [AuthValidatorNames.DataKey] = validatorName };

        if (validator is JwtBearerValidator)
        {
            var audiences = options.Value.ValidAudiences;
            // Reject empty lists AND lists where every entry is whitespace or the
            // configuration placeholder — both produce a validator that rejects every
            // token at runtime, which is far worse than failing the readiness probe.
            var hasUsableAudience = audiences.Any(a =>
                !string.IsNullOrWhiteSpace(a) && !string.Equals(a, ConfigurationPlaceholder, StringComparison.Ordinal));

            if (!hasUsableAudience)
            {
                return Task.FromResult(new HealthCheckResult(
                    context.Registration.FailureStatus,
                    description: $"{ConfigurationKeys.EntraValidAudiences} has no usable entry " +
                                 $"(empty, whitespace, or '{ConfigurationPlaceholder}'); every token would be rejected.",
                    data: data));
            }
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            description: $"Auth validator: {validatorName}",
            data: data));
    }

    private const string ConfigurationPlaceholder = "<SET-IN-ENVIRONMENT>";
}
