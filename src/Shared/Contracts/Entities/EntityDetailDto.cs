using MemoryPack;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Contracts.Entities.Business;
using RajFinancial.Shared.Contracts.Entities.Trust;

namespace RajFinancial.Shared.Contracts.Entities;

/// <summary>
///     Detailed data transfer object for a single entity view.
/// </summary>
/// <remarks>
///     Used by <c>GET /api/entities/{id}</c>. Includes all fields from
///     <see cref="EntityDto" /> plus the full list of <see cref="EntityRoleDto" />
///     role assignments.
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record EntityDetailDto
{
    /// <summary>Unique identifier.</summary>
    [MemoryPackOrder(0)]
    public required Guid Id { get; init; }

    /// <summary>Entity classification: Personal, Business, or Trust.</summary>
    [MemoryPackOrder(1)]
    public required EntityType Type { get; init; }

    /// <summary>Display name.</summary>
    [MemoryPackOrder(2)]
    public required string Name { get; init; }

    /// <summary>URL-safe slug derived from name. Unique per owning user.</summary>
    [MemoryPackOrder(3)]
    public required string Slug { get; init; }

    /// <summary>Optional parent entity id (holding company → subsidiary nesting).</summary>
    [MemoryPackOrder(4)]
    public Guid? ParentEntityId { get; init; }

    /// <summary>Cloud storage connection for this entity's documents.</summary>
    [MemoryPackOrder(5)]
    public Guid? StorageConnectionId { get; init; }

    /// <summary>Whether this entity is currently active.</summary>
    [MemoryPackOrder(6)]
    public required bool IsActive { get; init; }

    /// <summary>Date and time the entity was created.</summary>
    [MemoryPackOrder(7)]
    public required DateTime CreatedAt { get; init; }

    /// <summary>Date and time the entity was last updated.</summary>
    [MemoryPackOrder(8)]
    public DateTime? UpdatedAt { get; init; }

    /// <summary>Business-specific metadata. Null for non-Business entities.</summary>
    [MemoryPackOrder(9)]
    public BusinessEntityMetadata? Business { get; init; }

    /// <summary>Trust-specific metadata. Null for non-Trust entities.</summary>
    [MemoryPackOrder(10)]
    public TrustEntityMetadata? Trust { get; init; }

    /// <summary>All role assignments on this entity.</summary>
    [MemoryPackOrder(11)]
    public EntityRoleDto[] Roles { get; init; } = [];

    /// <summary>Number of child (nested) entities.</summary>
    [MemoryPackOrder(12)]
    public required int ChildEntityCount { get; init; }
}
