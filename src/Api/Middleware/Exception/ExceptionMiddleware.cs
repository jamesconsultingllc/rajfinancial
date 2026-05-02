using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware.Content;
using RajFinancial.Api.Observability;

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
    private const string MiddlewareName = "ExceptionMiddleware";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Capture the outer invocation span BEFORE starting our own activity, so
        // RecordExceptionOutcome below classifies the Functions Invoke span
        // (not this middleware's span, which would become Activity.Current).
        var invocationActivity = Activity.Current;

        using var activity = MiddlewareTelemetry.StartActivity(MiddlewareTelemetry.ActivityException);
        activity?.SetTag(MiddlewareTelemetry.MiddlewareNameTag, MiddlewareName);
        activity?.SetTag(MiddlewareTelemetry.CodeFunctionTag, context.FunctionDefinition.Name);

        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        catch (OperationCanceledException)
        {
            throw; // Let cancellations propagate to the host
        }
        catch (System.Exception ex)
        {
            // Classify the Functions Invoke span centrally so per-function
            // code doesn't need to tag it. Service-level activities were
            // already classified by their own exception handlers and have
            // been disposed by the time execution reaches this middleware.
            invocationActivity?.RecordExceptionOutcome(ex);

            var exceptionType = ex.GetType().Name;
            var statusCode = (int)MapStatusCode(ex);
            activity?.SetTag(MiddlewareTelemetry.ExceptionTypeTag, exceptionType);
            activity?.SetTag(MiddlewareTelemetry.HttpStatusCodeTag, statusCode);
            MiddlewareTelemetry.RecordException(MiddlewareName, exceptionType, statusCode);

            await HandleExceptionAsync(context, ex, (HttpStatusCode)statusCode);
        }
        finally
        {
            sw.Stop();
            MiddlewareTelemetry.RecordDuration(MiddlewareName, sw.Elapsed.TotalMilliseconds);
        }
    }

    private static HttpStatusCode MapStatusCode(System.Exception ex) => ex switch
    {
        NotFoundException => HttpStatusCode.NotFound,
        ValidationException => HttpStatusCode.BadRequest,
        UnauthorizedException => HttpStatusCode.Unauthorized,
        ForbiddenException => HttpStatusCode.Forbidden,
        ConflictException or DbUpdateConcurrencyException => HttpStatusCode.Conflict,
        BusinessRuleException => HttpStatusCode.UnprocessableEntity,
        RateLimitedException { StoreUnavailable: true } => HttpStatusCode.ServiceUnavailable,
        RateLimitedException => HttpStatusCode.TooManyRequests,
        _ => HttpStatusCode.InternalServerError,
    };

    private async Task HandleExceptionAsync(FunctionContext context, System.Exception ex, HttpStatusCode statusCode)
    {
        // statusCode is resolved once by Invoke via MapStatusCode (single source of truth,
        // also used to tag the exception metric) and passed in. This switch owns logging +
        // body shape + the error code only.
        switch (ex)
        {
            case NotFoundException nfe:
                LogResourceNotFound(nfe, nfe.Message);
                await WriteErrorResponseAsync(context, statusCode, nfe.ErrorCode, nfe.Message);
                break;
            case ValidationException vex:
                LogValidationFailed(vex, vex.Message);
                await WriteErrorResponseAsync(context, statusCode, MiddlewareErrorCodes.ValidationFailed, vex.Message, vex.Errors);
                break;
            case UnauthorizedException uex:
                LogUnauthorized(uex, uex.Message);
                await WriteErrorResponseAsync(context, statusCode, MiddlewareErrorCodes.AuthRequired, uex.Message);
                break;
            case ForbiddenException fex:
                LogForbidden(fex, fex.Message);
                await WriteErrorResponseAsync(context, statusCode, MiddlewareErrorCodes.AuthForbidden, fex.Message);
                break;
            case ConflictException cex:
                LogConflict(cex, cex.ErrorCode, cex.Message);
                await WriteErrorResponseAsync(context, statusCode, cex.ErrorCode, cex.Message);
                break;
            case DbUpdateConcurrencyException dbex:
                // EF-level optimistic concurrency failure. Translate centrally
                // so every service that writes via DbContext benefits without
                // per-service boilerplate. Clients should reload and retry.
                LogDbConcurrency(dbex, dbex.Message);
                await WriteErrorResponseAsync(
                    context,
                    statusCode,
                    MiddlewareErrorCodes.DbConcurrencyConflict,
                    "The resource was modified concurrently. Please reload and retry.");
                break;
            case BusinessRuleException brex:
                LogBusinessRuleViolation(brex, brex.ErrorCode, brex.Message);
                await WriteErrorResponseAsync(context, statusCode, brex.ErrorCode, brex.Message);
                break;
            case RateLimitedException rlex:
                LogRateLimited(rlex, rlex.Window.ToString(), rlex.StoreUnavailable, (int)rlex.RetryAfter.TotalSeconds);
                await WriteRateLimitedResponseAsync(context, statusCode, rlex);
                break;
            case ConfigurationException confex:
                LogConfigurationError(confex, confex.Message);
                await WriteErrorResponseAsync(context, statusCode, MiddlewareErrorCodes.ConfigurationError, "Service configuration error");
                break;
            default:
                LogUnhandledException(ex, context.FunctionDefinition.Name, ex.Message);
                await WriteErrorResponseAsync(context, statusCode, MiddlewareErrorCodes.InternalError, "An unexpected error occurred");
                break;
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

    [LoggerMessage(EventId = 5090, Level = LogLevel.Warning, Message = "Concurrent update conflict: {Message}")]
    private partial void LogDbConcurrency(System.Exception ex, string message);

    [LoggerMessage(EventId = 5006, Level = LogLevel.Warning, Message = "Business rule violation: {Code} - {Message}")]
    private partial void LogBusinessRuleViolation(System.Exception ex, string code, string message);

    [LoggerMessage(EventId = 5007, Level = LogLevel.Error, Message = "Configuration error: {Message}")]
    private partial void LogConfigurationError(System.Exception ex, string message);

    [LoggerMessage(EventId = 5008, Level = LogLevel.Error, Message = "Unhandled exception in {FunctionName}: {Message}")]
    private partial void LogUnhandledException(System.Exception ex, string functionName, string message);

    [LoggerMessage(EventId = 5009, Level = LogLevel.Warning,
        Message = "Rate limit triggered: window={Window} storeUnavailable={StoreUnavailable} retryAfterSeconds={RetryAfterSeconds}")]
    private partial void LogRateLimited(System.Exception ex, string window, bool storeUnavailable, int retryAfterSeconds);

    private Task WriteRateLimitedResponseAsync(FunctionContext context, HttpStatusCode statusCode, RateLimitedException ex) =>
        WriteErrorResponseAsync(
            context,
            statusCode,
            RateLimitResponseHelper.ErrorCode(ex),
            ex.Message,
            additionalHeaders: RateLimitResponseHelper.BuildHeaders(ex));

    private async Task WriteErrorResponseAsync(
        FunctionContext context,
        HttpStatusCode statusCode,
        string code,
        string message,
        Dictionary<string, object>? details = null,
        Dictionary<string, string>? additionalHeaders = null)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();
        if (httpRequest == null) return;

        var response = httpRequest.CreateResponse(statusCode);

        // Remove the existing Content-Type header to prevent FormatException
        if (response.Headers.Contains(HttpHeaderNames.ContentType))
        {
            response.Headers.Remove(HttpHeaderNames.ContentType);
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
        response.Headers.Add(HttpHeaderNames.ContentType, contentType);

        if (additionalHeaders is not null)
        {
            foreach (var header in additionalHeaders)
            {
                if (response.Headers.Contains(header.Key))
                    response.Headers.Remove(header.Key);
                response.Headers.Add(header.Key, header.Value);
            }
        }

        await response.Body.WriteAsync(bytes);

        // Set the response on the context
        var invocationResult = context.GetInvocationResult();
        invocationResult.Value = response;
    }
}