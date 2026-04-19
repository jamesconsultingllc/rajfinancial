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
///     to the authenticated user. <see cref="Business" /> and <see cref="Trust" />
///     metadata are optional at creation — Personal entities use neither, while
///     Business/Trust entities may be created with just a name/type and have
///     metadata populated by a later update.
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record CreateEntityRequest
{
    /// <summary>Display name. Validated via FluentValidation; see <c>CreateEntityRequestValidator</c>.</summary>
    [MemoryPackOrder(0)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Entity classification. Validated via FluentValidation (NotNull + IsInEnum).</summary>
    [MemoryPackOrder(1)]
    public EntityType? Type { get; init; }

    /// <summary>Optional explicit slug. If omitted, the server generates one from <see cref="Name" />.</summary>
    [MemoryPackOrder(2)]
    public string? Slug { get; init; }

    /// <summary>Optional parent entity id (for nested structures like holding companies).</summary>
    [MemoryPackOrder(3)]
    public Guid? ParentEntityId { get; init; }

    /// <summary>Cloud storage connection for this entity's documents.</summary>
    [MemoryPackOrder(4)]
    public Guid? StorageConnectionId { get; init; }

    /// <summary>Business-specific metadata. Optional at creation (may be populated via update).</summary>
    [MemoryPackOrder(5)]
    public BusinessEntityMetadata? Business { get; init; }

    /// <summary>Trust-specific metadata. Optional at creation (may be populated via update).</summary>
    [MemoryPackOrder(6)]
    public TrustEntityMetadata? Trust { get; init; }
}
