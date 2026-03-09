// ============================================================================
// RAJ Financial - Auth Functions
// ============================================================================
// Endpoints for retrieving the authenticated user's profile and role
// information. These are the primary identity endpoints consumed by the
// Blazor WASM client for UI-level access control.
// ============================================================================

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Authorization;
using RajFinancial.Api.Services.UserProfiles;
using RajFinancial.Shared.Contracts.Auth;

namespace RajFinancial.Api.Functions;

/// <summary>
/// Auth endpoints for user identity and role management.
/// </summary>
/// <remarks>
///     <para>
///         <b>Endpoints:</b>
///         <list type="bullet">
///             <item><c>GET /api/auth/me</c> – Returns the authenticated user's profile
///                 (JIT-provisioned via <see cref="IUserProfileService"/>).</item>
///             <item><c>GET /api/auth/roles</c> – Returns the authenticated user's role
///                 assignments from Entra claims (no database access).</item>
///         </list>
///     </para>
///     <para>
///         Unlike <c>/api/profile/me</c> (which returns the raw persisted entity),
///         <c>/api/auth/me</c> triggers JIT provisioning and returns a
///         <see cref="UserProfileResponse"/> contract shaped for client consumption.
///     </para>
/// </remarks>
public class AuthFunctions(
    ILogger<AuthFunctions> logger,
    IUserProfileService userProfileService)
{

    /// <summary>
    /// Returns the authenticated user's profile, triggering JIT provisioning
    /// for first-time users and syncing mutable claims for returning users.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="context">The Azure Functions invocation context.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item><b>200 OK</b> – <see cref="UserProfileResponse"/> JSON body.</item>
    ///         <item><b>401 Unauthorized</b> – No authenticated user context available
    ///             (body includes <c>AUTH_REQUIRED</c> error code).</item>
    ///     </list>
    /// </returns>
    [RequireAuthentication]
    [Function("AuthMe")]
    public async Task<HttpResponseData> GetMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userIdGuid = context.GetUserIdAsGuid();

        if (!userIdGuid.HasValue)
        {
            logger.LogWarning("AuthMe called without UserIdGuid in context");
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.Unauthorized,
                "AUTH_REQUIRED", "Authentication is required");
        }

        var email = context.GetUserEmail() ?? string.Empty;
        var displayName = context.GetUserName();
        var roles = context.GetUserRoles();

        // JIT provisioning: creates profile on first access, syncs claims on return
        var profile = await userProfileService.EnsureProfileExistsAsync(
            userIdGuid.Value,
            email,
            displayName,
            roles,
            cancellationToken: default);

        var responseDto = new UserProfileResponse
        {
            UserId = profile.Id.ToString(),
            Email = profile.Email,
            DisplayName = profile.DisplayName ?? string.Empty,
            Role = profile.Role.ToString(),
            IsProfileComplete = profile.IsProfileComplete,
            IsAdministrator = profile.Role == Shared.Entities.UserRole.Administrator
        };

        logger.LogInformation(
            "AuthMe returning profile for user {UserId}",
            userIdGuid.Value);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        // TODO: Align with ContentNegotiationMiddleware for MemoryPack support
        await response.WriteStringAsync(
            JsonSerializer.Serialize(responseDto, FunctionHelpers.JsonOptions));

        return response;
    }

    /// <summary>
    /// Returns the authenticated user's role assignments from Entra claims.
    /// </summary>
    /// <remarks>
    ///     This endpoint reads directly from the claims set by authentication
    ///     middleware — it does <b>not</b> trigger JIT provisioning or perform
    ///     any database access.
    /// </remarks>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="context">The Azure Functions invocation context.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item><b>200 OK</b> – <see cref="UserRolesResponse"/> JSON body.</item>
    ///         <item><b>401 Unauthorized</b> – No authenticated user context available
    ///             (body includes <c>AUTH_REQUIRED</c> error code).</item>
    ///     </list>
    /// </returns>
    [RequireAuthentication]
    [Function("AuthRoles")]
    public async Task<HttpResponseData> GetRoles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/roles")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userIdGuid = context.GetUserIdAsGuid();

        if (!userIdGuid.HasValue)
        {
            logger.LogWarning("AuthRoles called without UserIdGuid in context");
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.Unauthorized,
                "AUTH_REQUIRED", "Authentication is required");
        }

        var roles = context.GetUserRoles();
        var isAdministrator = context.IsAdministrator();

        var responseDto = new UserRolesResponse
        {
            Roles = roles.ToArray(),
            IsAdministrator = isAdministrator
        };

        logger.LogInformation(
            "AuthRoles returning {RoleCount} role(s) for user {UserId}",
            roles.Count,
            userIdGuid.Value);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        // TODO: Align with ContentNegotiationMiddleware for MemoryPack support
        await response.WriteStringAsync(
            JsonSerializer.Serialize(responseDto, FunctionHelpers.JsonOptions));

        return response;
    }
}
