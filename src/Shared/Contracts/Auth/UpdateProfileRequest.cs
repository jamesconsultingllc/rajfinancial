// ============================================================================
// RAJ Financial - Update Profile Request DTO
// ============================================================================
// Request contract for PUT /api/profile/me — updates the authenticated user's
// editable profile fields (display name, locale, timezone, currency).
// ============================================================================

using MemoryPack;

namespace RajFinancial.Shared.Contracts.Auth;

/// <summary>
///     Request DTO for the PUT /api/profile/me endpoint.
/// </summary>
/// <remarks>
///     <para>
///         Only user-editable fields are exposed. Fields like email and role
///         are synced from Entra claims and cannot be changed via this endpoint.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     {
///         "displayName": "Jane Advisor",
///         "locale": "en-US",
///         "timezone": "America/New_York",
///         "currency": "USD"
///     }
///     </code>
/// </example>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public sealed partial record UpdateProfileRequest
{
    /// <summary>
    ///     The user's display name (max 200 characters).
    /// </summary>
    [MemoryPackOrder(0)]
    public required string DisplayName { get; init; }

    /// <summary>
    ///     ISO locale code (e.g., "en-US", "es-MX").
    /// </summary>
    [MemoryPackOrder(1)]
    public required string Locale { get; init; }

    /// <summary>
    ///     IANA timezone identifier (e.g., "America/New_York").
    /// </summary>
    [MemoryPackOrder(2)]
    public required string Timezone { get; init; }

    /// <summary>
    ///     ISO 4217 currency code (e.g., "USD", "EUR").
    /// </summary>
    [MemoryPackOrder(3)]
    public required string Currency { get; init; }
}
