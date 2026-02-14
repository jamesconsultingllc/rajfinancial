using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Authorization;

namespace RajFinancial.Api.Functions;

/// <summary>
///     Test endpoints for validating authentication and role-based access control.
///     These endpoints are useful for verifying Entra External ID integration.
/// </summary>
/// <remarks>
///     <para>
///         <b>Endpoints:</b>
///         <list type="bullet">
///             <item><c>GET /api/auth/me</c> - Returns current user info (requires authentication)</item>
///             <item><c>GET /api/auth/client</c> - Accessible by any authenticated user (implicit Client role)</item>
///             <item><c>GET /api/auth/admin</c> - Requires explicit Administrator role</item>
///             <item><c>GET /api/auth/public</c> - No authentication required</item>
///         </list>
///     </para>
/// </remarks>
public class AuthTestFunctions(
    ILogger<AuthTestFunctions> logger,
    IOptions<AppRoleOptions> appRoleOptions)
{
    private readonly AppRoleOptions appRoles = appRoleOptions.Value;

    /// <summary>
    ///     Returns information about the currently authenticated user.
    ///     Requires authentication but no specific role.
    /// </summary>
    [RequireAuthentication]
    [Function("AuthMe")]
    public async Task<HttpResponseData> GetMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userId = context.GetUserId();
        var email = context.GetUserEmail();
        var name = context.GetUserName();
        var roles = context.GetUserRoles();

        logger.LogInformation("User {UserId} accessed /api/auth/me", userId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        var rolesJson = string.Join(",", roles.Select(r => $"\"{r}\""));
        await response.WriteStringAsync($$"""
            {
                "authenticated": true,
                "userId": "{{userId}}",
                "email": "{{email}}",
                "name": "{{name}}",
                "roles": [{{rolesJson}}],
                "isAdministrator": {{(context.HasRole("Administrator") ? "true" : "false")}},
                "timestamp": "{{DateTime.UtcNow:O}}"
            }
            """);

        return response;
    }

    /// <summary>
    ///     Endpoint accessible by any authenticated user.
    ///     Demonstrates implicit Client role - all authenticated users are Clients.
    /// </summary>
    [RequireAuthentication]
    [Function("AuthClient")]
    public async Task<HttpResponseData> GetClientData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/client")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userId = context.GetUserId();
        logger.LogInformation("Client {UserId} accessed /api/auth/client", userId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync($$"""
            {
                "message": "Welcome, Client!",
                "access": "client",
                "userId": "{{userId}}",
                "description": "This endpoint is accessible by any authenticated user (implicit Client role)",
                "timestamp": "{{DateTime.UtcNow:O}}"
            }
            """);

        return response;
    }

    /// <summary>
    ///     Endpoint restricted to users with explicit Administrator role.
    ///     Demonstrates role-based access control.
    /// </summary>
    [RequireRole("Administrator")]
    [Function("AuthAdmin")]
    public async Task<HttpResponseData> GetAdminData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/admin")]
        HttpRequestData req,
        FunctionContext context)
    {
        var adminUserId = context.GetUserId();
        logger.LogInformation("Administrator {UserId} accessed /api/auth/admin", adminUserId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync($$"""
            {
                "message": "Welcome, Administrator!",
                "access": "administrator",
                "userId": "{{adminUserId}}",
                "description": "This endpoint requires explicit Administrator role assignment",
                "adminRoleId": "{{appRoles.Administrator}}",
                "timestamp": "{{DateTime.UtcNow:O}}"
            }
            """);

        return response;
    }

    /// <summary>
    ///     Public endpoint that does not require authentication.
    ///     Useful for verifying the API is reachable.
    /// </summary>
    [Function("AuthPublic")]
    public async Task<HttpResponseData> GetPublicData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/public")]
        HttpRequestData req,
        FunctionContext context)
    {
        var isAuthenticated = context.IsAuthenticated();
        var userId = isAuthenticated ? context.GetUserId() : null;

        logger.LogInformation("Public endpoint accessed. Authenticated: {IsAuthenticated}", isAuthenticated);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync($$"""
            {
                "message": "This is a public endpoint",
                "access": "public",
                "authenticated": {{(isAuthenticated ? "true" : "false")}},
                "userId": {{(userId != null ? $"\"{userId}\"" : "null")}},
                "description": "No authentication required to access this endpoint",
                "timestamp": "{{DateTime.UtcNow:O}}"
            }
            """);

        return response;
    }
}
