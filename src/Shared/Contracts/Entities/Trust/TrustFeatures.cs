using System.ComponentModel;

namespace RajFinancial.Shared.Contracts.Entities.Trust;

/// <summary>
///     Stackable provisions and add-on features layered onto a trust. Multiple
///     features can apply to a single trust, so this is a [Flags] enum.
/// </summary>
[Flags]
public enum TrustFeatures
{
    [Description("None — no special features selected.")]
    None = 0,

    [Description("Spendthrift clause — restricts a beneficiary from transferring their interest and shields distributions from the beneficiary's creditors.")]
    Spendthrift = 1 << 0,

    [Description("Asset Protection Provisions — layered clauses (spendthrift, discretionary distributions, independent trustee) designed to shield assets from creditors.")]
    AssetProtectionProvisions = 1 << 1,

    [Description("Dynasty Provisions — structured to last for multiple generations, minimizing transfer taxes across descendants.")]
    DynastyProvisions = 1 << 2,

    [Description("Generation-Skipping — leverages the GST exemption to transfer wealth directly to grandchildren or later generations.")]
    GenerationSkipping = 1 << 3,

    [Description("Crummey Powers — grants beneficiaries a temporary withdrawal right so gifts to the trust qualify for the annual gift-tax exclusion.")]
    CrummeyPowers = 1 << 4,

    [Description("Power of Appointment — authorizes a beneficiary (or third party) to direct where trust assets go, either during life or at death.")]
    PowerOfAppointment = 1 << 5,

    [Description("Pour-Over — companion to a will; assets not otherwise placed in the trust at death \"pour over\" into it via the probate process.")]
    PourOver = 1 << 6,

    [Description("Incentive Provisions — ties distributions to beneficiary behavior (education milestones, employment, sobriety, etc.).")]
    IncentiveProvisions = 1 << 7,

    [Description("Bypass Trust — credit-shelter / AB-trust mechanism that preserves the deceased spouse's estate tax exemption on first death.")]
    BypassTrust = 1 << 8,

    [Description("Life Estate — grants a beneficiary the right to use trust property for life; remainder passes to other beneficiaries at death.")]
    LifeEstate = 1 << 9,
}