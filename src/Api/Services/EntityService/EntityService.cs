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

        if (requestedType == EntityType.Personal)
        {
            var personalExists = await db.Entities
                .AnyAsync(e => e.UserId == userId && e.Type == EntityType.Personal);

            if (personalExists)
                throw new ConflictException(
                    EntityErrorCodes.PERSONAL_ALREADY_EXISTS,
                    "User already has a Personal entity.");
        }

        if (request.ParentEntityId.HasValue)
        {
            var parent = await db.Entities
                .FirstOrDefaultAsync(e => e.Id == request.ParentEntityId.Value)
                ?? throw new BusinessRuleException(
                    EntityErrorCodes.PARENT_NOT_FOUND,
                    $"Parent entity {request.ParentEntityId.Value} was not found.");

            if (parent.UserId != userId)
                throw new BusinessRuleException(
                    EntityErrorCodes.PARENT_NOT_FOUND,
                    $"Parent entity {request.ParentEntityId.Value} was not found.");
        }

        var slug = !string.IsNullOrWhiteSpace(request.Slug)
            ? NormalizeSlug(request.Slug)
            : GenerateSlug(request.Name);

        if (string.IsNullOrWhiteSpace(slug))
            throw new BusinessRuleException(
                EntityErrorCodes.SLUG_INVALID,
                "Slug could not be generated from the provided name. Supply an explicit ASCII slug.");

        var slugTaken = await db.Entities
            .AnyAsync(e => e.UserId == userId && e.Slug == slug);

        if (slugTaken)
            throw new ConflictException(
                EntityErrorCodes.SLUG_DUPLICATE,
                $"Slug '{slug}' is already in use.");

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
                EntityErrorCodes.SLUG_DUPLICATE,
                $"Slug '{slug}' is already in use.");
        }

        logger.LogInformation(
            "Entity {EntityId} ({EntityType} '{EntityName}') created for user {UserId}",
            entity.Id, entity.Type, entity.Name, userId);

        return MapToDto(entity);
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
                EntityErrorCodes.PERSONAL_NAME_IMMUTABLE,
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

        logger.LogInformation(
            "Entity {EntityId} updated by user {UserId}", entity.Id, requestingUserId);

        return MapToDto(entity);
    }

    public async Task DeleteEntityAsync(Guid requestingUserId, Guid entityId)
    {
        var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == entityId)
                     ?? throw NotFound(entityId);

        await AuthorizeWriteAsync(requestingUserId, entity.UserId);

        if (entity.Type == EntityType.Personal)
            throw new BusinessRuleException(
                EntityErrorCodes.PERSONAL_CANNOT_DELETE,
                "Personal entities cannot be deleted.");

        var hasChildren = await db.Entities.AnyAsync(e => e.ParentEntityId == entityId);
        if (hasChildren)
            throw new BusinessRuleException(
                EntityErrorCodes.HAS_CHILDREN_CANNOT_DELETE,
                "Cannot delete an entity that has child entities.");

        var roles = await db.EntityRoles.Where(r => r.EntityId == entityId).ToListAsync();
        if (roles.Count > 0)
            db.EntityRoles.RemoveRange(roles);

        db.Entities.Remove(entity);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Entity {EntityId} deleted by user {UserId}", entityId, requestingUserId);
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

            logger.LogInformation(
                "Personal entity {EntityId} was concurrently provisioned for user {UserId}",
                winner.Id, userId);

            return MapToDto(winner);
        }

        logger.LogInformation("Personal entity {EntityId} auto-provisioned for user {UserId}",
            entity.Id, userId);

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

        if (!IsRoleCompatibleWithEntity(roleType, entity.Type))
            throw new BusinessRuleException(
                EntityErrorCodes.ROLE_INVALID_FOR_ENTITY_TYPE,
                $"Role '{roleType}' is not valid for entity type '{entity.Type}'.");

        // Field-level role compatibility: reject percent fields on roles that don't own them.
        if (request.OwnershipPercent.HasValue && roleType != EntityRoleType.Owner)
            throw new BusinessRuleException(
                EntityErrorCodes.ROLE_OWNERSHIP_NOT_ALLOWED,
                $"OwnershipPercent is only valid for Owner roles; received on '{roleType}'.");

        if (request.BeneficialInterestPercent.HasValue && roleType != EntityRoleType.Beneficiary)
            throw new BusinessRuleException(
                EntityErrorCodes.ROLE_BENEFICIAL_INTEREST_NOT_ALLOWED,
                $"BeneficialInterestPercent is only valid for Beneficiary roles; " +
                $"received on '{roleType}'.");

        if (request.OwnershipPercent.HasValue)
        {
            if (request.OwnershipPercent < 0 || request.OwnershipPercent > 100)
                throw new BusinessRuleException(
                    EntityErrorCodes.ROLE_OWNERSHIP_OUT_OF_RANGE,
                    "Ownership percent must be between 0 and 100.");

            var existingOwnership = await db.EntityRoles
                .Where(r => r.EntityId == entityId
                            && r.RoleType == EntityRoleType.Owner
                            && r.OwnershipPercent.HasValue)
                .SumAsync(r => r.OwnershipPercent!.Value);

            if (roleType == EntityRoleType.Owner
                && existingOwnership + (decimal)request.OwnershipPercent.Value > 100m)
                throw new BusinessRuleException(
                    EntityErrorCodes.ROLE_OWNERSHIP_EXCEEDS_100,
                    "Total ownership across all Owner roles would exceed 100%.");
        }

        if (request.BeneficialInterestPercent.HasValue)
        {
            if (request.BeneficialInterestPercent < 0 || request.BeneficialInterestPercent > 100)
                throw new BusinessRuleException(
                    EntityErrorCodes.ROLE_BENEFICIAL_INTEREST_OUT_OF_RANGE,
                    "Beneficial interest percent must be between 0 and 100.");

            var existingInterest = await db.EntityRoles
                .Where(r => r.EntityId == entityId
                            && r.RoleType == EntityRoleType.Beneficiary
                            && r.BeneficialInterestPercent.HasValue)
                .SumAsync(r => r.BeneficialInterestPercent!.Value);

            if (roleType == EntityRoleType.Beneficiary
                && existingInterest + (decimal)request.BeneficialInterestPercent.Value > 100m)
                throw new BusinessRuleException(
                    EntityErrorCodes.ROLE_BENEFICIAL_INTEREST_EXCEEDS_100,
                    "Total beneficial interest across all Beneficiary roles would exceed 100%.");
        }

        if (request.EffectiveDate.HasValue && request.EndDate.HasValue
            && request.EndDate.Value < request.EffectiveDate.Value)
            throw new BusinessRuleException(
                EntityErrorCodes.ROLE_DATE_RANGE_INVALID,
                "End date cannot precede effective date.");

        var role = new EntityRole
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

        db.EntityRoles.Add(role);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Role {RoleId} ({RoleType}) assigned to entity {EntityId} by user {UserId}",
            role.Id, role.RoleType, entityId, requestingUserId);

        return MapToRoleDto(role);
    }

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
                       EntityErrorCodes.ROLE_NOT_FOUND,
                       $"Role {roleId} was not found on entity {entityId}.");

        db.EntityRoles.Remove(role);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Role {RoleId} removed from entity {EntityId} by user {UserId}",
            roleId, entityId, requestingUserId);
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
            throw new NotFoundException(EntityErrorCodes.NOT_FOUND, "Entity was not found.");
    }

    private async Task AuthorizeWriteAsync(Guid requestingUserId, Guid ownerUserId)
    {
        var decision = await authorizationService.CheckAccessAsync(
            requestingUserId, ownerUserId, DataCategories.Entities, AccessType.Full);

        if (!decision.IsGranted)
            throw new NotFoundException(EntityErrorCodes.NOT_FOUND, "Entity was not found.");
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
        new(EntityErrorCodes.NOT_FOUND, $"Entity with ID {entityId} was not found.");

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
}
