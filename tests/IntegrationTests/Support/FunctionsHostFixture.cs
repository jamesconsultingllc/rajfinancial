using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Fixture that provides a shared <see cref="HttpClient"/> for integration tests.
/// Expects the Azure Functions host to already be running (locally via <c>func start</c>,
/// or a deployed endpoint in CI/CD). Does NOT auto-start the host.
/// </summary>
public class FunctionsHostFixture
{
    public FunctionsHostFixture()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        BaseUrl = Configuration["FunctionsHost:BaseUrl"] ?? "http://localhost:7071";

        // Accept self-signed certificates issued by Azure Functions Core Tools (func start --useHttps).
        // This is scoped to localhost only — remote endpoints use real certificates.
        var handler = new HttpClientHandler();
        if (IsLocalhost(BaseUrl))
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        Client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }

    /// <summary>
    /// Pre-configured HttpClient pointing at the Functions host.
    /// </summary>
    public HttpClient Client { get; }

    /// <summary>
    /// Application configuration (appsettings.json + environment).
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// The base URL of the running Functions host.
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// Verifies the host is reachable. Throws a clear message if not.
    /// Called from the Reqnroll "Given the Functions host is running" step.
    /// </summary>
    public async Task EnsureHostIsRunningAsync()
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await Client.GetAsync("/api/health/live");
            if (response.StatusCode == HttpStatusCode.OK)
                return;

            var body = await SafeReadBodyAsync(response);
            throw new InvalidOperationException(UnreachableMessage(response.StatusCode, body));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or SocketException)
        {
            throw new InvalidOperationException(UnreachableMessage(statusCode: null, body: null), ex);
        }
        finally
        {
            response?.Dispose();
        }
    }

    private static async Task<string?> SafeReadBodyAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(content) ? null : content[..Math.Min(content.Length, 500)];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return null;
        }
    }

    private string UnreachableMessage(HttpStatusCode? statusCode, string? body)
    {
        string prefix;
        if (statusCode is null)
        {
            prefix = $"Functions host is not reachable at {BaseUrl}. ";
        }
        else
        {
            var bodySuffix = body is null ? ". " : $" (body: {body}). ";
            prefix = $"Functions host at {BaseUrl} returned {(int)statusCode} {statusCode} from /api/health/live" + bodySuffix;
        }

        var remediation = IsLocalhost(BaseUrl)
            ? "Start it manually: cd src/Api && func start"
            : "Ensure the target environment is deployed and accessible.";

        return prefix + remediation;
    }

    /// <summary>
    /// Whether tests are running against a local Functions host (dev mode).
    /// When true, unsigned test JWTs are accepted. When false, real Entra ROPC tokens are required.
    /// </summary>
    public bool IsLocal => IsLocalhost(BaseUrl);

    private static bool IsLocalhost(string url)
    {
        var uri = new Uri(url);
        return uri.Host is "localhost" or "127.0.0.1" or "::1";
    }
}
