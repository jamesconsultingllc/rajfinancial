namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
/// Exception thrown when authentication is required but missing or invalid.
/// </summary>
public class UnauthorizedException(string message = "Authentication required") : System.Exception(message);