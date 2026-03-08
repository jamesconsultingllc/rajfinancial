namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Classification of real estate properties.
/// </summary>
public enum PropertyType
{
    /// <summary>Single-family detached home.</summary>
    SingleFamily,

    /// <summary>Condominium unit.</summary>
    Condo,

    /// <summary>Townhouse or row house.</summary>
    Townhouse,

    /// <summary>Multi-family residential (duplex, triplex, etc.).</summary>
    MultiFamily,

    /// <summary>Undeveloped land or lot.</summary>
    Land,

    /// <summary>Commercial property (office, retail, industrial).</summary>
    Commercial,

    /// <summary>Other property type not listed.</summary>
    Other
}
