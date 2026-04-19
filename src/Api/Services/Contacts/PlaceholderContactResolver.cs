using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Shared.Contracts.Entities;

namespace RajFinancial.Api.Services.Contacts;

/// <summary>
///     Production-default resolver for Phase 1. The Contacts table does not exist
///     yet, so no <c>ContactId</c> is considered valid — which is the safe default
///     and prevents cross-tenant linking via arbitrary client-supplied GUIDs.
/// </summary>
/// <remarks>
///     Replaced by the real implementation in Phase 2 when the Contacts aggregate
///     ships. Test runs swap this for <see cref="SeedableContactResolver" /> via
///     the <c>ENABLE_CONTACT_TEST_SEEDING</c> env flag.
/// </remarks>
internal sealed class PlaceholderContactResolver : IContactResolver
{
    public Task EnsureOwnedByAsync(Guid contactId, Guid userId, CancellationToken ct = default)
        => throw new NotFoundException(
            EntityErrorCodes.RoleContactNotFound,
            $"Contact {contactId} was not found.");
}
