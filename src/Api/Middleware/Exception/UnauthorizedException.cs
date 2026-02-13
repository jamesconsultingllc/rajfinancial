namespace RajFinancial.Api.Middleware;

/// <summary>
/// Exception thrown when authentication is required but missing or invalid.
/// </summary>
public class UnauthorizedException(string message = "Authentication required") : Exception(message);