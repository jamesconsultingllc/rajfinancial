using RajFinancial.Api.Middleware.Exception;

namespace RajFinancial.Api.Services.EntityService;

/// <summary>
///     Authorization-deny exception factory for the Entity aggregate.
///     <para>
///         Per-resource denies mimic <see cref="EntityDbErrors.NotFound(Guid)"/> exactly so a
///         cross-user GET/PUT/DELETE-by-id is byte-indistinguishable from a missing id;
///         collection-scope denies mimic a missing owner
///         (<see cref="NotFoundException.User(string)"/>).
///     </para>
///     <para>Enforced contract: ADR 0001 — IDOR returns 404 / OWASP A01:2021.</para>
/// </summary>
internal static class EntityAuthorizationDenials
{
    public static NotFoundException Deny(Guid ownerUserId, Guid? entityId) =>
        entityId is { } id
            ? EntityDbErrors.NotFound(id)
            : NotFoundException.User(ownerUserId.ToString());
}
