namespace RajFinancial.Api.Middleware.Exception;

/// <summary>
/// Exception thrown when the user lacks permission for the requested action.
/// </summary>
public class ForbiddenException(string message = "Access denied") : System.Exception(message);