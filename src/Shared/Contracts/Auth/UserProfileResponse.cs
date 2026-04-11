// ============================================================================
// RAJ Financial - User Profile Response DTO
// ============================================================================
// Response contract for GET/PUT /api/profile/me — returns the authenticated
// user's editable profile settings. Auth-concern fields (email, role) are
// available from Entra claims via useAuth() and are not duplicated here.
// ============================================================================

using MemoryPack;

namespace RajFinancial.Shared.Contracts.Auth;

/// <summary>
///     Response DTO for the /api/profile/me endpoints (GET and PUT).
/// </summary>
/// <remarks>
///     Contains only user-editable profile settings and timestamps.
///     Auth identity fields (email, role, isAdmin) come from Entra claims.
/// </remarks>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record UserProfileResponse
{
    /// <summary>The user's unique identifier (Entra Object ID).</summary>
    [MemoryPackOrder(0)]
    public required string UserId { get; init; }

    /// <summary>The user's display name.</summary>
    [MemoryPackOrder(1)]
    public required string DisplayName { get; init; }

    /// <summary>ISO locale code (e.g., "en-US").</summary>
    [MemoryPackOrder(2)]
    public required string Locale { get; init; }

    /// <summary>IANA timezone (e.g., "America/New_York").</summary>
    [MemoryPackOrder(3)]
    public required string Timezone { get; init; }

    /// <summary>ISO 4217 currency code (e.g., "USD").</summary>
    [MemoryPackOrder(4)]
    public required string Currency { get; init; }

    /// <summary>When the user profile was created (member since).</summary>
    [MemoryPackOrder(5)]
    public required DateTime CreatedAt { get; init; }
}
