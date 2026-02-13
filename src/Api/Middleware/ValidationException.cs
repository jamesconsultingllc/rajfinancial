namespace RajFinancial.Api.Middleware;

/// <summary>
/// Exception thrown when request validation fails.
/// </summary>
public class ValidationException(string message, Dictionary<string, object>? errors = null) : Exception(message)
{
    public Dictionary<string, object>? Errors { get; } = errors;
}