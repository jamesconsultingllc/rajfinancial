using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Api.Services.Contacts;
using RajFinancial.Shared.Contracts.Entities;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Services.EntityService;

/// <summary>
///     EF Core implementation of <see cref="IEntityService"/>.
/// </summary>
public partial class EntityService(
    ApplicationDbContext db,
    IAuthorizationService authorizationService,
    IContactResolver contactResolver,
    ILogger<EntityService> logger) : IEntityService
{
    // =========================================================================
    // Entity queries
    // =========================================================================

    public async Task<IReadOnlyList<EntityDto>> GetEntitiesAsync(
        Guid requestingUserId,
        Guid ownerUserId,
        EntityType? filterType = null)
    {
        await AuthorizeReadAsync(requestingUserId, ownerUserId);

        var query = db.Entities
            .AsNoTracking()
            .Where(e => e.UserId == ownerUserId);

        if (filterType.HasValue)
            query = query.Where(e => e.Type == filterType.Value);

        var entities = await query
            .OrderBy(e => e.Type)
            .ThenBy(e => e.Name)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    public async Task<EntityDetailDto?> GetEntityByIdAsync(Guid requestingUserId, Guid entityId)
    {
        var ownerId = await db.Entities
            .AsNoTracking()
            .Where(e => e.Id == entityId)
            .Select(e => (Guid?)e.UserId)
            .FirstOrDefaultAsync()
            ?? throw NotFound(entityId);

        await AuthorizeReadAsync(requestingUserId, ownerId);

        var entity = await db.Entities
            .AsNoTracking()
            .Include(e => e.Roles)
            .Include(e => e.ChildEntities)
            .FirstOrDefaultAsync(e => e.Id == entityId)
            ?? throw NotFound(entityId);

        return MapToDetailDto(entity);
    }

    // =========================================================================
    // Entity mutations
    // =========================================================================

    public async Task<EntityDto> CreateEntityAsync(Guid userId, CreateEntityRequest request)
    {
        await AuthorizeWriteAsync(userId, userId);

        // Validator guarantees Type.HasValue; treat absence as an internal bug.
        var requestedType = request.Type
            ?? throw new InvalidOperationException(
                "CreateEntityRequest.Type was null despite validation; validator likely misconfigured.");

        await ValidatePersonalUniquenessAsync(userId, requestedType);
        await ValidateParentEntityAsync(userId, request.ParentEntityId);

        var slug = await ResolveUniqueSlugAsync(userId, request.Name, request.Slug);

        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = requestedType,
            Name = request.Name,
            Slug = slug,
            ParentEntityId = request.ParentEntityId,
            StorageConnectionId = request.StorageConnectionId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Business = requestedType == EntityType.Business ? request.Business : null,
            Trust = requestedType == EntityType.Trust ? request.Trust : null
        };

        db.Entities.Add(entity);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Concurrent caller inserted the same (UserId, Slug) between the
            // AnyAsync pre-check and SaveChangesAsync. Translate the SQL-level
            // unique-index violation into the domain 409 ENTITY_SLUG_DUPLICATE
            // rather than letting a raw 500 surface.
            db.Entities.Remove(entity);
            throw new ConflictException(
                EntityErrorCodes.SlugDuplicate,
                $"Slug '{slug}' is already in use.");
        }

        LogEntityCreated(entity.Id, entity.Type, entity.Name, userId);

        return MapToDto(entity);
    }

    private async Task ValidatePersonalUniquenessAsync(Guid userId, EntityType requestedType)
    {
        if (requestedType != EntityType.Personal)
            return;

        var personalExists = await db.Entities
            .AnyAsync(e => e.UserId == userId && e.Type == EntityType.Personal);

        if (personalExists)
            throw new ConflictException(
                EntityErrorCodes.PersonalAlreadyExists,
                "User already has a Personal entity.");
    }

    private async Task ValidateParentEntityAsync(Guid userId, Guid? parentEntityId)
    {
        if (!parentEntityId.HasValue)
            return;

        var parent = await db.Entities
            .FirstOrDefaultAsync(e => e.Id == parentEntityId.Value)
            ?? throw new BusinessRuleException(
                EntityErrorCodes.ParentNotFound,
                $"Parent entity {parentEntityId.Value} was not found.");

        if (parent.UserId != userId)
            throw new BusinessRuleException(
                EntityErrorCodes.ParentNotFound,
                $"Parent entity {parentEntityId.Value} was not found.");
    }

    private async Task<string> ResolveUniqueSlugAsync(Guid userId, string name, string? explicitSlug)
    {
        var slug = !string.IsNullOrWhiteSpace(explicitSlug)
            ? NormalizeSlug(explicitSlug)
            : GenerateSlug(name);

        if (string.IsNullOrWhiteSpace(slug))
            throw new BusinessRuleException(
                EntityErrorCodes.SlugInvalid,
                "Slug could not be generated from the provided name. Supply an explicit ASCII slug.");

        var slugTaken = await db.Entities
            .AnyAsync(e => e.UserId == userId && e.Slug == slug);

        if (slugTaken)
            throw new ConflictException(
                EntityErrorCodes.SlugDuplicate,
                $"Slug '{slug}' is already in use.");

        return slug;
    }

    public async Task<EntityDto> UpdateEntityAsync(
        Guid requestingUserId,
        Guid entityId,
        UpdateEntityRequest request)
    {
        var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == entityId)
                     ?? throw NotFound(entityId);

        await AuthorizeWriteAsync(requestingUserId, entity.UserId);

        if (entity.Type == EntityType.Personal && !string.Equals(entity.Name, request.Name, StringComparison.Ordinal))
            throw new BusinessRuleException(
                EntityErrorCodes.PersonalNameImmutable,
                "Personal entity name cannot be changed.");

        entity.Name = request.Name;
        if (request.IsActive.HasValue)
            entity.IsActive = request.IsActive.Value;
        // Partial-update semantics: only overwrite StorageConnectionId when the caller
        // supplied a non-null value. Clients sending { name: "..." } must not
        // inadvertently detach an existing storage connection. A dedicated
        // "clear storage connection" mechanism can be added if that use case arises.
        if (request.StorageConnectionId.HasValue)
            entity.StorageConnectionId = request.StorageConnectionId.Value;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        switch (entity.Type)
        {
            case EntityType.Business when request.Business is not null:
                entity.Business = request.Business;
                break;
            case EntityType.Trust when request.Trust is not null:
                entity.Trust = request.Trust;
                break;
        }

        await db.SaveChangesAsync();

        LogEntityUpdated(entity.Id, requestingUserId);

        return MapToDto(entity);
    }

    public async Task DeleteEntityAsync(Guid requestingUserId, Guid entityId)
    {
        var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == entityId)
                     ?? throw NotFound(entityId);

        await AuthorizeWriteAsync(requestingUserId, entity.UserId);

        if (entity.Type == EntityType.Personal)
            throw new BusinessRuleException(
                EntityErrorCodes.PersonalCannotDelete,
                "Personal entities cannot be deleted.");

        var hasChildren = await db.Entities.AnyAsync(e => e.ParentEntityId == entityId);
        if (hasChildren)
            throw new BusinessRuleException(
                EntityErrorCodes.HasChildrenCannotDelete,
                "Cannot delete an entity that has child entities.");

        var roles = await db.EntityRoles.Where(r => r.EntityId == entityId).ToListAsync();
        if (roles.Count > 0)
            db.EntityRoles.RemoveRange(roles);

        db.Entities.Remove(entity);
        await db.SaveChangesAsync();

        LogEntityDeleted(entityId, requestingUserId);
    }

    public async Task<EntityDto> EnsurePersonalEntityAsync(Guid userId)
    {
        var existing = await db.Entities
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Type == EntityType.Personal);

        if (existing is not null)
            return MapToDto(existing);

        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = EntityType.Personal,
            Name = "Personal",
            Slug = "personal",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Entities.Add(entity);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Concurrent first-auth requests for the same brand-new user can both
            // pass the "no existing Personal" check and race to insert. The unique
            // (UserId, Slug) index raises SqlException 2601/2627 — treat that as
            // "someone else just provisioned it" and return the winner instead of
            // bubbling a 500.
            db.Entities.Remove(entity);
            var winner = await db.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == userId && e.Type == EntityType.Personal)
                ?? throw new InvalidOperationException(
                    "Unique constraint violation on Personal entity insert, but no " +
                    "Personal entity exists for the user. Database state is inconsistent.");

            LogPersonalEntityConcurrentlyProvisioned(winner.Id, userId);

            return MapToDto(winner);
        }

        LogPersonalEntityProvisioned(entity.Id, userId);

        return MapToDto(entity);
    }

    // =========================================================================
    // Role operations
    // =========================================================================

    public async Task<EntityRoleDto> AssignRoleAsync(
        Guid requestingUserId,
        Guid entityId,
        CreateEntityRoleRequest request)
    {
        var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == entityId)
                     ?? throw NotFound(entityId);

        await AuthorizeWriteAsync(requestingUserId, entity.UserId);

        // Validator guarantees RoleType.HasValue; treat absence as an internal bug.
        var roleType = request.RoleType
            ?? throw new InvalidOperationException(
                "CreateEntityRoleRequest.RoleType was null despite validation; " +
                "validator likely misconfigured.");

        // Phase 1: there is no Contacts table yet. The production-default
        // IContactResolver rejects every GUID (PlaceholderContactResolver),
        // preventing cross-tenant linking via arbitrary client-supplied ids.
        // Integration tests swap in SeedableContactResolver via the
        // ENABLE_CONTACT_TEST_SEEDING env flag.
        await contactResolver.EnsureOwnedByAsync(request.ContactId, entity.UserId);

        ValidateRoleCompatibility(roleType, entity.Type, request);
        await ValidateOwnershipAsync(entityId, roleType, request.OwnershipPercent);
        await ValidateBeneficialInterestAsync(entityId, roleType, request.BeneficialInterestPercent);
        ValidateRoleDateRange(request.EffectiveDate, request.EndDate);

        var role = BuildEntityRole(entityId, roleType, request);

        db.EntityRoles.Add(role);
        await db.SaveChangesAsync();

        LogRoleAssigned(role.Id, role.RoleType, entityId, requestingUserId);

        return MapToRoleDto(role);
    }

    private static void ValidateRoleCompatibility(
        EntityRoleType roleType,
        EntityType entityType,
        CreateEntityRoleRequest request)
    {
        if (!IsRoleCompatibleWithEntity(roleType, entityType))
            throw new BusinessRuleException(
                EntityErrorCodes.RoleInvalidForEntityType,
                $"Role '{roleType}' is not valid for entity type '{entityType}'.");

        // Field-level role compatibility: reject percent fields on roles that don't own them.
        if (request.OwnershipPercent.HasValue && roleType != EntityRoleType.Owner)
            throw new BusinessRuleException(
                EntityErrorCodes.RoleOwnershipNotAllowed,
                $"OwnershipPercent is only valid for Owner roles; received on '{roleType}'.");

        if (request.BeneficialInterestPercent.HasValue && roleType != EntityRoleType.Beneficiary)
            throw new BusinessRuleException(
                EntityErrorCodes.RoleBeneficialInterestNotAllowed,
                $"BeneficialInterestPercent is only valid for Beneficiary roles; " +
                $"received on '{roleType}'.");
    }

    private async Task ValidateOwnershipAsync(Guid entityId, EntityRoleType roleType, double? ownershipPercent)
    {
        if (!ownershipPercent.HasValue)
            return;

        if (ownershipPercent < 0 || ownershipPercent > 100)
            throw new BusinessRuleException(
                EntityErrorCodes.RoleOwnershipOutOfRange,
                "Ownership percent must be between 0 and 100.");

        if (roleType != EntityRoleType.Owner)
            return;

        var existingOwnership = await db.EntityRoles
            .Where(r => r.EntityId == entityId
                        && r.RoleType == EntityRoleType.Owner
                        && r.OwnershipPercent.HasValue)
            .SumAsync(r => r.OwnershipPercent!.Value);

        if (existingOwnership + (decimal)ownershipPercent.Value > 100m)
            throw new BusinessRuleException(
                EntityErrorCodes.RoleOwnershipExceeds100,
                "Total ownership across all Owner roles would exceed 100%.");
    }

    private async Task ValidateBeneficialInterestAsync(
        Guid entityId,
        EntityRoleType roleType,
        double? beneficialInterestPercent)
    {
        if (!beneficialInterestPercent.HasValue)
            return;

        if (beneficialInterestPercent < 0 || beneficialInterestPercent > 100)
            throw new BusinessRuleException(
                EntityErrorCodes.RoleBeneficialInterestOutOfRange,
                "Beneficial interest percent must be between 0 and 100.");

        if (roleType != EntityRoleType.Beneficiary)
            return;

        var existingInterest = await db.EntityRoles
            .Where(r => r.EntityId == entityId
                        && r.RoleType == EntityRoleType.Beneficiary
                        && r.BeneficialInterestPercent.HasValue)
            .SumAsync(r => r.BeneficialInterestPercent!.Value);

        if (existingInterest + (decimal)beneficialInterestPercent.Value > 100m)
            throw new BusinessRuleException(
                EntityErrorCodes.RoleBeneficialInterestExceeds100,
                "Total beneficial interest across all Beneficiary roles would exceed 100%.");
    }

    private static void ValidateRoleDateRange(DateTime? effectiveDate, DateTime? endDate)
    {
        if (effectiveDate.HasValue && endDate.HasValue && endDate.Value < effectiveDate.Value)
            throw new BusinessRuleException(
                EntityErrorCodes.RoleDateRangeInvalid,
                "End date cannot precede effective date.");
    }

    private static EntityRole BuildEntityRole(
        Guid entityId,
        EntityRoleType roleType,
        CreateEntityRoleRequest request) => new()
    {
        Id = Guid.NewGuid(),
        EntityId = entityId,
        ContactId = request.ContactId,
        RoleType = roleType,
        Title = request.Title,
        OwnershipPercent = request.OwnershipPercent.HasValue
            ? (decimal)request.OwnershipPercent.Value
            : null,
        BeneficialInterestPercent = request.BeneficialInterestPercent.HasValue
            ? (decimal)request.BeneficialInterestPercent.Value
            : null,
        IsSignatory = request.IsSignatory,
        IsPrimary = request.IsPrimary,
        SortOrder = request.SortOrder,
        EffectiveDate = request.EffectiveDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(request.EffectiveDate.Value, DateTimeKind.Utc))
            : null,
        EndDate = request.EndDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc))
            : null,
        Notes = request.Notes
    };

    public async Task<IReadOnlyList<EntityRoleDto>> GetRolesAsync(Guid requestingUserId, Guid entityId)
    {
        var entity = await db.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId)
            ?? throw NotFound(entityId);

        await AuthorizeReadAsync(requestingUserId, entity.UserId);

        var roles = await db.EntityRoles
            .AsNoTracking()
            .Where(r => r.EntityId == entityId)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.RoleType)
            .ToListAsync();

        return roles.Select(MapToRoleDto).ToList();
    }

    public async Task RemoveRoleAsync(Guid requestingUserId, Guid entityId, Guid roleId)
    {
        var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == entityId)
                     ?? throw NotFound(entityId);

        await AuthorizeWriteAsync(requestingUserId, entity.UserId);

        var role = await db.EntityRoles.FirstOrDefaultAsync(r => r.Id == roleId && r.EntityId == entityId)
                   ?? throw new NotFoundException(
                       EntityErrorCodes.RoleNotFound,
                       $"Role {roleId} was not found on entity {entityId}.");

        db.EntityRoles.Remove(role);
        await db.SaveChangesAsync();

        LogRoleRemoved(roleId, entityId, requestingUserId);
    }

    // =========================================================================
    // Authorization helpers
    // =========================================================================

    private async Task AuthorizeReadAsync(Guid requestingUserId, Guid ownerUserId)
    {
        var decision = await authorizationService.CheckAccessAsync(
            requestingUserId, ownerUserId, DataCategories.Entities, AccessType.Read);

        // Return NotFound (not Forbidden) on cross-user access to prevent
        // resource enumeration (OWASP A01 — IDOR). An attacker guessing IDs
        // must not be able to distinguish "exists but forbidden" from "does not exist".
        if (!decision.IsGranted)
            throw new NotFoundException(EntityErrorCodes.NotFound, "Entity was not found.");
    }

    private async Task AuthorizeWriteAsync(Guid requestingUserId, Guid ownerUserId)
    {
        var decision = await authorizationService.CheckAccessAsync(
            requestingUserId, ownerUserId, DataCategories.Entities, AccessType.Full);

        if (!decision.IsGranted)
            throw new NotFoundException(EntityErrorCodes.NotFound, "Entity was not found.");
    }

    // =========================================================================
    // Role/entity compatibility
    // =========================================================================

    private static bool IsRoleCompatibleWithEntity(EntityRoleType roleType, EntityType entityType)
    {
        if (entityType == EntityType.Personal)
            return false;

        if (roleType == EntityRoleType.Other)
            return true;

        var code = (int)roleType;

        return entityType switch
        {
            EntityType.Business => code is >= 0 and <= 9,
            EntityType.Trust => code is >= 10 and <= 19,
            _ => false
        };
    }

    // =========================================================================
    // Slug helpers
    // =========================================================================

    [GeneratedRegex(@"[^a-z0-9\s-]+")]
    private static partial Regex NonAlphaNumRegex();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex WhitespaceRegex();

    private static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var lower = name.Trim().ToLowerInvariant();
        var cleaned = NonAlphaNumRegex().Replace(lower, "");
        var hyphenated = WhitespaceRegex().Replace(cleaned, "-").Trim('-');
        return hyphenated;
    }

    private static string NormalizeSlug(string slug) => GenerateSlug(slug);

    // =========================================================================
    // Not-found factory
    // =========================================================================

    private static NotFoundException NotFound(Guid entityId) =>
        new(EntityErrorCodes.NotFound, $"Entity with ID {entityId} was not found.");

    // SQL Server error numbers:
    //   2601 — Cannot insert duplicate key row in object with unique index.
    //   2627 — Violation of UNIQUE KEY / PRIMARY KEY constraint.
    // Both indicate a unique-index/key collision on the underlying table.
    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql
        && (sql.Number == 2601 || sql.Number == 2627);

    // =========================================================================
    // Mapping helpers
    // =========================================================================

    private static EntityDto MapToDto(Entity entity) => new()
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

    private static EntityDetailDto MapToDetailDto(Entity entity) => new()
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
        Roles = entity.Roles.Select(MapToRoleDto).ToArray(),
        ChildEntityCount = entity.ChildEntities.Count
    };

    private static EntityRoleDto MapToRoleDto(EntityRole role) => new()
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

    // =========================================================================
    // Source-generated logging (EventId 3000-3999)
    // =========================================================================

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
