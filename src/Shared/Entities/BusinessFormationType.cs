namespace RajFinancial.Shared.Entities;

/// <summary>
///     Legal formation type for business entities.
/// </summary>
public enum BusinessFormationType
{
    SoleProprietorship = 0,
    SingleMemberLLC = 1,
    MultiMemberLLC = 2,
    SCorporation = 3,
    CCorporation = 4,
    Partnership = 5,
    LimitedPartnership = 6,
    NonProfit = 7,
}
