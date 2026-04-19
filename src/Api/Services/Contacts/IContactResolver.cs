namespace RajFinancial.Api.Services.Contacts;

/// <summary>
///     Verifies that a contact exists and is owned by the given user.
/// </summary>
/// <remarks>
///     <para>
///         The Contacts table does not yet exist in Phase 1. This interface is the
///         single enforcement point so that, until Phase 2 lands the real model,
///         no arbitrary client-supplied <c>ContactId</c> GUID can be linked to an
///         entity — which would otherwise allow cross-tenant linking.
///     </para>
///     <para>
///         The production-default implementation (<c>PlaceholderContactResolver</c>)
///         rejects every GUID. Integration tests register <c>SeedableContactResolver</c>
///         via the <c>ENABLE_CONTACT_TEST_SEEDING</c> environment flag and seed
///         contact ids through the test-only <c>/api/testing/seed-contact</c>
///         endpoint.
///     </para>
/// </remarks>
public interface IContactResolver
{
    /// <summary>
    ///     Throws <c>NotFoundException</c> with <c>ROLE_CONTACT_NOT_FOUND</c> if the
    ///     contact does not exist or is not owned by <paramref name="userId" />.
    /// </summary>
    Task EnsureOwnedByAsync(Guid contactId, Guid userId, CancellationToken ct = default);
}
