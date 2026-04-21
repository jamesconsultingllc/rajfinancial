using System.Diagnostics;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware.Content;

namespace RajFinancial.Api.Middleware;

/// <summary>
/// Middleware that prepares request body data for downstream validation and deserialization.
/// </summary>
/// <remarks>
/// <para>
/// <b>Content-Aware Body Handling:</b> Only converts request body bytes to a UTF-8 string
/// (stored as <c>Items["RequestBody"]</c>) when the request Content-Type is JSON.
/// For binary formats like MemoryPack, the raw bytes in <c>Items["RequestBodyBytes"]</c>
/// are preserved and deserialized directly by <see cref="ValidationExtensions.GetBody{T}"/>.
/// </para>
/// <para>
/// <b>Usage in Functions:</b>
/// <code>
/// var request = await context.GetValidatedBodyAsync&lt;CreateAssetRequest&gt;();
/// </code>
/// </para>
/// </remarks>
public partial class ValidationMiddleware(ILogger<ValidationMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        using var activity = MiddlewareTelemetry.StartActivity("Middleware.Validation");
        activity?.SetTag("middleware.name", "ValidationMiddleware");
        activity?.SetTag("code.function", context.FunctionDefinition.Name);

        var sw = Stopwatch.StartNew();
        try
        {
#pragma warning disable S125 // False positive: documentation comment mentions code-like context key.
            // ContentNegotiationMiddleware runs before this and stores the raw body bytes
            // in context.Items["RequestBodyBytes"]. Only convert to string for JSON payloads;
            // MemoryPack binary payloads would be corrupted by UTF-8 string conversion.
#pragma warning restore S125
            if (context.Items.TryGetValue(FunctionContextKeys.RequestBodyBytes, out var bytesObj) &&
                bytesObj is byte[] { Length: > 0 } bodyBytes)
            {
                var contentType = context.Items.TryGetValue(FunctionContextKeys.RequestContentType, out var ctObj)
                    ? ctObj as string ?? SerializationFactory.JsonContentType
                    : SerializationFactory.JsonContentType;

                if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    context.Items[FunctionContextKeys.RequestBody] = Encoding.UTF8.GetString(bodyBytes);
                }

                LogBodyCaptured(contentType);
            }

            await next(context);
        }
        catch (System.Exception ex)
        {
            MiddlewareTelemetry.RecordException("ValidationMiddleware", ex.GetType().Name, 0);
            throw;
        }
        finally
        {
            sw.Stop();
            MiddlewareTelemetry.RecordDuration("ValidationMiddleware", sw.Elapsed.TotalMilliseconds);
        }
    }

    [LoggerMessage(EventId = 5400, Level = LogLevel.Debug,
        Message = "Request body captured for validation ({ContentType})")]
    private partial void LogBodyCaptured(string contentType);
}
