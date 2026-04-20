using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Shared.Entities.Access;
using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Services.Authorization;

/// <summary>
/// Concrete implementation of <see cref="IAuthorizationService"/> that evaluates
/// a three-tier resource-level authorization check against the database.
/// </summary>
/// <remarks>
/// <para>
/// <b>Authorization Flow (checked in order):</b>
/// <list type="number">
///   <item><b>Tier 1 — Resource Owner:</b> <c>requestingUserId == resourceOwnerId</c>
///         → grant with <see cref="AccessType.Owner"/>.</item>
///   <item><b>Tier 2 — Data Access Grant:</b> Query <see cref="DataAccessGrant"/> table
///         for an active, non-expired grant from <c>resourceOwnerId</c> to <c>requestingUserId</c>
///         covering the requested category and access level.</item>
///   <item><b>Tier 3 — Administrator:</b> Query <see cref="UserProfile"/> table to check
///         if the requesting user has <see cref="UserRole.Administrator"/>
///         → grant with <see cref="AccessType.Full"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>OWASP A01:2025 — Broken Access Control:</b> Prevents IDOR by requiring explicit
/// ownership, grant, or admin role before any data access is permitted.
/// </para>
/// </remarks>
// ReSharper disable once ClassNeverInstantiated.Global
public partial class AuthorizationService(
    ApplicationDbContext dbContext,
    ILogger<AuthorizationService> logger) : IAuthorizationService
{
    private static readonly ActivitySource ActivitySource = new("RajFinancial.Api.Authorization");
    private static readonly Meter Meter = new("RajFinancial.Api.Authorization");

    private static readonly Counter<long> AuthzAllowed =
        Meter.CreateCounter<long>("authorization.allowed.count");

    private static readonly Counter<long> AuthzDenied =
        Meter.CreateCounter<long>("authorization.denied.count");

    private static readonly Histogram<double> AuthzCheckDuration =
        Meter.CreateHistogram<double>("authorization.check.duration.ms");

    // Authorization tier tag values — emitted on activities and counters as `authz.tier`.
    private const string TierOwner = "owner";
    private const string TierGrant = "grant";
    private const string TierAdmin = "admin";
    private const string TierDenied = "denied";

    /// <inheritdoc />
    public async Task<AccessDecision> CheckAccessAsync(
        Guid requestingUserId,
        Guid resourceOwnerId,
        string category,
        AccessType requiredLevel)
    {
        // Owner access type is implicit (Tier 1 only) and cannot be requested directly
        if (requiredLevel == AccessType.Owner)
            throw new ArgumentException(
                "AccessType.Owner cannot be used as a required level. Owner access is implicit via Tier 1 (resource ownership).",
                nameof(requiredLevel));

        using var activity = ActivitySource.StartActivity("Authorization.CheckAccess");
        activity?.SetTag("user.id", requestingUserId);
        activity?.SetTag("resource.type", category);
        activity?.SetTag("resource.id", resourceOwnerId);
        activity?.SetTag("resource.owner.id", resourceOwnerId);
        activity?.SetTag("authz.required_level", requiredLevel.ToString());

        var sw = Stopwatch.StartNew();
        try
        {
            // =====================================================================
            // Tier 1: Resource Owner — immediate grant, no DB query needed
            // =====================================================================
            if (requestingUserId == resourceOwnerId)
            {
                activity?.SetTag("authz.tier", TierOwner);
                AuthzAllowed.Add(1,
                    new KeyValuePair<string, object?>("authz.tier", TierOwner),
                    new KeyValuePair<string, object?>("authz.reason", AccessDecisionReason.ResourceOwner.ToString()));
                LogAccessGrantedOwner(requestingUserId);
                return AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Owner);
            }

            // =====================================================================
            // Tier 2: Data Access Grant — query for a valid grant
            // =====================================================================
            var grant = await FindValidGrantAsync(requestingUserId, resourceOwnerId, category, requiredLevel);

            if (grant is not null)
            {
                activity?.SetTag("authz.tier", TierGrant);
                activity?.SetTag("authz.granted_level", grant.AccessType.ToString());
                AuthzAllowed.Add(1,
                    new KeyValuePair<string, object?>("authz.tier", TierGrant),
                    new KeyValuePair<string, object?>("authz.reason", AccessDecisionReason.DataAccessGrant.ToString()));
                LogAccessGrantedGrant(requestingUserId, grant.AccessType, resourceOwnerId);
                return AccessDecision.Grant(AccessDecisionReason.DataAccessGrant, grant.AccessType);
            }

            // =====================================================================
            // Tier 3: Administrator — check user role
            // =====================================================================
            var isAdmin = await dbContext.UserProfiles
                .AsNoTracking()
                .AnyAsync(u => u.Id == requestingUserId && u.Role == UserRole.Administrator);

            if (isAdmin)
            {
                activity?.SetTag("authz.tier", TierAdmin);
                AuthzAllowed.Add(1,
                    new KeyValuePair<string, object?>("authz.tier", TierAdmin),
                    new KeyValuePair<string, object?>("authz.reason", AccessDecisionReason.Administrator.ToString()));
                LogAccessGrantedAdmin(requestingUserId, resourceOwnerId);
                return AccessDecision.Grant(AccessDecisionReason.Administrator, AccessType.Full);
            }

            // =====================================================================
            // Denied: No matching tier
            // =====================================================================
            activity?.SetTag("authz.tier", TierDenied);
            activity?.SetStatus(ActivityStatusCode.Error, "Access denied");
            AuthzDenied.Add(1,
                new KeyValuePair<string, object?>("authz.tier", TierDenied),
                new KeyValuePair<string, object?>("authz.reason", AccessDecisionReason.Denied.ToString()));
            LogAccessDenied(requestingUserId, resourceOwnerId, category, requiredLevel);
            return AccessDecision.Deny();
        }
        finally
        {
            sw.Stop();
            AuthzCheckDuration.Record(sw.Elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Finds a valid <see cref="DataAccessGrant"/> from the resource owner to the requesting user
    /// that covers the requested category and access level.
    /// </summary>
    /// <returns>The matching grant, or <c>null</c> if no valid grant exists.</returns>
    private async Task<DataAccessGrant?> FindValidGrantAsync(
        Guid requestingUserId,
        Guid resourceOwnerId,
        string category,
        AccessType requiredLevel)
    {
        var now = DateTimeOffset.UtcNow;

        // Query for grants where:
        // - Grantor is the resource owner
        // - Grantee is the requesting user (GranteeUserId is Guid? (nullable) — pending grants have null GranteeUserId and won't match this WHERE clause)
        // - Status is Active
        // - Not expired (ExpiresAt is null or in the future)
        // Then evaluate category/level match in memory (GrantCoversRequest requires C# logic)
        var grant = await dbContext.DataAccessGrants
            .AsNoTracking()
            .Where(g =>
                g.GrantorUserId == resourceOwnerId &&
                g.GranteeUserId == requestingUserId &&
                g.Status == GrantStatus.Active &&
                (g.ExpiresAt == null || g.ExpiresAt > now))
            .AsAsyncEnumerable()
            .FirstOrDefaultAsync(g => g.Covers(category, requiredLevel));

        return grant;
    }

    [LoggerMessage(EventId = 7001, Level = LogLevel.Debug,
        Message = "Access granted (ResourceOwner): user {UserId} owns the resource")]
    private partial void LogAccessGrantedOwner(Guid userId);

    [LoggerMessage(EventId = 7002, Level = LogLevel.Debug,
        Message = "Access granted (DataAccessGrant): user {UserId} has {AccessType} grant from {OwnerId}")]
    private partial void LogAccessGrantedGrant(Guid userId, AccessType accessType, Guid ownerId);

    [LoggerMessage(EventId = 7003, Level = LogLevel.Debug,
        Message = "Access granted (Administrator): user {UserId} is admin, accessing resource owned by {OwnerId}")]
    private partial void LogAccessGrantedAdmin(Guid userId, Guid ownerId);

    [LoggerMessage(EventId = 7010, Level = LogLevel.Warning,
        Message = "Access denied: user {UserId} attempted to access resource owned by {OwnerId} in category {Category} requiring {RequiredLevel}")]
    private partial void LogAccessDenied(Guid userId, Guid ownerId, string category, AccessType requiredLevel);
}
