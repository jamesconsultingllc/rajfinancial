using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Middleware;

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
    [Function("AuthMe")]
    public async Task<HttpResponseData> GetMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")]
        HttpRequestData req,
        FunctionContext context)
    {
        if (!context.IsAuthenticated())
        {
            logger.LogWarning("Unauthenticated request to /api/auth/me");
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            unauthorizedResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await unauthorizedResponse.WriteStringAsync(
                """{"error":"Unauthorized","message":"Authentication required"}""");
            return unauthorizedResponse;
        }

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
    [Function("AuthClient")]
    public async Task<HttpResponseData> GetClientData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/client")]
        HttpRequestData req,
        FunctionContext context)
    {
        if (!context.IsAuthenticated())
        {
            logger.LogWarning("Unauthenticated request to /api/auth/client");
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            unauthorizedResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await unauthorizedResponse.WriteStringAsync(
                """{"error":"Unauthorized","message":"Authentication required"}""");
            return unauthorizedResponse;
        }

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
    [Function("AuthAdmin")]
    public async Task<HttpResponseData> GetAdminData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/admin")]
        HttpRequestData req,
        FunctionContext context)
    {
        if (!context.IsAuthenticated())
        {
            logger.LogWarning("Unauthenticated request to /api/auth/admin");
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            unauthorizedResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await unauthorizedResponse.WriteStringAsync(
                """{"error":"Unauthorized","message":"Authentication required"}""");
            return unauthorizedResponse;
        }

        // Check for Administrator role
        if (!context.HasRole("Administrator"))
        {
            var userId = context.GetUserId();
            logger.LogWarning("User {UserId} denied access to /api/auth/admin - missing Administrator role", userId);

            var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
            forbiddenResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await forbiddenResponse.WriteStringAsync(
                """{"error":"Forbidden","message":"Administrator role required"}""");
            return forbiddenResponse;
        }

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
