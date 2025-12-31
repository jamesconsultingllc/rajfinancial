namespace RajFinancial.Shared.Entities;

/// <summary>
///     Status of a data access grant.
/// </summary>
public enum GrantStatus
{
    /// <summary>
    ///     Invitation sent but not yet accepted.
    /// </summary>
    Pending = 0,

    /// <summary>
    ///     Grant is active and grantee can access data.
    /// </summary>
    Active = 1,

    /// <summary>
    ///     Grant has expired based on ExpiresAt date.
    /// </summary>
    Expired = 2,

    /// <summary>
    ///     Grant was revoked by the grantor.
    /// </summary>
    Revoked = 3
}