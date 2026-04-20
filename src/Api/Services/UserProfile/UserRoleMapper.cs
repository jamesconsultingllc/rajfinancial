using RajFinancial.Shared.Entities.Users;

namespace RajFinancial.Api.Services.UserProfile;

/// <summary>
///     Maps Entra role-claim strings to the strongly-typed <see cref="UserRole"/>
///     enum. When multiple roles are claimed, the highest-priority role wins:
///     Administrator (0) &gt; Advisor (1) &gt; Client (2).
/// </summary>
internal static class UserRoleMapper
{
    /// <summary>
    ///     Maps a list of role claim strings to the single highest-priority
    ///     <see cref="UserRole"/>. Defaults to <see cref="UserRole.Client"/> when
    ///     no recognized roles are present.
    /// </summary>
    public static UserRole MapHighestPriority(this IReadOnlyList<string> roles) =>
        roles
            .Select(r => Enum.TryParse<UserRole>(r, ignoreCase: true, out var parsed) ? parsed : (UserRole?)null)
            .Where(r => r.HasValue)
            .Select(r => r!.Value)
            .DefaultIfEmpty(UserRole.Client)
            .Min();
}
