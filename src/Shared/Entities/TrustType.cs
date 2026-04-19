using System.ComponentModel;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Named legal type of a trust entity. Categorizes the specific kind of
///     trust vehicle, distinct from its <see cref="TrustCategory" /> (Revocable/Irrevocable),
///     <see cref="TrustCreationMethod" /> (Inter Vivos/Testamentary), and
///     <see cref="TrustGoal" /> (motivations for creating it).
/// </summary>
public enum TrustType
{
    [Description("Family Trust — manages and transfers wealth among family members; most common household estate-planning trust.")]
    Family = 0,

    [Description("Business Trust — holds business interests or operates a business through trustees for the benefit of beneficiaries.")]
    Business = 1,

    [Description("Asset Protection Trust — shields assets from future creditors, lawsuits, and claims (often irrevocable, often domestic or offshore).")]
    AssetProtection = 2,

    [Description("Special Needs Trust — provides for a disabled beneficiary without disqualifying them from means-tested public benefits (SSI, Medicaid).")]
    SpecialNeeds = 3,

    [Description("Charitable Trust — benefits a charitable cause (CRT, CLT, private foundation alternative).")]
    Charitable = 4,

    [Description("Dynasty Trust — designed to last multiple generations, leveraging GST exemption to transfer wealth tax-efficiently.")]
    Dynasty = 5,

    [Description("Medicaid Trust — irrevocable trust used for Medicaid long-term-care planning (5-year look-back applies).")]
    Medicaid = 6,

    [Description("Spendthrift Trust — restricts a beneficiary's ability to transfer their interest and shields distributions from the beneficiary's creditors.")]
    Spendthrift = 7,

    [Description("Land Trust — holds title to real estate for privacy and simplified transfer; beneficiary is not of public record.")]
    Land = 8,

    [Description("Gun Trust / NFA Trust — holds Title II NFA firearms (suppressors, SBRs, machine guns) to simplify ownership, use, and transfer.")]
    Gun = 9,

    [Description("Other — trust type not covered by the predefined list; populate OtherTypeDescription with details.")]
    Other = 99,
}