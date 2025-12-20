using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace RajFinancial.Api.Functions;

/// <summary>
/// Sample HTTP function for testing Azure Functions integration.
/// </summary>
public class HealthCheckFunction
{
    private readonly ILogger _logger;

    public HealthCheckFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<HealthCheckFunction>();
    }

    /// <summary>
    /// Health check endpoint for the API.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>A 200 OK response with status information.</returns>
    [Function("HealthCheck")]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check requested");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString("{\"status\":\"healthy\",\"service\":\"RajFinancial API\"}");

        return response;
    }
}

