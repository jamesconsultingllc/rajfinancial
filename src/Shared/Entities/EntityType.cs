using System.ComponentModel;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Classification of a financial entity.
/// </summary>
public enum EntityType
{
    [Description("Personal — individual or family finances; one per user, auto-created.")]
    Personal = 0,

    [Description("Business — LLC, corporation, partnership, or sole proprietorship.")]
    Business = 1,

    [Description("Trust — revocable, irrevocable, special needs, charitable, or other trust vehicle.")]
    Trust = 2,
}