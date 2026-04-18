namespace RajFinancial.Shared.Entities;

/// <summary>
///     Authority the trustee has over distributions to beneficiaries.
///     <list type="bullet">
///         <item>
///             <term>Mandatory</term>
///             <description>Trustee must make distributions per a fixed schedule defined in the trust document.</description>
///         </item>
///         <item>
///             <term>Discretionary</term>
///             <description>Trustee decides whether, when, and how much to distribute, within the trust's stated purposes.</description>
///         </item>
///     </list>
/// </summary>
public enum TrustDistributionRule
{
    Mandatory = 0,
    Discretionary = 1,
}
