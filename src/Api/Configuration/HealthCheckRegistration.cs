using Microsoft.Extensions.DependencyInjection;
using RajFinancial.Api.HealthChecks;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     Registers application health checks. Tag <see cref="HealthCheckTags.Ready"/> is
///     filtered by <c>/health/ready</c>; unchecked probes stay out of the liveness path.
/// </summary>
internal static class HealthCheckRegistration
{
    internal static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: [HealthCheckTags.Ready])
            .AddCheck<ConfigHealthCheck>("config", tags: [HealthCheckTags.Ready]);

        return services;
    }
}
