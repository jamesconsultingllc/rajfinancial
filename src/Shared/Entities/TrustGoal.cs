using System.ComponentModel;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Motivations for creating a trust. Multiple goals may apply to a single
///     trust, so this is a [Flags] enum that can be OR'd together.
/// </summary>
[Flags]
public enum TrustGoal
{
    [Description("None — no motivating goal recorded.")]
    None = 0,

    [Description("Avoid Probate — keep assets out of the probate court to save time and fees on death.")]
    AvoidProbate = 1 << 0,

    [Description("Privacy — keep the disposition of assets out of public probate records.")]
    Privacy = 1 << 1,

    [Description("Protect Children — ensure children are provided for on the grantor's terms (age-gated distributions, guardianship, etc.).")]
    ProtectChildren = 1 << 2,

    [Description("Avoid Family Fights — reduce the likelihood of estate disputes by codifying distributions in a trust.")]
    AvoidFamilyFights = 1 << 3,

    [Description("Protect Spouse — provide lifetime support and/or estate-tax planning for a surviving spouse.")]
    ProtectSpouse = 1 << 4,

    [Description("Control Distribution — control when, how, and for what purposes beneficiaries receive assets.")]
    ControlDistribution = 1 << 5,

    [Description("Reduce Taxes — minimize estate, gift, generation-skipping, or income taxes through trust structure.")]
    ReduceTaxes = 1 << 6,

    [Description("Protect Assets — shield assets from creditors, lawsuits, divorce claims, or long-term-care spend-down.")]
    ProtectAssets = 1 << 7,
}