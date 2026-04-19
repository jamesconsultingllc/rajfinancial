using MemoryPack;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Business;
using RajFinancial.Shared.Entities.Trust;

namespace RajFinancial.Shared.Contracts.Entities;

/// <summary>
///     Request body for updating an existing entity.
/// </summary>
/// <remarks>
///     Used by <c>PUT /api/entities/{id}</c>. The entity's <c>Type</c> is
///     immutable and therefore not present here. For Personal entities, the
///     service rejects any change to <see cref="Name" />. Only the metadata
///     record matching the entity's existing type is honored; the other is
///     ignored.
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record UpdateEntityRequest
{
    /// <summary>Display name. Validated via FluentValidation; see <c>UpdateEntityRequestValidator</c>.</summary>
    [MemoryPackOrder(0)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Whether this entity is active.</summary>
    [MemoryPackOrder(1)]
    public bool? IsActive { get; init; }

    /// <summary>Cloud storage connection for this entity's documents.</summary>
    [MemoryPackOrder(2)]
    public Guid? StorageConnectionId { get; init; }

    /// <summary>Business-specific metadata. Applied only when the entity is of type Business.</summary>
    [MemoryPackOrder(3)]
    public BusinessEntityMetadata? Business { get; init; }

    /// <summary>Trust-specific metadata. Applied only when the entity is of type Trust.</summary>
    [MemoryPackOrder(4)]
    public TrustEntityMetadata? Trust { get; init; }
}
