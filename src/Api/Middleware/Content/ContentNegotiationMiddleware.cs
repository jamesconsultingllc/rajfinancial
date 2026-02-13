using Microsoft.Azure.Functions.Worker;
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
///   <item>Stores the serialization factory in context for use by functions</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage in Functions:</b>
/// <code>
/// // Get request body
/// var request = context.DeserializeBody&lt;CreateAssetRequest&gt;();
/// 
/// // Create response with content negotiation
/// return await context.CreateSerializedResponseAsync(req, HttpStatusCode.OK, data);
/// </code>
/// </para>
/// </remarks>
public class ContentNegotiationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ContentNegotiationMiddleware> logger;
    private readonly ISerializationFactory serializationFactory;

    public ContentNegotiationMiddleware(
        ILogger<ContentNegotiationMiddleware> logger,
        ISerializationFactory serializationFactory)
    {
        this.logger = logger;
        this.serializationFactory = serializationFactory;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();

        string? acceptHeader = null;
        string? contentTypeHeader = null;

        if (httpRequest != null)
        {
            // Get Accept header for response content type
            if (httpRequest.Headers.TryGetValues("Accept", out var acceptValues))
            {
                acceptHeader = acceptValues.FirstOrDefault();
            }

            // Get Content-Type header for request body deserialization
            if (httpRequest.Headers.TryGetValues("Content-Type", out var contentTypeValues))
            {
                contentTypeHeader = contentTypeValues.FirstOrDefault();
            }

            // Read and store request body for later deserialization
            if (httpRequest.Body.CanRead && httpRequest.Body.Length > 0)
            {
                httpRequest.Body.Position = 0;
                var bodyBytes = new byte[httpRequest.Body.Length];
                await httpRequest.Body.ReadExactlyAsync(bodyBytes);
                httpRequest.Body.Position = 0; // Reset for potential re-reading

                context.Items["RequestBodyBytes"] = bodyBytes;
                context.Items["RequestContentType"] = contentTypeHeader ?? SerializationFactory.JsonContentType;
            }
        }

        // Determine response content type
        var responseContentType = serializationFactory.GetPreferredContentType(acceptHeader);

        // Store in context for use by functions
        context.Items["ResponseContentType"] = responseContentType;
        context.Items["SerializationFactory"] = serializationFactory;

        logger.LogDebug(
            "Content negotiation: Accept={Accept}, ContentType={ContentType}, ResponseFormat={ResponseFormat}",
            acceptHeader,
            contentTypeHeader,
            responseContentType);

        await next(context);
    }
}