using Microsoft.Extensions.DependencyInjection;
using RajFinancial.Api.HealthChecks;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     Registers application health checks. Tag <c>ready</c> is filtered by the
///     <c>/health/ready</c> endpoint; unchecked probes stay out of the liveness path.
/// </summary>
internal static class HealthCheckRegistration
{
    private const string ReadyTag = "ready";

    internal static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: [ReadyTag])
            .AddCheck<ConfigHealthCheck>("config", tags: [ReadyTag]);

        return services;
    }
}
