using System.Text.RegularExpressions;

namespace RajFinancial.Api.Services.EntityService;

/// <summary>
///     Slug generation and normalization for Entity names. Pure string transforms — no I/O.
/// </summary>
internal static partial class EntitySlug
{
    [GeneratedRegex(@"[^a-z0-9\s-]+")]
    private static partial Regex NonAlphaNumRegex();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex WhitespaceRegex();

    public static string Generate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var lower = name.Trim().ToLowerInvariant();
        var cleaned = NonAlphaNumRegex().Replace(lower, "");
        var hyphenated = WhitespaceRegex().Replace(cleaned, "-").Trim('-');
        return hyphenated;
    }

    public static string Normalize(string slug) => Generate(slug);
}
