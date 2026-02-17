using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using RajFinancial.Api.Middleware.Content;

namespace RajFinancial.Api.Middleware;

/// <summary>
/// Extension methods for request body deserialization and validation in Azure Functions.
/// </summary>
/// <remarks>
/// <para>
/// Supports both JSON and MemoryPack request bodies via content-type-aware deserialization.
/// JSON bodies are deserialized from <c>Items["RequestBody"]</c> (UTF-8 string);
/// MemoryPack bodies are deserialized from <c>Items["RequestBodyBytes"]</c> (raw bytes)
/// using <see cref="ISerializationFactory"/> resolved from DI.
/// </para>
/// </remarks>
public static class ValidationExtensions
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Gets and validates the request body using the registered FluentValidation validator.
    /// Supports both JSON and MemoryPack content types.
    /// </summary>
    /// <typeparam name="T">The type to deserialize and validate.</typeparam>
    /// <param name="context">The function context.</param>
    /// <returns>The validated request body.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails or body is missing.</exception>
    public static async Task<T> GetValidatedBodyAsync<T>(this FunctionContext context) where T : class
    {
        var body = await context.GetBodyAsync<T>();
        if (body == null)
        {
            throw new ValidationException("Request body is required");
        }

        // Get validator from DI if registered
        var validator = context.InstanceServices.GetService<IValidator<T>>();
        if (validator != null)
        {
            var result = await validator.ValidateAsync(body);
            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => (object)g.Select(e => e.ErrorMessage).ToList()
                    );

                throw new ValidationException(
                    result.Errors.First().ErrorMessage,
                    errors
                );
            }
        }

        return body;
    }

    /// <summary>
    /// Gets the request body, deserializing from the appropriate format based on content type.
    /// Uses <see cref="ISerializationFactory"/> for MemoryPack payloads and falls back to
    /// JSON string deserialization for JSON payloads.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="context">The function context.</param>
    /// <returns>The deserialized request body, or null if not available.</returns>
    public static async Task<T?> GetBodyAsync<T>(this FunctionContext context) where T : class
    {
        // Try raw bytes + content type first (supports both JSON and MemoryPack)
        if (context.Items.TryGetValue("RequestBodyBytes", out var bytesObj) &&
            bytesObj is byte[] bodyBytes &&
            bodyBytes.Length > 0)
        {
            var contentType = context.Items.TryGetValue("RequestContentType", out var ctObj)
                ? ctObj as string ?? SerializationFactory.JsonContentType
                : SerializationFactory.JsonContentType;

            // For non-JSON content types (e.g. MemoryPack), use ISerializationFactory
            if (!contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                var factory = context.InstanceServices.GetService<ISerializationFactory>();
                if (factory != null)
                {
                    return await factory.DeserializeAsync<T>(bodyBytes, contentType);
                }
            }
        }

        // Fall back to JSON string deserialization (set by ValidationMiddleware for JSON requests)
        if (context.Items.TryGetValue("RequestBody", out var bodyObj) && bodyObj is string bodyString)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(bodyString, jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the request body without validation (synchronous JSON-only fallback).
    /// Prefer <see cref="GetBodyAsync{T}"/> for full content-type support.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="context">The function context.</param>
    /// <returns>The deserialized request body, or null if not available.</returns>
    public static T? GetBody<T>(this FunctionContext context) where T : class
    {
        if (context.Items.TryGetValue("RequestBody", out var bodyObj) && bodyObj is string bodyString)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(bodyString, jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the request body as a dictionary for dynamic access.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The request body as a dictionary, or null if not available.</returns>
    public static Dictionary<string, object>? GetBodyAsDictionary(this FunctionContext context)
    {
        return context.GetBody<Dictionary<string, object>>();
    }

    /// <summary>
    /// Validates an object using the registered validator.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    /// <param name="context">The function context.</param>
    /// <param name="instance">The object to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static async Task ValidateAsync<T>(this FunctionContext context, T instance) where T : class
    {
        var validator = context.InstanceServices.GetService<IValidator<T>>();
        if (validator != null)
        {
            var result = await validator.ValidateAsync(instance);
            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => (object)g.Select(e => e.ErrorMessage).ToList()
                    );

                throw new ValidationException(
                    result.Errors.First().ErrorMessage,
                    errors
                );
            }
        }
    }
}