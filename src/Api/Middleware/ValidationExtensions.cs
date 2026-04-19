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
    private static readonly JsonSerializerOptions JsonOptions = new()
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
        var memoryPackBody = await TryDeserializeBinaryBodyAsync<T>(context);
        if (memoryPackBody is not null)
        {
            return memoryPackBody;
        }

        // Fall back to JSON string deserialization (set by ValidationMiddleware for JSON requests)
        if (context.Items.TryGetValue(FunctionContextKeys.RequestBody, out var bodyObj) && bodyObj is string bodyString)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(bodyString, JsonOptions);
            }
            catch (JsonException ex)
            {
                // Surface deserialization failures as field-level ValidationExceptions so
                // callers see `{ field: [message] }` rather than the generic "Request body
                // is required" that GetValidatedBodyAsync would produce if we returned null.
                // Uses JsonException.Path (e.g. "$.name") to attribute the error to a field.
                throw ToValidationException(ex);
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to deserialize a non-JSON (e.g. MemoryPack) body via the registered
    /// <see cref="ISerializationFactory"/>. Returns null when the context has no raw body
    /// bytes, when the content type is JSON (handled by the caller), or when no factory
    /// is registered.
    /// </summary>
    private static async Task<T?> TryDeserializeBinaryBodyAsync<T>(FunctionContext context) where T : class
    {
        if (!context.Items.TryGetValue(FunctionContextKeys.RequestBodyBytes, out var bytesObj) ||
            bytesObj is not byte[] bodyBytes ||
            bodyBytes.Length == 0)
        {
            return null;
        }

        var contentType = context.Items.TryGetValue(FunctionContextKeys.RequestContentType, out var ctObj)
            ? ctObj as string ?? SerializationFactory.JsonContentType
            : SerializationFactory.JsonContentType;

        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var factory = context.InstanceServices.GetService<ISerializationFactory>();
        if (factory is null)
        {
            return null;
        }

        return await factory.DeserializeAsync<T>(bodyBytes, contentType);
    }

    private static ValidationException ToValidationException(JsonException ex)
    {
        var field = ExtractFieldFromJsonPath(ex.Path) ?? "body";
        var message = ex.Message;
        var errors = new Dictionary<string, object>
        {
            [field] = new List<string> { message }
        };
        return new ValidationException(message, errors);
    }

    private static string? ExtractFieldFromJsonPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "$")
            return null;

        // System.Text.Json paths look like "$.name" or "$.items[0].id". Pull the top-level segment.
        var stripped = path.StartsWith("$.", StringComparison.Ordinal) ? path[2..] : path;
        var end = stripped.IndexOfAny(new[] { '.', '[' });
        return end < 0 ? stripped : stripped[..end];
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
        if (context.Items.TryGetValue(FunctionContextKeys.RequestBody, out var bodyObj) && bodyObj is string bodyString)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(bodyString, JsonOptions);
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