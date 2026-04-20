using System.Diagnostics;
using System.Diagnostics.Metrics;
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
public partial class EntityFunctions(
    IEntityService entityService,
    ISerializationFactory serializationFactory,
    ILogger<EntityFunctions> logger)
{
    private static readonly ActivitySource ActivitySource = new("RajFinancial.Api.Entities");
    private static readonly Meter Meter = new("RajFinancial.Api.Entities");

    private static readonly Counter<long> EntitiesCreated =
        Meter.CreateCounter<long>("entities.created.count");

    private static readonly Counter<long> EntityRolesAssigned =
        Meter.CreateCounter<long>("entities.roles.assigned.count");

    private static readonly Histogram<double> EntitiesQueryDuration =
        Meter.CreateHistogram<double>("entities.query.duration.ms");

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
        using var activity = ActivitySource.StartActivity("Entities.GetEntities");
        var stopwatch = Stopwatch.StartNew();

        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        var ownerUserId = ParseGuid(req, "ownerUserId") ?? userId;
        var filterType = ParseEntityType(req);

        activity?.SetTag("user.id", userId);
        activity?.SetTag("entity.owner.id", ownerUserId);
        if (filterType.HasValue)
            activity?.SetTag("entity.type", filterType.Value.ToString());

        LogFetchingEntities(ownerUserId, userId, filterType);

        var entities = await entityService.GetEntitiesAsync(userId, ownerUserId, filterType);

        stopwatch.Stop();
        EntitiesQueryDuration.Record(
            stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("entities.query.op", "list"));

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
        using var activity = ActivitySource.StartActivity("Entities.GetEntityById");
        var stopwatch = Stopwatch.StartNew();

        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        activity?.SetTag("user.id", userId);
        activity?.SetTag("entity.id", entityId);

        LogFetchingEntityById(entityId, userId);

        var entity = await entityService.GetEntityByIdAsync(userId, entityId)
                     ?? throw new NotFoundException(
                         EntityErrorCodes.NotFound,
                         $"Entity with ID {entityId} was not found.");

        activity?.SetTag("entity.type", entity.Type.ToString());
        activity?.SetTag("entity.slug", entity.Slug);

        stopwatch.Stop();
        EntitiesQueryDuration.Record(
            stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("entities.query.op", "get"));

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
        using var activity = ActivitySource.StartActivity("Entities.CreateEntity");

        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        var request = await context.GetValidatedBodyAsync<CreateEntityRequest>();

        activity?.SetTag("user.id", userId);
        if (request.Type.HasValue)
            activity?.SetTag("entity.type", request.Type.Value.ToString());

        LogCreatingEntity(request.Type, request.Name, userId);

        var entity = await entityService.CreateEntityAsync(userId, request);

        activity?.SetTag("entity.id", entity.Id);
        activity?.SetTag("entity.slug", entity.Slug);

        EntitiesCreated.Add(
            1,
            new KeyValuePair<string, object?>("entity.type", entity.Type.ToString()));

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
        using var activity = ActivitySource.StartActivity("Entities.UpdateEntity");

        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        var request = await context.GetValidatedBodyAsync<UpdateEntityRequest>();

        activity?.SetTag("user.id", userId);
        activity?.SetTag("entity.id", entityId);

        LogUpdatingEntity(entityId, userId);

        var entity = await entityService.UpdateEntityAsync(userId, entityId, request);

        activity?.SetTag("entity.type", entity.Type.ToString());
        activity?.SetTag("entity.slug", entity.Slug);

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
        using var activity = ActivitySource.StartActivity("Entities.DeleteEntity");

        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        activity?.SetTag("user.id", userId);
        activity?.SetTag("entity.id", entityId);

        LogDeletingEntity(entityId, userId);

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
        using var activity = ActivitySource.StartActivity("Entities.GetEntityRoles");
        var stopwatch = Stopwatch.StartNew();

        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        activity?.SetTag("user.id", userId);
        activity?.SetTag("entity.id", entityId);

        var roles = await entityService.GetRolesAsync(userId, entityId);

        stopwatch.Stop();
        EntitiesQueryDuration.Record(
            stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("entities.query.op", "list-roles"));

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
        using var activity = ActivitySource.StartActivity("Entities.AssignEntityRole");

        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        var request = await context.GetValidatedBodyAsync<CreateEntityRoleRequest>();

        activity?.SetTag("user.id", userId);
        activity?.SetTag("entity.id", entityId);
        if (request.RoleType.HasValue)
            activity?.SetTag("entity.role.type", request.RoleType.Value.ToString());

        LogAssigningRole(request.RoleType, entityId, userId);

        var role = await entityService.AssignRoleAsync(userId, entityId, request);

        activity?.SetTag("entity.role.id", role.Id);

        EntityRolesAssigned.Add(
            1,
            new KeyValuePair<string, object?>("entity.role.type", role.RoleType.ToString()));

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
        using var activity = ActivitySource.StartActivity("Entities.RemoveEntityRole");

        var userId = context.GetUserIdAsGuid()
                     ?? throw new UnauthorizedException("User ID not found");

        if (!Guid.TryParse(id, out var entityId))
            throw new ValidationException($"Invalid entity ID format: '{id}'");

        if (!Guid.TryParse(roleId, out var roleGuid))
            throw new ValidationException($"Invalid role ID format: '{roleId}'");

        activity?.SetTag("user.id", userId);
        activity?.SetTag("entity.id", entityId);
        activity?.SetTag("entity.role.id", roleGuid);

        LogRemovingRole(roleGuid, entityId, userId);

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

    [LoggerMessage(EventId = 3101, Level = LogLevel.Information,
        Message = "Fetching entities for owner {OwnerUserId} (requested by {UserId}, type={FilterType})")]
    private partial void LogFetchingEntities(Guid ownerUserId, Guid userId, EntityType? filterType);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Information, Message = "Fetching entity {EntityId} for user {UserId}")]
    private partial void LogFetchingEntityById(Guid entityId, Guid userId);

    [LoggerMessage(EventId = 3103, Level = LogLevel.Information, Message = "Creating {Type} entity '{Name}' for user {UserId}")]
    private partial void LogCreatingEntity(EntityType? type, string name, Guid userId);

    [LoggerMessage(EventId = 3104, Level = LogLevel.Information, Message = "Updating entity {EntityId} for user {UserId}")]
    private partial void LogUpdatingEntity(Guid entityId, Guid userId);

    [LoggerMessage(EventId = 3105, Level = LogLevel.Information, Message = "Deleting entity {EntityId} for user {UserId}")]
    private partial void LogDeletingEntity(Guid entityId, Guid userId);

    [LoggerMessage(EventId = 3106, Level = LogLevel.Information, Message = "Assigning role {RoleType} on entity {EntityId} by user {UserId}")]
    private partial void LogAssigningRole(EntityRoleType? roleType, Guid entityId, Guid userId);

    [LoggerMessage(EventId = 3107, Level = LogLevel.Information, Message = "Removing role {RoleId} from entity {EntityId} by user {UserId}")]
    private partial void LogRemovingRole(Guid roleId, Guid entityId, Guid userId);
}
