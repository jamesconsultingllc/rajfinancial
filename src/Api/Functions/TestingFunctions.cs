using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Services.Contacts;

namespace RajFinancial.Api.Functions;

/// <summary>
///     Test-only endpoints for seeding integration test data into the running host.
///     Guarded by the <c>ENABLE_CONTACT_TEST_SEEDING</c> environment flag; returns
///     404 from every route when the flag is not set, so the endpoint cannot be
///     reached in production even if the assembly is deployed there.
/// </summary>
/// <remarks>
///     The Functions host runs out-of-process from the test runner, so Reqnroll
///     steps cannot directly mutate DI singletons. This bridge lets
///     <c>Given a contact "x" exists for user "y"</c> steps seed the
///     <see cref="SeedableContactResolver" /> via an HTTP POST.
/// </remarks>
public partial class TestingFunctions(
    ILogger<TestingFunctions> logger,
    IContactResolver contactResolver)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    ///     Seeds a single <c>(contactId, ownerUserId)</c> pair into the seedable
    ///     contact resolver. Returns 404 if the feature is disabled or if the
    ///     resolver is not the seedable variant.
    /// </summary>
    [Function("SeedContact")]
    public async Task<HttpResponseData> SeedContact(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "testing/seed-contact")]
        HttpRequestData req)
    {
        if (!IsTestSeedingEnabled() || contactResolver is not SeedableContactResolver seedable)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        SeedContactRequest? payload;
        string bodyText = string.Empty;
        try
        {
            if (req.FunctionContext.Items.TryGetValue(FunctionContextKeys.RequestBody, out var bodyObj)
                && bodyObj is string stashed
                && !string.IsNullOrWhiteSpace(stashed))
            {
                bodyText = stashed;
            }
            else
            {
                using var reader = new StreamReader(req.Body);
                bodyText = await reader.ReadToEndAsync();
            }

            payload = string.IsNullOrWhiteSpace(bodyText)
                ? null
                : JsonSerializer.Deserialize<SeedContactRequest>(bodyText, JsonOptions);
        }
        catch (JsonException ex)
        {
            LogInvalidSeedJson(ex);
            var errResp = req.CreateResponse(HttpStatusCode.BadRequest);
            await errResp.WriteStringAsync($"Invalid JSON: {ex.Message}");
            return errResp;
        }

        if (payload is null || payload.ContactId == Guid.Empty || payload.UserId == Guid.Empty)
        {
            var errResp = req.CreateResponse(HttpStatusCode.BadRequest);
            await errResp.WriteStringAsync(
                $"Bad seed payload. Parsed ContactId={payload?.ContactId}, UserId={payload?.UserId}. RawBody={bodyText}");
            return errResp;
        }

        seedable.Seed(payload.ContactId, payload.UserId);
        LogContactSeeded(payload.ContactId, payload.UserId);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    /// <summary>Clears every seeded contact. Called between scenarios.</summary>
    [Function("ResetSeededContacts")]
    public HttpResponseData ResetSeededContacts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "testing/reset-contacts")]
        HttpRequestData req)
    {
        if (!IsTestSeedingEnabled() || contactResolver is not SeedableContactResolver seedable)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        seedable.Clear();
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    private static bool IsTestSeedingEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("ENABLE_CONTACT_TEST_SEEDING"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private sealed record SeedContactRequest(Guid ContactId, Guid UserId);

    [LoggerMessage(EventId = 9501, Level = LogLevel.Warning, Message = "Invalid JSON on seed-contact request")]
    private partial void LogInvalidSeedJson(Exception ex);

    [LoggerMessage(EventId = 9502, Level = LogLevel.Information, Message = "Seeded test contact {ContactId} -> user {UserId}")]
    private partial void LogContactSeeded(Guid contactId, Guid userId);
}
