using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Services.UserProfiles;

/// <summary>
/// Manages local <see cref="UserProfile"/> shadow records that mirror
/// Microsoft Entra External ID users in the application database.
/// </summary>
/// <remarks>
/// <para>
/// <b>JIT Provisioning:</b> On first authenticated request, a local
/// <see cref="UserProfile"/> is created with the user's Entra Object ID
/// as the primary key. On subsequent requests, mutable claims (email, display name,
/// role) are synced and <c>LastLoginAt</c> is stamped.
/// </para>
/// <para>
/// <b>Role Priority:</b> When multiple role claims are present, the highest-priority
/// role wins: Administrator (0) &gt; Advisor (1) &gt; Client (2).
/// </para>
/// </remarks>
public class UserProfileService(
    ApplicationDbContext dbContext,
    ILogger<UserProfileService> logger) : IUserProfileService
{
    /// <inheritdoc/>
    public async Task<UserProfile> EnsureProfileExistsAsync(
        Guid userId,
        string email,
        string? displayName,
        IReadOnlyList<string> roles,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.UserProfiles.FindAsync([userId], cancellationToken);
        var mappedRole = MapHighestPriorityRole(roles);
        var now = DateTimeOffset.UtcNow;

        if (profile is null)
        {
            // JIT provisioning — first authenticated request
            profile = new UserProfile
            {
                Id = userId,
                Email = email,
                DisplayName = displayName ?? string.Empty,
                Role = mappedRole,
                TenantId = tenantId ?? Guid.Empty,
                IsActive = true,
                CreatedAt = now,
                LastLoginAt = now
            };

            dbContext.UserProfiles.Add(profile);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "JIT provisioned UserProfile for {UserId} ({Email}) with role {Role}",
                userId, email, mappedRole);

            return profile;
        }

        // Returning user — sync mutable claim data
        var changed = false;

        if (!string.Equals(profile.Email, email, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Syncing email for {UserId}: {OldEmail} → {NewEmail}",
                userId, profile.Email, email);
            profile.Email = email;
            changed = true;
        }

        var resolvedDisplayName = displayName ?? string.Empty;
        if (!string.Equals(profile.DisplayName, resolvedDisplayName, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Syncing display name for {UserId}: {OldName} → {NewName}",
                userId, profile.DisplayName, resolvedDisplayName);
            profile.DisplayName = resolvedDisplayName;
            changed = true;
        }

        if (profile.Role != mappedRole)
        {
            logger.LogInformation(
                "Syncing role for {UserId}: {OldRole} → {NewRole}",
                userId, profile.Role, mappedRole);
            profile.Role = mappedRole;
            changed = true;
        }

        // Always stamp LastLoginAt
        profile.LastLoginAt = now;

        if (changed)
        {
            profile.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return profile;
    }

    /// <inheritdoc/>
    public async Task<UserProfile?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserProfiles.FindAsync([userId], cancellationToken);
    }

    /// <summary>
    /// Maps a list of role claim strings to the single highest-priority
    /// <see cref="UserRole"/>. Priority: Administrator (0) &gt; Advisor (1) &gt; Client (2).
    /// Defaults to <see cref="UserRole.Client"/> when no recognized roles are present.
    /// </summary>
    private static UserRole MapHighestPriorityRole(IReadOnlyList<string> roles)
    {
        // Lower enum value = higher priority
        var bestRole = UserRole.Client;

        foreach (var role in roles)
        {
            if (Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed) && parsed < bestRole)
            {
                bestRole = parsed;
            }
        }

        return bestRole;
    }
}
