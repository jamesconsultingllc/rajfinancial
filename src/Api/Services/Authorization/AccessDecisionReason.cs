namespace RajFinancial.Api.Services.Authorization;

/// <summary>
/// Indicates why an <see cref="AccessDecision"/> was granted or denied.
/// </summary>
/// <remarks>
/// Used by <see cref="IAuthorizationService.CheckAccessAsync"/> to communicate
/// which tier of the three-tier authorization check produced the result.
/// </remarks>
public enum AccessDecisionReason
{
    /// <summary>
    /// Access denied — the user is not the resource owner,
    /// has no valid <see cref="DataAccessGrant"/>,
    /// and is not an Administrator.
    /// </summary>
    Denied = 0,

    /// <summary>
    /// Access granted because the requesting user owns the resource.
    /// </summary>
    ResourceOwner = 1,

    /// <summary>
    /// Access granted through a valid <see cref="DataAccessGrant"/>
    /// covering the requested category and access level.
    /// </summary>
    DataAccessGrant = 2,

    /// <summary>
    /// Access granted because the requesting user has the Administrator role.
    /// </summary>
    Administrator = 3
}
