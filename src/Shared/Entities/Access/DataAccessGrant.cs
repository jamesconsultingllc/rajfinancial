// ============================================================================
// RAJ Financial - Data Access Grant Entity
// ============================================================================
// Represents a grant of access from one user (Grantor) to another user (Grantee).
// This enables secure data sharing between users (e.g., spouse, attorney, CPA).
// ============================================================================

namespace RajFinancial.Shared.Entities.Access;

/// <summary>
///     Represents a grant of access from one user (Grantor) to another user (Grantee).
///     Enables secure data sharing between family members, professionals, etc.
/// </summary>
public class DataAccessGrant
{
    /// <summary>
    ///     Unique identifier for this grant.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     The user who owns the data and is granting access.
    /// </summary>
    public Guid GrantorUserId { get; set; }

    /// <summary>
    ///     The user receiving access to the data.
    ///     Null until the invitation is accepted.
    /// </summary>
    public Guid? GranteeUserId { get; set; }

    /// <summary>
    ///     The email address used to invite the grantee.
    ///     Used to match the grant when the invitee registers or logs in.
    /// </summary>
    public string GranteeEmail { get; set; } = string.Empty;

    /// <summary>
    ///     Type of access granted (Owner, Full, Read, Limited).
    /// </summary>
    public AccessType AccessType { get; set; }

    /// <summary>
    ///     Data categories accessible when AccessType is Limited.
    ///     Examples: "accounts", "assets", "beneficiaries", "documents"
    /// </summary>
    public List<string> Categories { get; set; } = [];

    /// <summary>
    ///     Optional label describing the relationship.
    ///     Examples: "Spouse", "Financial Advisor", "Estate Attorney", "CPA"
    /// </summary>
    public string? RelationshipLabel { get; set; }

    /// <summary>
    ///     Secure token for the invitation link.
    /// </summary>
    public string? InvitationToken { get; set; }

    /// <summary>
    ///     When the invitation token expires.
    /// </summary>
    public DateTimeOffset? InvitationExpiresAt { get; set; }

    /// <summary>
    ///     When the grant was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    ///     When the grantee accepted the invitation.
    ///     Null if still pending.
    /// </summary>
    public DateTimeOffset? AcceptedAt { get; set; }

    /// <summary>
    ///     When the grant expires.
    ///     Null for no expiration (permanent until revoked).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    ///     When the grant was revoked.
    ///     Null if still active.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    ///     Current status of the grant.
    /// </summary>
    public GrantStatus Status { get; set; }

    /// <summary>
    ///     Optional notes from the grantor about this access grant.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    ///     When the record was last modified.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}