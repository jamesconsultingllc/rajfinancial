using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Observability;
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
    // Personal-entity domain defaults. The name/slug for a user's built-in
    // Personal entity are treated as a domain constant, not a localizable label.
    private const string PersonalEntityName = "Personal";
    private const string PersonalEntitySlug = "personal";

    // =========================================================================
    // Entity queries
    // =========================================================================

    public async Task<IReadOnlyList<EntityDto>> GetEntitiesAsync(
        Guid requestingUserId,
        Guid ownerUserId,
        EntityType? filterType = null)
    {
        using var activity = EntityTelemetry.StartActivity(EntityTelemetry.ActivityGetEntitiesService);
        activity?.SetTag(EntityTelemetry.UserIdTag, requestingUserId.ToString());
        if (filterType.HasValue)
            activity?.SetTag(EntityTelemetry.EntityTypeTag, filterType.Value.ToString());

        try
        {
            await AuthorizeReadAsync(requestingUserId, ownerUserId);

            // Tag owner.id only after authorization succeeds so denied
            // requests don't leak the requested owner id (user-supplied) into
            // telemetry. Matches the pattern used in AssetService.
            activity?.SetTag(EntityTelemetry.EntityOwnerIdTag, ownerUserId.ToString());

            var query = db.Entities
                .AsNoTracking()
                .Where(e => e.UserId == ownerUserId);

            if (filterType.HasValue)
                query = query.Where(e => e.Type == filterType.Value);

            // Stopwatch scoped to the EF query only so the histogram reflects
            // pure DB time (not authorization or other non-query work).
            var stopwatch = Stopwatch.StartNew();
            var entities = await query
                .OrderBy(e => e.Type)
                .ThenBy(e => e.Name)
                .ToListAsync();
            stopwatch.Stop();

            EntityTelemetry.RecordQueryDuration(
                EntityTelemetry.QueryOpGetEntities,
                stopwatch.Elapsed.TotalMilliseconds);

            return entities.Select(e => e.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    public async Task<EntityDetailDto> GetEntityByIdAsync(Guid requestingUserId, Guid entityId)
    {
        using var activity = EntityTelemetry.StartActivity(EntityTelemetry.ActivityGetEntityByIdService);
        activity?.SetTag(EntityTelemetry.UserIdTag, requestingUserId.ToString());
        activity?.SetTag(EntityTelemetry.EntityIdTag, entityId.ToString());

        try
        {
            var ownerId = await db.Entities
                .AsNoTracking()
                .Where(e => e.Id == entityId)
                .Select(e => (Guid?)e.UserId)
                .FirstOrDefaultAsync()
                ?? throw EntityDbErrors.NotFound(entityId);

            await AuthorizeReadAsync(requestingUserId, ownerId);

            // Stopwatch scoped to the EF fetch only (excludes ownership lookup
            // and authorization so the histogram reflects pure query time).
            var stopwatch = Stopwatch.StartNew();
            var entity = await db.Entities
                .AsNoTracking()
                .Include(e => e.Roles)
                .Include(e => e.ChildEntities)
                .FirstOrDefaultAsync(e => e.Id == entityId)
                ?? throw EntityDbErrors.NotFound(entityId);
            stopwatch.Stop();

            activity?.SetTag(EntityTelemetry.EntityTypeTag, entity.Type.ToString());
            activity?.SetTag(EntityTelemetry.EntitySlugTag, entity.Slug);

            EntityTelemetry.RecordQueryDuration(
                EntityTelemetry.QueryOpGetEntityById,
                stopwatch.Elapsed.TotalMilliseconds);

            return entity.ToDetailDto();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    // =========================================================================
    // Entity mutations
    // =========================================================================

    public async Task<EntityDto> CreateEntityAsync(Guid userId, CreateEntityRequest request)
    {
        using var activity = EntityTelemetry.StartActivity(EntityTelemetry.ActivityCreateEntityService);
        activity?.SetTag(EntityTelemetry.UserIdTag, userId.ToString());
        if (request.Type.HasValue)
            activity?.SetTag(EntityTelemetry.EntityTypeTag, request.Type.Value.ToString());

        try
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
            catch (DbUpdateException ex) when (EntityDbErrors.IsUniqueConstraintViolation(ex))
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

            activity?.SetTag(EntityTelemetry.EntityIdTag, entity.Id.ToString());
            activity?.SetTag(EntityTelemetry.EntitySlugTag, entity.Slug);

            EntityTelemetry.RecordEntityCreated(entity.Type.ToString());

            LogEntityCreated(entity.Id, entity.Type, entity.Name, userId);

            return entity.ToDto();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
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
            ? EntitySlug.Normalize(explicitSlug)
            : EntitySlug.Generate(name);

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
        using var activity = EntityTelemetry.StartActivity(EntityTelemetry.ActivityUpdateEntityService);
        activity?.SetTag(EntityTelemetry.UserIdTag, requestingUserId.ToString());
        activity?.SetTag(EntityTelemetry.EntityIdTag, entityId.ToString());

        try
        {
            var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == entityId)
                         ?? throw EntityDbErrors.NotFound(entityId);

            await AuthorizeWriteAsync(requestingUserId, entity.UserId);

            // Tag DB-derived values only after authorization succeeds. Denied
            // requests throw NotFound (anti-enumeration); pre-auth tagging
            // would leak cross-tenant entity details into telemetry.
            activity?.SetTag(EntityTelemetry.EntityTypeTag, entity.Type.ToString());
            activity?.SetTag(EntityTelemetry.EntitySlugTag, entity.Slug);

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

            return entity.ToDto();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    public async Task DeleteEntityAsync(Guid requestingUserId, Guid entityId)
    {
        using var activity = EntityTelemetry.StartActivity(EntityTelemetry.ActivityDeleteEntityService);
        activity?.SetTag(EntityTelemetry.UserIdTag, requestingUserId.ToString());
        activity?.SetTag(EntityTelemetry.EntityIdTag, entityId.ToString());

        try
        {
            var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == entityId)
                         ?? throw EntityDbErrors.NotFound(entityId);

            await AuthorizeWriteAsync(requestingUserId, entity.UserId);

            // Tag DB-derived values only after authorization succeeds. Denied
            // requests throw NotFound (anti-enumeration); pre-auth tagging
            // would leak cross-tenant entity details into telemetry.
            activity?.SetTag(EntityTelemetry.EntityTypeTag, entity.Type.ToString());
            activity?.SetTag(EntityTelemetry.EntitySlugTag, entity.Slug);

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
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    public async Task<EntityDto> EnsurePersonalEntityAsync(Guid userId)
    {
        using var activity = EntityTelemetry.StartActivity(EntityTelemetry.ActivityEnsurePersonalEntityService);
        activity?.SetTag(EntityTelemetry.UserIdTag, userId.ToString());
        activity?.SetTag(EntityTelemetry.EntityTypeTag, EntityType.Personal.ToString());

        try
        {
            var existing = await db.Entities
                .FirstOrDefaultAsync(e => e.UserId == userId && e.Type == EntityType.Personal);

            if (existing is not null)
            {
                activity?.SetTag(EntityTelemetry.EntityIdTag, existing.Id.ToString());
                activity?.SetTag(EntityTelemetry.EntitySlugTag, existing.Slug);
                return existing.ToDto();
            }

            var entity = new Entity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = EntityType.Personal,
                Name = PersonalEntityName,
                Slug = PersonalEntitySlug,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Entities.Add(entity);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (EntityDbErrors.IsUniqueConstraintViolation(ex))
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

                activity?.SetTag(EntityTelemetry.EntityIdTag, winner.Id.ToString());
                activity?.SetTag(EntityTelemetry.EntitySlugTag, winner.Slug);

                return winner.ToDto();
            }

            LogPersonalEntityProvisioned(entity.Id, userId);

            activity?.SetTag(EntityTelemetry.EntityIdTag, entity.Id.ToString());
            activity?.SetTag(EntityTelemetry.EntitySlugTag, entity.Slug);

            EntityTelemetry.RecordEntityCreated(entity.Type.ToString());

            return entity.ToDto();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    // =========================================================================
    // Role operations
    // =========================================================================

    public async Task<EntityRoleDto> AssignRoleAsync(
        Guid requestingUserId,
        Guid entityId,
        CreateEntityRoleRequest request)
    {
        using var activity = EntityTelemetry.StartActivity(EntityTelemetry.ActivityAssignRoleService);
        activity?.SetTag(EntityTelemetry.UserIdTag, requestingUserId.ToString());
        activity?.SetTag(EntityTelemetry.EntityIdTag, entityId.ToString());
        if (request.RoleType.HasValue)
            activity?.SetTag(EntityTelemetry.EntityRoleTypeTag, request.RoleType.Value.ToString());

        try
        {
            var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == entityId)
                         ?? throw EntityDbErrors.NotFound(entityId);

            await AuthorizeWriteAsync(requestingUserId, entity.UserId);

            // Tag DB-derived values only after authorization succeeds. Denied
            // requests throw NotFound (anti-enumeration); pre-auth tagging
            // would leak cross-tenant entity details into telemetry.
            activity?.SetTag(EntityTelemetry.EntityTypeTag, entity.Type.ToString());
            activity?.SetTag(EntityTelemetry.EntitySlugTag, entity.Slug);

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

            EntityRoleRules.ValidateRoleCompatibility(roleType, entity.Type, request);
            await ValidateOwnershipAsync(entityId, roleType, request.OwnershipPercent);
            await ValidateBeneficialInterestAsync(entityId, roleType, request.BeneficialInterestPercent);
            EntityRoleRules.ValidateRoleDateRange(request.EffectiveDate, request.EndDate);

            var role = EntityRoleFactory.Build(entityId, roleType, request);

            db.EntityRoles.Add(role);
            await db.SaveChangesAsync();

            activity?.SetTag(EntityTelemetry.EntityRoleIdTag, role.Id.ToString());

            EntityTelemetry.RecordEntityRoleAssigned(role.RoleType.ToString());

            LogRoleAssigned(role.Id, role.RoleType, entityId, requestingUserId);

            return role.ToRoleDto();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
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

    public async Task<IReadOnlyList<EntityRoleDto>> GetRolesAsync(Guid requestingUserId, Guid entityId)
    {
        using var activity = EntityTelemetry.StartActivity(EntityTelemetry.ActivityGetRolesService);
        activity?.SetTag(EntityTelemetry.UserIdTag, requestingUserId.ToString());
        activity?.SetTag(EntityTelemetry.EntityIdTag, entityId.ToString());

        try
        {
            var entity = await db.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == entityId)
                ?? throw EntityDbErrors.NotFound(entityId);

            await AuthorizeReadAsync(requestingUserId, entity.UserId);

            // Tag DB-derived values only after authorization succeeds. Denied
            // requests throw NotFound (anti-enumeration); pre-auth tagging
            // would leak cross-tenant entity details into telemetry.
            activity?.SetTag(EntityTelemetry.EntityTypeTag, entity.Type.ToString());
            activity?.SetTag(EntityTelemetry.EntitySlugTag, entity.Slug);

            // Stopwatch scoped to the roles EF query only (excludes entity
            // lookup and authorization so the histogram reflects pure DB time).
            var stopwatch = Stopwatch.StartNew();
            var roles = await db.EntityRoles
                .AsNoTracking()
                .Where(r => r.EntityId == entityId)
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.RoleType)
                .ToListAsync();
            stopwatch.Stop();

            EntityTelemetry.RecordQueryDuration(
                EntityTelemetry.QueryOpGetRoles,
                stopwatch.Elapsed.TotalMilliseconds);

            return roles.Select(r => r.ToRoleDto()).ToList();
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    public async Task RemoveRoleAsync(Guid requestingUserId, Guid entityId, Guid roleId)
    {
        using var activity = EntityTelemetry.StartActivity(EntityTelemetry.ActivityRemoveRoleService);
        activity?.SetTag(EntityTelemetry.UserIdTag, requestingUserId.ToString());
        activity?.SetTag(EntityTelemetry.EntityIdTag, entityId.ToString());
        activity?.SetTag(EntityTelemetry.EntityRoleIdTag, roleId.ToString());

        try
        {
            var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == entityId)
                         ?? throw EntityDbErrors.NotFound(entityId);

            await AuthorizeWriteAsync(requestingUserId, entity.UserId);

            // Tag DB-derived values only after authorization succeeds. Denied
            // requests throw NotFound (anti-enumeration); pre-auth tagging
            // would leak cross-tenant entity details into telemetry.
            activity?.SetTag(EntityTelemetry.EntityTypeTag, entity.Type.ToString());
            activity?.SetTag(EntityTelemetry.EntitySlugTag, entity.Slug);

            var role = await db.EntityRoles.FirstOrDefaultAsync(r => r.Id == roleId && r.EntityId == entityId)
                       ?? throw new NotFoundException(
                           EntityErrorCodes.RoleNotFound,
                           $"Role {roleId} was not found on entity {entityId}.");

            db.EntityRoles.Remove(role);
            await db.SaveChangesAsync();

            LogRoleRemoved(roleId, entityId, requestingUserId);
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
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

}
