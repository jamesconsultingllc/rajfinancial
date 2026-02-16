using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Services.UserProfiles;

/// <summary>
/// Manages local <see cref="UserProfile"/> shadow records that mirror
/// Microsoft Entra External ID users in the application database.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Entra External ID handles authentication and provides claims at runtime.
/// This service maintains a local database record for each authenticated user so that
/// application entities (assets, accounts, beneficiaries) can hold foreign-key references
/// to <see cref="UserProfile.Id"/> (the Entra Object ID).
/// </para>
/// <para>
/// <b>JIT Provisioning:</b> The primary method <see cref="EnsureProfileExistsAsync"/> is called
/// by <c>UserProfileProvisioningMiddleware</c> on every authenticated request.
/// On first login it creates the local record; on subsequent requests it syncs
/// mutable claim data (email, display name, roles) and stamps <c>LastLoginAt</c>.
/// </para>
/// </remarks>
public interface IUserProfileService
{
    /// <summary>
    /// Ensures a local <see cref="UserProfile"/> exists for the authenticated user.
    /// Creates the record on first access (JIT provisioning) or updates claim-sourced
    /// fields if they have changed since the last request.
    /// </summary>
    /// <param name="userId">
    /// The Entra Object ID (<c>oid</c> claim) — used as the primary key.
    /// </param>
    /// <param name="email">
    /// The user's email address from the <c>emails</c> / <c>email</c> claim.
    /// </param>
    /// <param name="displayName">
    /// The user's display name from the <c>name</c> claim. May be <c>null</c>.
    /// </param>
    /// <param name="roles">
    /// App role values from the <c>roles</c> claim (e.g., "Administrator", "Advisor").
    /// The highest-priority role is mapped to <see cref="UserRole"/>.
    /// </param>
    /// <param name="tenantId">
    /// Optional Entra tenant ID (<c>tid</c> claim). Falls back to a well-known
    /// default tenant when not supplied.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing or newly created <see cref="UserProfile"/>.</returns>
    Task<UserProfile> EnsureProfileExistsAsync(
        Guid userId,
        string email,
        string? displayName,
        IReadOnlyList<string> roles,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a <see cref="UserProfile"/> by its primary key (Entra Object ID).
    /// </summary>
    /// <param name="userId">The Entra Object ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile, or <c>null</c> if none exists.</returns>
    Task<UserProfile?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
