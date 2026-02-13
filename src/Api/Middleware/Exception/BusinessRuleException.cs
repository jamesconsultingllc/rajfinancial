namespace RajFinancial.Api.Middleware;

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}