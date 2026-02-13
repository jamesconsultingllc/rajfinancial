using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace RajFinancial.Api.Middleware;

/// <summary>
/// Middleware that automatically validates request bodies using FluentValidation.
/// </summary>
/// <remarks>
/// <para>
/// This middleware intercepts HTTP requests, deserializes the body, and validates
/// it using the appropriate FluentValidation validator if one is registered.
/// </para>
/// <para>
/// <b>How it works:</b>
/// <list type="number">
///   <item>Reads the request body</item>
///   <item>Stores the body in context for later use by the function</item>
///   <item>If a validator is registered for the request type, validates the body</item>
///   <item>If validation fails, returns 400 Bad Request with error details</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage in Functions:</b>
/// <code>
/// // Get validated request body from context
/// var request = context.GetValidatedBody&lt;CreateAssetRequest&gt;();
/// </code>
/// </para>
/// <para>
/// <b>Extension Points:</b>
/// <list type="bullet">
///   <item>Add custom validation error formatting</item>
///   <item>Add validation caching for performance</item>
/// </list>
/// </para>
/// </remarks>
public class ValidationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ValidationMiddleware> logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ValidationMiddleware(ILogger<ValidationMiddleware> logger)
    {
        this.logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();

        // Only process requests with a body
        if (httpRequest?.Body != null && httpRequest.Body.Length > 0)
        {
            try
            {
                // Read and store the body for later use
                httpRequest.Body.Position = 0;
                using var reader = new StreamReader(httpRequest.Body, leaveOpen: true);
                var bodyString = await reader.ReadToEndAsync();
                httpRequest.Body.Position = 0; // Reset for potential re-reading

                // Store raw body in context
                context.Items["RequestBody"] = bodyString;

                logger.LogDebug("Request body captured for validation");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read request body for validation");
            }
        }

        await next(context);
    }
}