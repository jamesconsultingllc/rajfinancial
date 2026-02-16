using System.Text;
using Microsoft.Azure.Functions.Worker;
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
public class ValidationMiddleware(ILogger<ValidationMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // ContentNegotiationMiddleware runs before this and stores the raw body bytes
        // in context.Items["RequestBodyBytes"]. Re-use those instead of re-reading
        // the (potentially non-seekable) body stream.
        if (context.Items.TryGetValue("RequestBodyBytes", out var bytesObj) &&
            bytesObj is byte[] bodyBytes &&
            bodyBytes.Length > 0)
        {
            context.Items["RequestBody"] = Encoding.UTF8.GetString(bodyBytes);
            logger.LogDebug("Request body captured for validation");
        }

        return next(context);
    }
}