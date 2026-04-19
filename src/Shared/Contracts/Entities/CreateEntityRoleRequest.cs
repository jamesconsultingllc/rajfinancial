using MemoryPack;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Shared.Contracts.Entities;

/// <summary>
///     Request body for creating a new entity role assignment.
/// </summary>
/// <remarks>
///     Used by <c>POST /api/entities/{entityId}/roles</c>. The entity id comes
///     from the route. The service validates that <see cref="RoleType" /> is
///     compatible with the entity's type (e.g., Trustee only on Trust entities).
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record CreateEntityRoleRequest
{
    /// <summary>The contact being assigned (required).</summary>
    [MemoryPackOrder(0)]
    public required Guid ContactId { get; init; }

    /// <summary>Type of role to assign (required).</summary>
    [MemoryPackOrder(1)]
    public required EntityRoleType RoleType { get; init; }

    /// <summary>Display title (e.g., "CEO", "Primary Trustee").</summary>
    [MemoryPackOrder(2)]
    public string? Title { get; init; }

    /// <summary>Ownership percentage for business entities (0–100).</summary>
    [MemoryPackOrder(3)]
    public double? OwnershipPercent { get; init; }

    /// <summary>Beneficial interest percentage for trust entities (0–100).</summary>
    [MemoryPackOrder(4)]
    public double? BeneficialInterestPercent { get; init; }

    /// <summary>Whether this person can sign on behalf of the entity.</summary>
    [MemoryPackOrder(5)]
    public bool IsSignatory { get; init; }

    /// <summary>Whether this is the primary role holder (e.g., primary trustee).</summary>
    [MemoryPackOrder(6)]
    public bool IsPrimary { get; init; }

    /// <summary>Sort order for succession planning or display ordering.</summary>
    [MemoryPackOrder(7)]
    public int SortOrder { get; init; }

    /// <summary>Date when this role became effective.</summary>
    [MemoryPackOrder(8)]
    public DateTime? EffectiveDate { get; init; }

    /// <summary>Date when this role ends (null if ongoing).</summary>
    [MemoryPackOrder(9)]
    public DateTime? EndDate { get; init; }

    /// <summary>Optional notes about this role assignment.</summary>
    [MemoryPackOrder(10)]
    public string? Notes { get; init; }
}
