using RajFinancial.Shared.Contracts.Entities;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Services.EntityService;

/// <summary>
///     Provides CRUD operations for financial entities (Personal, Business, Trust)
///     and management of roles linking contacts to those entities.
/// </summary>
/// <remarks>
///     <para>
///         All operations enforce the three-tier authorization model via
///         <see cref="Authorization.IAuthorizationService"/>:
///         <list type="number">
///             <item><b>Owner:</b> The user owns the entity.</item>
///             <item><b>DataAccessGrant:</b> Another user has been granted access to the owner's entities.</item>
///             <item><b>Administrator:</b> Platform administrators have full access.</item>
///         </list>
///     </para>
///     <para>
///         <b>Personal entities:</b> Exactly one per user. Auto-provisioned via
///         <see cref="EnsurePersonalEntityAsync"/>. Cannot be renamed, duplicated, or deleted.
///     </para>
///     <para>
///         <b>Data Category:</b> <see cref="DataCategories.Entities"/>
///     </para>
/// </remarks>
public interface IEntityService
{
    /// <summary>Retrieves all entities owned by <paramref name="ownerUserId"/>, optionally filtered by type.</summary>
    Task<IReadOnlyList<EntityDto>> GetEntitiesAsync(
        Guid requestingUserId,
        Guid ownerUserId,
        EntityType? filterType = null);

    /// <summary>Retrieves a single entity by ID with full details including roles.</summary>
    /// <remarks>Throws <see cref="NotFoundException"/> when the entity does not exist or the caller is not authorized.</remarks>
    Task<EntityDetailDto> GetEntityByIdAsync(Guid requestingUserId, Guid entityId);

    /// <summary>Creates a new Business or Trust entity for the authenticated user.</summary>
    Task<EntityDto> CreateEntityAsync(Guid userId, CreateEntityRequest request);

    /// <summary>Updates an existing entity. Type is immutable; Personal name cannot be changed.</summary>
    Task<EntityDto> UpdateEntityAsync(Guid requestingUserId, Guid entityId, UpdateEntityRequest request);

    /// <summary>Deletes an entity. Personal entities cannot be deleted.</summary>
    Task DeleteEntityAsync(Guid requestingUserId, Guid entityId);

    /// <summary>Creates the user's Personal entity if it does not already exist.</summary>
    Task<EntityDto> EnsurePersonalEntityAsync(Guid userId);

    /// <summary>Assigns a role (Owner, Trustee, Beneficiary, etc.) to a contact on an entity.</summary>
    Task<EntityRoleDto> AssignRoleAsync(Guid requestingUserId, Guid entityId, CreateEntityRoleRequest request);

    /// <summary>Retrieves all role assignments for an entity.</summary>
    Task<IReadOnlyList<EntityRoleDto>> GetRolesAsync(Guid requestingUserId, Guid entityId);

    /// <summary>Removes a role assignment from an entity.</summary>
    Task RemoveRoleAsync(Guid requestingUserId, Guid entityId, Guid roleId);
}
