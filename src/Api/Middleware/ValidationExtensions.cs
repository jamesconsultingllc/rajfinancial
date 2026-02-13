using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace RajFinancial.Api.Middleware;

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