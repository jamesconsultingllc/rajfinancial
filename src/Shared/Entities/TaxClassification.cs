using System.ComponentModel;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     IRS tax classification for business entities.
/// </summary>
public enum TaxClassification
{
    [Description("Sole Proprietor — reports business income on Schedule C of the owner's Form 1040.")]
    SoleProprietor = 0,

    [Description("Partnership — files Form 1065; issues Schedule K-1 to partners.")]
    Partnership = 1,

    [Description("S-Corporation — pass-through that files Form 1120-S; income/loss flows to shareholders via K-1.")]
    SCorporation = 2,

    [Description("C-Corporation — separately taxable entity that files Form 1120 at the corporate tax rate.")]
    CCorporation = 3,

    [Description("Non-Profit 501(c)(3) — tax-exempt charitable, religious, or educational organization.")]
    NonProfit501c3 = 4,

    [Description("Disregarded Entity — separate legal entity that is ignored for federal tax (typically single-member LLCs).")]
    DisregardedEntity = 5,
}