using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Shared.Contracts.Auth;
using RajFinancial.Shared.Entities.Users;

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
                CreatedAt = now,
                LastLoginAt = now
            };

            try
            {
                dbContext.UserProfiles.Add(profile);
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "JIT provisioned UserProfile for {UserId} with role {Role}",
                    userId, mappedRole);

                return profile;
            }
            catch (DbUpdateException)
            {
                // Concurrent request already created this profile — detach and reload
                logger.LogInformation(
                    "Concurrent JIT provisioning detected for {UserId}; reloading existing profile",
                    userId);

                dbContext.Entry(profile).State = EntityState.Detached;
                profile = await dbContext.UserProfiles.FindAsync([userId], cancellationToken);

                if (profile is null)
                {
                    throw; // Re-throw if reload also fails — something is truly wrong
                }
            }
        }

        // Returning user — sync mutable claim data
        var changed = false;

        if (!string.Equals(profile.Email, email, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Syncing email for {UserId}",
                userId);
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

        // Always stamp LastLoginAt on every authenticated request
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

    /// <inheritdoc/>
    public async Task<UserProfile?> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.UserProfiles.FindAsync([userId], cancellationToken);

        if (profile is null)
        {
            logger.LogWarning("UpdateProfile: profile not found for user {UserId}", userId);
            return null;
        }

        profile.DisplayName = request.DisplayName;
        profile.PreferencesJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            locale = request.Locale,
            timezone = request.Timezone,
            currency = request.Currency
        });
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated profile for user {UserId} (displayName={DisplayName}, locale={Locale}, timezone={Timezone}, currency={Currency})",
            userId, request.DisplayName, request.Locale, request.Timezone, request.Currency);

        return profile;
    }

    /// <summary>
    /// Maps a list of role claim strings to the single highest-priority
    /// <see cref="UserRole"/>. Priority: Administrator (0) &gt; Advisor (1) &gt; Client (2).
    /// Defaults to <see cref="UserRole.Client"/> when no recognized roles are present.
    /// </summary>
    private static UserRole MapHighestPriorityRole(IReadOnlyList<string> roles)
    {
        // Lower enum value = higher priority
        return roles
            .Select(r => Enum.TryParse<UserRole>(r, ignoreCase: true, out var parsed) ? parsed : (UserRole?)null)
            .Where(r => r.HasValue)
            .Select(r => r!.Value)
            .DefaultIfEmpty(UserRole.Client)
            .Min();
    }
}
