using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;

namespace RajFinancial.Api.Functions;

/// <summary>
///     Health check endpoint that reports API status and database connectivity.
/// </summary>
public class HealthCheckFunction(ILoggerFactory loggerFactory, ApplicationDbContext context)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<HealthCheckFunction>();

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
        _logger.LogInformation("Health check requested");

        var dbHealthy = false;
        try
        {
            dbHealthy = await context.Database.CanConnectAsync();
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Database connectivity check failed");
        }

        var status = dbHealthy ? "healthy" : "degraded";
        var statusCode = dbHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(
            $"{{\"status\":\"{status}\",\"service\":\"RajFinancial API\",\"database\":\"{(dbHealthy ? "connected" : "unavailable")}\"}}");

        return response;
    }
}