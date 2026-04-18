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
    Business = 1,
    AssetProtection = 2,
    SpecialNeeds = 3,
    Charitable = 4,
    Dynasty = 5,
    Medicaid = 6,
    Spendthrift = 7,
    Land = 8,
    Gun = 9,
    Other = 99,
}
