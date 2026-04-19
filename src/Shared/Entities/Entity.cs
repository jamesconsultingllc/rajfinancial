using MemoryPack;
using RajFinancial.Shared.Entities.Business;
using RajFinancial.Shared.Entities.Trust;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Core organizing unit for financial data. Every asset, income source, bill,
///     account, and document is scoped to an entity.
/// </summary>
/// <remarks>
///     <para>
///         Three entity types exist: Personal (one per user, auto-created),
///         Business (LLCs, corporations, etc.), and Trust (revocable, irrevocable, etc.).
///     </para>
///     <para>
///         Uses TPH (Table-Per-Hierarchy) with JSON metadata columns for type-specific
///         fields via EF Core <c>ToJson()</c>.
///     </para>
/// </remarks>
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class Entity
{
    /// <summary>Unique identifier.</summary>
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }

    /// <summary>The user who owns this entity (Entra Object ID).</summary>
    [MemoryPackOrder(1)]
    public Guid UserId { get; set; }

    /// <summary>Entity classification: Personal, Business, or Trust.</summary>
    [MemoryPackOrder(2)]
    public EntityType Type { get; set; }

    /// <summary>Display name (e.g., "Personal", "Acme LLC", "Family Trust").</summary>
    [MemoryPackOrder(3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe slug derived from name (e.g., "acme-llc"). Unique per owning user.</summary>
    [MemoryPackOrder(4)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional parent entity for nesting (e.g., holding company → subsidiary).</summary>
    [MemoryPackOrder(5)]
    public Guid? ParentEntityId { get; set; }

    /// <summary>Cloud storage connection for this entity's documents.</summary>
    [MemoryPackOrder(6)]
    public Guid? StorageConnectionId { get; set; }

    /// <summary>Whether this entity is currently active.</summary>
    [MemoryPackOrder(7)]
    public bool IsActive { get; set; } = true;

    /// <summary>Date and time the entity was created.</summary>
    [MemoryPackOrder(8)]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Date and time the entity was last updated.</summary>
    [MemoryPackOrder(9)]
    public DateTimeOffset? UpdatedAt { get; set; }

    // ── Type-specific metadata (JSON columns via ToJson()) ──

    /// <summary>Business-specific metadata. Null for non-Business entities.</summary>
    [MemoryPackOrder(10)]
    public BusinessEntityMetadata? Business { get; set; }

    /// <summary>Trust-specific metadata. Null for non-Trust entities.</summary>
    [MemoryPackOrder(11)]
    public TrustEntityMetadata? Trust { get; set; }

    // ── Navigation properties (ignored by MemoryPack) ──

    /// <summary>Parent entity (for nested entities).</summary>
    [MemoryPackIgnore]
    public Entity? ParentEntity { get; set; }

    /// <summary>Child entities (subsidiaries, sub-trusts).</summary>
    [MemoryPackIgnore]
    public ICollection<Entity> ChildEntities { get; set; } = [];

    /// <summary>Roles assigned to contacts on this entity.</summary>
    [MemoryPackIgnore]
    public ICollection<EntityRole> Roles { get; set; } = [];
}
