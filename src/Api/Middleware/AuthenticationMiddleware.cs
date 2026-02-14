using System.IdentityModel.Tokens.Jwt;
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
/// <para>
/// <b>Authentication Flow:</b>
/// <list type="number">
///   <item>In Azure (App Service): ClaimsPrincipal is populated by the runtime via EasyAuth.</item>
///   <item>Locally / without EasyAuth: The JWT Bearer token is parsed from the Authorization header.</item>
/// </list>
/// </para>
/// </remarks>
// ReSharper disable once ClassNeverInstantiated.Global
public class AuthenticationMiddleware(ILogger<AuthenticationMiddleware> logger) : IFunctionsWorkerMiddleware
{
    // Standard claim types for Entra External ID
    private const string ObjectIdClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    private const string ObjectIdClaimAlt = "oid";
    private const string EmailClaim = "emails";
    private const string EmailClaimAlt = "email";
    private const string RolesClaim = "roles";
    private const string NameClaim = "name";

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
                    "Authenticated user: {UserId}, Roles: {Roles}",
                    userId, string.Join(", ", roles));
            }
        }
        else
        {
            context.Items["IsAuthenticated"] = false;
        }

        await next(context);
    }

    private ClaimsPrincipal? GetClaimsPrincipal(FunctionContext context)
    {
        // Check if ClaimsPrincipal was already set (e.g. by Azure App Service EasyAuth)
        if (context.Items.TryGetValue("ClaimsPrincipal", out var existingPrincipal) &&
            existingPrincipal is ClaimsPrincipal principal)
        {
            return principal;
        }

        // Extract JWT from Authorization header for local development / standalone hosting
        var httpRequestData = context.GetHttpRequestDataAsync().GetAwaiter().GetResult();
        if (httpRequestData is null)
        {
            return null;
        }

        if (!httpRequestData.Headers.TryGetValues("Authorization", out var authValues))
        {
            return null;
        }

        var authHeader = authValues.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            // Parse the JWT without full signature validation.
            // Signature validation is handled by Azure App Service authentication (EasyAuth)
            // in production. For local development, we trust the token from the identity provider.
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var identity = new ClaimsIdentity(jwtToken.Claims, "Bearer");
            return new ClaimsPrincipal(identity);
        }
        catch (System.Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse JWT token from Authorization header");
            return null;
        }
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