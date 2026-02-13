using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;

namespace RajFinancial.Api.Functions;

/// <summary>
///     Sample HTTP function for testing Azure Functions integration.
/// </summary>
public class HealthCheckFunction(ILoggerFactory loggerFactory, ApplicationDbContext context)
{
    private readonly ILogger logger = loggerFactory.CreateLogger<HealthCheckFunction>();

    /// <summary>
    ///     Health check endpoint for the API.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>A 200 OK response with status information.</returns>
    [Function("HealthCheck")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")]
        HttpRequestData req)
    {
        logger.LogInformation("Health check requested");
        var grants = await context.DataAccessGrants.ToListAsync();
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync($"{{\"status\":\"healthy\",\"service\":\"RajFinancial API\",\"grantsCount\":{grants.Count}}}");

        return response;
    }
}