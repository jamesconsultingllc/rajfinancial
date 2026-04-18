namespace RajFinancial.Shared.Entities;

/// <summary>
///     IRS tax classification for business entities.
/// </summary>
public enum TaxClassification
{
    SoleProprietor = 0,
    Partnership = 1,
    SCorporation = 2,
    CCorporation = 3,
    NonProfit501c3 = 4,
    DisregardedEntity = 5,
}
