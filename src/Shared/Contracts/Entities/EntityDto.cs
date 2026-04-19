using MemoryPack;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Business;
using RajFinancial.Shared.Entities.Trust;

namespace RajFinancial.Shared.Contracts.Entities;

/// <summary>
///     Data transfer object for entity list/summary views.
/// </summary>
/// <remarks>
///     Used by <c>GET /api/entities</c> to return a collection of entities the
///     authenticated user owns. Metadata summaries are included so the list UI
///     can display key fields (EIN, state, category) without a second round trip.
///     For the full role list, use <see cref="EntityDetailDto" />.
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record EntityDto
{
    /// <summary>Unique identifier.</summary>
    [MemoryPackOrder(0)]
    public required Guid Id { get; init; }

    /// <summary>Entity classification: Personal, Business, or Trust.</summary>
    [MemoryPackOrder(1)]
    public required EntityType Type { get; init; }

    /// <summary>Display name (e.g., "Personal", "Acme LLC", "Family Trust").</summary>
    [MemoryPackOrder(2)]
    public required string Name { get; init; }

    /// <summary>URL-safe slug derived from name (e.g., "acme-llc"). Unique per owning user.</summary>
    [MemoryPackOrder(3)]
    public required string Slug { get; init; }

    /// <summary>Optional parent entity id (holding company → subsidiary nesting).</summary>
    [MemoryPackOrder(4)]
    public Guid? ParentEntityId { get; init; }

    /// <summary>Whether this entity is currently active.</summary>
    [MemoryPackOrder(5)]
    public required bool IsActive { get; init; }

    /// <summary>Date and time the entity was created.</summary>
    [MemoryPackOrder(6)]
    public required DateTime CreatedAt { get; init; }

    /// <summary>Date and time the entity was last updated.</summary>
    [MemoryPackOrder(7)]
    public DateTime? UpdatedAt { get; init; }

    /// <summary>Business-specific metadata summary. Null for non-Business entities.</summary>
    [MemoryPackOrder(8)]
    public BusinessEntityMetadata? Business { get; init; }

    /// <summary>Trust-specific metadata summary. Null for non-Trust entities.</summary>
    [MemoryPackOrder(9)]
    public TrustEntityMetadata? Trust { get; init; }
}
