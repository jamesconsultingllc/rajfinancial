namespace RajFinancial.Shared.Entities;

/// <summary>
///     Named legal type of a trust entity. Categorizes the specific kind of
///     trust vehicle, distinct from its <see cref="TrustCategory" /> (legal
///     structure: Revocable / Irrevocable / Testamentary) and its
///     <see cref="TrustGoal" /> (motivations for creating it).
/// </summary>
public enum TrustType
{
    Family = 0,
    AssetProtection = 1,
    SpecialNeeds = 2,
    Charitable = 3,
    Dynasty = 4,
    Medicaid = 5,
    Spendthrift = 6,
    Land = 7,
    Gun = 8,
    Other = 99,
}
