using System.Text.Json;
using MemoryPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RajFinancial.Api.Middleware;

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
    private readonly ILogger<ContentNegotiationMiddleware> _logger;
    private readonly ISerializationFactory _serializationFactory;

    public ContentNegotiationMiddleware(
        ILogger<ContentNegotiationMiddleware> logger,
        ISerializationFactory serializationFactory)
    {
        _logger = logger;
        _serializationFactory = serializationFactory;
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
        var responseContentType = _serializationFactory.GetPreferredContentType(acceptHeader);

        // Store in context for use by functions
        context.Items["ResponseContentType"] = responseContentType;
        context.Items["SerializationFactory"] = _serializationFactory;

        _logger.LogDebug(
            "Content negotiation: Accept={Accept}, ContentType={ContentType}, ResponseFormat={ResponseFormat}",
            acceptHeader,
            contentTypeHeader,
            responseContentType);

        await next(context);
    }
}

/// <summary>
/// Factory for serialization based on content type and environment.
/// </summary>
public interface ISerializationFactory
{
    /// <summary>
    /// Gets the preferred content type based on Accept header and environment.
    /// </summary>
    string GetPreferredContentType(string? acceptHeader);

    /// <summary>
    /// Serializes an object to bytes based on content type.
    /// </summary>
    byte[] Serialize<T>(T value, string contentType);

    /// <summary>
    /// Deserializes bytes to an object based on content type.
    /// </summary>
    T? Deserialize<T>(byte[] data, string contentType);
}

/// <summary>
/// Implementation of serialization factory supporting JSON and MemoryPack.
/// </summary>
/// <remarks>
/// <para>
/// <b>Strategy:</b>
/// <list type="bullet">
///   <item>Development: Always JSON for easy debugging in browser/Postman</item>
///   <item>Production: MemoryPack by default for performance, JSON if explicitly requested</item>
/// </list>
/// </para>
/// <para>
/// <b>Performance Benefits of MemoryPack:</b>
/// <list type="bullet">
///   <item>7-8x faster serialization than System.Text.Json</item>
///   <item>60% smaller payload sizes</item>
///   <item>Near-zero memory allocation</item>
/// </list>
/// </para>
/// </remarks>
public class SerializationFactory : ISerializationFactory
{
    public const string JsonContentType = "application/json";
    public const string MemoryPackContentType = "application/x-memorypack";

    private readonly IConfiguration _configuration;
    private readonly ILogger<SerializationFactory> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SerializationFactory(
        IConfiguration configuration,
        ILogger<SerializationFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Whether to use MemoryPack in production (configurable).
    /// </summary>
    private bool UseMemoryPackInProduction =>
        _configuration.GetValue("Serialization:UseMemoryPackInProduction", true);

    /// <summary>
    /// Current environment name.
    /// </summary>
    private string Environment =>
        _configuration.GetValue<string>("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development";

    /// <inheritdoc />
    public string GetPreferredContentType(string? acceptHeader)
    {
        // In development, always prefer JSON for debugging
        if (Environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Development environment: using JSON serialization");
            return JsonContentType;
        }

        // In production, check configuration and Accept header
        if (!UseMemoryPackInProduction)
        {
            return JsonContentType;
        }

        // If client explicitly requests only JSON, honor it
        if (!string.IsNullOrEmpty(acceptHeader))
        {
            var acceptsJson = acceptHeader.Contains(JsonContentType, StringComparison.OrdinalIgnoreCase);
            var acceptsMemoryPack = acceptHeader.Contains(MemoryPackContentType, StringComparison.OrdinalIgnoreCase);

            // Client only wants JSON
            if (acceptsJson && !acceptsMemoryPack)
            {
                return JsonContentType;
            }

            // Client explicitly wants MemoryPack
            if (acceptsMemoryPack)
            {
                return MemoryPackContentType;
            }
        }

        // Default to MemoryPack in production
        return MemoryPackContentType;
    }

    /// <inheritdoc />
    public byte[] Serialize<T>(T value, string contentType)
    {
        if (contentType.Equals(MemoryPackContentType, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Serializing {Type} with MemoryPack", typeof(T).Name);
            return MemoryPackSerializer.Serialize(value);
        }

        _logger.LogDebug("Serializing {Type} with JSON", typeof(T).Name);
        return JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
    }

    /// <inheritdoc />
    public T? Deserialize<T>(byte[] data, string contentType)
    {
        if (data.Length == 0)
        {
            return default;
        }

        if (contentType.Equals(MemoryPackContentType, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Deserializing {Type} with MemoryPack", typeof(T).Name);
            return MemoryPackSerializer.Deserialize<T>(data);
        }

        _logger.LogDebug("Deserializing {Type} with JSON", typeof(T).Name);
        return JsonSerializer.Deserialize<T>(data, _jsonOptions);
    }
}

/// <summary>
/// Extension methods for content negotiation in Azure Functions.
/// </summary>
public static class ContentNegotiationExtensions
{
    /// <summary>
    /// Deserializes the request body using the appropriate serializer.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="context">The function context.</param>
    /// <returns>The deserialized object, or null if body is empty.</returns>
    public static T? DeserializeBody<T>(this FunctionContext context) where T : class
    {
        if (!context.Items.TryGetValue("RequestBodyBytes", out var bodyObj) ||
            bodyObj is not byte[] bodyBytes ||
            bodyBytes.Length == 0)
        {
            return null;
        }

        var contentType = context.Items.TryGetValue("RequestContentType", out var ct)
            ? ct as string ?? SerializationFactory.JsonContentType
            : SerializationFactory.JsonContentType;

        var factory = context.Items.TryGetValue("SerializationFactory", out var f)
            ? f as ISerializationFactory
            : null;

        if (factory == null)
        {
            // Fallback to JSON if factory not available
            return JsonSerializer.Deserialize<T>(bodyBytes, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        return factory.Deserialize<T>(bodyBytes, contentType);
    }

    /// <summary>
    /// Creates an HTTP response with content negotiation.
    /// </summary>
    /// <typeparam name="T">The type of the response body.</typeparam>
    /// <param name="context">The function context.</param>
    /// <param name="request">The HTTP request.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="data">The data to serialize.</param>
    /// <returns>The HTTP response with serialized data.</returns>
    public static async Task<HttpResponseData> CreateSerializedResponseAsync<T>(
        this FunctionContext context,
        HttpRequestData request,
        System.Net.HttpStatusCode statusCode,
        T data)
    {
        var contentType = context.Items.TryGetValue("ResponseContentType", out var ct)
            ? ct as string ?? SerializationFactory.JsonContentType
            : SerializationFactory.JsonContentType;

        var factory = context.Items.TryGetValue("SerializationFactory", out var f)
            ? f as ISerializationFactory
            : null;

        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", contentType);

        if (factory != null)
        {
            var bytes = factory.Serialize(data, contentType);
            await response.Body.WriteAsync(bytes);
        }
        else
        {
            // Fallback to JSON
            await response.WriteAsJsonAsync(data);
        }

        return response;
    }

    /// <summary>
    /// Gets the response content type determined by content negotiation.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The content type string.</returns>
    public static string GetResponseContentType(this FunctionContext context)
    {
        return context.Items.TryGetValue("ResponseContentType", out var ct)
            ? ct as string ?? SerializationFactory.JsonContentType
            : SerializationFactory.JsonContentType;
    }

    /// <summary>
    /// Gets the serialization factory from the context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The serialization factory, or null if not available.</returns>
    public static ISerializationFactory? GetSerializationFactory(this FunctionContext context)
    {
        return context.Items.TryGetValue("SerializationFactory", out var f)
            ? f as ISerializationFactory
            : null;
    }
}
