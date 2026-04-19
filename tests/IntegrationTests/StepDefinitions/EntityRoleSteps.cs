// ============================================================================
// RAJ Financial - Entity Role API Step Definitions
// ============================================================================
// Reqnroll step definitions for EntityRoles.feature integration tests.
// ============================================================================

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Reqnroll;
using RajFinancial.IntegrationTests.Support;

namespace RajFinancial.IntegrationTests.StepDefinitions;

[Binding]
[Scope(Tag = "entity-roles")]
public class EntityRoleSteps
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
    private readonly Dictionary<string, string> entityIds = new();
    private readonly Dictionary<string, Guid> contactIds = new();
    private string? lastRoleId;
    private string? victimEntityId;
    private string? victimRoleId;

    public EntityRoleSteps(FunctionsHostFixture fixture, TestAuthHelper authHelper)
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

    [Given(@"I have a business entity ""(.*)""")]
    public async Task GivenIHaveABusinessEntity(string name)
    {
        entityIds[name] = await CreateEntityWithTokenAsync(authToken!, name, "Business");
    }

    [Given(@"I have a trust entity ""(.*)""")]
    public async Task GivenIHaveATrustEntity(string name)
    {
        entityIds[name] = await CreateEntityWithTokenAsync(authToken!, name, "Trust");
    }

    [Given(@"I have a contact ""(.*)""")]
    public void GivenIHaveAContact(string contactName)
    {
        // Phase 1: Contacts API does not yet exist. Synthesize a stable Guid per name so roles can reference it.
        contactIds[contactName] = DeterministicGuid(contactName);
    }

    [Given(@"I have a business entity ""(.*)"" with roles assigned")]
    public async Task GivenIHaveABusinessEntityWithRolesAssigned(string name)
    {
        entityIds[name] = await CreateEntityWithTokenAsync(authToken!, name, "Business");
        var contactId = DeterministicGuid("Setup Owner");
        await AssignRoleAsync(entityIds[name], contactId, "Owner",
            new Dictionary<string, object> { ["ownershipPercent"] = 100.00m });
    }

    [Given(@"I have a business entity ""(.*)"" with a role assigned")]
    public async Task GivenIHaveABusinessEntityWithARoleAssigned(string name)
    {
        await GivenIHaveABusinessEntityWithRolesAssigned(name);
    }

    [Given(@"I have a business entity ""(.*)"" with an owner at (.*)%")]
    public async Task GivenIHaveABusinessEntityWithAnOwnerAt(string name, decimal percent)
    {
        entityIds[name] = await CreateEntityWithTokenAsync(authToken!, name, "Business");
        var contactId = DeterministicGuid("Existing Owner");
        await AssignRoleAsync(entityIds[name], contactId, "Owner",
            new Dictionary<string, object> { ["ownershipPercent"] = percent });
    }

    [Given(@"another user ""(.*)"" has a business entity ""(.*)""")]
    public async Task GivenAnotherUserHasABusinessEntity(string email, string name)
    {
        var otherToken = await authHelper.GetTokenForRoleAsync(email, "Client");
        victimEntityId = await CreateEntityWithTokenAsync(otherToken, name, "Business");
        entityIds[name] = victimEntityId;
    }

    [Given(@"another user ""(.*)"" has a business entity ""(.*)"" with a role assigned")]
    public async Task GivenAnotherUserHasABusinessEntityWithARoleAssigned(string email, string name)
    {
        var otherToken = await authHelper.GetTokenForRoleAsync(email, "Client");
        victimEntityId = await CreateEntityWithTokenAsync(otherToken, name, "Business");
        entityIds[name] = victimEntityId;

        var body = new
        {
            contactId = DeterministicGuid("Victim Owner"),
            roleType = "Owner",
            ownershipPercent = 100.00m
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/api/entities/{victimEntityId}/roles");
        req.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var resp = await client.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        victimRoleId = doc.RootElement.GetProperty("id").GetString();
    }

    // =========================================================================
    // When
    // =========================================================================

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendAGetRequestTo(string path)
    {
        path = ResolvePlaceholders(path);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I send a POST request to ""(.*)"" with an empty body")]
    public async Task WhenISendAPostRequestToWithAnEmptyBody(string path)
    {
        path = ResolvePlaceholders(path);
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I send a DELETE request to ""(.*)""")]
    public async Task WhenISendADeleteRequestTo(string path)
    {
        path = ResolvePlaceholders(path);
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I assign role ""(.*)"" to contact ""(.*)"" on entity ""(.*)"" with:")]
    public async Task WhenIAssignRoleToContactOnEntityWith(string roleType, string contactName, string entityName, Table table)
    {
        var row = table.Rows[0];
        var extras = BuildBodyFromRow(row);
        await AssignRoleWithExtrasAsync(entityName, contactName, roleType, extras);
    }

    [When(@"I assign role ""(.*)"" to contact ""(.*)"" on entity ""(.*)""")]
    public async Task WhenIAssignRoleToContactOnEntity(string roleType, string contactName, string entityName)
    {
        await AssignRoleWithExtrasAsync(entityName, contactName, roleType, new Dictionary<string, object>());
    }

    [When(@"I assign role ""(.*)"" to contact ""(.*)"" on entity with id ""(.*)""")]
    public async Task WhenIAssignRoleToContactOnEntityWithId(string roleType, string contactName, string entityIdPlaceholder)
    {
        var entityId = ResolvePlaceholders(entityIdPlaceholder);
        var contactId = contactIds.TryGetValue(contactName, out var cid) ? cid : DeterministicGuid(contactName);
        var body = new Dictionary<string, object>
        {
            ["contactId"] = contactId,
            ["roleType"] = roleType
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/entities/{entityId}/roles");
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I delete the role by its ID")]
    public async Task WhenIDeleteTheRoleByItsId()
    {
        lastRoleId.Should().NotBeNullOrEmpty();
        var entityId = entityIds.Values.LastOrDefault();
        entityId.Should().NotBeNullOrEmpty();

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/entities/{entityId}/roles/{lastRoleId}");
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

    [Then(@"the response should contain role type ""(.*)""")]
    public void ThenTheResponseShouldContainRoleType(string expected)
    {
        responseBody.Should().NotBeNullOrEmpty();
        responseBody.Should().Contain(expected);
    }

    [Then(@"the response should contain ownership percent (.*)")]
    public void ThenTheResponseShouldContainOwnershipPercent(decimal expected)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        doc.RootElement.GetProperty("ownershipPercent").GetDecimal().Should().Be(expected);
    }

    [Then(@"the response should contain beneficial interest percent (.*)")]
    public void ThenTheResponseShouldContainBeneficialInterestPercent(decimal expected)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        doc.RootElement.GetProperty("beneficialInterestPercent").GetDecimal().Should().Be(expected);
    }

    [Then(@"the response should contain at least (.*) role")]
    public void ThenTheResponseShouldContainAtLeastNRole(int expectedCount)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(expectedCount);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private async Task AssignRoleWithExtrasAsync(string entityName, string contactName, string roleType, Dictionary<string, object> extras)
    {
        var entityId = entityIds[entityName];
        var contactId = contactIds.TryGetValue(contactName, out var cid) ? cid : DeterministicGuid(contactName);

        var body = new Dictionary<string, object>
        {
            ["contactId"] = contactId,
            ["roleType"] = roleType
        };
        foreach (var kv in extras)
            body[kv.Key] = kv.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/entities/{entityId}/roles");
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        SetAuthHeader(request);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var doc = JsonDocument.Parse(responseBody);
            lastRoleId = doc.RootElement.GetProperty("id").GetString();
        }
    }

    private async Task AssignRoleAsync(string entityId, Guid contactId, string roleType, Dictionary<string, object> extras)
    {
        var body = new Dictionary<string, object>
        {
            ["contactId"] = contactId,
            ["roleType"] = roleType
        };
        foreach (var kv in extras)
            body[kv.Key] = kv.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/entities/{entityId}/roles");
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        SetAuthHeader(request);
        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.Created, $"setup: assigning role '{roleType}' should succeed");

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        lastRoleId = doc.RootElement.GetProperty("id").GetString();
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

    private void SetAuthHeader(HttpRequestMessage request)
    {
        if (authToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
    }

    private string ResolvePlaceholders(string path)
    {
        if (victimEntityId is not null)
            path = path.Replace("{victimEntityId}", victimEntityId);
        if (victimRoleId is not null)
            path = path.Replace("{victimRoleId}", victimRoleId);

        var anyEntityId = entityIds.Values.LastOrDefault();
        if (anyEntityId is not null)
            path = path.Replace("{entityId}", anyEntityId);

        return path;
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
            else if (decimal.TryParse(value, out var numericValue))
                body[camelKey] = numericValue;
            else
                body[camelKey] = value;
        }
        return body;
    }

    private static Guid DeterministicGuid(string seed)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var guidBytes = new byte[16];
        Array.Copy(bytes, guidBytes, 16);
        return new Guid(guidBytes);
    }

    private string TruncateBody()
        => responseBody?[..Math.Min(responseBody?.Length ?? 0, 500)] ?? "(empty)";
}
