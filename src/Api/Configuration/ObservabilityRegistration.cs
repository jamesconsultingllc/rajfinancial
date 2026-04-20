using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     Registers OpenTelemetry tracing, metrics, and environment-aware logging for the
///     Azure Functions isolated worker host.
/// </summary>
/// <remarks>
///     Dev: Console exporter + rolling file logger; Azure Monitor disabled to avoid ingestion cost.
///     Staging/Prod: Azure Monitor (Application Insights) via the OpenTelemetry pipeline.
///     Uses <c>UseFunctionsWorkerDefaults()</c> (not AspNetCore instrumentation) because the
///     isolated worker does not run an ASP.NET Core server; host/worker trace correlation is
///     provided by the Functions.Worker.OpenTelemetry package.
///     Sources and meters for all 7 reserved domains are registered here so domain classes
///     can reference them before this wiring runs.
/// </remarks>
internal static class ObservabilityRegistration
{
    private static readonly string[] DomainSources =
    [
        "RajFinancial.Api.Auth",
        "RajFinancial.Api.Assets",
        "RajFinancial.Api.Entities",
        "RajFinancial.Api.UserProfile",
        "RajFinancial.Api.Middleware",
        "RajFinancial.Api.ClientManagement",
        "RajFinancial.Api.Authorization",
    ];

    /// <summary>
    ///     Adds OpenTelemetry tracing + metrics and environment-aware logging to the DI container.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="env">The hosting environment used to gate Dev-only providers.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddApplicationObservability(
        this IServiceCollection services,
        IHostEnvironment env)
    {
        ConfigureFileLogging(services, env);
        ConfigureOpenTelemetry(services, env);
        return services;
    }

    private static void ConfigureFileLogging(IServiceCollection services, IHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return;

        services.AddLogging(logging =>
        {
            logging.AddFile("logs/rajfinancial-{Date}.log", options =>
            {
                options.MaxRollingFiles = 7;
                options.FileSizeLimitBytes = 10 * 1024 * 1024;
            });
        });
    }

    private static void ConfigureOpenTelemetry(IServiceCollection services, IHostEnvironment env)
    {
        var otelBuilder = services.AddOpenTelemetry();

        otelBuilder.WithTracing(tracing =>
        {
            tracing
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource(DomainSources);

            if (env.IsDevelopment())
                tracing.AddConsoleExporter();
        });

        otelBuilder.WithMetrics(metrics =>
        {
            metrics
                .AddHttpClientInstrumentation()
                .AddMeter(DomainSources);

            if (env.IsDevelopment())
                metrics.AddConsoleExporter();
        });

        // Functions host ↔ worker trace correlation + log bridging (required before exporter).
        otelBuilder.UseFunctionsWorkerDefaults();

        // Azure Monitor exporter — non-Dev only. Reads APPLICATIONINSIGHTS_CONNECTION_STRING
        // from App Settings (Azure) or environment variable (local non-Dev).
        if (!env.IsDevelopment())
            otelBuilder.UseAzureMonitorExporter();
    }
}
