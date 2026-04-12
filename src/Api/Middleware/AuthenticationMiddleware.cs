using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

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
/// <b>Authentication Flow (priority order):</b>
/// <list type="number">
///   <item><c>FunctionContext.Items["ClaimsPrincipal"]</c> — explicit principal set by a prior middleware or test harness.</item>
///   <item><c>HttpContext.User</c> — principal populated by Azure App Service EasyAuth when using <c>ConfigureFunctionsWebApplication()</c>.</item>
///   <item>Authorization header JWT parse — Development environment only; allows local testing without EasyAuth.</item>
/// </list>
/// </para>
/// </remarks>
// ReSharper disable once ClassNeverInstantiated.Global
public class AuthenticationMiddleware(
    ILogger<AuthenticationMiddleware> logger,
    IHostEnvironment environment) : IFunctionsWorkerMiddleware
{
    // Standard claim types for Entra External ID
    private const string OBJECT_ID_CLAIM = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    private const string OBJECT_ID_CLAIM_ALT = "oid";
    private const string EMAIL_CLAIM = "emails";
    private const string EMAIL_CLAIM_ALT = "email";
    private const string PREFERRED_USERNAME_CLAIM = "preferred_username";
    private const string UPN_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
    private const string ROLES_CLAIM = "roles";
    private const string NAME_CLAIM = "name";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Extract ClaimsPrincipal from the function context
        var principal = await GetClaimsPrincipalAsync(context);

        if (principal?.Identity?.IsAuthenticated == true)
        {
            var userId = GetUserId(principal);
            var email = GetEmail(principal);
            var roles = GetRoles(principal);
            var name = GetName(principal);

            if (!string.IsNullOrEmpty(userId))
            {
                // Store user context for use by functions
                // UserId is stored as both string (backward compat) and Guid (for IAuthorizationService)
                context.Items["UserId"] = userId;
                if (Guid.TryParse(userId, out var userGuid))
                    context.Items["UserIdGuid"] = userGuid;
                context.Items["UserEmail"] = email ?? string.Empty;
                context.Items["UserName"] = name ?? string.Empty;
                context.Items["UserRoles"] = roles;
                context.Items["ClaimsPrincipal"] = principal;
                context.Items["IsAuthenticated"] = true;

                if (string.IsNullOrEmpty(email))
                {
                    var claimTypeNames = principal.Claims.Select(c => c.Type);
                    logger.LogWarning(
                        "No email claim found for user {UserId}. Available claim types: {ClaimTypes}",
                        userId, string.Join("; ", claimTypeNames));
                }

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

    private async Task<ClaimsPrincipal?> GetClaimsPrincipalAsync(FunctionContext context)
    {
        // Check if ClaimsPrincipal was already set (e.g. by Azure App Service EasyAuth)
        if (context.Items.TryGetValue("ClaimsPrincipal", out var existingPrincipal) &&
            existingPrincipal is ClaimsPrincipal principal)
        {
            return principal;
        }

        // Check HttpContext.User — populated by EasyAuth via ConfigureFunctionsWebApplication()
        var httpContext = context.GetHttpContext();
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return httpContext.User;
        }

        // Extract JWT from Authorization header for local development / standalone hosting
        var httpRequestData = await context.GetHttpRequestDataAsync();
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
            // SECURITY: Only parse JWTs without signature validation in Development.
            // In production, Azure App Service authentication (EasyAuth) sets the ClaimsPrincipal
            // directly — if we reach this point in production, the token was not validated by EasyAuth
            // and must be rejected to prevent forged JWT attacks.
            if (!environment.IsDevelopment())
            {
                logger.LogWarning(
                    "JWT token found in Authorization header but EasyAuth did not set ClaimsPrincipal. " +
                    "Rejecting unvalidated token in {Environment} environment",
                    environment.EnvironmentName);
                return null;
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var identity = new ClaimsIdentity(jwtToken.Claims, "Bearer");
            return new ClaimsPrincipal(identity);
        }
        catch (System.Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException)
        {
            logger.LogWarning(ex, "Failed to parse JWT token from Authorization header");
            return null;
        }
    }

    private static string? GetUserId(ClaimsPrincipal principal)
    {
        // Try standard Entra claim first, then alternative
        return principal.FindFirst(OBJECT_ID_CLAIM)?.Value ??
               principal.FindFirst(OBJECT_ID_CLAIM_ALT)?.Value;
    }

    private static string? GetEmail(ClaimsPrincipal principal)
    {
        // Entra External ID uses "emails" claim (array), we take first
        var emailsClaim = principal.FindFirst(EMAIL_CLAIM)?.Value;
        if (!string.IsNullOrEmpty(emailsClaim))
        {
            return emailsClaim;
        }

        // Standard email claims
        return principal.FindFirst(EMAIL_CLAIM_ALT)?.Value ??
               principal.FindFirst(ClaimTypes.Email)?.Value ??
               principal.FindFirst(PREFERRED_USERNAME_CLAIM)?.Value ??
               principal.FindFirst(UPN_CLAIM)?.Value ??
               principal.FindFirst(ClaimTypes.Upn)?.Value;
    }

    private static string? GetName(ClaimsPrincipal principal)
    {
        return principal.FindFirst(NAME_CLAIM)?.Value ??
               principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    private static IReadOnlyList<string> GetRoles(ClaimsPrincipal principal)
    {
        // Collect all role claims
        return principal.FindAll(ROLES_CLAIM)
            .Select(c => c.Value)
            .Concat(principal.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Distinct()
            .ToList();
    }
}