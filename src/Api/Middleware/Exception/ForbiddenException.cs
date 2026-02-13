namespace RajFinancial.Api.Middleware;

/// <summary>
/// Exception thrown when the user lacks permission for the requested action.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Access denied") : base(message) { }
}