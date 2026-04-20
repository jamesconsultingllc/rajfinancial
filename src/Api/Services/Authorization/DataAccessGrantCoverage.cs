using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Services.Authorization;

/// <summary>
///     Coverage rules for <see cref="DataAccessGrant"/>: determines whether a
///     grant satisfies a requested category + access level.
/// </summary>
/// <remarks>
///     Access type hierarchy: Owner &gt; Full &gt; Read &gt; Limited.
///     <list type="bullet">
///         <item><b>Full</b> grants cover all categories at Read and Full levels.</item>
///         <item><b>Read</b> grants cover all categories at Read level only.</item>
///         <item><b>Limited</b> grants cover only specific categories at Read level.</item>
///     </list>
/// </remarks>
internal static class DataAccessGrantCoverage
{
    /// <summary>
    ///     Determines whether <paramref name="grant"/> covers the requested
    ///     <paramref name="category"/> at the given <paramref name="requiredLevel"/>.
    /// </summary>
    public static bool Covers(this DataAccessGrant grant, string category, AccessType requiredLevel) =>
        grant.AccessType switch
        {
            AccessType.Full => requiredLevel is AccessType.Read or AccessType.Full,
            AccessType.Read => requiredLevel == AccessType.Read,
            AccessType.Limited => requiredLevel == AccessType.Read && grant.CoversCategory(category),
            _ => false,
        };

    /// <summary>
    ///     Checks whether a Limited grant's category list includes the requested
    ///     category. The pseudo-category <see cref="DataCategories.All"/> is
    ///     never matched by a Limited grant — that requires Full or Read.
    /// </summary>
    private static bool CoversCategory(this DataAccessGrant grant, string category)
    {
        if (string.Equals(category, DataCategories.All, StringComparison.OrdinalIgnoreCase))
            return false;

        return grant.Categories.Any(c =>
            string.Equals(c, category, StringComparison.OrdinalIgnoreCase));
    }
}
