using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RajFinancial.Api.Middleware;

/// <summary>
/// Middleware that automatically validates request bodies using FluentValidation.
/// </summary>
/// <remarks>
/// <para>
/// This middleware intercepts HTTP requests, deserializes the body, and validates
/// it using the appropriate FluentValidation validator if one is registered.
/// </para>
/// <para>
/// <b>How it works:</b>
/// <list type="number">
///   <item>Reads the request body</item>
///   <item>Stores the body in context for later use by the function</item>
///   <item>If a validator is registered for the request type, validates the body</item>
///   <item>If validation fails, returns 400 Bad Request with error details</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage in Functions:</b>
/// <code>
/// // Get validated request body from context
/// var request = context.GetValidatedBody&lt;CreateAssetRequest&gt;();
/// </code>
/// </para>
/// <para>
/// <b>Extension Points:</b>
/// <list type="bullet">
///   <item>Add custom validation error formatting</item>
///   <item>Add validation caching for performance</item>
/// </list>
/// </para>
/// </remarks>
public class ValidationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ValidationMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ValidationMiddleware(ILogger<ValidationMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();

        // Only process requests with a body
        if (httpRequest?.Body != null && httpRequest.Body.Length > 0)
        {
            try
            {
                // Read and store the body for later use
                httpRequest.Body.Position = 0;
                using var reader = new StreamReader(httpRequest.Body, leaveOpen: true);
                var bodyString = await reader.ReadToEndAsync();
                httpRequest.Body.Position = 0; // Reset for potential re-reading

                // Store raw body in context
                context.Items["RequestBody"] = bodyString;

                _logger.LogDebug("Request body captured for validation");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read request body for validation");
            }
        }

        await next(context);
    }
}

/// <summary>
/// Extension methods for validation in Azure Functions.
/// </summary>
public static class ValidationExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Gets and validates the request body using the registered FluentValidation validator.
    /// </summary>
    /// <typeparam name="T">The type to deserialize and validate.</typeparam>
    /// <param name="context">The function context.</param>
    /// <returns>The validated request body.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static async Task<T> GetValidatedBodyAsync<T>(this FunctionContext context) where T : class
    {
        var body = GetBody<T>(context);
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
    /// Gets the request body without validation.
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
        return GetBody<Dictionary<string, object>>(context);
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
