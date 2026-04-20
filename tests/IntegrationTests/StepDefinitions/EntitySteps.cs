// ============================================================================
// RAJ Financial - Entity API Step Definitions
// ============================================================================
// Reqnroll step definitions for Entities.feature integration tests.
// Uses FunctionsHostFixture and TestClaimsBuilder for HTTP API testing.
// ============================================================================

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using RajFinancial.IntegrationTests.Support;
using Reqnroll;

namespace RajFinancial.IntegrationTests.StepDefinitions;

/// <summary>
///     Step definitions for Entity CRUD integration tests.
///     Tests hit real HTTP endpoints via the Azure Functions host.
/// </summary>
[Binding]
[Scope(Tag = "entities")]
public class EntitySteps
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly FunctionsHostFixture fixture;
    private readonly TestAuthHelper authHelper;
    private readonly HttpClient client;
    private HttpResponseMessage? response;
    private string? responseBody;
    private string? authToken;
    private string? lastCreatedEntityId;
    private string? ownerUserId;

    // createdEntityIds map retained for step definitions that reference prior "Owner Co" / named
    // entities by label; queries happen in downstream step assertions via lastCreatedEntityId.
    // ReSharper disable once CollectionNeverQueried.Local
    private readonly Dictionary<string, string> createdEntityIds = new();

    public EntitySteps(FunctionsHostFixture fixture, TestAuthHelper authHelper)
    {
        this.fixture = fixture;
        this.authHelper = authHelper;
        client = fixture.Client;
    }

    // =========================================================================
    // Given
    // =========================================================================

    [Given("the API is running")]
    public async Task GivenTheApiIsRunning()
    {
        await fixture.EnsureHostIsRunningAsync();
    }

    [Given("I am not authenticated")]
    public void GivenIAmNotAuthenticated()
    {
        authToken = null;
    }

    [Given(@"I am authenticated as user ""(.*)"" with role ""(.*)""")]
    public async Task GivenIAmAuthenticatedAsUserWithRole(string email, string role)
    {
        authToken = await authHelper.GetTokenForRoleAsync(email, role);
    }

    [Given(@"I have created a business entity ""(.*)""")]
    public async Task GivenIHaveCreatedABusinessEntity(string name)
    {
        await CreateEntityAsync(name, "Business");
    }

    [Given(@"I have created a trust entity ""(.*)""")]
    public async Task GivenIHaveCreatedATrustEntity(string name)
    {
        await CreateEntityAsync(name, "Trust");
    }

    [Given(@"user ""(.*)"" has a business entity")]
    public async Task GivenUserHasABusinessEntity(string email)
    {
        var ownerToken = await authHelper.GetTokenForRoleAsync(email, "Client");
        ownerUserId = TestClaimsBuilder.DeterministicUserId(email);
        lastCreatedEntityId = await CreateEntityWithTokenAsync(ownerToken, "Owner Co", "Business");
        createdEntityIds["Owner Co"] = lastCreatedEntityId;
    }

    [Given(@"user ""(.*)"" has entities")]
    public async Task GivenUserHasEntities(string email)
    {
        var ownerToken = await authHelper.GetTokenForRoleAsync(email, "Client");
        ownerUserId = TestClaimsBuilder.DeterministicUserId(email);
        await CreateEntityWithTokenAsync(ownerToken, "Owner Business", "Business");
    }

    // =========================================================================
    // When
    // =========================================================================

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendAGetRequestTo(string path)
    {
        if (ownerUserId is not null)
            path = path.Replace("{ownerUserId}", ownerUserId);

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I send a POST request to ""(.*)"" with an empty body")]
    public async Task WhenISendAPostRequestToWithAnEmptyBody(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I create an entity with the following details:")]
    public async Task WhenICreateAnEntityWithTheFollowingDetails(Table table)
    {
        var row = table.Rows[0];
        var body = BuildBodyFromRow(row);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/entities");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8, "application/json");
        SetAuthHeader(request);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var doc = JsonDocument.Parse(responseBody);
            lastCreatedEntityId = doc.RootElement.GetProperty("id").GetString();
        }
    }

    [When(@"I create a business entity ""(.*)"" with:")]
    public async Task WhenICreateABusinessEntityWith(string name, Table table)
    {
        var row = table.Rows[0];
        var metadata = BuildBodyFromRow(row);
        var body = new Dictionary<string, object>
        {
            ["name"] = name,
            ["type"] = "Business",
            ["business"] = metadata
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/entities");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8, "application/json");
        SetAuthHeader(request);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var doc = JsonDocument.Parse(responseBody);
            lastCreatedEntityId = doc.RootElement.GetProperty("id").GetString();
        }
    }

    [When(@"I create a trust entity ""(.*)"" with:")]
    public async Task WhenICreateATrustEntityWith(string name, Table table)
    {
        var row = table.Rows[0];
        var metadata = BuildBodyFromRow(row);
        var body = new Dictionary<string, object>
        {
            ["name"] = name,
            ["type"] = "Trust",
            ["trust"] = metadata
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/entities");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8, "application/json");
        SetAuthHeader(request);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var doc = JsonDocument.Parse(responseBody);
            lastCreatedEntityId = doc.RootElement.GetProperty("id").GetString();
        }
    }

    [When(@"I request the entity by its ID")]
    public async Task WhenIRequestTheEntityByItsId()
    {
        lastCreatedEntityId.Should().NotBeNullOrEmpty();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/entities/{lastCreatedEntityId}");
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I request that entity by ID")]
    public async Task WhenIRequestThatEntityById()
    {
        await WhenIRequestTheEntityByItsId();
    }

    [When(@"I update the entity with name ""(.*)""")]
    public async Task WhenIUpdateTheEntityWithName(string newName)
    {
        lastCreatedEntityId.Should().NotBeNullOrEmpty();
        var body = new { name = newName };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/entities/{lastCreatedEntityId}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8, "application/json");
        SetAuthHeader(request);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I update my Personal entity with name ""(.*)""")]
    public async Task WhenIUpdateMyPersonalEntityWithName(string newName)
    {
        var personalId = await GetPersonalEntityIdAsync();
        var body = new { name = newName };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/entities/{personalId}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8, "application/json");
        SetAuthHeader(request);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I delete the entity by its ID")]
    public async Task WhenIDeleteTheEntityByItsId()
    {
        lastCreatedEntityId.Should().NotBeNullOrEmpty();
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/entities/{lastCreatedEntityId}");
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I try to delete my Personal entity")]
    public async Task WhenITryToDeleteMyPersonalEntity()
    {
        var personalId = await GetPersonalEntityIdAsync();
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/entities/{personalId}");
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    // =========================================================================
    // Then
    // =========================================================================

    [Then(@"the response status should be (.*)")]
    public void ThenTheResponseStatusShouldBe(int expectedStatus)
    {
        response.Should().NotBeNull();
        ((int)response!.StatusCode).Should().Be(expectedStatus,
            $"expected HTTP {expectedStatus} but got {(int)response.StatusCode}. Body: {TruncateBody()}");
    }

    [Then(@"the error code should be ""(.*)""")]
    public void ThenTheErrorCodeShouldBe(string expectedCode)
    {
        responseBody.Should().NotBeNullOrEmpty();
        responseBody.Should().Contain(expectedCode);
    }

    [Then(@"the response should contain the entity name ""(.*)""")]
    public void ThenTheResponseShouldContainTheEntityName(string expectedName)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        doc.RootElement.GetProperty("name").GetString().Should().Be(expectedName);
    }

    [Then(@"the response should contain the entity type ""(.*)""")]
    public void ThenTheResponseShouldContainTheEntityType(string expectedType)
    {
        responseBody.Should().NotBeNullOrEmpty();
        responseBody.Should().Contain(expectedType);
    }

    [Then(@"the response should contain a non-empty slug")]
    public void ThenTheResponseShouldContainANonEmptySlug()
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        doc.RootElement.GetProperty("slug").GetString().Should().NotBeNullOrEmpty();
    }

    [Then(@"the response should contain EIN ""(.*)""")]
    public void ThenTheResponseShouldContainEin(string expectedEin)
    {
        responseBody.Should().NotBeNullOrEmpty();
        responseBody.Should().Contain(expectedEin);
    }

    [Then(@"the response should contain jurisdiction ""(.*)""")]
    public void ThenTheResponseShouldContainJurisdiction(string expected)
    {
        responseBody.Should().NotBeNullOrEmpty();
        responseBody.Should().Contain(expected);
    }

    [Then(@"the response should contain at least (.*) entities")]
    public void ThenTheResponseShouldContainAtLeastNEntities(int expectedCount)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(expectedCount);
    }

    [Then(@"the response should contain at least (.*) entity")]
    public void ThenTheResponseShouldContainAtLeastNEntity(int expectedCount)
    {
        ThenTheResponseShouldContainAtLeastNEntities(expectedCount);
    }

    [Then(@"the response should contain an entity of type ""(.*)""")]
    public void ThenTheResponseShouldContainAnEntityOfType(string expectedType)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        var found = false;
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            if (string.Equals(element.GetProperty("type").GetString(), expectedType, StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                break;
            }
        }
        found.Should().BeTrue($"response should contain an entity of type '{expectedType}'");
    }

    [Then(@"all returned entities should have type ""(.*)""")]
    public void ThenAllReturnedEntitiesShouldHaveType(string expectedType)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            element.GetProperty("type").GetString().Should().Be(expectedType);
        }
    }

    [Then(@"access should be denied by the service tier")]
    public void ThenAccessShouldBeDeniedByTheServiceTier()
    {
        response.Should().NotBeNull();
        var status = (int)response!.StatusCode;
        status.Should().Be(404,
            "cross-user entity access must return 404 (not 403) to avoid leaking resource existence (OWASP A01).");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void SetAuthHeader(HttpRequestMessage request)
    {
        if (authToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
    }

    private async Task CreateEntityAsync(string name, string type)
    {
        var body = new { name, type };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/entities");
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        SetAuthHeader(request);

        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.Created,
            $"setup: creating entity '{name}' should succeed");

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        lastCreatedEntityId = doc.RootElement.GetProperty("id").GetString()!;
        createdEntityIds[name] = lastCreatedEntityId;
    }

    private async Task<string> CreateEntityWithTokenAsync(string token, string name, string type)
    {
        var body = new { name, type };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/entities");
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    private async Task<string> GetPersonalEntityIdAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/entities?type=Personal");
        SetAuthHeader(request);
        var resp = await client.SendAsync(request);
        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var first = doc.RootElement.EnumerateArray().FirstOrDefault();
        first.ValueKind.Should().NotBe(JsonValueKind.Undefined, "a Personal entity should exist");
        return first.GetProperty("id").GetString()!;
    }

    private static Dictionary<string, object> BuildBodyFromRow(DataTableRow row)
    {
        var body = new Dictionary<string, object>();
        foreach (var header in row.Keys)
        {
            var value = row[header];
            var camelKey = char.ToLowerInvariant(header[0]) + header[1..];

            if (bool.TryParse(value, out var boolValue))
                body[camelKey] = boolValue;
            else if (decimal.TryParse(value, out var numericValue) && !string.IsNullOrEmpty(value))
                body[camelKey] = numericValue;
            else
                body[camelKey] = value;
        }
        return body;
    }

    private string TruncateBody()
        => responseBody?[..Math.Min(responseBody?.Length ?? 0, 500)] ?? "(empty)";
}
