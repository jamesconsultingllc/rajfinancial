using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RajFinancial.Shared.HealthContract;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Fixture that provides a shared <see cref="HttpClient"/> for integration tests.
/// Expects the Azure Functions host to already be running (locally via <c>func start</c>,
/// or a deployed endpoint in CI/CD). Does NOT auto-start the host.
/// </summary>
public class FunctionsHostFixture
{
    /// <summary>
    ///     Maximum number of characters of an HTTP response body included in diagnostic
    ///     messages when the fixture fails. Keeps failure messages readable without
    ///     flooding test output with full payloads.
    /// </summary>
    private const int MaxBodyPreviewLength = 500;

    /// <summary>
    ///     Paths for the in-proc health check endpoints exposed by
    ///     <c>HealthCheckFunction</c>. Centralised here so tests reference a single
    ///     constant rather than duplicating route literals.
    /// </summary>
    private const string LivePath = "/api/health/live";

    private const string ReadyPath = "/api/health/ready";

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
            response = await Client.GetAsync(LivePath);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var body = await SafeReadBodyAsync(response);
                throw new InvalidOperationException(UnreachableMessage(response.StatusCode, body));
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or SocketException)
        {
            throw new InvalidOperationException(UnreachableMessage(statusCode: null, body: null), ex);
        }
        finally
        {
            response?.Dispose();
        }

        await VerifyAuthValidatorAsync();
    }

    /// <summary>
    ///     Calls <c>/api/health/ready</c> and — when the host runs in Development and exposes
    ///     per-check data — asserts that the configured JWT bearer validator matches the
    ///     expected mode (<c>unsigned_local</c> locally, <c>jwt</c> remote). Skips the
    ///     identity assertion when per-check details aren't present (production payloads
    ///     omit the <c>checks</c> array entirely or omit the <c>data</c> field on each
    ///     check) so that production deployments still run the suite. If the ready
    ///     endpoint is reachable but returns a non-200 status, this method throws
    ///     <see cref="InvalidOperationException"/> with the response status and body so
    ///     readiness failures (unhealthy validator, missing config, database probe failure)
    ///     surface immediately instead of being masked by the first authenticated request.
    /// </summary>
    private async Task VerifyAuthValidatorAsync()
    {
        string? payload;
        HttpStatusCode statusCode;
        try
        {
            using var readyResponse = await Client.GetAsync(ReadyPath);
            statusCode = readyResponse.StatusCode;
            // Read the full body (not the truncated preview) so JsonDocument.Parse
            // succeeds even when the readiness payload grows past MaxBodyPreviewLength.
            // SafeReadBodyAsync is still used below for error diagnostics only.
            payload = await SafeReadFullBodyAsync(readyResponse);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or SocketException)
        {
            // EnsureHostIsRunningAsync already confirmed /api/health/live was reachable,
            // so failing on /api/health/ready here is almost certainly a routing, proxy,
            // or configuration issue — surface it instead of letting downstream tests
            // fail in less obvious ways.
            throw new InvalidOperationException(
                $"Functions host at {BaseUrl} was reachable on {LivePath} but {ReadyPath} failed. " +
                "This usually indicates a routing/proxy/config issue between /live and /ready.",
                ex);
        }

        // /api/health/ready is Anonymous and returns 503 when any registered check
        // is unhealthy (e.g. AuthValidatorHealthCheck rejecting an empty
        // ValidAudiences list, or the database probe failing). Failing fast here
        // — instead of parsing the body and skipping — surfaces the underlying
        // readiness problem instead of letting the suite blow up on first request.
        if (statusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Functions host at {BaseUrl} returned {(int)statusCode} from {ReadyPath}. " +
                $"Body: {TruncateForDiagnostics(payload) ?? "<empty>"}");
        }

        if (string.IsNullOrWhiteSpace(payload))
            return;

        string? validatorName;
        bool foundAuthValidatorCheck = false;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("checks", out var checks) ||
                checks.ValueKind != JsonValueKind.Array)
            {
                // Local dev is expected to run in Development mode, which always emits
                // per-check details; a missing `checks` array there indicates a regression
                // in HealthCheckFunction.Ready. For remote/production payloads the checks
                // array is intentionally omitted, so we return silently.
                if (IsLocal)
                {
                    throw new InvalidOperationException(
                        $"Functions host at {BaseUrl} returned a 200 readiness payload without a `checks` array. " +
                        "Local runs are expected to be in Development mode and include per-check details. " +
                        $"Body (truncated): {TruncateForDiagnostics(payload)}");
                }
                return;
            }

            validatorName = null;
            foreach (var check in checks.EnumerateArray())
            {
                if (!check.TryGetProperty("name", out var nameElem) ||
                    nameElem.GetString() != HealthCheckContract.AuthValidatorCheckName)
                {
                    continue;
                }

                foundAuthValidatorCheck = true;
                if (check.TryGetProperty("data", out var data) &&
                    data.ValueKind == JsonValueKind.Object &&
                    data.TryGetProperty(HealthCheckContract.AuthValidatorDataKey, out var validatorElem))
                {
                    validatorName = validatorElem.GetString();
                }
                break;
            }
        }
        catch (JsonException ex)
        {
            // /api/health/ready returned 200 but the body isn't valid JSON — the host
            // is almost certainly behind a broken proxy or misconfigured. Surface this
            // as a fail-fast so a confusing HTTP 200-but-unusable host doesn't propagate
            // into downstream test failures.
            throw new InvalidOperationException(
                $"Functions host at {BaseUrl} returned a non-JSON 200 payload from {ReadyPath}. " +
                $"Body (truncated): {TruncateForDiagnostics(payload)}",
                ex);
        }

        if (!foundAuthValidatorCheck)
        {
            throw new InvalidOperationException(
                $"Functions host at {BaseUrl} did not register the '{HealthCheckContract.AuthValidatorCheckName}' " +
                "health check. Ensure AuthValidatorHealthCheck is wired into HealthCheckRegistration and that the " +
                "host build includes Middleware/HealthCheck changes.");
        }

        // In Production, HealthCheckFunction.Ready omits per-check data fields by design,
        // so validatorName will be null here. That's expected and NOT a misconfiguration —
        // skip the identity assertion and rely on the separate config health check.
        if (validatorName is null)
            return;

        var expected = IsLocal ? HealthCheckContract.AuthValidatorUnsignedLocal : HealthCheckContract.AuthValidatorJwt;
        if (!string.Equals(validatorName, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Functions host at {BaseUrl} reports {HealthCheckContract.AuthValidatorDataKey}='{validatorName}', " +
                $"but integration tests expect '{expected}' (IsLocal={IsLocal}). " +
                (IsLocal
                    ? "Ensure AUTH__USE_UNSIGNED_LOCAL_VALIDATOR=true is set in src/Api/local.settings.json."
                    : "Deploy a build without AUTH__USE_UNSIGNED_LOCAL_VALIDATOR set."));
        }
    }

    private static async Task<string?> SafeReadBodyAsync(HttpResponseMessage response)
        => TruncateForDiagnostics(await SafeReadFullBodyAsync(response));

    /// <summary>
    ///     Reads the full response body without truncation. Returns <c>null</c> when the
    ///     body is empty/whitespace or the read failed for a transient network reason, so
    ///     callers can always treat a non-null value as the complete payload.
    /// </summary>
    private static async Task<string?> SafeReadFullBodyAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Caps a response body at <see cref="MaxBodyPreviewLength"/> characters so
    ///     failure diagnostics stay readable. Returns <c>null</c> when input is <c>null</c>.
    /// </summary>
    private static string? TruncateForDiagnostics(string? body)
        => body is null ? null : body[..Math.Min(body.Length, MaxBodyPreviewLength)];

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
            prefix = $"Functions host at {BaseUrl} returned {(int)statusCode} {statusCode} from {LivePath}" + bodySuffix;
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
