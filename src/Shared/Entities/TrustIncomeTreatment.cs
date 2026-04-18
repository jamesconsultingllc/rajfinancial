namespace RajFinancial.Shared.Entities;

/// <summary>
///     IRS classification of how a trust treats income (only meaningful for non-grantor trusts).
///     <list type="bullet">
///         <item>
///             <term>Simple</term>
///             <description>Required to distribute all income annually (IRC §651). Cannot accumulate income or distribute principal.</description>
///         </item>
///         <item>
///             <term>Complex</term>
///             <description>May accumulate income, distribute principal, or make charitable contributions (IRC §661).</description>
///         </item>
///     </list>
/// </summary>
public enum TrustIncomeTreatment
{
    Simple = 0,
    Complex = 1,
}
