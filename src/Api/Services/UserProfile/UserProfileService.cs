using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Api.Observability;
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
        using var activity = UserProfileTelemetry.ActivitySource.StartActivity(UserProfileTelemetry.ActivityEnsureProfileExists);
        activity?.SetTag(UserProfileTelemetry.TagUserId, userId);
        if (tenantId.HasValue)
        {
            activity?.SetTag(UserProfileTelemetry.TagUserTenantId, tenantId.Value);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var profile = await dbContext.UserProfiles.FindAsync([userId], cancellationToken);
            var mappedRole = roles.MapHighestPriority();
            var now = DateTimeOffset.UtcNow;

            var created = false;
            if (profile is null)
            {
                (profile, created) = await JitProvisionAsync(userId, email, displayName, mappedRole, tenantId, now, cancellationToken);
            }

            if (!created)
            {
                await SyncClaimsAsync(profile, userId, email, displayName, mappedRole, now, cancellationToken);
            }

            UserProfileTelemetry.SyncCount.Add(1);
            return profile;
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            UserProfileTelemetry.EnsureDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private async Task<(Shared.Entities.Users.UserProfile Profile, bool Created)> JitProvisionAsync(
        Guid userId,
        string email,
        string? displayName,
        UserRole mappedRole,
        Guid? tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var profile = new Shared.Entities.Users.UserProfile
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

            UserProfileTelemetry.JitProvisioned.Add(1);
            LogJitProvisioned(userId, mappedRole);

            return (profile, true);
        }
        catch (DbUpdateException)
        {
            UserProfileTelemetry.ConcurrentConflicts.Add(1);
            LogConcurrentJitDetected(userId);

            dbContext.Entry(profile).State = EntityState.Detached;
            var reloaded = await dbContext.UserProfiles.FindAsync([userId], cancellationToken);

            if (reloaded is null)
            {
                throw;
            }

            return (reloaded, false);
        }
    }

    private async Task SyncClaimsAsync(
        Shared.Entities.Users.UserProfile profile,
        Guid userId,
        string email,
        string? displayName,
        UserRole mappedRole,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
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

        profile.LastLoginAt = now;

        if (changed)
        {
            profile.UpdatedAt = now;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            UserProfileTelemetry.ConcurrentConflicts.Add(1);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Shared.Entities.Users.UserProfile?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var activity = UserProfileTelemetry.ActivitySource.StartActivity(UserProfileTelemetry.ActivityGetById);
        activity?.SetTag(UserProfileTelemetry.TagUserId, userId);

        try
        {
            return await dbContext.UserProfiles.FindAsync([userId], cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Shared.Entities.Users.UserProfile?> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = UserProfileTelemetry.ActivitySource.StartActivity(UserProfileTelemetry.ActivityUpdateProfile);
        activity?.SetTag(UserProfileTelemetry.TagUserId, userId);

        try
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

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                UserProfileTelemetry.ConcurrentConflicts.Add(1);
                throw;
            }

            LogProfileUpdated(userId, request.DisplayName, request.Locale, request.Timezone, request.Currency);

            return profile;
        }
        catch (Exception ex)
        {
            activity?.RecordExceptionOutcome(ex);
            throw;
        }
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
