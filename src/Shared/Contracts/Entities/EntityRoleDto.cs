using MemoryPack;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Shared.Contracts.Entities;

/// <summary>
///     Data transfer object for an entity role assignment (contact assigned to a role on an entity).
/// </summary>
/// <remarks>
///     Handles both business org chart positions (Owner, Officer, Director) and
///     trust roles (Grantor, Trustee, Beneficiary).
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record EntityRoleDto
{
    /// <summary>Unique identifier.</summary>
    [MemoryPackOrder(0)]
    public required Guid Id { get; init; }

    /// <summary>The entity this role belongs to.</summary>
    [MemoryPackOrder(1)]
    public required Guid EntityId { get; init; }

    /// <summary>The contact assigned to this role.</summary>
    [MemoryPackOrder(2)]
    public required Guid ContactId { get; init; }

    /// <summary>Type of role (Owner, Trustee, Beneficiary, etc.).</summary>
    [MemoryPackOrder(3)]
    public required EntityRoleType RoleType { get; init; }

    /// <summary>Display title (e.g., "CEO", "Managing Member", "Primary Trustee").</summary>
    [MemoryPackOrder(4)]
    public string? Title { get; init; }

    /// <summary>Ownership percentage for business entities (0–100).</summary>
    [MemoryPackOrder(5)]
    public double? OwnershipPercent { get; init; }

    /// <summary>Beneficial interest percentage for trust entities (0–100).</summary>
    [MemoryPackOrder(6)]
    public double? BeneficialInterestPercent { get; init; }

    /// <summary>Whether this person can sign on behalf of the entity.</summary>
    [MemoryPackOrder(7)]
    public required bool IsSignatory { get; init; }

    /// <summary>Whether this is the primary role holder (e.g., primary trustee).</summary>
    [MemoryPackOrder(8)]
    public required bool IsPrimary { get; init; }

    /// <summary>Sort order for succession planning or display ordering.</summary>
    [MemoryPackOrder(9)]
    public required int SortOrder { get; init; }

    /// <summary>Date when this role became effective.</summary>
    [MemoryPackOrder(10)]
    public DateTime? EffectiveDate { get; init; }

    /// <summary>Date when this role ended (null if still active).</summary>
    [MemoryPackOrder(11)]
    public DateTime? EndDate { get; init; }

    /// <summary>Optional notes about this role assignment.</summary>
    [MemoryPackOrder(12)]
    public string? Notes { get; init; }
}
