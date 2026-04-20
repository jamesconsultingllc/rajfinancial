using System.ComponentModel;

namespace RajFinancial.Shared.Contracts.Entities.Trust;

/// <summary>
///     When a trust is created.
/// </summary>
public enum TrustCreationMethod
{
    [Description("Inter Vivos — \"living trust\" created during the grantor's lifetime.")]
    InterVivos = 0,

    [Description("Testamentary — created via the grantor's will; takes effect after death and probate. Always irrevocable once active.")]
    Testamentary = 1,
}