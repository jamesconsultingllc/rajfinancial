using System.ComponentModel;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Primary classification of a trust entity by whether it can be amended.
/// </summary>
public enum TrustCategory
{
    [Description("Revocable — the grantor retains the right to amend or revoke the trust during their lifetime.")]
    Revocable = 0,

    [Description("Irrevocable — the trust cannot be amended or revoked once established (with narrow legal exceptions).")]
    Irrevocable = 1,
}