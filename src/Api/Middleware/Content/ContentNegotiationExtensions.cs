using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace RajFinancial.Api.Middleware.Content;

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
    /// <param name="serializationFactory">The serialization factory (injected via DI).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized object, or null if body is empty.</returns>
    public static async Task<T?> DeserializeBodyAsync<T>(
        this FunctionContext context,
        ISerializationFactory serializationFactory,
        CancellationToken cancellationToken = default) where T : class
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

        return await serializationFactory.DeserializeAsync<T>(bodyBytes, contentType, cancellationToken);
    }

    /// <summary>
    /// Creates an HTTP response with content negotiation.
    /// </summary>
    /// <typeparam name="T">The type of the response body.</typeparam>
    /// <param name="context">The function context.</param>
    /// <param name="request">The HTTP request.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="data">The data to serialize.</param>
    /// <param name="serializationFactory">The serialization factory (injected via DI).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response with serialized data.</returns>
    public static async Task<HttpResponseData> CreateSerializedResponseAsync<T>(
        this FunctionContext context,
        HttpRequestData request,
        HttpStatusCode statusCode,
        T data,
        ISerializationFactory serializationFactory,
        CancellationToken cancellationToken = default)
    {
        var contentType = context.Items.TryGetValue("ResponseContentType", out var ct)
            ? ct as string ?? SerializationFactory.JsonContentType
            : SerializationFactory.JsonContentType;

        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", contentType);

        var bytes = await serializationFactory.SerializeAsync(data, contentType, cancellationToken);
        await response.Body.WriteAsync(bytes, cancellationToken);

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

}