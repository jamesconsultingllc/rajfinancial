using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.HealthChecks;

namespace RajFinancial.Api.Functions;

/// <summary>
///     Health check endpoints.
///     <list type="bullet">
///         <item><c>/health/live</c> — process alive. Always 200 unless the host is gone.</item>
///         <item><c>/health/ready</c> — runs all checks tagged <c>ready</c>; 200 healthy, 503 otherwise.</item>
///     </list>
///     Response bodies include per-check detail ONLY in Development; outside Dev only
///     the overall status and total duration are returned to avoid disclosing internal
///     dependency names / config structure to anonymous callers.
/// </summary>
public partial class HealthCheckFunction(
    HealthCheckService healthCheckService,
    IHostEnvironment environment,
    ILogger<HealthCheckFunction> logger)
{
    [Function("HealthLive")]
    public async Task<HttpResponseData> Live(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/live")]
        HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add(FunctionHelpers.ContentTypeHeader, FunctionHelpers.JsonContentType);
        await response.WriteStringAsync(
            JsonSerializer.Serialize(new { status = "alive" }, FunctionHelpers.JsonOptions));
        return response;
    }

    [Function("HealthReady")]
    public async Task<HttpResponseData> Ready(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/ready")]
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(
            r => r.Tags.Contains(HealthCheckTags.Ready),
            cancellationToken);

        if (report.Status != HealthStatus.Healthy)
            LogReadinessDegraded(report.Status.ToString());

        var statusCode = report.Status == HealthStatus.Healthy
            ? HttpStatusCode.OK
            : HttpStatusCode.ServiceUnavailable;

        var response = req.CreateResponse(statusCode);
        response.Headers.Add(FunctionHelpers.ContentTypeHeader, FunctionHelpers.JsonContentType);

        object payload;
        if (environment.IsDevelopment())
        {
            payload = new
            {
                status = report.Status.ToString().ToLowerInvariant(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString().ToLowerInvariant(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                }),
            };
        }
        else
        {
            payload = new
            {
                status = report.Status.ToString().ToLowerInvariant(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
            };
        }

        await response.WriteStringAsync(JsonSerializer.Serialize(payload, FunctionHelpers.JsonOptions));
        return response;
    }

    [LoggerMessage(EventId = 9901, Level = LogLevel.Warning, Message = "Readiness probe returned non-healthy status {Status}")]
    private partial void LogReadinessDegraded(string status);
}

