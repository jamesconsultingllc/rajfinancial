namespace RajFinancial.Shared.Entities;

/// <summary>
///     Classification of a financial entity.
/// </summary>
public enum EntityType
{
    /// <summary>Individual/family finances. One per user, auto-created.</summary>
    Personal = 0,

    /// <summary>LLC, corporation, partnership, sole proprietorship.</summary>
    Business = 1,

    /// <summary>Revocable, irrevocable, special needs, charitable trust.</summary>
    Trust = 2,
}
