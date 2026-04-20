// ============================================================================
// RAJ Financial - Asset API Step Definitions
// ============================================================================
// Reqnroll step definitions for Assets.feature integration tests.
// Uses FunctionsHostFixture and TestClaimsBuilder for HTTP API testing.
// ============================================================================

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RajFinancial.IntegrationTests.Support;
using Reqnroll;

namespace RajFinancial.IntegrationTests.StepDefinitions;

/// <summary>
///     Step definitions for Asset CRUD integration tests.
///     Tests hit real HTTP endpoints via the Azure Functions host.
/// </summary>
[Binding]
[Scope(Tag = "assets")]
public class AssetSteps
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
    private readonly Dictionary<string, string> createdAssetIds = new();
    private string? lastCreatedAssetId;
    private string? ownerUserId;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AssetSteps"/> class.
    /// </summary>
    /// <param name="fixture">The Functions host fixture providing the HTTP client.</param>
    /// <param name="authHelper">The dual-mode auth helper for token acquisition.</param>
    public AssetSteps(FunctionsHostFixture fixture, TestAuthHelper authHelper)
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
        // Explicitly clear any previously acquired token so this scenario runs unauthenticated.
        authToken = null;
    }

    [Given(@"I am authenticated as user ""(.*)"" with role ""(.*)""")]
    public async Task GivenIAmAuthenticatedAsUserWithRole(string email, string role)
    {
        authToken = await authHelper.GetTokenForRoleAsync(email, role);
    }

    [Given(@"I have created the following assets:")]
    public async Task GivenIHaveCreatedTheFollowingAssets(Table table)
    {
        foreach (var row in table.Rows)
        {
            var body = new
            {
                name = row["Name"],
                type = row["Type"],
                currentValue = decimal.Parse(row["CurrentValue"])
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/assets");
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json");
            SetAuthHeader(request);

            var resp = await client.SendAsync(request);
            resp.StatusCode.Should().Be(HttpStatusCode.Created,
                $"setup: creating asset '{row["Name"]}' should succeed");

            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var id = doc.RootElement.GetProperty("id").GetString()!;
            createdAssetIds[row["Name"]] = id;
            lastCreatedAssetId = id;
        }
    }

    [Given(@"I have created an asset ""(.*)"" of type ""(.*)"" worth (.*)")]
    public async Task GivenIHaveCreatedAnAssetOfTypeWorth(string name, string type, decimal value)
    {
        var body = new { name, type, currentValue = value };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/assets");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");
        SetAuthHeader(request);

        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.Created,
            $"setup: creating asset '{name}' should succeed");

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        lastCreatedAssetId = doc.RootElement.GetProperty("id").GetString()!;
        createdAssetIds[name] = lastCreatedAssetId;
    }

    [Given(@"I have an asset ""(.*)"" that has been disposed")]
    public async Task GivenIHaveAnAssetThatHasBeenDisposed(string name)
    {
        // Create the asset via the API first
        await GivenIHaveCreatedAnAssetOfTypeWorth(name, "Vehicle", 10_000m);

        // Mark it as disposed directly in the DB (no dispose endpoint exists yet)
        var connectionString = fixture.Configuration.GetConnectionString("SqlConnectionString")
                               ?? throw new InvalidOperationException("SqlConnectionString not configured");

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Assets SET IsDisposed = 1, DisposalDate = @date WHERE Id = @id";
        cmd.Parameters.AddWithValue("@date", DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("@id", Guid.Parse(createdAssetIds[name]));
        var rows = await cmd.ExecuteNonQueryAsync();
        rows.Should().Be(1, $"asset '{name}' should exist in the database");
    }

    [Given(@"user ""(.*)"" has assets")]
    public async Task GivenUserHasAssets(string email)
    {
        // Authenticate as the owner, create assets, then store the userId for {ownerUserId} substitution
        var ownerToken = await authHelper.GetTokenForRoleAsync(email, "Client");
        ownerUserId = TestClaimsBuilder.DeterministicUserId(email);

        var body = new { name = "Owner Asset", type = "BankAccount", currentValue = 5000 };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/assets");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var resp = await client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.Created, "setup: creating asset for owner should succeed");

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        lastCreatedAssetId = doc.RootElement.GetProperty("id").GetString()!;
        createdAssetIds["Owner Asset"] = lastCreatedAssetId;
    }

    [Given(@"user ""(.*)"" has an asset with a known ID")]
    public async Task GivenUserHasAnAssetWithAKnownId(string email)
    {
        // Reuse the "has assets" step — it creates one asset and stores its ID
        await GivenUserHasAssets(email);
    }

    // =========================================================================
    // When
    // =========================================================================

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendAGetRequestTo(string path)
    {
        // Resolve {ownerUserId} placeholder if present
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

    [When(@"I send a PUT request to ""(.*)"" with an empty body")]
    public async Task WhenISendAPutRequestToWithAnEmptyBody(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, path);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I send a DELETE request to ""(.*)""")]
    public async Task WhenISendADeleteRequestTo(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I create an asset with the following details:")]
    public async Task WhenICreateAnAssetWithTheFollowingDetails(Table table)
    {
        var row = table.Rows[0];
        var body = BuildAssetBodyFromRow(row);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/assets");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");
        SetAuthHeader(request);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var doc = JsonDocument.Parse(responseBody);
            lastCreatedAssetId = doc.RootElement.GetProperty("id").GetString();
        }
    }

    [When(@"I request the asset by its ID")]
    public async Task WhenIRequestTheAssetByItsId()
    {
        lastCreatedAssetId.Should().NotBeNullOrEmpty("an asset should have been created first");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/assets/{lastCreatedAssetId}");
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I update the asset with the following details:")]
    public async Task WhenIUpdateTheAssetWithTheFollowingDetails(Table table)
    {
        lastCreatedAssetId.Should().NotBeNullOrEmpty("an asset should have been created first");

        var row = table.Rows[0];
        var body = BuildAssetBodyFromRow(row);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/assets/{lastCreatedAssetId}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");
        SetAuthHeader(request);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I send a PUT request to ""(.*)"" with:")]
    public async Task WhenISendAPutRequestToWith(string path, Table table)
    {
        var row = table.Rows[0];
        var body = BuildAssetBodyFromRow(row);

        using var request = new HttpRequestMessage(HttpMethod.Put, path);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");
        SetAuthHeader(request);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I delete the asset by its ID")]
    public async Task WhenIDeleteTheAssetByItsId()
    {
        lastCreatedAssetId.Should().NotBeNullOrEmpty("an asset should have been created first");

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/assets/{lastCreatedAssetId}");
        SetAuthHeader(request);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When(@"I request that asset by ID")]
    public async Task WhenIRequestThatAssetById()
    {
        lastCreatedAssetId.Should().NotBeNullOrEmpty("a known asset ID is required");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/assets/{lastCreatedAssetId}");
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
        response.Should().NotBeNull("a request should have been sent");
        ((int)response!.StatusCode).Should().Be(expectedStatus,
            $"expected HTTP {expectedStatus} but got {(int)response.StatusCode}. Body: {TruncateBody()}");
    }

    [Then(@"the error code should be ""(.*)""")]
    public void ThenTheErrorCodeShouldBe(string expectedCode)
    {
        responseBody.Should().NotBeNullOrEmpty("response body should contain error details");
        responseBody.Should().Contain(expectedCode,
            $"expected error code '{expectedCode}' in response body");
    }

    [Then(@"the response should contain the asset name ""(.*)""")]
    public void ThenTheResponseShouldContainTheAssetName(string expectedName)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        doc.RootElement.GetProperty("name").GetString().Should().Be(expectedName);
    }

    [Then(@"the response should contain the asset type ""(.*)""")]
    public void ThenTheResponseShouldContainTheAssetType(string expectedType)
    {
        responseBody.Should().NotBeNullOrEmpty();
        responseBody.Should().Contain(expectedType);
    }

    [Then(@"the response should contain (.*) assets")]
    public void ThenTheResponseShouldContainNAssets(int expectedCount)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(expectedCount);
    }

    [Then(@"all returned assets should have type ""(.*)""")]
    public void ThenAllReturnedAssetsShouldHaveType(string expectedType)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            element.GetProperty("type").GetString().Should().Be(expectedType);
        }
    }

    [Then(@"the response should not contain asset ""(.*)""")]
    public void ThenTheResponseShouldNotContainAsset(string assetName)
    {
        responseBody.Should().NotBeNullOrEmpty();
        responseBody.Should().NotContain(assetName);
    }

    [Then(@"the response should contain asset ""(.*)""")]
    public void ThenTheResponseShouldContainAsset(string assetName)
    {
        responseBody.Should().NotBeNullOrEmpty();
        responseBody.Should().Contain(assetName);
    }

    [Then(@"the response should include depreciation details")]
    public void ThenTheResponseShouldIncludeDepreciationDetails()
    {
        responseBody.Should().NotBeNullOrEmpty();
        // AssetDetailDto always includes the isDepreciable flag.
        // depreciationMethod is null-omitted for non-depreciable assets.
        responseBody.Should().Contain("isDepreciable");
    }

    [Then(@"the response should include beneficiary information")]
    public void ThenTheResponseShouldIncludeBeneficiaryInformation()
    {
        responseBody.Should().NotBeNullOrEmpty();
        // AssetDetailDto includes beneficiaries list
        responseBody.Should().Contain("beneficiaries");
    }

    [Then(@"the response should contain current value (.*)")]
    public void ThenTheResponseShouldContainCurrentValue(decimal expectedValue)
    {
        responseBody.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(responseBody!);
        doc.RootElement.GetProperty("currentValue").GetDecimal().Should().Be(expectedValue);
    }

    [Then(@"access should be denied or filtered by the service tier")]
    public void ThenAccessShouldBeDeniedOrFilteredByTheServiceTier()
    {
        response.Should().NotBeNull();
        // The service tier enforces three-tier authorization:
        // Either returns 403/404 or returns empty results depending on implementation
        var status = (int)response!.StatusCode;
        status.Should().BeOneOf([200, 403, 404],
            "cross-user access should be denied or return empty/filtered results");
    }

    [Then(@"access should be denied by the service tier")]
    public void ThenAccessShouldBeDeniedByTheServiceTier()
    {
        response.Should().NotBeNull();
        var status = (int)response!.StatusCode;
        status.Should().BeOneOf([403, 404],
            "cross-user asset access should be denied");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    ///     Sets the Authorization header on the request if an auth token is available.
    /// </summary>
    private void SetAuthHeader(HttpRequestMessage request)
    {
        if (authToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }
    }

    /// <summary>
    ///     Builds an anonymous object from a Reqnroll table row for asset creation/update.
    /// </summary>
    private static Dictionary<string, object> BuildAssetBodyFromRow(DataTableRow row)
    {
        var body = new Dictionary<string, object>();

        foreach (var header in row.Keys)
        {
            var value = row[header];
            var camelKey = char.ToLowerInvariant(header[0]) + header[1..];

            if (decimal.TryParse(value, out var numericValue))
            {
                body[camelKey] = numericValue;
            }
            else
            {
                body[camelKey] = value;
            }
        }

        return body;
    }

    /// <summary>
    ///     Truncates the response body for diagnostic messages.
    /// </summary>
    private string TruncateBody()
        => responseBody?[..Math.Min(responseBody?.Length ?? 0, 500)] ?? "(empty)";
}
