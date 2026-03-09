// ============================================================================
// RAJ Financial - User Roles Response DTO
// ============================================================================
// Response contract for GET /api/auth/roles — returns the authenticated user's
// role assignments for client-side access control decisions.
// ============================================================================

using MemoryPack;

namespace RajFinancial.Shared.Contracts.Auth;

/// <summary>
///     Response DTO for the GET /api/auth/roles endpoint.
/// </summary>
/// <remarks>
///     Returns all roles assigned to the authenticated user, plus a convenience
///     flag indicating administrator status. Used by the Blazor WASM client to
///     enforce UI-level access control (hiding unauthorized features).
/// </remarks>
/// <example>
///     <code>
///     // 200 OK response
///     {
///         "roles": ["Advisor"],
///         "isAdministrator": false
///     }
///     </code>
/// </example>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record UserRolesResponse
{
    /// <summary>
    ///     All roles assigned to the current user.
    /// </summary>
    /// <remarks>
    ///     Roles are returned as string values matching the <c>UserRole</c> enum names:
    ///     "Administrator", "Advisor", "Client".
    /// </remarks>
    [MemoryPackOrder(0)]
    public required string[] Roles { get; init; }

    /// <summary>
    ///     Convenience flag indicating whether the user has the Administrator role.
    /// </summary>
    [MemoryPackOrder(1)]
    public required bool IsAdministrator { get; init; }
}
