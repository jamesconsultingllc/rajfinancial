namespace RajFinancial.Api.Services.Auth;

/// <summary>
///     Canonical names of JWT claim types emitted by Entra External ID and consumed
///     by the API. Centralised so middleware, validators, and tests reference the
///     same source of truth (no string drift across the auth pipeline).
/// </summary>
internal static class JwtClaimNames
{
    /// <summary>Standard Entra object identifier claim (long URI form).</summary>
    internal const string ObjectIdLongForm = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>Short-name <c>oid</c> claim (Entra v2 tokens with MapInboundClaims=false).</summary>
    internal const string Oid = "oid";

    /// <summary>Entra tenant id claim.</summary>
    internal const string Tid = "tid";

    /// <summary>Entra External ID emails array claim.</summary>
    internal const string Emails = "emails";

    /// <summary>Standard email claim (single value).</summary>
    internal const string Email = "email";

    /// <summary>Display name claim.</summary>
    internal const string Name = "name";

    /// <summary>App roles claim.</summary>
    internal const string Roles = "roles";

    /// <summary>Preferred username claim.</summary>
    internal const string PreferredUsername = "preferred_username";

    /// <summary>UPN claim (long URI form).</summary>
    internal const string UpnLongForm = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
}
