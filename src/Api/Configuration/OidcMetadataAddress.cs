namespace RajFinancial.Api.Configuration;

/// <summary>
///     Helper for constructing the Entra External ID OIDC discovery endpoint from
///     <see cref="EntraExternalIdOptions"/>. Centralised here so Program.cs registration
///     stays declarative and the URL-building rules can be unit tested in isolation.
/// </summary>
internal static class OidcMetadataAddress
{
    /// <summary>
    ///     Builds the v2.0 OIDC discovery URL for the configured tenant. Normalises the
    ///     <see cref="EntraExternalIdOptions.Instance"/> value so a missing or duplicated
    ///     trailing slash never produces a malformed URL, and validates the result is a
    ///     well-formed absolute https URI so misconfiguration fails fast at startup. Only
    ///     https is accepted because the OIDC discovery client
    ///     (<c>HttpDocumentRetriever</c>) rejects non-TLS endpoints by default.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when Instance / TenantId are missing or the resulting address is not a
    ///     valid absolute https URL.
    /// </exception>
    public static string Build(EntraExternalIdOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Instance) || string.IsNullOrWhiteSpace(options.TenantId))
        {
            throw new InvalidOperationException(
                $"{EntraExternalIdOptions.SectionName}:Instance and {EntraExternalIdOptions.SectionName}:TenantId " +
                "must be set before the OIDC discovery URL can be constructed.");
        }

        var instanceBase = options.Instance.EndsWith('/') ? options.Instance : options.Instance + "/";
        var address = $"{instanceBase}{options.TenantId}/v2.0/.well-known/openid-configuration";

        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"OIDC discovery URL '{address}' is not a valid absolute https URL. " +
                $"Check {EntraExternalIdOptions.SectionName}:Instance (must be an https:// URL).");
        }

        return uri.AbsoluteUri;
    }
}
