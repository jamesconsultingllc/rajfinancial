using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;

namespace RajFinancial.Api.Middleware;

/// <summary>
/// Extension methods for accessing user context from FunctionContext.
/// </summary>
public static class FunctionContextExtensions
{
    /// <summary>
    /// Gets the authenticated user's ID from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The user ID, or null if not authenticated.</returns>
    public static string? GetUserId(this FunctionContext context)
    {
        return context.Items.TryGetValue("UserId", out var userId) ? userId as string : null;
    }

    /// <summary>
    /// Gets the authenticated user's email from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The user email, or null if not authenticated.</returns>
    public static string? GetUserEmail(this FunctionContext context)
    {
        return context.Items.TryGetValue("UserEmail", out var email) ? email as string : null;
    }

    /// <summary>
    /// Gets the authenticated user's display name from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The user name, or null if not authenticated.</returns>
    public static string? GetUserName(this FunctionContext context)
    {
        return context.Items.TryGetValue("UserName", out var name) ? name as string : null;
    }

    /// <summary>
    /// Gets the authenticated user's roles from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The user's roles, or an empty collection if not authenticated.</returns>
    public static IReadOnlyList<string> GetUserRoles(this FunctionContext context)
    {
        return context.Items.TryGetValue("UserRoles", out var roles)
            ? roles as IReadOnlyList<string> ?? []
            : [];
    }

    /// <summary>
    /// Checks if the current request is authenticated.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>True if authenticated, false otherwise.</returns>
    public static bool IsAuthenticated(this FunctionContext context)
    {
        return context.Items.TryGetValue("IsAuthenticated", out var isAuth) &&
               isAuth is true;
    }

    /// <summary>
    /// Checks if the authenticated user has a specific role.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user has the role, false otherwise.</returns>
    public static bool HasRole(this FunctionContext context, string role)
    {
        return context.GetUserRoles().Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the authenticated user is an administrator.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>True if the user is an administrator, false otherwise.</returns>
    public static bool IsAdministrator(this FunctionContext context)
    {
        return context.HasRole("Administrator");
    }

    /// <summary>
    /// Gets the ClaimsPrincipal from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The ClaimsPrincipal, or null if not authenticated.</returns>
    public static ClaimsPrincipal? GetClaimsPrincipal(this FunctionContext context)
    {
        return context.Items.TryGetValue("ClaimsPrincipal", out var principal)
            ? principal as ClaimsPrincipal
            : null;
    }

    /// <summary>
    /// Requires authentication. Throws UnauthorizedException if not authenticated.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <exception cref="UnauthorizedException">Thrown when not authenticated.</exception>
    public static void RequireAuthentication(this FunctionContext context)
    {
        if (!context.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }
    }

    /// <summary>
    /// Requires a specific role. Throws ForbiddenException if role is missing.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <param name="role">The required role.</param>
    /// <exception cref="UnauthorizedException">Thrown when not authenticated.</exception>
    /// <exception cref="ForbiddenException">Thrown when role is missing.</exception>
    public static void RequireRole(this FunctionContext context, string role)
    {
        context.RequireAuthentication();

        if (!context.HasRole(role))
        {
            throw new ForbiddenException($"Role '{role}' is required");
        }
    }

    /// <summary>
    /// Requires administrator role. Throws ForbiddenException if not an admin.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <exception cref="UnauthorizedException">Thrown when not authenticated.</exception>
    /// <exception cref="ForbiddenException">Thrown when not an administrator.</exception>
    public static void RequireAdministrator(this FunctionContext context)
    {
        context.RequireRole("Administrator");
    }
}