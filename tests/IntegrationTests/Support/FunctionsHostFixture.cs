using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// xUnit collection fixture that starts the Azure Functions host process
/// and provides a shared <see cref="HttpClient"/> for integration tests.
/// The host is started once per test collection and stopped after all tests complete.
/// </summary>
public class FunctionsHostFixture : IAsyncLifetime
{
    private Process? hostProcess;
    private readonly string baseUrl;
    private readonly string projectPath;
    private readonly int startupTimeoutSeconds;

    public FunctionsHostFixture()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        baseUrl = config["FunctionsHost:BaseUrl"] ?? "https://localhost:7071";
        projectPath = config["FunctionsHost:ProjectPath"] ?? "../../src/Api";
        startupTimeoutSeconds = int.Parse(config["FunctionsHost:StartupTimeoutSeconds"] ?? "30");

        // Accept self-signed certs from local func start --useHttps
        var handler = new HttpClientHandler();
        if (IsLocalhost(baseUrl))
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        Client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    /// Pre-configured HttpClient pointing at the Functions host.
    /// </summary>
    public HttpClient Client { get; }

    /// <summary>
    /// The base URL of the running Functions host.
    /// </summary>
    public string BaseUrl => baseUrl;

    public async Task InitializeAsync()
    {
        // Check if a host is already running (manual start, or remote endpoint)
        if (await IsHostReachableAsync())
        {
            return;
        }

        // Only auto-launch func start for localhost — remote endpoints must already be running
        if (!IsLocalhost(baseUrl))
        {
            throw new InvalidOperationException(
                $"Remote Functions host at {baseUrl} is not reachable. " +
                "Ensure the target environment is deployed and accessible.");
        }

        // Resolve absolute path to Api project
        var absoluteProjectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, projectPath));

        hostProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "func",
                Arguments = IsHttps(baseUrl) ? "start --useHttps --port 7071" : "start --port 7071",
                WorkingDirectory = absoluteProjectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Environment =
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["AzureWebJobsStorage"] = "UseDevelopmentStorage=true"
                }
            }
        };

        hostProcess.Start();

        // Wait for the host to be reachable
        var timeout = TimeSpan.FromSeconds(startupTimeoutSeconds);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (await IsHostReachableAsync())
            {
                return;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Azure Functions host did not become reachable at {baseUrl} " +
            $"within {startupTimeoutSeconds} seconds.");
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();

        if (hostProcess is { HasExited: false })
        {
            hostProcess.Kill(entireProcessTree: true);
            await hostProcess.WaitForExitAsync();
        }

        hostProcess?.Dispose();
    }

    private async Task<bool> IsHostReachableAsync()
    {
        try
        {
            var response = await Client.GetAsync("/api/health");
            return response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsLocalhost(string url)
    {
        var uri = new Uri(url);
        return uri.Host is "localhost" or "127.0.0.1" or "::1";
    }

    private static bool IsHttps(string url) => new Uri(url).Scheme == "https";
}

/// <summary>
/// xUnit collection definition that shares the Functions host across all integration tests.
/// </summary>
[CollectionDefinition(Name)]
public class FunctionsHostCollection : ICollectionFixture<FunctionsHostFixture>
{
    public const string Name = "FunctionsHost";
}
