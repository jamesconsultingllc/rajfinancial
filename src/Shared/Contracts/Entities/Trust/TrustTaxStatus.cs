using System.ComponentModel;

namespace RajFinancial.Shared.Contracts.Entities.Trust;

/// <summary>
///     IRS tax treatment of the trust.
/// </summary>
public enum TrustTaxStatus
{
    [Description("Grantor Trust — income is taxed to the grantor personally under IRC §§671–679; the trust is not a separate taxpayer.")]
    Grantor = 0,

    [Description("Non-Grantor Trust — the trust is a separate taxpayer that files its own Form 1041 and issues K-1s to beneficiaries.")]
    NonGrantor = 1,
}