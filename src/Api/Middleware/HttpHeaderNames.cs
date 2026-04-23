namespace RajFinancial.Api.Middleware;

/// <summary>
///     Canonical HTTP header names used across middleware, functions, and helpers.
///     Prefer these constants over string literals to avoid typos and
///     inconsistent casing.
/// </summary>
internal static class HttpHeaderNames
{
    /// <summary>HTTP <c>Content-Type</c> header.</summary>
    public const string ContentType = "Content-Type";

    /// <summary>HTTP <c>Authorization</c> header.</summary>
    public const string Authorization = "Authorization";

    /// <summary>Bearer scheme prefix (with trailing space) for the <c>Authorization</c> header.</summary>
    public const string BearerSchemePrefix = "Bearer ";
}

