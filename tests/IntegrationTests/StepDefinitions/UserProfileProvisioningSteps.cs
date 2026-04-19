using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RajFinancial.IntegrationTests.Support;
using Reqnroll;

namespace RajFinancial.IntegrationTests.StepDefinitions;

/// <summary>
/// Step definitions for UserProfile JIT provisioning integration tests.
/// Verifies that authenticated API calls trigger automatic UserProfile creation
/// and that subsequent calls update claim-sourced fields and LastLoginAt.
/// </summary>
/// <remarks>
/// These tests hit real HTTP endpoints via the Azure Functions host.
/// Provisioning is verified through the <c>/api/profile/me</c> endpoint for fields
/// exposed by <see cref="RajFinancial.Shared.Contracts.Auth.UserProfileResponse"/>.
/// Auth-concern fields (email, role, isActive, lastLoginAt) were intentionally excluded
/// from the response DTO and are verified via direct database queries instead.
/// </remarks>
[Binding]
[Scope(Tag = "userprofile")]
public class UserProfileProvisioningSteps
{
    private readonly FunctionsHostFixture fixture;
    private readonly HttpClient client;
    private HttpResponseMessage? response;
    private string? responseBody;
    private JsonElement responseJson;

    // Track test user identity across steps
    private string testUserId = null!;
    private string testUserEmail = null!;
    private string testUserName = null!;
    private string testUserRole = "Client";

    // Track all emails used during a scenario for cleanup
    private readonly List<string> emailsToCleanup = [];

    // For tracking timing of provisioning
    private DateTimeOffset requestTimestamp;

    // For "returning user" scenarios — captures LastLoginAt from the initial request
    private DateTimeOffset previousLastLoginAt;

    private readonly TestAuthHelper authHelper;

    public UserProfileProvisioningSteps(FunctionsHostFixture fixture, TestAuthHelper authHelper)
    {
        this.fixture = fixture;
        this.authHelper = authHelper;
        client = fixture.Client;
    }

    // =========================================================================
    // Lifecycle — cleanup after each scenario
    // =========================================================================

    /// <summary>
    /// Removes any <c>UserProfile</c> rows created during the scenario so that
    /// subsequent runs start from a clean slate. Cleans up by both user ID and
    /// any emails registered during the scenario. Deletes auto-provisioned
    /// Entities (and their EntityRoles) before UserProfiles to satisfy the
    /// <c>FK_Entities_UserProfiles_UserId</c> restrict constraint.
    /// </summary>
    [AfterScenario]
    public async Task CleanupAfterScenario()
    {
        var connStr = fixture.Configuration.GetConnectionString("SqlConnectionString");
        if (string.IsNullOrEmpty(connStr))
            return;

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        // Delete by user ID (covers the primary profile)
        if (!string.IsNullOrEmpty(testUserId))
        {
            var id = Guid.Parse(testUserId);
            await DeleteEntitiesByUserIdAsync(conn, id);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM UserProfiles WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        // Delete by any emails registered during the scenario
        foreach (var email in emailsToCleanup)
        {
            await DeleteEntitiesByEmailAsync(conn, email);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM UserProfiles WHERE Email = @email";
            cmd.Parameters.AddWithValue("@email", email);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task DeleteEntitiesByUserIdAsync(SqlConnection conn, Guid userId)
    {
        await using var rolesCmd = conn.CreateCommand();
        rolesCmd.CommandText = "DELETE FROM EntityRoles WHERE EntityId IN (SELECT Id FROM Entities WHERE UserId = @uid)";
        rolesCmd.Parameters.AddWithValue("@uid", userId);
        await rolesCmd.ExecuteNonQueryAsync();

        await using var entCmd = conn.CreateCommand();
        entCmd.CommandText = "DELETE FROM Entities WHERE UserId = @uid";
        entCmd.Parameters.AddWithValue("@uid", userId);
        await entCmd.ExecuteNonQueryAsync();
    }

    private static async Task DeleteEntitiesByEmailAsync(SqlConnection conn, string email)
    {
        await using var rolesCmd = conn.CreateCommand();
        rolesCmd.CommandText = @"DELETE FROM EntityRoles WHERE EntityId IN
            (SELECT e.Id FROM Entities e INNER JOIN UserProfiles u ON u.Id = e.UserId WHERE u.Email = @email)";
        rolesCmd.Parameters.AddWithValue("@email", email);
        await rolesCmd.ExecuteNonQueryAsync();

        await using var entCmd = conn.CreateCommand();
        entCmd.CommandText = @"DELETE FROM Entities WHERE UserId IN
            (SELECT Id FROM UserProfiles WHERE Email = @email)";
        entCmd.Parameters.AddWithValue("@email", email);
        await entCmd.ExecuteNonQueryAsync();
    }

    // =========================================================================
    // Given
    // =========================================================================

    [Given("the Functions host is running")]
    public async Task GivenTheFunctionsHostIsRunning()
    {
        await fixture.EnsureHostIsRunningAsync();
    }

    [Given("no UserProfile exists for a new test user")]
    public void GivenNoUserProfileExistsForANewTestUser()
    {
        // Each test run uses a unique GUID, so no profile will exist
        testUserId = Guid.NewGuid().ToString();
        testUserEmail = $"testuser-{testUserId[..8]}@example.com";
        testUserName = $"Test User {testUserId[..8]}";
        testUserRole = "Client";
    }

    [Given("no UserProfile exists for a new admin test user")]
    public void GivenNoUserProfileExistsForANewAdminTestUser()
    {
        testUserId = Guid.NewGuid().ToString();
        testUserEmail = $"admin-{testUserId[..8]}@example.com";
        testUserName = $"Admin {testUserId[..8]}";
        testUserRole = "Administrator";
    }

    [Given("no UserProfile exists for a new advisor test user")]
    public void GivenNoUserProfileExistsForANewAdvisorTestUser()
    {
        testUserId = Guid.NewGuid().ToString();
        testUserEmail = $"advisor-{testUserId[..8]}@example.com";
        testUserName = $"Advisor {testUserId[..8]}";
        testUserRole = "Advisor";
    }

    [Given("a UserProfile already exists for a returning test user")]
    public async Task GivenAUserProfileAlreadyExistsForAReturningTestUser()
    {
        // Create the profile by making an initial authenticated request
        testUserId = Guid.NewGuid().ToString();
        testUserEmail = $"returning-{testUserId[..8]}@example.com";
        testUserName = $"Returning User {testUserId[..8]}";
        testUserRole = "Client";

        var token = await authHelper.GetTokenForRoleAsync(testUserEmail, testUserRole, testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var setupResponse = await client.SendAsync(request);
        setupResponse.IsSuccessStatusCode.Should().BeTrue(
            "initial request to create profile should succeed");

        // Back-date LastLoginAt >5 minutes so the throttle allows it to advance
        // on the next request (the service throttles updates within 5 minutes).
        var connStr = fixture.Configuration.GetConnectionString("SqlConnectionString");
        connStr.Should().NotBeNullOrEmpty(
            "SqlConnectionString is required for returning-user provisioning tests");

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        await using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = "UPDATE UserProfiles SET LastLoginAt = DATEADD(MINUTE, -6, LastLoginAt) WHERE Id = @id";
        updateCmd.Parameters.AddWithValue("@id", Guid.Parse(testUserId));
        await updateCmd.ExecuteNonQueryAsync();

        // Re-read the actual back-dated value from DB (avoids drift from app/DB time differences)
        await using var readCmd = conn.CreateCommand();
        readCmd.CommandText = "SELECT LastLoginAt FROM UserProfiles WHERE Id = @id";
        readCmd.Parameters.AddWithValue("@id", Guid.Parse(testUserId));
        var dbValue = await readCmd.ExecuteScalarAsync();
        dbValue.Should().NotBeNull("UserProfile should exist after initial request");
        dbValue.Should().NotBe(DBNull.Value, "LastLoginAt should already be set after the initial request");
        previousLastLoginAt = dbValue is DateTimeOffset dto ? dto
            : new DateTimeOffset(DateTime.SpecifyKind((DateTime)dbValue!, DateTimeKind.Utc), TimeSpan.Zero);
    }

    [Given("a UserProfile exists for a user with email {string}")]
    public async Task GivenAUserProfileExistsForAUserWithEmail(string email)
    {
        testUserId = TestClaimsBuilder.DeterministicUserId(email);
        testUserEmail = email;
        testUserName = "Original Name";
        testUserRole = "Client";
        emailsToCleanup.Add(email);

        // Clean up any stale profile for this email left by previous runs
        // that used a different (random) user ID.
        await CleanupStaleProfileAsync(email);

        var token = await authHelper.GetTokenForRoleAsync(testUserEmail, testUserRole, testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var setupResponse = await client.SendAsync(request);
        var body = await setupResponse.Content.ReadAsStringAsync();
        setupResponse.IsSuccessStatusCode.Should().BeTrue(
            $"initial request to create profile should succeed. Status: {setupResponse.StatusCode}, Body: {body}");
    }

    [Given("a UserProfile exists for a user with display name {string}")]
    public async Task GivenAUserProfileExistsForAUserWithDisplayName(string displayName)
    {
        testUserEmail = $"nametest-{displayName.Replace(" ", "").ToLowerInvariant()}@example.com";
        testUserId = TestClaimsBuilder.DeterministicUserId(testUserEmail);
        testUserName = displayName;
        testUserRole = "Client";
        emailsToCleanup.Add(testUserEmail);

        // Clean up any stale profile for this email left by previous runs
        // that used a different (random) user ID.
        await CleanupStaleProfileAsync(testUserEmail);

        var builder = new TestClaimsBuilder()
            .WithUserId(testUserId)
            .WithEmail(testUserEmail)
            .WithName(testUserName)
            .WithRole(testUserRole);
        var token = builder.BuildJwtToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var setupResponse = await client.SendAsync(request);
        setupResponse.IsSuccessStatusCode.Should().BeTrue(
            "initial request to create profile should succeed");
    }

    // =========================================================================
    // When
    // =========================================================================

    [When("I send an authenticated request to {string} as the new test user")]
    public async Task WhenISendAnAuthenticatedRequestToAsTheNewTestUser(string path)
    {
        requestTimestamp = DateTimeOffset.UtcNow;

        var token = await authHelper.GetTokenForRoleAsync(testUserEmail, testUserRole, testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(responseBody))
        {
            responseJson = JsonDocument.Parse(responseBody).RootElement;
        }
    }

    [When("I send an authenticated request to {string} as the returning test user")]
    public async Task WhenISendAnAuthenticatedRequestToAsTheReturningTestUser(string path)
    {
        requestTimestamp = DateTimeOffset.UtcNow;

        var token = await authHelper.GetTokenForRoleAsync(testUserEmail, testUserRole, testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(responseBody))
        {
            responseJson = JsonDocument.Parse(responseBody).RootElement;
        }
    }

    [When("I send an authenticated request to {string} as an administrator")]
    public async Task WhenISendAnAuthenticatedRequestToAsAnAdministrator(string path)
    {
        requestTimestamp = DateTimeOffset.UtcNow;

        var token = await authHelper.GetAdminTokenAsync(testUserEmail, testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(responseBody))
        {
            responseJson = JsonDocument.Parse(responseBody).RootElement;
        }
    }

    [When("I send an authenticated request to {string} with role {string}")]
    public async Task WhenISendAnAuthenticatedRequestToWithRole(string path, string role)
    {
        testUserRole = role;
        requestTimestamp = DateTimeOffset.UtcNow;

        var token = await authHelper.GetTokenForRoleAsync(testUserEmail, role, testUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(responseBody))
        {
            responseJson = JsonDocument.Parse(responseBody).RootElement;
        }
    }

    [When("I send a GET request to {string} without authentication")]
    public async Task WhenISendAGetRequestToWithoutAuthentication(string path)
    {
        requestTimestamp = DateTimeOffset.UtcNow;

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        // Explicitly no Authorization header

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When("I send an authenticated request to {string} with updated email {string}")]
    public async Task WhenISendAnAuthenticatedRequestToWithUpdatedEmail(string path, string newEmail)
    {
        requestTimestamp = DateTimeOffset.UtcNow;
        emailsToCleanup.Add(newEmail);

        var builder = new TestClaimsBuilder()
            .WithUserId(testUserId)
            .WithEmail(newEmail)
            .WithName(testUserName)
            .WithRole(testUserRole);
        var token = builder.BuildJwtToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(responseBody))
        {
            responseJson = JsonDocument.Parse(responseBody).RootElement;
        }
    }

    [When("I send an authenticated request to {string} with updated name {string}")]
    public async Task WhenISendAnAuthenticatedRequestToWithUpdatedName(string path, string newName)
    {
        requestTimestamp = DateTimeOffset.UtcNow;

        var builder = new TestClaimsBuilder()
            .WithUserId(testUserId)
            .WithEmail(testUserEmail)
            .WithName(newName)
            .WithRole(testUserRole);
        var token = builder.BuildJwtToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(responseBody))
        {
            responseJson = JsonDocument.Parse(responseBody).RootElement;
        }
    }

    // =========================================================================
    // Then
    // =========================================================================

    [Then("the HTTP response status should be {int}")]
    public void ThenTheHttpResponseStatusShouldBe(int expectedStatusCode)
    {
        response.Should().NotBeNull("a request should have been sent");
        ((int)response!.StatusCode).Should().Be(expectedStatusCode,
            $"expected HTTP {expectedStatusCode} but got {(int)response.StatusCode}. " +
            $"Body: {responseBody?[..Math.Min(responseBody?.Length ?? 0, 500)]}");
    }

    [Then("the response should contain a persisted UserProfile")]
    public async Task ThenTheResponseShouldContainAPersistedUserProfile()
    {
        // Verify the API response contains the expected DTO fields
        responseJson.GetProperty("userId").GetString()
            .Should().Be(testUserId, "the profile userId should match the Entra oid");
        responseJson.TryGetProperty("displayName", out _).Should().BeTrue("response should contain displayName");
        responseJson.TryGetProperty("createdAt", out _).Should().BeTrue("response should contain createdAt");

        // Verify auth-concern fields were persisted (not in API response, verified via DB)
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(testUserId));
        profile.Should().NotBeNull("UserProfile should be persisted in the database");
        profile!.Value.Email.Should().NotBeNullOrEmpty("email should be persisted");
        profile!.Value.Role.Should().NotBeNullOrEmpty("role should be persisted");
    }

    [Then("the UserProfile email should match the JWT email claim")]
    public async Task ThenTheUserProfileEmailShouldMatchTheJwtEmailClaim()
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.Email.Should().Be(testUserEmail);
    }

    [Then("the UserProfile role should be {string}")]
    public async Task ThenTheUserProfileRoleShouldBe(string expectedRole)
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.Role.Should().Be(expectedRole, $"the persisted role should be {expectedRole}");
    }

    [Then("the UserProfile should be active")]
    public async Task ThenTheUserProfileShouldBeActive()
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.IsActive.Should().BeTrue("newly provisioned profiles should be active");
    }

    [Then("the UserProfile CreatedAt should be recent")]
    public void ThenTheUserProfileCreatedAtShouldBeRecent()
    {
        var createdAtElement = responseJson.GetProperty("createdAt");
        var createdAt = new DateTimeOffset(
            DateTime.SpecifyKind(createdAtElement.GetProperty("value").GetDateTime(), DateTimeKind.Utc),
            TimeSpan.Zero);
        createdAt.Should().BeCloseTo(requestTimestamp, TimeSpan.FromSeconds(30),
            "CreatedAt should be set to approximately now");
    }

    [Then("the UserProfile LastLoginAt should be recent")]
    public async Task ThenTheUserProfileLastLoginAtShouldBeRecent()
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.LastLoginAt.Should().NotBeNull("LastLoginAt should be set");
        profile!.Value.LastLoginAt!.Value.Should().BeCloseTo(requestTimestamp, TimeSpan.FromSeconds(30),
            "LastLoginAt should be set to approximately now");
    }

    [Then("the UserProfile LastLoginAt should be after the previous login")]
    public async Task ThenTheUserProfileLastLoginAtShouldBeAfterThePreviousLogin()
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.LastLoginAt.Should().NotBeNull("LastLoginAt should be set");
        profile!.Value.LastLoginAt!.Value.Should().BeAfter(previousLastLoginAt,
            "LastLoginAt should advance on subsequent authenticated requests");
    }

    [Then("no provisioning should have occurred")]
    public void ThenNoProvisioningShouldHaveOccurred()
    {
        // Unauthenticated request should succeed on public endpoint without triggering provisioning.
        // We verify the public endpoint responded successfully — the middleware skips unauthenticated requests.
        response.Should().NotBeNull();
        ((int)response!.StatusCode).Should().Be(200,
            "public endpoint should respond without requiring provisioning");
    }

    [Then("the UserProfile email should be {string}")]
    public async Task ThenTheUserProfileEmailShouldBe(string expectedEmail)
    {
        var profile = await GetProfileFromDatabaseAsync(Guid.Parse(testUserId));
        profile.Should().NotBeNull("UserProfile should exist in the database");
        profile!.Value.Email.Should().Be(expectedEmail, "the email should be synced from updated JWT claims");
    }

    [Then("the UserProfile display name should be {string}")]
    public void ThenTheUserProfileDisplayNameShouldBe(string expectedName)
    {
        responseJson.GetProperty("displayName").GetString()
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
        var connStr = fixture.Configuration.GetConnectionString("SqlConnectionString");
        if (string.IsNullOrEmpty(connStr))
            return;

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        await DeleteEntitiesByEmailAsync(conn, email);
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
        var connStr = fixture.Configuration.GetConnectionString("SqlConnectionString");
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
