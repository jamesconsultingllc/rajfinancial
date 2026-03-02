namespace RajFinancial.Shared.Entities.Assets;

/// <summary>
///     Classification of life insurance and annuity policy types.
/// </summary>
public enum InsurancePolicyType
{
    /// <summary>Whole life insurance with cash value accumulation.</summary>
    WholeLife,

    /// <summary>Universal life insurance with flexible premiums.</summary>
    UniversalLife,

    /// <summary>Term life insurance for a fixed period.</summary>
    TermLife,

    /// <summary>Annuity contract (fixed, variable, or indexed).</summary>
    Annuity,

    /// <summary>Other insurance policy type.</summary>
    Other
}
