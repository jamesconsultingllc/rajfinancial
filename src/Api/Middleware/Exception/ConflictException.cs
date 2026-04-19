namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
/// Exception thrown when a request conflicts with the current state of a
/// resource (e.g., uniqueness constraint, "only-one-allowed" singletons).
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException(string errorCode, string message) : System.Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
