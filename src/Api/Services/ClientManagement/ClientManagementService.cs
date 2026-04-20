// ============================================================================
// RAJ Financial — Client Management Service
// ============================================================================
// Manages advisor–client data-access grants (DataAccessGrant entities).
// Operations: assign, list, lookup, and soft-delete client relationships.
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Data;
using RajFinancial.Shared.Contracts.Auth;
using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Services.ClientManagement;

/// <summary>
/// Manages advisor–client data-access grants by operating on
/// <see cref="DataAccessGrant"/> entities via <see cref="ApplicationDbContext"/>.
/// </summary>
/// <remarks>
///     <para>
///         <b>Assign:</b> Creates a new <see cref="DataAccessGrant"/> in
///         <see cref="GrantStatus.Pending"/> status, linking the calling
///         advisor (grantor) to a client email (grantee).
///     </para>
///     <para>
///         <b>List:</b> Returns grants owned by the calling user. If the
///         caller is an administrator, returns all grants across advisors.
///     </para>
///     <para>
///         <b>Remove:</b> Soft-deletes a grant by setting
///         <see cref="GrantStatus.Revoked"/> and stamping
///         <see cref="DataAccessGrant.RevokedAt"/>.
///     </para>
/// </remarks>
public partial class ClientManagementService(
    ApplicationDbContext dbContext,
    ILogger<ClientManagementService> logger) : IClientManagementService
{
    /// <inheritdoc/>
    public async Task<DataAccessGrant> AssignClientAsync(
        Guid grantorUserId,
        AssignClientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AccessType>(request.AccessType, ignoreCase: true, out var accessType))
        {
            throw new ArgumentException(
                $"Invalid access type '{request.AccessType}'. Expected one of: {string.Join(", ", Enum.GetNames<AccessType>())}",
                nameof(request));
        }

        var grant = new DataAccessGrant
        {
            Id = Guid.NewGuid(),
            GrantorUserId = grantorUserId,
            GranteeEmail = request.ClientEmail,
            AccessType = accessType,
            Categories = request.Categories.ToList(),
            RelationshipLabel = request.RelationshipLabel,
            Status = GrantStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.DataAccessGrants.Add(grant);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogClientAssigned(grant.Id, grantorUserId, accessType);
        return grant;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DataAccessGrant>> GetClientAssignmentsAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        IQueryable<DataAccessGrant> query = dbContext.DataAccessGrants
            .Where(g => g.Status != GrantStatus.Revoked);

        if (!isAdmin)
        {
            // Advisors see only their own grants
            query = query.Where(g => g.GrantorUserId == userId);
        }

        var grants = await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        LogGetClientAssignments(grants.Count, userId, isAdmin);
        return grants;
    }

    /// <inheritdoc/>
    public async Task<DataAccessGrant?> GetGrantByIdAsync(
        Guid grantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DataAccessGrants.FindAsync(
            [grantId], cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveClientAccessAsync(
        Guid grantId,
        CancellationToken cancellationToken = default)
    {
        var grant = await dbContext.DataAccessGrants.FindAsync(
            [grantId], cancellationToken);

        if (grant is null)
        {
            LogGrantNotFoundForRemoval(grantId);
            throw new InvalidOperationException(
                $"Grant '{grantId}' not found. The caller should verify existence before revoking.");
        }

        // Soft-delete: revoke the grant
        grant.Status = GrantStatus.Revoked;
        grant.RevokedAt = DateTimeOffset.UtcNow;
        grant.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        LogClientAccessRevoked(grantId);
    }

    [LoggerMessage(EventId = 6001, Level = LogLevel.Information,
        Message = "Client assigned: Grant {GrantId} from {GrantorId} ({AccessType})")]
    private partial void LogClientAssigned(Guid grantId, Guid grantorId, AccessType accessType);

    [LoggerMessage(EventId = 6002, Level = LogLevel.Information,
        Message = "GetClientAssignments returning {Count} grant(s) for user {UserId} (admin={IsAdmin})")]
    private partial void LogGetClientAssignments(int count, Guid userId, bool isAdmin);

    [LoggerMessage(EventId = 6003, Level = LogLevel.Information,
        Message = "Client access revoked: Grant {GrantId}")]
    private partial void LogClientAccessRevoked(Guid grantId);

    [LoggerMessage(EventId = 6010, Level = LogLevel.Warning,
        Message = "RemoveClientAccess called for non-existent grant {GrantId}")]
    private partial void LogGrantNotFoundForRemoval(Guid grantId);
}
