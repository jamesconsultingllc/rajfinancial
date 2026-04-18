namespace RajFinancial.Shared.Entities;

/// <summary>
///     Stackable provisions and add-on features layered onto a trust. Multiple
///     features can apply to a single trust, so this is a [Flags] enum.
/// </summary>
[Flags]
public enum TrustFeatures
{
    None = 0,
    Spendthrift = 1 << 0,
    AssetProtectionProvisions = 1 << 1,
    DynastyProvisions = 1 << 2,
    GenerationSkipping = 1 << 3,
    CrummeyPowers = 1 << 4,
    PowerOfAppointment = 1 << 5,
    PourOver = 1 << 6,
    IncentiveProvisions = 1 << 7,
    BypassTrust = 1 << 8,
    LifeEstate = 1 << 9,
}
