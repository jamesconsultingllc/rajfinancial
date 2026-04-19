using MemoryPack;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Business;
using RajFinancial.Shared.Entities.Trust;

namespace RajFinancial.Shared.Contracts.Entities;

/// <summary>
///     Request body for creating a new entity.
/// </summary>
/// <remarks>
///     Used by <c>POST /api/entities</c>. The entity is automatically assigned
///     to the authenticated user. When <see cref="Type" /> is Business,
///     <see cref="Business" /> must be provided; when Trust, <see cref="Trust" />
///     must be provided; Personal entities use neither.
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record CreateEntityRequest
{
    /// <summary>Display name (required, max 200 characters).</summary>
    [MemoryPackOrder(0)]
    public required string Name { get; init; }

    /// <summary>Entity classification (required).</summary>
    [MemoryPackOrder(1)]
    public required EntityType Type { get; init; }

    /// <summary>Optional explicit slug. If omitted, the server generates one from <see cref="Name" />.</summary>
    [MemoryPackOrder(2)]
    public string? Slug { get; init; }

    /// <summary>Optional parent entity id (for nested structures like holding companies).</summary>
    [MemoryPackOrder(3)]
    public Guid? ParentEntityId { get; init; }

    /// <summary>Cloud storage connection for this entity's documents.</summary>
    [MemoryPackOrder(4)]
    public Guid? StorageConnectionId { get; init; }

    /// <summary>Business-specific metadata. Required when <see cref="Type" /> is Business.</summary>
    [MemoryPackOrder(5)]
    public BusinessEntityMetadata? Business { get; init; }

    /// <summary>Trust-specific metadata. Required when <see cref="Type" /> is Trust.</summary>
    [MemoryPackOrder(6)]
    public TrustEntityMetadata? Trust { get; init; }
}
