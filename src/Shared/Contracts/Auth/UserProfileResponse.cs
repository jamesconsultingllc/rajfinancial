// ============================================================================
// RAJ Financial - User Profile Response DTO
// ============================================================================
// Response contract for GET /api/auth/me — returns the authenticated user's
// profile information derived from Entra claims and local UserProfile data.
// ============================================================================

using MemoryPack;

namespace RajFinancial.Shared.Contracts.Auth;

/// <summary>
///     Response DTO for the GET /api/auth/me endpoint.
/// </summary>
/// <remarks>
///     <para>
///         Combines data from the authenticated user's Entra claims (via middleware)
///         with persistent application data from the local <c>UserProfile</c> entity.
///     </para>
///     <para>
///         If the user has no local profile, JIT provisioning creates one automatically
///         on the first request to this endpoint.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // 200 OK response
///     {
///         "userId": "550e8400-e29b-41d4-a716-446655440000",
///         "email": "advisor@rajfinancial.com",
///         "displayName": "Jane Advisor",
///         "role": "Advisor",
///         "isProfileComplete": true,
///         "isAdministrator": false
///     }
///     </code>
/// </example>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record UserProfileResponse
{
    /// <summary>
    ///     The user's unique identifier (matches Entra Object ID).
    /// </summary>
    [MemoryPackOrder(0)]
    public required string UserId { get; init; }

    /// <summary>
    ///     The user's email address.
    /// </summary>
    [MemoryPackOrder(1)]
    public required string Email { get; init; }

    /// <summary>
    ///     The user's display name.
    /// </summary>
    [MemoryPackOrder(2)]
    public required string DisplayName { get; init; }

    /// <summary>
    ///     The user's primary role in the system (Administrator, Advisor, or Client).
    /// </summary>
    [MemoryPackOrder(3)]
    public required string Role { get; init; }

    /// <summary>
    ///     Whether the user has completed their profile setup.
    /// </summary>
    [MemoryPackOrder(4)]
    public required bool IsProfileComplete { get; init; }

    /// <summary>
    ///     Whether the user has the Administrator role.
    /// </summary>
    [MemoryPackOrder(5)]
    public required bool IsAdministrator { get; init; }
}
