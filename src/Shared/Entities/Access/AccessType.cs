namespace RajFinancial.Shared.Entities.Access;

/// <summary>
///     Type of access granted to a user.
/// </summary>
public enum AccessType
{
    /// <summary>
    ///     Full control - the data owner. Cannot be granted, only implicit.
    /// </summary>
    Owner = 0,

    /// <summary>
    ///     Full access - read and write, but cannot delete account or manage shares.
    ///     Suitable for spouse or trusted family member.
    /// </summary>
    Full = 1,

    /// <summary>
    ///     Read-only access - can view but not modify any data.
    ///     Suitable for professionals reviewing information.
    /// </summary>
    Read = 2,

    /// <summary>
    ///     Limited access - read-only for specific data categories only.
    ///     Suitable for CPA (financial only) or attorney (estate only).
    /// </summary>
    Limited = 3
}