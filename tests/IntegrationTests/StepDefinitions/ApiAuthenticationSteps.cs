using FluentAssertions;
using Reqnroll;
using RajFinancial.IntegrationTests.Support;

namespace RajFinancial.IntegrationTests.StepDefinitions;

/// <summary>
/// Step definitions for API authentication integration tests.
/// These tests hit real HTTP endpoints via the Azure Functions host.
/// Fixture is injected via Reqnroll DI (registered in FunctionsHostHooks).
/// </summary>
[Binding]
public class ApiAuthenticationSteps
{
    private readonly FunctionsHostFixture fixture;
    private readonly HttpClient client;
    private HttpResponseMessage? response;
    private string? responseBody;

    public ApiAuthenticationSteps(FunctionsHostFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.Client;
    }

    // =========================================================================
    // Given
    // =========================================================================

    [Given("the Functions host is running")]
    public async Task GivenTheFunctionsHostIsRunning()
    {
        await fixture.EnsureHostIsRunningAsync();
    }

    // =========================================================================
    // When
    // =========================================================================

    [When("I send a GET request to {string}")]
    public async Task WhenISendAGetRequestTo(string path)
    {
        response = await client.GetAsync(path);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When("I send a GET request to {string} without authentication")]
    public async Task WhenISendAGetRequestToWithoutAuthentication(string path)
    {
        // Ensure no auth headers are present
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = null;
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When("I send a GET request to {string} with a valid user token")]
    public async Task WhenISendAGetRequestToWithAValidUserToken(string path)
    {
        var token = TestClaimsBuilder.JwtForUser("testuser@example.com", null, "Client");
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When("I send a GET request to {string} with a {string} role token")]
    public async Task WhenISendAGetRequestToWithARoleToken(string path, string role)
    {
        var token = TestClaimsBuilder.JwtForUser("testuser@example.com", null, role);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    [When("I send a GET request to {string} with an administrator token")]
    public async Task WhenISendAGetRequestToWithAnAdministratorToken(string path)
    {
        var token = TestClaimsBuilder.JwtForAdmin();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        response = await client.SendAsync(request);
        responseBody = await response.Content.ReadAsStringAsync();
    }

    // =========================================================================
    // Then
    // =========================================================================

    [Then("the HTTP response status should be {int}")]
    public void ThenTheHttpResponseStatusShouldBe(int expectedStatusCode)
    {
        response.Should().NotBeNull("a request should have been sent");
        ((int)response!.StatusCode).Should().Be(expectedStatusCode,
            $"expected HTTP {expectedStatusCode} but got {(int)response.StatusCode} {response.StatusCode}. " +
            $"Body: {TruncateBody()}");
    }

    [Then("the HTTP response status should be {int} or {int}")]
    public void ThenTheHttpResponseStatusShouldBeEither(int status1, int status2)
    {
        response.Should().NotBeNull("a request should have been sent");
        var actual = (int)response!.StatusCode;
        actual.Should().BeOneOf([status1, status2],
            $"expected HTTP {status1} or {status2} but got {actual} {response.StatusCode}. " +
            $"Body: {TruncateBody()}");
    }

    [Then("the response body should contain {string}")]
    public void ThenTheResponseBodyShouldContain(string expected)
    {
        responseBody.Should().NotBeNull("response body should have been read");
        responseBody.Should().Contain(expected);
    }

    private string TruncateBody()
        => responseBody?[..Math.Min(responseBody?.Length ?? 0, 500)] ?? "(empty)";
}
