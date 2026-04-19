using System.Net;
using System.Net.Http.Json;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
///     Posts seeded <c>(contactId, ownerUserId)</c> pairs to the running Functions
///     host's <c>/api/testing/seed-contact</c> endpoint. The host only exposes the
///     endpoint when the <c>ENABLE_CONTACT_TEST_SEEDING</c> environment variable
///     is set (see <c>src/Api/local.settings.json</c>). A 404 response means the
///     flag is off — in that case the tests should fail loudly rather than pretend
///     to have seeded.
/// </summary>
public static class ContactSeedingHelper
{
    /// <summary>Seeds a single contact → owner mapping.</summary>
    public static async Task SeedAsync(HttpClient client, Guid contactId, Guid ownerUserId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/testing/seed-contact",
            new { contactId, userId = ownerUserId });

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                "Functions host returned 404 from /api/testing/seed-contact. " +
                "Ensure ENABLE_CONTACT_TEST_SEEDING=true is set in src/Api/local.settings.json " +
                "and that the host has been restarted after the change.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Seed-contact returned {(int)response.StatusCode} {response.StatusCode} for " +
                $"contactId={contactId}, userId={ownerUserId}. Body: {body}");
        }
    }

    /// <summary>Clears every seeded contact. Invoke between scenarios.</summary>
    public static async Task ResetAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/testing/reset-contacts", content: null);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }
}
