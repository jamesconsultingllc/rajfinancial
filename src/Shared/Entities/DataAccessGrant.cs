// ============================================================================
// RAJ Financial - Data Access Grant Entity
// ============================================================================
// Represents a grant of access from one user (Grantor) to another user (Grantee).
// This enables secure data sharing between users (e.g., spouse, attorney, CPA).
// ============================================================================

using MemoryPack;

namespace RajFinancial.Shared.Entities;

/// <summary>
/// Represents a grant of access from one user (Grantor) to another user (Grantee).
/// Enables secure data sharing between family members, professionals, etc.
/// </summary>
[MemoryPackable]
public partial class DataAccessGrant
{
    /// <summary>
    /// Unique identifier for this grant.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user who owns the data and is granting access.
    /// </summary>
    public Guid GrantorUserId { get; set; }

    /// <summary>
    /// The user receiving access to the data.
    /// Null until the invitation is accepted.
    /// </summary>
    public Guid? GranteeUserId { get; set; }

    /// <summary>
    /// The email address used to invite the grantee.
    /// Used to match the grant when the invitee registers or logs in.
    /// </summary>
    public string GranteeEmail { get; set; } = string.Empty;

    /// <summary>
    /// Type of access granted (Owner, Full, Read, Limited).
    /// </summary>
    public AccessType AccessType { get; set; }

    /// <summary>
    /// Data categories accessible when AccessType is Limited.
    /// Examples: "accounts", "assets", "beneficiaries", "documents"
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// Optional label describing the relationship.
    /// Examples: "Spouse", "Financial Advisor", "Estate Attorney", "CPA"
    /// </summary>
    public string? RelationshipLabel { get; set; }

    /// <summary>
    /// Secure token for the invitation link.
    /// </summary>
    public string? InvitationToken { get; set; }

    /// <summary>
    /// When the invitation token expires.
    /// </summary>
    public DateTime? InvitationExpiresAt { get; set; }

    /// <summary>
    /// When the grant was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the grantee accepted the invitation.
    /// Null if still pending.
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// When the grant expires.
    /// Null for no expiration (permanent until revoked).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the grant was revoked.
    /// Null if still active.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Current status of the grant.
    /// </summary>
    public GrantStatus Status { get; set; }

    /// <summary>
    /// Optional notes from the grantor about this access grant.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When the record was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Type of access granted to a user.
/// </summary>
public enum AccessType
{
    /// <summary>
    /// Full control - the data owner. Cannot be granted, only implicit.
    /// </summary>
    Owner = 0,

    /// <summary>
    /// Full access - read and write, but cannot delete account or manage shares.
    /// Suitable for spouse or trusted family member.
    /// </summary>
    Full = 1,

    /// <summary>
    /// Read-only access - can view but not modify any data.
    /// Suitable for professionals reviewing information.
    /// </summary>
    Read = 2,

    /// <summary>
    /// Limited access - read-only for specific data categories only.
    /// Suitable for CPA (financial only) or attorney (estate only).
    /// </summary>
    Limited = 3
}

/// <summary>
/// Status of a data access grant.
/// </summary>
public enum GrantStatus
{
    /// <summary>
    /// Invitation sent but not yet accepted.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Grant is active and grantee can access data.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Grant has expired based on ExpiresAt date.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Grant was revoked by the grantor.
    /// </summary>
    Revoked = 3
}

/// <summary>
/// Data categories that can be granted access to.
/// </summary>
public static class DataCategories
{
    public const string Accounts = "accounts";
    public const string Assets = "assets";
    public const string Liabilities = "liabilities";
    public const string Beneficiaries = "beneficiaries";
    public const string Documents = "documents";
    public const string Analysis = "analysis";
    public const string All = "all";

    public static readonly string[] AllCategories = new[]
    {
        Accounts,
        Assets,
        Liabilities,
        Beneficiaries,
        Documents,
        Analysis
    };
}

