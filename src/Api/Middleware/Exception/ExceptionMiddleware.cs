using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace RajFinancial.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches and formats all unhandled exceptions.
/// </summary>
/// <remarks>
/// <para>
/// This middleware implements the following OWASP security requirements:
/// <list type="bullet">
///   <item>A10:2025 - Mishandling Exceptional Conditions: Never leaks stack traces</item>
///   <item>A09:2025 - Security Logging: Logs all errors with correlation IDs</item>
/// </list>
/// </para>
/// <para>
/// <b>Extension Points:</b>
/// <list type="bullet">
///   <item>Add custom exception types with specific HTTP status codes</item>
///   <item>Add integration with external error tracking (e.g., Application Insights)</item>
/// </list>
/// </para>
/// </remarks>
public class ExceptionMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionMiddleware> logger;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        this.logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.NotFound, ex.ErrorCode, ex.Message);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, "VALIDATION_FAILED", ex.Message, ex.Errors);
        }
        catch (UnauthorizedException ex)
        {
            logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.Unauthorized, "AUTH_REQUIRED", ex.Message);
        }
        catch (ForbiddenException ex)
        {
            logger.LogWarning(ex, "Forbidden: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.Forbidden, "AUTH_FORBIDDEN", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning(ex, "Business rule violation: {Code} - {Message}", ex.ErrorCode, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.UnprocessableEntity, ex.ErrorCode, ex.Message);
        }
        catch (ConfigurationException ex)
        {
            logger.LogError(ex, "Configuration error: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, "CONFIGURATION_ERROR", "Service configuration error");
        }
        catch (Exception ex)
        {
            // Log the full exception details server-side
            logger.LogError(ex, "Unhandled exception in {FunctionName}: {Message}",
                context.FunctionDefinition.Name, ex.Message);

            // Return generic error to client (never expose stack traces)
            await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred");
        }
    }

    private static async Task WriteErrorResponseAsync(
        FunctionContext context,
        HttpStatusCode statusCode,
        string code,
        string message,
        Dictionary<string, object>? details = null)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();
        if (httpRequest == null) return;

        var response = httpRequest.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        var error = new ApiErrorResponse
        {
            Code = code,
            Message = message,
            Details = details,
            TraceId = context.TraceContext.TraceParent
        };

        await response.WriteAsJsonAsync(error);

        // Set the response on the context
        var invocationResult = context.GetInvocationResult();
        invocationResult.Value = response;
    }
}

#region Custom Exceptions

#endregion
