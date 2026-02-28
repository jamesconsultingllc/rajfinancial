namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Types of intellectual property.
/// </summary>
public enum IpType
{
    /// <summary>Utility or design patent.</summary>
    Patent,

    /// <summary>Registered trademark.</summary>
    Trademark,

    /// <summary>Copyright registration.</summary>
    Copyright,

    /// <summary>Trade secret (unregistered proprietary information).</summary>
    TradeSecret,

    /// <summary>Other intellectual property type.</summary>
    Other
}
