namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Types of insurance policy riders (add-on benefits).
/// </summary>
public enum PolicyRiderType
{
    /// <summary>Paid-Up Additions rider for additional death benefit and cash value.</summary>
    PaidUpAdditions,

    /// <summary>Accidental Death Benefit rider.</summary>
    AccidentalDeath,

    /// <summary>Waiver of Premium rider (waives premiums during disability).</summary>
    WaiverOfPremium,

    /// <summary>Child Term rider covering dependent children.</summary>
    ChildTerm,

    /// <summary>Long-Term Care rider providing LTC benefits.</summary>
    LongTermCare,

    /// <summary>Chronic Illness rider for accelerated death benefit.</summary>
    ChronicIllness,

    /// <summary>Guaranteed Insurability rider for future coverage increases.</summary>
    GuaranteedInsurability,

    /// <summary>Other rider type not listed.</summary>
    Other
}
