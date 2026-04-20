using System.Data.Common;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;

namespace RajFinancial.Api.Functions;

/// <summary>
///     Health check endpoint that reports API status and database connectivity.
/// </summary>
public partial class HealthCheckFunction(ILoggerFactory loggerFactory, ApplicationDbContext context)
{
    private readonly ILogger logger = loggerFactory.CreateLogger<HealthCheckFunction>();

    /// <summary>
    ///     Returns the health status of the API and database connectivity.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>A 200 OK response with status information, or 503 if degraded.</returns>
    [Function("HealthCheck")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")]
        HttpRequestData req)
    {
        LogHealthCheckRequested();

        var dbHealthy = false;
        try
        {
            dbHealthy = await context.Database.CanConnectAsync();
        }
        catch (DbException ex)
        {
            LogDatabaseCheckFailed(ex);
        }

        var status = dbHealthy ? "healthy" : "degraded";
        var statusCode = dbHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(
            $"{{\"status\":\"{status}\",\"service\":\"RajFinancial API\",\"database\":\"{(dbHealthy ? "connected" : "unavailable")}\"}}");

        return response;
    }

    [LoggerMessage(EventId = 8001, Level = LogLevel.Information, Message = "Health check requested")]
    private partial void LogHealthCheckRequested();

    [LoggerMessage(EventId = 8002, Level = LogLevel.Warning, Message = "Database connectivity check failed")]
    private partial void LogDatabaseCheckFailed(Exception ex);
}
