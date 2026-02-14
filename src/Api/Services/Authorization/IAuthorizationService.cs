using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Services.Authorization;

/// <summary>
/// Provides resource-level access control by evaluating a three-tier authorization check.
/// </summary>
/// <remarks>
/// <para>
/// <b>Authorization Flow (checked in order):</b>
/// <list type="number">
///   <item><b>Tier 1 — Resource Owner:</b> Is the requesting user the owner of the resource?
///         If yes, access is granted with <see cref="AccessType.Owner"/>.</item>
///   <item><b>Tier 2 — Data Access Grant:</b> Does the requesting user hold a valid
///         <see cref="DataAccessGrant"/> (status <see cref="GrantStatus.Active"/>) covering
///         the requested <paramref name="category"/> and <paramref name="requiredLevel"/>?</item>
///   <item><b>Tier 3 — Administrator:</b> Does the requesting user have the Administrator role?
///         Administrators can access any resource with <see cref="AccessType.Full"/>.</item>
/// </list>
/// The first matching tier produces an <see cref="AccessDecision"/> with the corresponding
/// <see cref="AccessDecisionReason"/>. If no tier matches, access is denied.
/// </para>
/// <para>
/// <b>OWASP Coverage:</b>
/// <list type="bullet">
///   <item>A01:2025 — Broken Access Control: Prevents IDOR by verifying resource ownership
///         or explicit grants before allowing data access.</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b> Inject this service into Azure Function handlers to gate access to
/// user-scoped resources (accounts, assets, beneficiaries, documents).
/// </para>
/// </remarks>
public interface IAuthorizationService
{
    /// <summary>
    /// Evaluates whether <paramref name="requestingUserId"/> may access a resource owned by
    /// <paramref name="resourceOwnerId"/> in the specified <paramref name="category"/> at
    /// the given <paramref name="requiredLevel"/>.
    /// </summary>
    /// <param name="requestingUserId">The Entra Object ID of the user making the request.</param>
    /// <param name="resourceOwnerId">The Entra Object ID of the user who owns the resource.</param>
    /// <param name="category">
    /// The data category being accessed (see <see cref="DataCategories"/>).
    /// Use <see cref="DataCategories.All"/> to check access to all categories.
    /// </param>
    /// <param name="requiredLevel">The minimum <see cref="AccessType"/> needed for the operation.</param>
    /// <returns>
    /// An <see cref="AccessDecision"/> indicating whether access is granted and why.
    /// Callers should check <see cref="AccessDecision.IsGranted"/> before proceeding.
    /// </returns>
    Task<AccessDecision> CheckAccessAsync(
        Guid requestingUserId,
        Guid resourceOwnerId,
        string category,
        AccessType requiredLevel);
}
