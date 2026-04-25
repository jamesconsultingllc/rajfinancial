using RajFinancial.Api.Middleware.Exception;

namespace RajFinancial.Api.Services.AssetService;

/// <summary>
///     Authorization-deny exception factory for the Asset aggregate.
///     <para>
///         Per-resource denies mimic <see cref="NotFoundException.Asset(Guid)"/> exactly so
///         cross-user per-resource operations (GET-by-id, PUT, DELETE) are byte-indistinguishable
///         from a missing id; collection-scope denies mimic a missing owner
///         (<see cref="NotFoundException.User(string)"/>).
///     </para>
///     <para>Enforced contract: ADR 0001 — IDOR returns 404 / OWASP A01:2021.</para>
/// </summary>
internal static class AssetAuthorizationDenials
{
    public static NotFoundException Deny(Guid ownerUserId, Guid? assetId) =>
        assetId is { } id
            ? NotFoundException.Asset(id)
            : NotFoundException.User(ownerUserId.ToString());
}
