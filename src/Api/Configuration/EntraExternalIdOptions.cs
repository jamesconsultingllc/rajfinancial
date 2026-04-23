namespace RajFinancial.Api.Configuration;

/// <summary>
///     Strongly-typed options bound from the <c>EntraExternalId</c> configuration section.
///     Used by <see cref="RajFinancial.Api.Services.Auth.JwtBearerValidator"/> to build
///     the OpenID Connect discovery URL and to validate the token's <c>aud</c> claim.
/// </summary>
/// <remarks>
///     <para>
///         Discovery URL is composed as <c>{Instance}{TenantId}/v2.0/.well-known/openid-configuration</c>
///         via <see cref="RajFinancial.Api.Configuration.OidcMetadataAddress.Build"/>, which
///         normalizes a missing trailing slash on <see cref="Instance"/> before concatenation.
///     </para>
///     <para>
///         <see cref="ValidAudiences"/> is intentionally a list. CIAM access tokens carry
///         the App ID URI (<c>api://rajfinancial-api-dev</c>) as the <c>aud</c> claim, while
///         some token issuance flows emit the GUID client-id form. Include both to avoid
///         audience-mismatch failures after tenant reconfiguration.
///     </para>
/// </remarks>
internal sealed class EntraExternalIdOptions
{
    internal const string SectionName = "EntraExternalId";

    /// <summary>
    ///     Authority instance base URL — e.g. <c>https://rajfinancialdev.ciamlogin.com/</c>.
    ///     A missing trailing slash is normalized by
    ///     <see cref="RajFinancial.Api.Configuration.OidcMetadataAddress.Build"/>.
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>Entra tenant GUID used for both the discovery URL and the <c>tid</c> claim check.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>API application (client) ID registered in Entra External ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    ///     Accepted values for the token's <c>aud</c> claim. Typically contains both the
    ///     App ID URI and the client-id GUID.
    /// </summary>
    public IList<string> ValidAudiences { get; set; } = new List<string>();
}
