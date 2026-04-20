using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RajFinancial.Api.Functions;
using RajFinancial.Api.Tests.Middleware;

namespace RajFinancial.Api.Tests.Functions;

/// <summary>
///     Unit tests for <see cref="HealthCheckFunction"/>.
/// </summary>
/// <remarks>
///     The Dev vs non-Dev response shape is the security-sensitive invariant: per-check
///     names and descriptions must only appear in Dev. These tests pin that contract
///     alongside the 200/503 status-code mapping.
/// </remarks>
public class HealthCheckFunctionTests
{
    [Fact]
    public async Task Live_Returns_200_With_Alive_Payload()
    {
        var function = CreateFunction(healthReport: HealthStatus.Healthy, isDevelopment: false);
        var (request, _) = CreateRequest();

        var response = await function.Live(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.Should().Contain("\"status\":\"alive\"");
    }

    [Fact]
    public async Task Ready_Returns_200_When_All_Checks_Healthy()
    {
        var function = CreateFunction(healthReport: HealthStatus.Healthy, isDevelopment: true);
        var (request, _) = CreateRequest();

        var response = await function.Ready(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_Returns_503_When_Unhealthy()
    {
        var function = CreateFunction(healthReport: HealthStatus.Unhealthy, isDevelopment: true);
        var (request, _) = CreateRequest();

        var response = await function.Ready(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Ready_Returns_503_When_Degraded()
    {
        var function = CreateFunction(healthReport: HealthStatus.Degraded, isDevelopment: true);
        var (request, _) = CreateRequest();

        var response = await function.Ready(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Ready_In_Dev_Includes_Per_Check_Details()
    {
        var function = CreateFunction(
            healthReport: HealthStatus.Unhealthy,
            isDevelopment: true,
            entryName: "database",
            entryDescription: "Database unreachable");
        var (request, _) = CreateRequest();

        var response = await function.Ready(request, CancellationToken.None);

        var body = await ReadBodyAsync(response);
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("checks", out var checks).Should().BeTrue(
            "Dev response must expose per-check detail for developer ergonomics.");
        checks.EnumerateArray().Should().ContainSingle();
        var first = checks.EnumerateArray().First();
        first.GetProperty("name").GetString().Should().Be("database");
        first.GetProperty("description").GetString().Should().Be("Database unreachable");
    }

    [Fact]
    public async Task Ready_In_NonDev_Omits_Per_Check_Details()
    {
        // Security invariant: anonymous probe must not enumerate internal dependency
        // names or descriptions outside Development.
        var function = CreateFunction(
            healthReport: HealthStatus.Unhealthy,
            isDevelopment: false,
            entryName: "database",
            entryDescription: "Database unreachable");
        var (request, _) = CreateRequest();

        var response = await function.Ready(request, CancellationToken.None);

        var body = await ReadBodyAsync(response);
        body.Should().NotContain("database");
        body.Should().NotContain("Database unreachable");

        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("checks", out _).Should().BeFalse();
        doc.RootElement.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetString().Should().Be("unhealthy");
    }

    private static HealthCheckFunction CreateFunction(
        HealthStatus healthReport,
        bool isDevelopment,
        string entryName = "test",
        string? entryDescription = "ok")
    {
        var entry = new HealthReportEntry(
            status: healthReport,
            description: entryDescription,
            duration: TimeSpan.FromMilliseconds(1),
            exception: null,
            data: null);

        var report = new HealthReport(
            entries: new Dictionary<string, HealthReportEntry> { [entryName] = entry },
            totalDuration: TimeSpan.FromMilliseconds(2));

        var environment = new TestHostEnvironment(
            isDevelopment ? Environments.Development : Environments.Production);

        return new HealthCheckFunction(
            new StubHealthCheckService(report),
            environment,
            NullLogger<HealthCheckFunction>.Instance);
    }

    private static (HttpRequestData Request, TestFunctionContext Context) CreateRequest()
    {
        var context = new TestFunctionContext();
        var mockRequest = new Mock<HttpRequestData>(context);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri("https://localhost/api/health"));
        mockRequest.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
        mockRequest.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var mockResponse = new Mock<HttpResponseData>(context);
            mockResponse.SetupProperty(r => r.StatusCode);
            mockResponse.SetupProperty(r => r.Body, new MemoryStream());
            mockResponse.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
            return mockResponse.Object;
        });
        return (mockRequest.Object, context);
    }

    private static async Task<string> ReadBodyAsync(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private sealed class StubHealthCheckService(HealthReport report) : HealthCheckService
    {
        public override Task<HealthReport> CheckHealthAsync(
            Func<HealthCheckRegistration, bool>? predicate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(report);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "RajFinancial.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
