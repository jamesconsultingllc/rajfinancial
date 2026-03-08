namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Status of an intellectual property registration.
/// </summary>
public enum IpStatus
{
    /// <summary>Application filed, awaiting review.</summary>
    Pending,

    /// <summary>Granted/registered and in force.</summary>
    Active,

    /// <summary>Protection period has ended.</summary>
    Expired,

    /// <summary>Application or registration abandoned.</summary>
    Abandoned
}
