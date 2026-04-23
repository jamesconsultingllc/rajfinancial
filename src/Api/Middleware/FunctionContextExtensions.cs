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
        return context.Items.TryGetValue(FunctionContextKeys.UserId, out var userId) ? userId as string : null;
    }

    /// <summary>
    /// Gets the authenticated user's ID as a <see cref="Guid"/> from the function context.
    /// Prefer this over <see cref="GetUserId"/> when passing to <c>IAuthorizationService</c>.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The user ID as a Guid, or null if not authenticated or not a valid Guid.</returns>
    public static Guid? GetUserIdAsGuid(this FunctionContext context)
    {
        return context.Items.TryGetValue(FunctionContextKeys.UserIdGuid, out var guid) ? guid as Guid? : null;
    }

    /// <summary>
    /// Gets the authenticated user's email from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The user email, or null if not authenticated.</returns>
    public static string? GetUserEmail(this FunctionContext context)
    {
        return context.Items.TryGetValue(FunctionContextKeys.UserEmail, out var email) ? email as string : null;
    }

    /// <summary>
    /// Gets the authenticated user's display name from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The user name, or null if not authenticated.</returns>
    public static string? GetUserName(this FunctionContext context)
    {
        return context.Items.TryGetValue(FunctionContextKeys.UserName, out var name) ? name as string : null;
    }

    /// <summary>
    /// Gets the authenticated user's roles from the function context.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The user's roles, or an empty collection if not authenticated.</returns>
    public static IReadOnlyList<string> GetUserRoles(this FunctionContext context)
    {
        return context.Items.TryGetValue(FunctionContextKeys.UserRoles, out var roles)
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
        return context.Items.TryGetValue(FunctionContextKeys.IsAuthenticated, out var isAuth) &&
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
        return context.Items.TryGetValue(FunctionContextKeys.ClaimsPrincipal, out var principal)
            ? principal as ClaimsPrincipal
            : null;
    }

    /// <summary>
    /// Gets the authenticated user's Entra tenant id (<c>tid</c> claim) as a <see cref="Guid"/>.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The tenant id, or <c>null</c> if the claim was absent or not a valid Guid.</returns>
    public static Guid? GetTenantId(this FunctionContext context)
    {
        return context.Items.TryGetValue(FunctionContextKeys.TenantId, out var tid) ? tid as Guid? : null;
    }
}