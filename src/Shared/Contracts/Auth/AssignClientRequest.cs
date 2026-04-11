// ============================================================================
// RAJ Financial - Assign Client Request DTO
// ============================================================================
// Request contract for POST /api/auth/clients — allows an Advisor or
// Administrator to assign a client by email with specified access parameters.
// ============================================================================

using MemoryPack;

namespace RajFinancial.Shared.Contracts.Auth;

/// <summary>
///     Request DTO for the POST /api/auth/clients endpoint.
/// </summary>
/// <remarks>
///     <para>
///         Only users with the Advisor or Administrator role may submit this request.
///         Advisors create grants where they are the grantor; Administrators can create
///         grants on behalf of any advisor.
///     </para>
///     <para>
///         Self-assignment (assigning your own email) is rejected with
///         error code <c>SELF_ASSIGNMENT_NOT_ALLOWED</c>.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // POST /api/auth/clients
///     {
///         "clientEmail": "client@example.com",
///         "accessType": "Read",
///         "categories": ["accounts", "investments"],
///         "relationshipLabel": "Primary Advisor"
///     }
///     </code>
/// </example>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record AssignClientRequest
{
    /// <summary>
    ///     The email address of the client to assign.
    /// </summary>
    /// <remarks>
    ///     Used to match the client when they register or log in.
    ///     Must be a valid email address format.
    /// </remarks>
    [MemoryPackOrder(0)]
    public required string ClientEmail { get; init; }

    /// <summary>
    ///     The type of access to grant (Full, Read, or Limited).
    /// </summary>
    /// <remarks>
    ///     Must match a valid <c>AccessType</c> enum value as a string.
    /// </remarks>
    [MemoryPackOrder(1)]
    public required string AccessType { get; init; }

    /// <summary>
    ///     Data categories the client can access.
    /// </summary>
    /// <remarks>
    ///     Must contain at least one category. Valid categories include:
    ///     "accounts", "assets", "beneficiaries", "documents", "investments".
    /// </remarks>
    [MemoryPackOrder(2)]
    public required string[] Categories { get; init; }

    /// <summary>
    ///     Optional label describing the advisor-client relationship.
    /// </summary>
    /// <example>"Primary Advisor", "Estate Attorney", "CPA"</example>
    [MemoryPackOrder(3)]
    public string? RelationshipLabel { get; init; }
}
