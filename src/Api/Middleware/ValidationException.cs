namespace RajFinancial.Api.Middleware;

/// <summary>
/// Exception thrown when request validation fails.
/// </summary>

public class ValidationException(string message, Dictionary<string, object>? errors = null) : System.Exception(message)
{
    public Dictionary<string, object>? Errors { get; } = errors;
}