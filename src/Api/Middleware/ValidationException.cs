namespace RajFinancial.Api.Middleware;

/// <summary>
/// Exception thrown when request validation fails.
/// </summary>
public class ValidationException : Exception
{
    public Dictionary<string, object>? Errors { get; }

    public ValidationException(string message, Dictionary<string, object>? errors = null) : base(message)
    {
        Errors = errors;
    }
}