namespace RajFinancial.Api.Middleware;

/// <summary>
/// Exception thrown when the user lacks permission for the requested action.
/// </summary>
public class ForbiddenException(string message = "Access denied") : Exception(message);