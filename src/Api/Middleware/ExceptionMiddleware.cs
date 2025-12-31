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
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.NotFound, ex.ErrorCode, ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, "VALIDATION_FAILED", ex.Message, ex.Errors);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.Unauthorized, "AUTH_REQUIRED", ex.Message);
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex, "Forbidden: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.Forbidden, "AUTH_FORBIDDEN", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation: {Code} - {Message}", ex.ErrorCode, ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.UnprocessableEntity, ex.ErrorCode, ex.Message);
        }
        catch (ConfigurationException ex)
        {
            _logger.LogError(ex, "Configuration error: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, "CONFIGURATION_ERROR", "Service configuration error");
        }
        catch (Exception ex)
        {
            // Log the full exception details server-side
            _logger.LogError(ex, "Unhandled exception in {FunctionName}: {Message}",
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

/// <summary>
/// Standardized API error response.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// Machine-readable error code for client-side localization.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Human-readable message (default English, clients localize by Code).
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Optional additional details (field errors, resource IDs, etc.).
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }

    /// <summary>
    /// Trace ID for debugging and support tickets.
    /// </summary>
    public string? TraceId { get; set; }
}

#region Custom Exceptions

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public static NotFoundException Asset(Guid assetId) =>
        new("ASSET_NOT_FOUND", $"Asset with ID {assetId} was not found");

    public static NotFoundException Account(Guid accountId) =>
        new("ACCOUNT_NOT_FOUND", $"Account with ID {accountId} was not found");

    public static NotFoundException Beneficiary(Guid beneficiaryId) =>
        new("BENEFICIARY_NOT_FOUND", $"Beneficiary with ID {beneficiaryId} was not found");

    public static NotFoundException User(string userId) =>
        new("USER_NOT_FOUND", $"User with ID {userId} was not found");
}

/// <summary>
/// Exception thrown when request validation fails.
/// </summary>
public class ValidationException : Exception
{
    public Dictionary<string, object>? Errors { get; }

    public ValidationException(string message, Dictionary<string, object>? errors = null) : base(message)
    {
        Errors = errors;
    }
}

/// <summary>
/// Exception thrown when authentication is required but missing or invalid.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Authentication required") : base(message) { }
}

/// <summary>
/// Exception thrown when the user lacks permission for the requested action.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Access denied") : base(message) { }
}

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }

    public BusinessRuleException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when required configuration is missing or invalid.
/// </summary>
public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
}

#endregion
