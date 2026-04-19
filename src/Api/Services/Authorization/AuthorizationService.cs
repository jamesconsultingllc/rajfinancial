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
public class AuthorizationService(
    ApplicationDbContext dbContext,
    ILogger<AuthorizationService> logger) : IAuthorizationService
{
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

        // =====================================================================
        // Tier 1: Resource Owner — immediate grant, no DB query needed
        // =====================================================================
        if (requestingUserId == resourceOwnerId)
        {
            logger.LogDebug(
                "Access granted (ResourceOwner): user {UserId} owns the resource",
                requestingUserId);

            return AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Owner);
        }

        // =====================================================================
        // Tier 2: Data Access Grant — query for a valid grant
        // =====================================================================
        var grant = await FindValidGrantAsync(requestingUserId, resourceOwnerId, category, requiredLevel);

        if (grant is not null)
        {
            logger.LogDebug(
                "Access granted (DataAccessGrant): user {UserId} has {AccessType} grant from {OwnerId}",
                requestingUserId, grant.AccessType, resourceOwnerId);

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
            logger.LogDebug(
                "Access granted (Administrator): user {UserId} is admin, accessing resource owned by {OwnerId}",
                requestingUserId, resourceOwnerId);

            return AccessDecision.Grant(AccessDecisionReason.Administrator, AccessType.Full);
        }

        // =====================================================================
        // Denied: No matching tier
        // =====================================================================
        logger.LogWarning(
            "Access denied: user {UserId} attempted to access resource owned by {OwnerId} " +
            "in category {Category} requiring {RequiredLevel}",
            requestingUserId, resourceOwnerId, category, requiredLevel);

        return AccessDecision.Deny();
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
            .FirstOrDefaultAsync(g => GrantCoversRequest(g, category, requiredLevel));

        return grant;
    }

    /// <summary>
    /// Determines whether a <see cref="DataAccessGrant"/> covers the requested category and access level.
    /// </summary>
    /// <remarks>
    /// <para>Access type hierarchy: Owner > Full > Read > Limited.</para>
    /// <para>
    /// <list type="bullet">
    ///   <item><b>Full</b> grants cover all categories at Read and Full levels.</item>
    ///   <item><b>Read</b> grants cover all categories at Read level only.</item>
    ///   <item><b>Limited</b> grants cover only specific categories at Read level.</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static bool GrantCoversRequest(DataAccessGrant grant, string category, AccessType requiredLevel)
    {
        return grant.AccessType switch
        {
            // Full access: covers all categories, grants Read and Full
            AccessType.Full => requiredLevel is AccessType.Read or AccessType.Full,

            // Read access: covers all categories, grants Read only
            AccessType.Read => requiredLevel == AccessType.Read,

            // Limited access: covers only specified categories, grants Read only
            AccessType.Limited => requiredLevel == AccessType.Read && GrantCoversCategory(grant, category),

            // Owner access type cannot be granted to others
            _ => false
        };
    }

    /// <summary>
    /// Checks whether a Limited grant's category list includes the requested category.
    /// </summary>
    private static bool GrantCoversCategory(DataAccessGrant grant, string category)
    {
        // "all" category matches any grant
        if (string.Equals(category, DataCategories.All, StringComparison.OrdinalIgnoreCase))
            return false; // Limited grants cannot cover "all" — that requires Full/Read

        return grant.Categories.Any(c =>
            string.Equals(c, category, StringComparison.OrdinalIgnoreCase));
    }
}
