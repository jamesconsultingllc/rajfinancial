using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace RajFinancial.Api.Middleware.Content;

/// <summary>
/// Middleware that handles content negotiation between JSON and MemoryPack.
/// </summary>
/// <remarks>
/// <para>
/// This middleware provides dual serialization support:
/// <list type="bullet">
///   <item><b>Development</b>: Always uses JSON for easy debugging</item>
///   <item><b>Production</b>: Uses MemoryPack for 7-8x faster serialization and 60% smaller payloads</item>
/// </list>
/// </para>
/// <para>
/// The middleware:
/// <list type="number">
///   <item>Reads the request body and stores it in context for later deserialization</item>
///   <item>Determines the response content type based on Accept header and environment</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage in Functions:</b>
/// <code>
/// // Get request body
/// var request = await context.DeserializeBodyAsync&lt;CreateAssetRequest&gt;();
/// 
/// // Create response with content negotiation
/// return await context.CreateSerializedResponseAsync(req, HttpStatusCode.OK, data);
/// </code>
/// </para>
/// </remarks>
public partial class ContentNegotiationMiddleware(
    ILogger<ContentNegotiationMiddleware> logger,
    ISerializationFactory serializationFactory)
    : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        using var activity = MiddlewareTelemetry.StartActivity("Middleware.ContentNegotiation");
        activity?.SetTag("middleware.name", "ContentNegotiationMiddleware");
        activity?.SetTag("code.function", context.FunctionDefinition.Name);

        var sw = Stopwatch.StartNew();
        try
        {
            var httpRequest = await context.GetHttpRequestDataAsync();

            var (acceptHeader, contentTypeHeader) = ReadContentHeaders(httpRequest);

            if (httpRequest != null)
            {
                await CaptureRequestBodyAsync(httpRequest, context, contentTypeHeader);
            }

            var responseContentType = serializationFactory.GetPreferredContentType(acceptHeader);
            context.Items[FunctionContextKeys.ResponseContentType] = responseContentType;

            activity?.SetTag("http.request.content_type", contentTypeHeader);
            activity?.SetTag("http.response.content_type", responseContentType);

            LogContentNegotiation(acceptHeader, contentTypeHeader, responseContentType);

            await next(context);
        }
        catch
        {
            MiddlewareTelemetry.RecordException("ContentNegotiationMiddleware", null, 0);
            throw;
        }
        finally
        {
            sw.Stop();
            MiddlewareTelemetry.RecordDuration("ContentNegotiationMiddleware", sw.Elapsed.TotalMilliseconds);
        }
    }

    private static (string? Accept, string? ContentType) ReadContentHeaders(HttpRequestData? httpRequest)
    {
        if (httpRequest is null)
        {
            return (null, null);
        }

        string? accept = null;
        string? contentType = null;

        if (httpRequest.Headers.TryGetValues("Accept", out var acceptValues))
        {
            accept = acceptValues.FirstOrDefault();
        }

        if (httpRequest.Headers.TryGetValues(HttpHeaderNames.ContentType, out var contentTypeValues))
        {
            contentType = contentTypeValues.FirstOrDefault();
        }

        return (accept, contentType);
    }

    private static async Task CaptureRequestBodyAsync(
        HttpRequestData httpRequest,
        FunctionContext context,
        string? contentTypeHeader)
    {
        if (!httpRequest.Body.CanRead)
        {
            return;
        }

        using var memoryStream = new MemoryStream();
        await httpRequest.Body.CopyToAsync(memoryStream);
        var bodyBytes = memoryStream.ToArray();

        if (bodyBytes.Length > 0)
        {
            context.Items[FunctionContextKeys.RequestBodyBytes] = bodyBytes;
            context.Items[FunctionContextKeys.RequestContentType] = contentTypeHeader ?? SerializationFactory.JsonContentType;
        }

        // Reset body stream position so downstream functions can still read it
        if (httpRequest.Body.CanSeek)
        {
            httpRequest.Body.Position = 0;
        }
    }

    [LoggerMessage(EventId = 5501, Level = LogLevel.Debug,
        Message = "Content negotiation: Accept={Accept}, ContentType={ContentType}, ResponseFormat={ResponseFormat}")]
    private partial void LogContentNegotiation(string? accept, string? contentType, string responseFormat);
}
