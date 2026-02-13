using System.Text.Json;
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