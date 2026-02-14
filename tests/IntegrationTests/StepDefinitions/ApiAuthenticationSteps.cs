using System.Net;
using FluentAssertions;
using Reqnroll;
using RajFinancial.IntegrationTests.Support;

namespace RajFinancial.IntegrationTests.StepDefinitions;

/// <summary>
/// Step definitions for API authentication integration tests.
/// These tests hit real HTTP endpoints via the Azure Functions host.
/// </summary>
[Binding]
[Collection(FunctionsHostCollection.Name)]
public class ApiAuthenticationSteps
{
    private readonly HttpClient client;
    private HttpResponseMessage? response;
    private string? responseBody;

    public ApiAuthenticationSteps(FunctionsHostFixture fixture)
    {
        client = fixture.Client;
    }

    // =========================================================================
    // Given
    // =========================================================================

    [Given("the Functions host is running")]
    public void GivenTheFunctionsHostIsRunning()
    {
        // The FunctionsHostFixture handles startup via IAsyncLifetime.
        // If we got here, the host is running.
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

    // =========================================================================
    // Then
    // =========================================================================

    [Then("the HTTP response status should be {int}")]
    public void ThenTheHttpResponseStatusShouldBe(int expectedStatusCode)
    {
        response.Should().NotBeNull("a request should have been sent");
        ((int)response!.StatusCode).Should().Be(expectedStatusCode,
            $"expected HTTP {expectedStatusCode} but got {(int)response.StatusCode} {response.StatusCode}. " +
            $"Body: {responseBody?[..Math.Min(responseBody?.Length ?? 0, 500)]}");
    }

    [Then("the response body should contain {string}")]
    public void ThenTheResponseBodyShouldContain(string expected)
    {
        responseBody.Should().NotBeNull("response body should have been read");
        responseBody.Should().Contain(expected, Exactly.Once());
    }
}
