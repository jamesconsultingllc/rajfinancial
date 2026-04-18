namespace RajFinancial.Shared.Entities;

/// <summary>
///     IRS tax treatment of the trust.
///     <list type="bullet">
///         <item>
///             <term>Grantor</term>
///             <description>Income is taxed to the grantor personally (IRC §671–679).</description>
///         </item>
///         <item>
///             <term>NonGrantor</term>
///             <description>The trust is a separate taxpayer and files its own Form 1041.</description>
///         </item>
///     </list>
/// </summary>
public enum TrustTaxStatus
{
    Grantor = 0,
    NonGrantor = 1,
}
