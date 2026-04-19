using System.ComponentModel;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     IRS classification of how a trust treats income (only meaningful for non-grantor trusts).
/// </summary>
public enum TrustIncomeTreatment
{
    [Description("Simple — required to distribute all income annually (IRC §651); cannot accumulate income or distribute principal.")]
    Simple = 0,

    [Description("Complex — may accumulate income, distribute principal, or make charitable contributions (IRC §661).")]
    Complex = 1,
}