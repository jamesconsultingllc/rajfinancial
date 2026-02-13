namespace RajFinancial.Api.Middleware;

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }

    public BusinessRuleException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}