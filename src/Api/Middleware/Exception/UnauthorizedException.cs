namespace RajFinancial.Api.Middleware;

/// <summary>
/// Exception thrown when authentication is required but missing or invalid.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Authentication required") : base(message) { }
}