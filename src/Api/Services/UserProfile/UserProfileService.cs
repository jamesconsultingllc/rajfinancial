using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Shared.Contracts.Auth;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Services.UserProfile;

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
public partial class UserProfileService(
    ApplicationDbContext dbContext,
    ILogger<UserProfileService> logger) : IUserProfileService
{
    /// <inheritdoc/>
    public async Task<Shared.Entities.Users.UserProfile> EnsureProfileExistsAsync(
        Guid userId,
        string email,
        string? displayName,
        IReadOnlyList<string> roles,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.UserProfiles.FindAsync([userId], cancellationToken);
        var mappedRole = roles.MapHighestPriority();
        var now = DateTimeOffset.UtcNow;

        if (profile is null)
        {
            // JIT provisioning — first authenticated request
            profile = new Shared.Entities.Users.UserProfile
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

                LogJitProvisioned(userId, mappedRole);

                return profile;
            }
            catch (DbUpdateException)
            {
                // Concurrent request already created this profile — detach and reload
                LogConcurrentJitDetected(userId);

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
            LogSyncingEmail(userId);
            profile.Email = email;
            changed = true;
        }

        var resolvedDisplayName = displayName ?? string.Empty;
        if (!string.Equals(profile.DisplayName, resolvedDisplayName, StringComparison.Ordinal))
        {
            LogSyncingDisplayName(userId, profile.DisplayName, resolvedDisplayName);
            profile.DisplayName = resolvedDisplayName;
            changed = true;
        }

        if (profile.Role != mappedRole)
        {
            LogSyncingRole(userId, profile.Role, mappedRole);
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
    public async Task<Shared.Entities.Users.UserProfile?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserProfiles.FindAsync([userId], cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Shared.Entities.Users.UserProfile?> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.UserProfiles.FindAsync([userId], cancellationToken);

        if (profile is null)
        {
            LogProfileNotFoundForUpdate(userId);
            return null;
        }

        profile.DisplayName = request.DisplayName;
        profile.PreferencesJson = JsonSerializer.Serialize(new
        {
            locale = request.Locale,
            timezone = request.Timezone,
            currency = request.Currency
        });
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        LogProfileUpdated(userId, request.DisplayName, request.Locale, request.Timezone, request.Currency);

        return profile;
    }

    // =========================================================================
    // Source-generated logging (EventId 4000-4999)
    // =========================================================================

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "JIT provisioned UserProfile for {UserId} with role {Role}")]
    private partial void LogJitProvisioned(Guid userId, UserRole role);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "Concurrent JIT provisioning detected for {UserId}; reloading existing profile")]
    private partial void LogConcurrentJitDetected(Guid userId);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Information,
        Message = "Syncing email for {UserId}")]
    private partial void LogSyncingEmail(Guid userId);

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Information,
        Message = "Syncing display name for {UserId}: {OldName} -> {NewName}")]
    private partial void LogSyncingDisplayName(Guid userId, string oldName, string newName);

    [LoggerMessage(
        EventId = 4005,
        Level = LogLevel.Information,
        Message = "Syncing role for {UserId}: {OldRole} -> {NewRole}")]
    private partial void LogSyncingRole(Guid userId, UserRole oldRole, UserRole newRole);

    [LoggerMessage(
        EventId = 4010,
        Level = LogLevel.Warning,
        Message = "UpdateProfile: profile not found for user {UserId}")]
    private partial void LogProfileNotFoundForUpdate(Guid userId);

    [LoggerMessage(
        EventId = 4011,
        Level = LogLevel.Information,
        Message = "Updated profile for user {UserId} (displayName={DisplayName}, locale={Locale}, timezone={Timezone}, currency={Currency})")]
    private partial void LogProfileUpdated(Guid userId, string displayName, string? locale, string? timezone, string? currency);
}
