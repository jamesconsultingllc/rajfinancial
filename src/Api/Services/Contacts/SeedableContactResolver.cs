using System.Collections.Concurrent;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Shared.Contracts.Entities;

namespace RajFinancial.Api.Services.Contacts;

/// <summary>
///     Integration-test resolver that permits only explicitly seeded
///     <c>(contactId, ownerUserId)</c> pairs. Any unseeded GUID is rejected
///     with <c>ROLE_CONTACT_NOT_FOUND</c>, so tests still model the real
///     Phase 2 invariant ("a contact must exist before you can assign it a role").
/// </summary>
/// <remarks>
///     <para>
///         Registered as a singleton when the <c>ENABLE_CONTACT_TEST_SEEDING</c>
///         environment variable is set to <c>true</c>. Seeded from the test
///         runner via the <c>/api/testing/seed-contact</c> endpoint.
///     </para>
///     <para>
///         Never registered in production. The startup guard in <c>Program.cs</c>
///         refuses to wire this up unless the flag is explicitly set.
///     </para>
/// </remarks>
public sealed class SeedableContactResolver : IContactResolver
{
    private readonly ConcurrentDictionary<Guid, Guid> ownerByContact = new();

    /// <summary>Adds or updates a seeded <c>(contactId, ownerUserId)</c> pair.</summary>
    public void Seed(Guid contactId, Guid ownerUserId) =>
        ownerByContact[contactId] = ownerUserId;

    /// <summary>Clears all seeded contacts. Primarily for test-run resets.</summary>
    public void Clear() => ownerByContact.Clear();

    public Task EnsureOwnedByAsync(Guid contactId, Guid userId, CancellationToken ct = default)
    {
        if (!ownerByContact.TryGetValue(contactId, out var owner) || owner != userId)
        {
            throw new NotFoundException(
                EntityErrorCodes.ROLE_CONTACT_NOT_FOUND,
                $"Contact {contactId} was not found for user {userId}.");
        }

        return Task.CompletedTask;
    }
}
