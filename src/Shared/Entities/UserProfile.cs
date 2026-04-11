// ============================================================================
// RAJ Financial - User Profile Entity
// ============================================================================
// Represents application-specific user data stored in the database.
// Authentication is handled by Microsoft Entra External ID (CIAM) which provides
// ClaimsPrincipal at runtime. This entity stores persistent app data not in Entra.
// ============================================================================

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Represents application-specific user data stored in the database.
/// </summary>
/// <remarks>
///     <para>
///         This is NOT the authentication user - authentication is handled by Microsoft Entra
///         External ID which provides <see cref="System.Security.Claims.ClaimsPrincipal" /> at runtime.
///     </para>
///     <para>
///         This entity stores persistent application data that is not available in Entra claims,
///         such as advisor relationships, preferences, and profile completion status.
///     </para>
/// </remarks>
public class UserProfile
{
    /// <summary>
    ///     Unique identifier for the user profile (matches Entra Object ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     The user's email address (synced from Entra claims).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    ///     The user's display name (synced from Entra claims).
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    ///     The user's first name (synced from Entra claims).
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    ///     The user's last name (synced from Entra claims).
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    ///     The user's role in the system (Administrator, Advisor, Client).
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Client;

    /// <summary>
    ///     The tenant ID this user belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Phone number for the user.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    ///     Whether the user has completed their profile setup.
    /// </summary>
    public bool IsProfileComplete { get; set; }

    /// <summary>
    ///     Whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Date and time the user profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Date and time the user profile was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Date and time of the user's last login.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    ///     User preferences stored as JSON (theme, notifications, etc.).
    /// </summary>
    public string? PreferencesJson { get; set; }
}