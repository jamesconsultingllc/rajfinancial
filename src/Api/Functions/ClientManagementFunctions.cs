// ============================================================================
// RAJ Financial — Client Management Functions
// ============================================================================
// Endpoints for managing client–advisor data-access grants. Allows Advisors
// and Administrators to assign, list, and remove client relationships.
//
// Endpoints:
//   POST   /api/auth/clients      — Assign a client to the current advisor
//   GET    /api/auth/clients      — List client assignments
//   DELETE /api/auth/clients/{id} — Remove a client assignment
// ============================================================================

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Authorization;
using RajFinancial.Api.Services.ClientManagement;
using RajFinancial.Shared.Contracts.Auth;
using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Functions;

/// <summary>
/// Client management endpoints for advisor–client data-access grants.
/// </summary>
/// <remarks>
///     <para>
///         <b>Endpoints:</b>
///         <list type="bullet">
///             <item><c>POST /api/auth/clients</c> — Creates a new
///                 <see cref="DataAccessGrant"/> linking the calling advisor
///                 to a client email. Self-assignment is rejected.</item>
///             <item><c>GET /api/auth/clients</c> — Returns the caller's
///                 assignments. Administrators see all assignments across
///                 advisors.</item>
///             <item><c>DELETE /api/auth/clients/{id}</c> — Soft-deletes an
///                 assignment. Advisors may only remove their own grants;
///                 Administrators may remove any grant.</item>
///         </list>
///     </para>
///     <para>
///         Access is restricted to users with the <c>Advisor</c> or
///         <c>Administrator</c> role via <see cref="RequireRoleAttribute"/>.
///     </para>
/// </remarks>
[RequireRole("Advisor", "Administrator")]
public class ClientManagementFunctions(
    ILogger<ClientManagementFunctions> logger,
    IClientManagementService clientManagementService)
{

    /// <summary>
    /// Assigns a client to the calling advisor by creating a new
    /// <see cref="DataAccessGrant"/> in <c>Pending</c> status.
    /// </summary>
    /// <param name="req">The incoming HTTP request containing an
    /// <see cref="AssignClientRequest"/> JSON body.</param>
    /// <param name="context">The Azure Functions invocation context.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item><b>201 Created</b> — <see cref="ClientAssignmentResponse"/>
    ///             with the new grant details.</item>
    ///         <item><b>400 Bad Request</b> — Self-assignment attempted
    ///             (<c>SELF_ASSIGNMENT_NOT_ALLOWED</c>).</item>
    ///         <item><b>401 Unauthorized</b> — No authenticated user context
    ///             (<c>AUTH_REQUIRED</c>).</item>
    ///         <item><b>403 Forbidden</b> — Caller does not have the Advisor
    ///             or Administrator role (<c>AUTH_FORBIDDEN</c>).</item>
    ///     </list>
    /// </returns>
    [Function("AssignClient")]
    public async Task<HttpResponseData> AssignClient(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/clients")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userIdGuid = context.GetUserIdAsGuid();

        if (!userIdGuid.HasValue)
        {
            logger.LogWarning("AssignClient called without UserIdGuid in context");
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.Unauthorized,
                "AUTH_REQUIRED", "Authentication is required");
        }

        // Defense-in-depth: [RequireRole] on the class is enforced by
        // AuthorizationMiddleware in production, but middleware is bypassed
        // in unit tests. This inline check provides a safety net and
        // keeps the 403 response testable in isolation.
        if (!context.HasRole("Advisor") && !context.HasRole("Administrator"))
        {
            logger.LogWarning(
                "AssignClient forbidden for user {UserId} — missing Advisor/Administrator role",
                userIdGuid.Value);
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.Forbidden,
                "AUTH_FORBIDDEN", "Insufficient permissions");
        }

        var assignRequest = await context.GetValidatedBodyAsync<AssignClientRequest>();

        // Self-assignment check (case-insensitive email comparison)
        var userEmail = context.GetUserEmail();
        if (string.Equals(userEmail, assignRequest.ClientEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "AssignClient self-assignment rejected for user {UserId}",
                userIdGuid.Value);
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.BadRequest,
                "SELF_ASSIGNMENT_NOT_ALLOWED",
                "Cannot assign yourself as a client");
        }

        var grant = await clientManagementService.AssignClientAsync(
            userIdGuid.Value, assignRequest, default);

        var responseDto = MapToResponse(grant);

        logger.LogInformation(
            "Client assigned: Grant {GrantId} from {UserId}",
            grant.Id, userIdGuid.Value);

        var response = req.CreateResponse(HttpStatusCode.Created);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        // TODO: Align with ContentNegotiationMiddleware for MemoryPack support
        await response.WriteStringAsync(
            JsonSerializer.Serialize(responseDto, FunctionHelpers.JsonOptions));

        return response;
    }

    /// <summary>
    /// Returns the caller's client assignments. Administrators receive all
    /// assignments across all advisors.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="context">The Azure Functions invocation context.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item><b>200 OK</b> — JSON array of
    ///             <see cref="ClientAssignmentResponse"/> objects.</item>
    ///         <item><b>401 Unauthorized</b> — No authenticated user context
    ///             (<c>AUTH_REQUIRED</c>).</item>
    ///         <item><b>403 Forbidden</b> — Caller does not have the Advisor
    ///             or Administrator role (<c>AUTH_FORBIDDEN</c>).</item>
    ///     </list>
    /// </returns>
    [Function("GetClients")]
    public async Task<HttpResponseData> GetClients(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/clients")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userIdGuid = context.GetUserIdAsGuid();

        if (!userIdGuid.HasValue)
        {
            logger.LogWarning("GetClients called without UserIdGuid in context");
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.Unauthorized,
                "AUTH_REQUIRED", "Authentication is required");
        }

        // Defense-in-depth: [RequireRole] on the class is enforced by
        // AuthorizationMiddleware in production, but middleware is bypassed
        // in unit tests. This inline check provides a safety net and
        // keeps the 403 response testable in isolation.
        if (!context.HasRole("Advisor") && !context.HasRole("Administrator"))
        {
            logger.LogWarning(
                "GetClients forbidden for user {UserId} — missing Advisor/Administrator role",
                userIdGuid.Value);
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.Forbidden,
                "AUTH_FORBIDDEN", "Insufficient permissions");
        }

        var isAdmin = context.IsAdministrator();

        var grants = await clientManagementService.GetClientAssignmentsAsync(
            userIdGuid.Value, isAdmin, default);

        var responseDtos = grants.Select(MapToResponse).ToArray();

        logger.LogInformation(
            "GetClients returning {Count} assignment(s) for user {UserId} (admin={IsAdmin})",
            responseDtos.Length, userIdGuid.Value, isAdmin);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        // TODO: Align with ContentNegotiationMiddleware for MemoryPack support
        await response.WriteStringAsync(
            JsonSerializer.Serialize(responseDtos, FunctionHelpers.JsonOptions));

        return response;
    }

    /// <summary>
    /// Removes (soft-deletes) a client assignment by grant ID.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Advisors may only remove grants they created (ownership check).
    ///         Administrators may remove any grant regardless of ownership.
    ///     </para>
    /// </remarks>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="id">The grant ID from the route (<c>/api/auth/clients/{id}</c>).</param>
    /// <param name="context">The Azure Functions invocation context.</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item><b>204 No Content</b> — Grant successfully removed.</item>
    ///         <item><b>400 Bad Request</b> — Invalid GUID format
    ///             (<c>VALIDATION_FAILED</c>).</item>
    ///         <item><b>401 Unauthorized</b> — No authenticated user context
    ///             (<c>AUTH_REQUIRED</c>).</item>
    ///         <item><b>403 Forbidden</b> — Caller does not own the grant and
    ///             is not an Administrator (<c>AUTH_FORBIDDEN</c>).</item>
    ///         <item><b>404 Not Found</b> — Grant does not exist
    ///             (<c>RESOURCE_NOT_FOUND</c>).</item>
    ///     </list>
    /// </returns>
    [Function("RemoveClient")]
    public async Task<HttpResponseData> RemoveClient(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "auth/clients/{id}")]
        HttpRequestData req,
        string id,
        FunctionContext context)
    {
        var userIdGuid = context.GetUserIdAsGuid();

        if (!userIdGuid.HasValue)
        {
            logger.LogWarning("RemoveClient called without UserIdGuid in context");
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.Unauthorized,
                "AUTH_REQUIRED", "Authentication is required");
        }

        // Defense-in-depth: [RequireRole] on the class is enforced by
        // AuthorizationMiddleware in production, but middleware is bypassed
        // in unit tests. This inline check provides a safety net and
        // keeps the 403 response testable in isolation.
        if (!context.HasRole("Advisor") && !context.HasRole("Administrator"))
        {
            logger.LogWarning(
                "RemoveClient forbidden for user {UserId} — missing Advisor/Administrator role",
                userIdGuid.Value);
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.Forbidden,
                "AUTH_FORBIDDEN", "Insufficient permissions");
        }

        if (!Guid.TryParse(id, out var grantId))
        {
            logger.LogWarning("RemoveClient received invalid GUID: {Id}", id);
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.BadRequest,
                "VALIDATION_FAILED", "Invalid grant ID format");
        }

        var grant = await clientManagementService.GetGrantByIdAsync(grantId, default);

        if (grant is null)
        {
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.NotFound,
                "RESOURCE_NOT_FOUND", "Client assignment not found");
        }

        // Ownership check: only the grantor or an administrator may remove
        if (grant.GrantorUserId != userIdGuid.Value && !context.IsAdministrator())
        {
            logger.LogWarning(
                "RemoveClient ownership denied: user {UserId} attempted to remove grant {GrantId} owned by {GrantorId}",
                userIdGuid.Value, grantId, grant.GrantorUserId);
            return await FunctionHelpers.WriteErrorResponse(req, HttpStatusCode.Forbidden,
                "AUTH_FORBIDDEN",
                "You do not have permission to remove this assignment");
        }

        await clientManagementService.RemoveClientAccessAsync(grantId, default);

        logger.LogInformation(
            "Client assignment removed: Grant {GrantId} by user {UserId}",
            grantId, userIdGuid.Value);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Maps a <see cref="DataAccessGrant"/> entity to a
    /// <see cref="ClientAssignmentResponse"/> DTO.
    /// </summary>
    private static ClientAssignmentResponse MapToResponse(DataAccessGrant grant) => new()
    {
        GrantId = grant.Id,
        ClientEmail = grant.GranteeEmail,
        AccessType = grant.AccessType.ToString(),
        Categories = grant.Categories.ToArray(),
        RelationshipLabel = grant.RelationshipLabel,
        Status = grant.Status.ToString(),
        CreatedAt = grant.CreatedAt.UtcDateTime
    };
}
