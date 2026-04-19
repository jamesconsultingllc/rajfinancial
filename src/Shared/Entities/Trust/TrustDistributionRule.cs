using System.ComponentModel;

namespace RajFinancial.Shared.Entities.Trust;

/// <summary>
///     Authority the trustee has over distributions to beneficiaries.
/// </summary>
public enum TrustDistributionRule
{
    [Description("Mandatory — trustee must make distributions per a fixed schedule defined in the trust document.")]
    Mandatory = 0,

    [Description("Discretionary — trustee decides whether, when, and how much to distribute within the trust's stated purposes.")]
    Discretionary = 1,
}