using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware.Content;

namespace RajFinancial.Api.Middleware.Exception;

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
public partial class ExceptionMiddleware(
    ILogger<ExceptionMiddleware> logger,
    ISerializationFactory serializationFactory)
    : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            LogResourceNotFound(ex, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.NotFound, ex.ErrorCode, ex.Message);
        }
        catch (ValidationException ex)
        {
            LogValidationFailed(ex, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, "VALIDATION_FAILED", ex.Message, ex.Errors);
        }
        catch (UnauthorizedException ex)
        {
            LogUnauthorized(ex, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.Unauthorized, "AUTH_REQUIRED", ex.Message);
        }
        catch (ForbiddenException ex)
        {
            LogForbidden(ex, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.Forbidden, "AUTH_FORBIDDEN", ex.Message);
        }
        catch (ConflictException ex)
        {
            LogConflict(ex, ex.ErrorCode, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.Conflict, ex.ErrorCode, ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            LogBusinessRuleViolation(ex, ex.ErrorCode, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.UnprocessableEntity, ex.ErrorCode, ex.Message);
        }
        catch (ConfigurationException ex)
        {
            LogConfigurationError(ex, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, "CONFIGURATION_ERROR", "Service configuration error");
        }
        catch (OperationCanceledException)
        {
            throw; // Let cancellations propagate to the host
        }
        catch (System.Exception ex)
        {
            LogUnhandledException(ex, context.FunctionDefinition.Name, ex.Message);

            await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred");
        }
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Warning, Message = "Resource not found: {Message}")]
    private partial void LogResourceNotFound(System.Exception ex, string message);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Warning, Message = "Validation failed: {Message}")]
    private partial void LogValidationFailed(System.Exception ex, string message);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Warning, Message = "Unauthorized: {Message}")]
    private partial void LogUnauthorized(System.Exception ex, string message);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Warning, Message = "Forbidden: {Message}")]
    private partial void LogForbidden(System.Exception ex, string message);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Warning, Message = "Conflict: {Code} - {Message}")]
    private partial void LogConflict(System.Exception ex, string code, string message);

    [LoggerMessage(EventId = 5006, Level = LogLevel.Warning, Message = "Business rule violation: {Code} - {Message}")]
    private partial void LogBusinessRuleViolation(System.Exception ex, string code, string message);

    [LoggerMessage(EventId = 5007, Level = LogLevel.Error, Message = "Configuration error: {Message}")]
    private partial void LogConfigurationError(System.Exception ex, string message);

    [LoggerMessage(EventId = 5008, Level = LogLevel.Error, Message = "Unhandled exception in {FunctionName}: {Message}")]
    private partial void LogUnhandledException(System.Exception ex, string functionName, string message);

    private async Task WriteErrorResponseAsync(
        FunctionContext context,
        HttpStatusCode statusCode,
        string code,
        string message,
        Dictionary<string, object>? details = null)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();
        if (httpRequest == null) return;

        var response = httpRequest.CreateResponse(statusCode);

        // Remove the existing Content-Type header to prevent FormatException
        if (response.Headers.Contains("Content-Type"))
        {
            response.Headers.Remove("Content-Type");
        }

        var error = new ApiErrorResponse
        {
            Code = code,
            Message = message,
            Details = details,
            TraceId = context.TraceContext.TraceParent
        };

        // Use content negotiation: get request-specific content type from context
        var contentType = context.Items.TryGetValue(FunctionContextKeys.ResponseContentType, out var ct)
            ? ct as string ?? SerializationFactory.JsonContentType
            : SerializationFactory.JsonContentType;

        var bytes = await serializationFactory.SerializeAsync(error, contentType);
        response.Headers.Add("Content-Type", contentType);
        await response.Body.WriteAsync(bytes);

        // Set the response on the context
        var invocationResult = context.GetInvocationResult();
        invocationResult.Value = response;
    }
}