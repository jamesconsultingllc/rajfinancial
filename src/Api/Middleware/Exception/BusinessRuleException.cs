namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException(string errorCode, string message) : System.Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}