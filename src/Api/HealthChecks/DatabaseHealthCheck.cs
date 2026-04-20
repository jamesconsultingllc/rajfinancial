using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;

namespace RajFinancial.Api.HealthChecks;

/// <summary>
///     Readiness probe that verifies the application can reach SQL.
/// </summary>
/// <remarks>
///     Registered with the <c>ready</c> tag so /health/ready fails closed when SQL is
///     unreachable and App Service / Container Apps probes drop the instance out of
///     rotation automatically.
/// </remarks>
public sealed partial class DatabaseHealthCheck(
    ApplicationDbContext context,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await context.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database reachable")
                : HealthCheckResult.Unhealthy("Database.CanConnectAsync returned false");
        }
        catch (DbException ex)
        {
            LogDatabaseUnreachable(ex);
            return HealthCheckResult.Unhealthy("Database unreachable", ex);
        }
        catch (InvalidOperationException ex)
        {
            LogDatabaseUnreachable(ex);
            return HealthCheckResult.Unhealthy("Database unreachable", ex);
        }
    }

    [LoggerMessage(EventId = 9900, Level = LogLevel.Warning, Message = "Database health check failed")]
    private partial void LogDatabaseUnreachable(Exception ex);
}
