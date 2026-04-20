using RajFinancial.Shared.Contracts.Entities;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Services.EntityService;

/// <summary>
///     Pure mapping functions between EF entity types and wire DTOs for the Entity aggregate.
///     Exposed as extension methods so call sites read naturally (<c>entity.ToDto()</c>).
/// </summary>
internal static class EntityMapper
{
    public static EntityDto ToDto(this Entity entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        Name = entity.Name,
        Slug = entity.Slug,
        ParentEntityId = entity.ParentEntityId,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt.UtcDateTime,
        UpdatedAt = entity.UpdatedAt?.UtcDateTime,
        Business = entity.Type == EntityType.Business ? entity.Business : null,
        Trust = entity.Type == EntityType.Trust ? entity.Trust : null
    };

    public static EntityDetailDto ToDetailDto(this Entity entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        Name = entity.Name,
        Slug = entity.Slug,
        ParentEntityId = entity.ParentEntityId,
        StorageConnectionId = entity.StorageConnectionId,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt.UtcDateTime,
        UpdatedAt = entity.UpdatedAt?.UtcDateTime,
        Business = entity.Type == EntityType.Business ? entity.Business : null,
        Trust = entity.Type == EntityType.Trust ? entity.Trust : null,
        Roles = entity.Roles.Select(r => r.ToRoleDto()).ToArray(),
        ChildEntityCount = entity.ChildEntities.Count
    };

    public static EntityRoleDto ToRoleDto(this EntityRole role) => new()
    {
        Id = role.Id,
        EntityId = role.EntityId,
        ContactId = role.ContactId,
        RoleType = role.RoleType,
        Title = role.Title,
        OwnershipPercent = role.OwnershipPercent.HasValue ? (double)role.OwnershipPercent.Value : null,
        BeneficialInterestPercent = role.BeneficialInterestPercent.HasValue
            ? (double)role.BeneficialInterestPercent.Value
            : null,
        IsSignatory = role.IsSignatory,
        IsPrimary = role.IsPrimary,
        SortOrder = role.SortOrder,
        EffectiveDate = role.EffectiveDate?.UtcDateTime,
        EndDate = role.EndDate?.UtcDateTime,
        Notes = role.Notes
    };
}
