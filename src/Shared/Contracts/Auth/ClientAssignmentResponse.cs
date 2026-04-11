// ============================================================================
// RAJ Financial - Client Assignment Response DTO
// ============================================================================
// Response contract for client management endpoints:
//   - POST /api/auth/clients (201 Created — single assignment)
//   - GET /api/auth/clients  (200 OK — list of assignments)
// ============================================================================

using MemoryPack;

namespace RajFinancial.Shared.Contracts.Auth;

/// <summary>
///     Response DTO representing a client assignment (DataAccessGrant).
/// </summary>
/// <remarks>
///     <para>
///         Used as the response body for POST /api/auth/clients (single item)
///         and as array elements for GET /api/auth/clients (list).
///     </para>
///     <para>
///         Exposes only the fields needed by the UI — sensitive internal details
///         like invitation tokens are excluded.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // 201 Created response (POST /api/auth/clients)
///     {
///         "grantId": "aaa00000-0000-0000-0000-000000000001",
///         "clientEmail": "client@example.com",
///         "accessType": "Read",
///         "categories": ["accounts", "investments"],
///         "relationshipLabel": "Primary Advisor",
///         "status": "Pending",
///         "createdAt": "2026-02-16T12:00:00Z"
///     }
///     </code>
/// </example>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record ClientAssignmentResponse
{
    /// <summary>
    ///     Unique identifier of the data access grant.
    /// </summary>
    [MemoryPackOrder(0)]
    public required Guid GrantId { get; init; }

    /// <summary>
    ///     The email address of the assigned client.
    /// </summary>
    [MemoryPackOrder(1)]
    public required string ClientEmail { get; init; }

    /// <summary>
    ///     The type of access granted (Full, Read, or Limited).
    ///     Owner access is implicit for data owners and is not assigned via these endpoints.
    /// </summary>
    [MemoryPackOrder(2)]
    public required string AccessType { get; init; }

    /// <summary>
    ///     Data categories accessible under this grant.
    /// </summary>
    [MemoryPackOrder(3)]
    public required string[] Categories { get; init; }

    /// <summary>
    ///     Optional label describing the relationship (e.g., "Primary Advisor").
    /// </summary>
    [MemoryPackOrder(4)]
    public string? RelationshipLabel { get; init; }

    /// <summary>
    ///     Current status of the grant (Pending, Active, Expired, or Revoked).
    /// </summary>
    [MemoryPackOrder(5)]
    public required string Status { get; init; }

    /// <summary>
    ///     When the grant was created.
    /// </summary>
    [MemoryPackOrder(6)]
    public required DateTime CreatedAt { get; init; }
}
