namespace RajFinancial.Shared.Entities;

/// <summary>
///     When a trust is created.
///     <list type="bullet">
///         <item>
///             <term>InterVivos</term>
///             <description>"Living trust" — created during the grantor's lifetime.</description>
///         </item>
///         <item>
///             <term>Testamentary</term>
///             <description>
///                 Created via the grantor's will; takes effect after death and probate
///                 approval. Always irrevocable once active (the grantor is no longer
///                 alive to amend it).
///             </description>
///         </item>
///     </list>
/// </summary>
public enum TrustCreationMethod
{
    InterVivos = 0,
    Testamentary = 1,
}
