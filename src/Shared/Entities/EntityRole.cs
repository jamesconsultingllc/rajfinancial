using MemoryPack;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Assigns a contact to a role on an entity. Handles both business org chart
///     positions (Owner, Officer, Director) and trust roles (Grantor, Trustee, Beneficiary).
/// </summary>
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class EntityRole
{
    /// <summary>Unique identifier.</summary>
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }

    /// <summary>The entity this role belongs to.</summary>
    [MemoryPackOrder(1)]
    public Guid EntityId { get; set; }

    /// <summary>The contact assigned to this role.</summary>
    [MemoryPackOrder(2)]
    public Guid ContactId { get; set; }

    /// <summary>Type of role (Owner, Trustee, Beneficiary, etc.).</summary>
    [MemoryPackOrder(3)]
    public EntityRoleType RoleType { get; set; }

    /// <summary>Display title (e.g., "CEO", "Managing Member", "Primary Trustee").</summary>
    [MemoryPackOrder(4)]
    public string? Title { get; set; }

    /// <summary>Ownership percentage for business entities (0–100).</summary>
    [MemoryPackOrder(5)]
    public decimal? OwnershipPercent { get; set; }

    /// <summary>Beneficial interest percentage for trust entities (0–100).</summary>
    [MemoryPackOrder(6)]
    public decimal? BeneficialInterestPercent { get; set; }

    /// <summary>Whether this person can sign on behalf of the entity.</summary>
    [MemoryPackOrder(7)]
    public bool IsSignatory { get; set; }

    /// <summary>Whether this is the primary role holder (e.g., primary trustee).</summary>
    [MemoryPackOrder(8)]
    public bool IsPrimary { get; set; }

    /// <summary>Sort order for succession planning or display ordering.</summary>
    [MemoryPackOrder(9)]
    public int SortOrder { get; set; }

    /// <summary>Date when this role became effective.</summary>
    [MemoryPackOrder(10)]
    public DateTimeOffset? EffectiveDate { get; set; }

    /// <summary>Date when this role ended (null if still active).</summary>
    [MemoryPackOrder(11)]
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>Optional notes about this role assignment.</summary>
    [MemoryPackOrder(12)]
    public string? Notes { get; set; }

    // ── Navigation properties ──

    /// <summary>The entity this role belongs to.</summary>
    [MemoryPackIgnore]
    public Entity Entity { get; set; } = null!;
}
