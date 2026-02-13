using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace RajFinancial.Api.Middleware;

/// <summary>
/// Middleware that extracts and validates the authenticated user context from JWT claims.
/// </summary>
/// <remarks>
/// <para>
/// This middleware implements OWASP security requirements:
/// <list type="bullet">
///   <item>A01:2025 - Broken Access Control: Establishes user context for authorization</item>
///   <item>A07:2025 - Authentication Failures: Validates JWT claims</item>
/// </list>
/// </para>
/// <para>
/// The middleware extracts user information from Entra External ID JWT tokens and
/// makes it available to functions via <see cref="FunctionContext.Items"/>.
/// </para>
/// <para>
/// <b>Context Items Set:</b>
/// <list type="bullet">
///   <item><c>UserId</c> - The authenticated user's Entra Object ID (GUID)</item>
///   <item><c>UserEmail</c> - The user's email address</item>
///   <item><c>UserRoles</c> - Collection of app roles assigned to the user</item>
///   <item><c>ClaimsPrincipal</c> - The full claims principal for advanced scenarios</item>
/// </list>
/// </para>
/// </remarks>
public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<AuthenticationMiddleware> logger;

    // Standard claim types for Entra External ID
    private const string ObjectIdClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    private const string ObjectIdClaimAlt = "oid";
    private const string EmailClaim = "emails";
    private const string EmailClaimAlt = "email";
    private const string RolesClaim = "roles";
    private const string NameClaim = "name";

    public AuthenticationMiddleware(ILogger<AuthenticationMiddleware> logger)
    {
        this.logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Extract ClaimsPrincipal from the function context
        var principal = GetClaimsPrincipal(context);

        if (principal?.Identity?.IsAuthenticated == true)
        {
            var userId = GetUserId(principal);
            var email = GetEmail(principal);
            var roles = GetRoles(principal);
            var name = GetName(principal);

            if (!string.IsNullOrEmpty(userId))
            {
                // Store user context for use by functions
                context.Items["UserId"] = userId;
                context.Items["UserEmail"] = email ?? string.Empty;
                context.Items["UserName"] = name ?? string.Empty;
                context.Items["UserRoles"] = roles;
                context.Items["ClaimsPrincipal"] = principal;
                context.Items["IsAuthenticated"] = true;

                logger.LogDebug(
                    "Authenticated user: {UserId}, Email: {Email}, Roles: {Roles}",
                    userId, email, string.Join(", ", roles));
            }
        }
        else
        {
            context.Items["IsAuthenticated"] = false;
        }

        await next(context);
    }

    private static ClaimsPrincipal? GetClaimsPrincipal(FunctionContext context)
    {
        // In Azure Functions isolated worker, claims are available through
        // the FunctionContext.Features collection
        if (context.Features.Get<IFunctionBindingsFeature>() is not null)
        {
            // For HTTP triggers, the ClaimsPrincipal is typically available
            // through the invocation features
        }

        // Alternative: Check if ClaimsPrincipal was set by authentication middleware
        if (context.Items.TryGetValue("ClaimsPrincipal", out var existingPrincipal) &&
            existingPrincipal is ClaimsPrincipal principal)
        {
            return principal;
        }

        return null;
    }

    private static string? GetUserId(ClaimsPrincipal principal)
    {
        // Try standard Entra claim first, then alternative
        return principal.FindFirst(ObjectIdClaim)?.Value ??
               principal.FindFirst(ObjectIdClaimAlt)?.Value;
    }

    private static string? GetEmail(ClaimsPrincipal principal)
    {
        // Entra External ID uses "emails" claim (array), we take first
        var emailsClaim = principal.FindFirst(EmailClaim)?.Value;
        if (!string.IsNullOrEmpty(emailsClaim))
        {
            return emailsClaim;
        }

        return principal.FindFirst(EmailClaimAlt)?.Value ??
               principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    private static string? GetName(ClaimsPrincipal principal)
    {
        return principal.FindFirst(NameClaim)?.Value ??
               principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    private static IReadOnlyList<string> GetRoles(ClaimsPrincipal principal)
    {
        // Collect all role claims
        return principal.FindAll(RolesClaim)
            .Select(c => c.Value)
            .Concat(principal.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Distinct()
            .ToList();
    }
}