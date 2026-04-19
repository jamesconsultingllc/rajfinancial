using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Authorization;
using RajFinancial.Api.Middleware.Content;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.EntityService;
using RajFinancial.Shared.Contracts.Entities;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Functions.Entities;

/// <summary>
///     Azure Function endpoints for entity (Personal/Business/Trust) CRUD operations
///     and role assignments.
/// </summary>
/// <remarks>
///     <para><b>Routes:</b></para>
///     <list type="bullet">
///         <item><c>GET    /api/entities</c>               — List entities (owner default or via ownerUserId)</item>
///         <item><c>GET    /api/entities/{id}</c>           — Get entity detail</item>
///         <item><c>POST   /api/entities</c>               — Create Business/Trust entity</item>
///         <item><c>PUT    /api/entities/{id}</c>           — Update entity</item>
///         <item><c>DELETE /api/entities/{id}</c>           — Delete entity (non-Personal)</item>
///         <item><c>GET    /api/entities/{id}/roles</c>     — List roles on entity</item>
///         <item><c>POST   /api/entities/{id}/roles</c>     — Assign role</item>
///         <item><c>DELETE /api/entities/{id}/roles/{roleId}</c> — Remove role</item>
///     </list>
/// </remarks>
public class EntityFunctions(
    IEntityService entityService,
    ISerializationFactory serializationFactory,
    ILogger<EntityFunctions> logger)
{
    // =========================================================================
    // GET /api/entities
    // =========================================================================

    [RequireAuthentication]
    [Function("GetEntities")]
    public async Task<HttpResponseData> GetEntities(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        var ownerUserId = ParseGuid(req, "ownerUserId") ?? userId;
        var filterType = ParseEntityType(req);

        logger.LogInformation(
            "Fetching entities for owner {OwnerUserId} (requested by {UserId}, type={FilterType})",
            ownerUserId, userId, filterType);

        var entities = await entityService.GetEntitiesAsync(userId, ownerUserId, filterType);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.OK, entities, serializationFactory);
    }

    // =========================================================================
    // GET /api/entities/{id}
    // =========================================================================

    [RequireAuthentication]
    [Function("GetEntityById")]
    public async Task<HttpResponseData> GetEntityById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities/{id}")]
        HttpRequestData req,
        FunctionContext context,
        string id)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        logger.LogInformation("Fetching entity {EntityId} for user {UserId}", entityId, userId);

        var entity = await entityService.GetEntityByIdAsync(userId, entityId)
                     ?? throw new NotFoundException(
                         EntityErrorCodes.NOT_FOUND,
                         $"Entity with ID {entityId} was not found.");

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.OK, entity, serializationFactory);
    }

    // =========================================================================
    // POST /api/entities
    // =========================================================================

    [RequireAuthentication]
    [Function("CreateEntity")]
    public async Task<HttpResponseData> CreateEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "entities")]
        HttpRequestData req,
        FunctionContext context)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        var request = await context.GetValidatedBodyAsync<CreateEntityRequest>();

        logger.LogInformation("Creating {Type} entity '{Name}' for user {UserId}",
            request.Type, request.Name, userId);

        var entity = await entityService.CreateEntityAsync(userId, request);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.Created, entity, serializationFactory);
    }

    // =========================================================================
    // PUT /api/entities/{id}
    // =========================================================================

    [RequireAuthentication]
    [Function("UpdateEntity")]
    public async Task<HttpResponseData> UpdateEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "entities/{id}")]
        HttpRequestData req,
        FunctionContext context,
        string id)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        var request = await context.GetValidatedBodyAsync<UpdateEntityRequest>();

        logger.LogInformation("Updating entity {EntityId} for user {UserId}", entityId, userId);

        var entity = await entityService.UpdateEntityAsync(userId, entityId, request);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.OK, entity, serializationFactory);
    }

    // =========================================================================
    // DELETE /api/entities/{id}
    // =========================================================================

    [RequireAuthentication]
    [Function("DeleteEntity")]
    public async Task<HttpResponseData> DeleteEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "entities/{id}")]
        HttpRequestData req,
        FunctionContext context,
        string id)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        logger.LogInformation("Deleting entity {EntityId} for user {UserId}", entityId, userId);

        await entityService.DeleteEntityAsync(userId, entityId);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    // =========================================================================
    // GET /api/entities/{id}/roles
    // =========================================================================

    [RequireAuthentication]
    [Function("GetEntityRoles")]
    public async Task<HttpResponseData> GetEntityRoles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities/{id}/roles")]
        HttpRequestData req,
        FunctionContext context,
        string id)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        var roles = await entityService.GetRolesAsync(userId, entityId);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.OK, roles, serializationFactory);
    }

    // =========================================================================
    // POST /api/entities/{id}/roles
    // =========================================================================

    [RequireAuthentication]
    [Function("AssignEntityRole")]
    public async Task<HttpResponseData> AssignEntityRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "entities/{id}/roles")]
        HttpRequestData req,
        FunctionContext context,
        string id)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        var request = await context.GetValidatedBodyAsync<CreateEntityRoleRequest>();

        logger.LogInformation("Assigning role {RoleType} on entity {EntityId} by user {UserId}",
            request.RoleType, entityId, userId);

        var role = await entityService.AssignRoleAsync(userId, entityId, request);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.Created, role, serializationFactory);
    }

    // =========================================================================
    // DELETE /api/entities/{id}/roles/{roleId}
    // =========================================================================

    [RequireAuthentication]
    [Function("RemoveEntityRole")]
    public async Task<HttpResponseData> RemoveEntityRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "entities/{id}/roles/{roleId}")]
        HttpRequestData req,
        FunctionContext context,
        string id,
        string roleId)
    {
        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        if (!Guid.TryParse(roleId, out var roleGuid))
            throw new ValidationException($"Invalid role ID format: '{roleId}'");

        logger.LogInformation("Removing role {RoleId} from entity {EntityId} by user {UserId}",
            roleGuid, entityId, userId);

        await entityService.RemoveRoleAsync(userId, entityId, roleGuid);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static Guid? ParseGuid(HttpRequestData req, string name)
    {
        var value = req.Query[name];
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static EntityType? ParseEntityType(HttpRequestData req)
    {
        var value = req.Query["type"];
        if (value is null) return null;

        if (int.TryParse(value, out var intValue) && Enum.IsDefined(typeof(EntityType), intValue))
            return (EntityType)intValue;

        if (Enum.TryParse<EntityType>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            return parsed;

        return null;
    }
}
