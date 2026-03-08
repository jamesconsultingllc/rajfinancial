namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Payment frequency for insurance premiums.
/// </summary>
public enum PremiumFrequency
{
    /// <summary>Premium paid monthly.</summary>
    Monthly,

    /// <summary>Premium paid quarterly.</summary>
    Quarterly,

    /// <summary>Premium paid semi-annually (twice per year).</summary>
    SemiAnnually,

    /// <summary>Premium paid annually (once per year).</summary>
    Annually
}
