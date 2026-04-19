using RajFinancial.Shared.Contracts.Auth;
using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Services.ClientManagement;

/// <summary>
/// Manages client–advisor data-access grants (CRUD over <see cref="DataAccessGrant"/>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Advisors (and Administrators) grant client users access to
/// specific categories of financial data. This service encapsulates all
/// business logic for creating, listing, and revoking those grants.
/// </para>
/// <para>
/// <b>Security model:</b> <see cref="DataAccessGrant"/> controls <em>data access</em>
/// (which accounts/categories a user may view), whereas Entra app roles
/// control <em>feature access</em> (which UI pages/API endpoints a user may call).
/// These are orthogonal concerns — see <c>RAJ_FINANCIAL_SECURITY_MODEL.md</c>.
/// </para>
/// </remarks>
public interface IClientManagementService
{
    /// <summary>
    /// Creates a new <see cref="DataAccessGrant"/> from an advisor to a client.
    /// </summary>
    /// <param name="grantorUserId">
    /// The Entra Object ID of the advisor (or admin) creating the grant.
    /// </param>
    /// <param name="request">
    /// The validated assignment request containing client email, access type,
    /// categories, and optional relationship label.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created <see cref="DataAccessGrant"/> entity.</returns>
    Task<DataAccessGrant> AssignClientAsync(
        Guid grantorUserId,
        AssignClientRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves client assignments visible to the requesting user.
    /// </summary>
    /// <param name="userId">
    /// The Entra Object ID of the requesting user.
    /// </param>
    /// <param name="isAdmin">
    /// When <c>true</c>, returns all grants across all advisors.
    /// When <c>false</c>, returns only grants where <c>GrantorUserId == userId</c>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of matching <see cref="DataAccessGrant"/> entities.</returns>
    Task<IReadOnlyList<DataAccessGrant>> GetClientAssignmentsAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single <see cref="DataAccessGrant"/> by its primary key.
    /// </summary>
    /// <param name="grantId">The grant's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The grant, or <c>null</c> if not found.</returns>
    Task<DataAccessGrant?> GetGrantByIdAsync(
        Guid grantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a <see cref="DataAccessGrant"/> by setting
    /// <see cref="DataAccessGrant.Status"/> to <see cref="GrantStatus.Revoked"/>
    /// and stamping <see cref="DataAccessGrant.RevokedAt"/>.
    /// </summary>
    /// <param name="grantId">The grant to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    Task RemoveClientAccessAsync(
        Guid grantId,
        CancellationToken cancellationToken = default);
}
