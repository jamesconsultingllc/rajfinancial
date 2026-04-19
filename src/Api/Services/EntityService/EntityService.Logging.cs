using Microsoft.Extensions.Logging;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Services.EntityService;

/// <summary>
///     Source-generated logging for <see cref="EntityService"/> (EventId 3000-3999).
/// </summary>
public partial class EntityService
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Entity {EntityId} ({EntityType} '{EntityName}') created for user {UserId}")]
    private partial void LogEntityCreated(Guid entityId, EntityType entityType, string entityName, Guid userId);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Entity {EntityId} updated by user {UserId}")]
    private partial void LogEntityUpdated(Guid entityId, Guid userId);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message = "Entity {EntityId} deleted by user {UserId}")]
    private partial void LogEntityDeleted(Guid entityId, Guid userId);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Information,
        Message = "Personal entity {EntityId} was concurrently provisioned for user {UserId}")]
    private partial void LogPersonalEntityConcurrentlyProvisioned(Guid entityId, Guid userId);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Information,
        Message = "Personal entity {EntityId} auto-provisioned for user {UserId}")]
    private partial void LogPersonalEntityProvisioned(Guid entityId, Guid userId);

    [LoggerMessage(
        EventId = 3010,
        Level = LogLevel.Information,
        Message = "Role {RoleId} ({RoleType}) assigned to entity {EntityId} by user {UserId}")]
    private partial void LogRoleAssigned(Guid roleId, EntityRoleType roleType, Guid entityId, Guid userId);

    [LoggerMessage(
        EventId = 3011,
        Level = LogLevel.Information,
        Message = "Role {RoleId} removed from entity {EntityId} by user {UserId}")]
    private partial void LogRoleRemoved(Guid roleId, Guid entityId, Guid userId);
}
