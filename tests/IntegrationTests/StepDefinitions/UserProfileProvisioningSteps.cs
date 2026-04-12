using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using RajFinancial.IntegrationTests.Support;

namespace RajFinancial.IntegrationTests.StepDefinitions;

/// <summary>
/// Step definitions for UserProfile JIT provisioning integration tests.
/// Verifies that authenticated API calls trigger automatic UserProfile creation
/// and that subsequent calls update claim-sourced fields and LastLoginAt.
/// </summary>
/// <remarks>
/// These tests hit real HTTP endpoints via the Azure Functions host.
/// Provisioning is verified through the <c>/api/profile/me</c> endpoint which
/// returns the persisted <see cref="RajFinancial.Shared.Entities.UserProfile"/> data,
/// eliminating the need for direct database access in integration tests.
/// </remarks>
[Binding]
[Scope(Tag = "userprofile")]
public class UserProfileProvisioningSteps
{
    private readonly FunctionsHostFixture _fixture;
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private string? _responseBody;
    private JsonElement _responseJson;

    // Track test user identity across steps
    private string _testUserId = null!;
    private string _testUserEmail = null!;
    private string _testUserName = null!;
    private string _testUserRole = "Client";

    // Track all emails used during a scenario for cleanup
    private readonly List<string> _emailsToCleanup = [];

    // For tracking timing of provisioning
    private DateTimeOffset _requestTimestamp;

    // For "returning user" scenarios — captures LastLoginAt from the initial request
    private DateTimeOffset _previousLastLoginAt;

    private readonly TestAuthHelper _authHelper;

    public UserProfileProvisioningSteps(FunctionsHostFixture fixture, TestAuthHelper authHelper)
    {
        _fixture = fixture;
        _authHelper = authHelper;
        _client = fixture.Client;
    }

    // =========================================================================
    // Lifecycle — cleanup after each scenario
    // =========================================================================

    /// <summary>
    /// Removes any <c>UserProfile</c> rows created during the scenario so that
    /// subsequent runs start from a clean slate. Cleans up by both user ID and
    /// any emails registered during the scenario.
    /// </summary>
    [AfterScenario]
    public async Task CleanupAfterScenario()
    {
        var connStr = _fixture.Configuration.GetConnectionString("SqlConnectionString");
        if (string.IsNullOrEmpty(connStr))
            return;

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        // Delete by user ID (covers the primary profile)
        if (!string.IsNullOrEmpty(_testUserId))
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM UserProfiles WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", Guid.Parse(_testUserId));
            await cmd.ExecuteNonQueryAsync();
        }

        // Delete by any emails registered during the scenario
        foreach (var email in _emailsToCleanup)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM UserProfiles WHERE Email = @email";
            cmd.Parameters.AddWithValue("@email", email);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // =========================================================================
    // Given
    // =========================================================================

    [Given("the Functions host is running")]
    public async Task GivenTheFunctionsHostIsRunning()
    {
        await _fixture.EnsureHostIsRunningAsync();
    }

    [Given("no UserProfile exists for a new test user")]
    public void GivenNoUserProfileExistsForANewTestUser()
    {
        // Each test run uses a unique GUID, so no profile will exist
        _testUserId = Guid.NewGuid().ToString();
        _testUserEmail = $"testuser-{_testUserId[..8]}@example.com";
        _testUserName = $"Test User {_testUserId[..8]}";
        _testUserRole = "Client";
    }

    [Given("no UserProfile exists for a new admin test user")]
    public void GivenNoUserProfileExistsForANewAdminTestUser()
    {
        _testUserId = Guid.NewGuid().ToString();
        _testUserEmail = $"admin-{_testUserId[..8]}@example.com";
        _testUserName = $"Admin {_testUserId[..8]}";
        _testUserRole = "Administrator";
    }

    [Given("no UserProfile exists for a new advisor test user")]
    public void GivenNoUserProfileExistsForANewAdvisorTestUser()
    {
        _testUserId = Guid.NewGuid().ToString();
        _testUserEmail = $"advisor-{_testUserId[..8]}@example.com";
        _testUserName = $"Advisor {_testUserId[..8]}";
        _testUserRole = "Advisor";
    }

    [Given("a UserProfile already exists for a returning test user")]
    public async Task GivenAUserProfileAlreadyExistsForAReturningTestUser()
    {
        // Create the profile by making an initial authenticated request
        _testUserId = Guid.NewGuid().ToString();
        _testUserEmail = $"returning-{_testUserId[..8]}@example.com";
        _testUserName = $"Returning User {_testUserId[..8]}";
        _testUserRole = "Client";

        var token = await _authHelper.GetTokenForRoleAsync(_testUserEmail, _testUserRole, _testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var setupResponse = await _client.SendAsync(request);
        setupResponse.IsSuccessStatusCode.Should().BeTrue(
            "initial request to create profile should succeed");

        // Back-date LastLoginAt >5 minutes so the throttle allows it to advance
        // on the next request (the service throttles updates within 5 minutes).
        var connStr = _fixture.Configuration.GetConnectionString("SqlConnectionString");
        connStr.Should().NotBeNullOrEmpty(
            "SqlConnectionString is required for returning-user provisioning tests");

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        await using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = "UPDATE UserProfiles SET LastLoginAt = DATEADD(MINUTE, -6, LastLoginAt) WHERE Id = @id";
        updateCmd.Parameters.AddWithValue("@id", Guid.Parse(_testUserId));
        await updateCmd.ExecuteNonQueryAsync();

        // Re-read the actual back-dated value from DB (avoids drift from app/DB time differences)
        await using var readCmd = conn.CreateCommand();
        readCmd.CommandText = "SELECT LastLoginAt FROM UserProfiles WHERE Id = @id";
        readCmd.Parameters.AddWithValue("@id", Guid.Parse(_testUserId));
        var dbValue = await readCmd.ExecuteScalarAsync();
        dbValue.Should().NotBeNull("UserProfile should exist after initial request");
        _previousLastLoginAt = dbValue is DateTimeOffset dto ? dto
            : new DateTimeOffset(DateTime.SpecifyKind((DateTime)dbValue!, DateTimeKind.Utc), TimeSpan.Zero);
    }

    [Given("a UserProfile exists for a user with email {string}")]
    public async Task GivenAUserProfileExistsForAUserWithEmail(string email)
    {
        _testUserId = TestClaimsBuilder.DeterministicUserId(email);
        _testUserEmail = email;
        _testUserName = "Original Name";
        _testUserRole = "Client";
        _emailsToCleanup.Add(email);

        // Clean up any stale profile for this email left by previous runs
        // that used a different (random) user ID.
        await CleanupStaleProfileAsync(email);

        var token = await _authHelper.GetTokenForRoleAsync(_testUserEmail, _testUserRole, _testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var setupResponse = await _client.SendAsync(request);
        var body = await setupResponse.Content.ReadAsStringAsync();
        setupResponse.IsSuccessStatusCode.Should().BeTrue(
            $"initial request to create profile should succeed. Status: {setupResponse.StatusCode}, Body: {body}");
    }

    [Given("a UserProfile exists for a user with display name {string}")]
    public async Task GivenAUserProfileExistsForAUserWithDisplayName(string displayName)
    {
        _testUserEmail = $"nametest-{displayName.Replace(" ", "").ToLowerInvariant()}@example.com";
        _testUserId = TestClaimsBuilder.DeterministicUserId(_testUserEmail);
        _testUserName = displayName;
        _testUserRole = "Client";
        _emailsToCleanup.Add(_testUserEmail);

        // Clean up any stale profile for this email left by previous runs
        // that used a different (random) user ID.
        await CleanupStaleProfileAsync(_testUserEmail);

        var builder = new TestClaimsBuilder()
            .WithUserId(_testUserId)
            .WithEmail(_testUserEmail)
            .WithName(_testUserName)
            .WithRole(_testUserRole);
        var token = builder.BuildJwtToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var setupResponse = await _client.SendAsync(request);
        setupResponse.IsSuccessStatusCode.Should().BeTrue(
            "initial request to create profile should succeed");
    }

    // =========================================================================
    // When
    // =========================================================================

    [When("I send an authenticated request to {string} as the new test user")]
    public async Task WhenISendAnAuthenticatedRequestToAsTheNewTestUser(string path)
    {
        _requestTimestamp = DateTimeOffset.UtcNow;

        var token = await _authHelper.GetTokenForRoleAsync(_testUserEmail, _testUserRole, _testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _response = await _client.SendAsync(request);
        _responseBody = await _response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(_responseBody))
        {
            _responseJson = JsonDocument.Parse(_responseBody).RootElement;
        }
    }

    [When("I send an authenticated request to {string} as the returning test user")]
    public async Task WhenISendAnAuthenticatedRequestToAsTheReturningTestUser(string path)
    {
        _requestTimestamp = DateTimeOffset.UtcNow;

        var token = await _authHelper.GetTokenForRoleAsync(_testUserEmail, _testUserRole, _testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _response = await _client.SendAsync(request);
        _responseBody = await _response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(_responseBody))
        {
            _responseJson = JsonDocument.Parse(_responseBody).RootElement;
        }
    }

    [When("I send an authenticated request to {string} as an administrator")]
    public async Task WhenISendAnAuthenticatedRequestToAsAnAdministrator(string path)
    {
        _requestTimestamp = DateTimeOffset.UtcNow;

        var token = await _authHelper.GetAdminTokenAsync(_testUserEmail, _testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _response = await _client.SendAsync(request);
        _responseBody = await _response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(_responseBody))
        {
            _responseJson = JsonDocument.Parse(_responseBody).RootElement;
        }
    }

    [When("I send an authenticated request to {string} with role {string}")]
    public async Task WhenISendAnAuthenticatedRequestToWithRole(string path, string role)
    {
        _testUserRole = role;
        _requestTimestamp = DateTimeOffset.UtcNow;

        var token = await _authHelper.GetTokenForRoleAsync(_testUserEmail, role, _testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _response = await _client.SendAsync(request);
        _responseBody = await _response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(_responseBody))
        {
            _responseJson = JsonDocument.Parse(_responseBody).RootElement;
        }
    }

    [When("I send a GET request to {string} without authentication")]
    public async Task WhenISendAGetRequestToWithoutAuthentication(string path)
    {
        _requestTimestamp = DateTimeOffset.UtcNow;

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        // Explicitly no Authorization header

        _response = await _client.SendAsync(request);
        _responseBody = await _response.Content.ReadAsStringAsync();
    }

    [When("I send an authenticated request to {string} with updated email {string}")]
    public async Task WhenISendAnAuthenticatedRequestToWithUpdatedEmail(string path, string newEmail)
    {
        _requestTimestamp = DateTimeOffset.UtcNow;
        _emailsToCleanup.Add(newEmail);

        var builder = new TestClaimsBuilder()
            .WithUserId(_testUserId)
            .WithEmail(newEmail)
            .WithName(_testUserName)
            .WithRole(_testUserRole);
        var token = builder.BuildJwtToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _response = await _client.SendAsync(request);
        _responseBody = await _response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(_responseBody))
        {
            _responseJson = JsonDocument.Parse(_responseBody).RootElement;
        }
    }

    [When("I send an authenticated request to {string} with updated name {string}")]
    public async Task WhenISendAnAuthenticatedRequestToWithUpdatedName(string path, string newName)
    {
        _requestTimestamp = DateTimeOffset.UtcNow;

        var builder = new TestClaimsBuilder()
            .WithUserId(_testUserId)
            .WithEmail(_testUserEmail)
            .WithName(newName)
            .WithRole(_testUserRole);
        var token = builder.BuildJwtToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _response = await _client.SendAsync(request);
        _responseBody = await _response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(_responseBody))
        {
            _responseJson = JsonDocument.Parse(_responseBody).RootElement;
        }
    }

    // =========================================================================
    // Then
    // =========================================================================

    [Then("the HTTP response status should be {int}")]
    public void ThenTheHttpResponseStatusShouldBe(int expectedStatusCode)
    {
        _response.Should().NotBeNull("a request should have been sent");
        ((int)_response!.StatusCode).Should().Be(expectedStatusCode,
            $"expected HTTP {expectedStatusCode} but got {(int)_response.StatusCode}. " +
            $"Body: {_responseBody?[..Math.Min(_responseBody?.Length ?? 0, 500)]}");
    }

    [Then("the response should contain a persisted UserProfile")]
    public async Task ThenTheResponseShouldContainAPersistedUserProfile()
    {
        // Verify the API response contains the expected DTO fields
        _responseJson.GetProperty("userId").GetString()
            .Should().Be(_testUserId, "the profile userId should match the Entra oid");
        _responseJson.TryGetProperty("displayName", out _).Should().BeTrue("response should contain displayName");
        _responseJson.TryGetProperty("createdAt", out _).Should().BeTrue("response should contain createdAt");

        // Verify auth-concern fields were persisted (not in API response, verified via DB)
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(_testUserId));
        profile.Should().NotBeNull("UserProfile should be persisted in the database");
        profile!.Value.Email.Should().NotBeNullOrEmpty("email should be persisted");
        profile!.Value.Role.Should().NotBeNullOrEmpty("role should be persisted");
    }

    [Then("the UserProfile email should match the JWT email claim")]
    public async Task ThenTheUserProfileEmailShouldMatchTheJwtEmailClaim()
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(_testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.Email.Should().Be(_testUserEmail);
    }

    [Then("the UserProfile role should be {string}")]
    public async Task ThenTheUserProfileRoleShouldBe(string expectedRole)
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(_testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.Role.Should().Be(expectedRole, $"the persisted role should be {expectedRole}");
    }

    [Then("the UserProfile should be active")]
    public async Task ThenTheUserProfileShouldBeActive()
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(_testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.IsActive.Should().BeTrue("newly provisioned profiles should be active");
    }

    [Then("the UserProfile CreatedAt should be recent")]
    public void ThenTheUserProfileCreatedAtShouldBeRecent()
    {
        var createdAtElement = _responseJson.GetProperty("createdAt");
        var createdAt = new DateTimeOffset(
            DateTime.SpecifyKind(createdAtElement.GetProperty("value").GetDateTime(), DateTimeKind.Utc),
            TimeSpan.Zero);
        createdAt.Should().BeCloseTo(_requestTimestamp, TimeSpan.FromSeconds(30),
            "CreatedAt should be set to approximately now");
    }

    [Then("the UserProfile LastLoginAt should be recent")]
    public async Task ThenTheUserProfileLastLoginAtShouldBeRecent()
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(_testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.LastLoginAt.Should().NotBeNull("LastLoginAt should be set");
        profile!.Value.LastLoginAt!.Value.Should().BeCloseTo(_requestTimestamp, TimeSpan.FromSeconds(30),
            "LastLoginAt should be set to approximately now");
    }

    [Then("the UserProfile LastLoginAt should be after the previous login")]
    public async Task ThenTheUserProfileLastLoginAtShouldBeAfterThePreviousLogin()
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(_testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.LastLoginAt.Should().NotBeNull("LastLoginAt should be set");
        profile!.Value.LastLoginAt!.Value.Should().BeAfter(_previousLastLoginAt,
            "LastLoginAt should advance on subsequent authenticated requests");
    }

    [Then("no provisioning should have occurred")]
    public void ThenNoProvisioningShouldHaveOccurred()
    {
        // Unauthenticated request should succeed on public endpoint without triggering provisioning.
        // We verify the public endpoint responded successfully — the middleware skips unauthenticated requests.
        _response.Should().NotBeNull();
        ((int)_response!.StatusCode).Should().Be(200,
            "public endpoint should respond without requiring provisioning");
    }

    [Then("the UserProfile email should be {string}")]
    public async Task ThenTheUserProfileEmailShouldBe(string expectedEmail)
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(_testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.Email.Should().Be(expectedEmail, "the email should be synced from updated JWT claims");
    }

    [Then("the UserProfile display name should be {string}")]
    public void ThenTheUserProfileDisplayNameShouldBe(string expectedName)
    {
        _responseJson.GetProperty("displayName").GetString()
            .Should().Be(expectedName, "the display name should be synced from updated JWT claims");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Deletes any existing <c>UserProfile</c> row for the given email so that
    /// JIT provisioning can create a fresh one with the current deterministic user ID.
    /// Stale rows are left behind when previous test runs used random GUIDs.
    /// Requires the <c>SqlConnectionString</c> from <c>appsettings.local.json</c>;
    /// silently skips if no connection string is configured.
    /// </summary>
    private async Task CleanupStaleProfileAsync(string email)
    {
        var connStr = _fixture.Configuration.GetConnectionString("SqlConnectionString");
        if (string.IsNullOrEmpty(connStr))
            return;

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM UserProfiles WHERE Email = @email";
        cmd.Parameters.AddWithValue("@email", email);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Queries the UserProfile table directly for fields not exposed by the API response DTO.
    /// Used by provisioning verification steps that need to assert on auth-concern fields
    /// (email, role, isActive, lastLoginAt) which are intentionally excluded from
    /// <see cref="RajFinancial.Shared.Contracts.Auth.UserProfileResponse"/>.
    /// </summary>
    private async Task<(string Email, string Role, bool IsActive, DateTimeOffset? LastLoginAt)?> GetProfileFromDatabaseAsync(Guid userId)
    {
        var connStr = _fixture.Configuration.GetConnectionString("SqlConnectionString");
        connStr.Should().NotBeNullOrEmpty(
            "SqlConnectionString is required for UserProfile provisioning verification tests");

        await using var conn = new SqlConnection(connStr!);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Email, Role, IsActive, LastLoginAt FROM UserProfiles WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", userId);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return (
            reader.GetString(0),
            reader.GetString(1),
            reader.GetBoolean(2),
            reader.IsDBNull(3) ? null : (reader.GetValue(3) is DateTimeOffset dto ? dto
                : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc), TimeSpan.Zero))
        );
    }
}
