using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using OpenTelemetry;
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
            {
                // Dev: sample everything so local debugging sees every span.
                tracing.AddConsoleExporter();
            }
            else
            {
                // Non-Dev: 10% parent-based sampling keeps Azure Monitor ingestion cost
                // predictable while preserving trace coherency (all spans in a given
                // request are kept or dropped together via ParentBased). Tune ratio as
                // traffic grows; override via a ParentBasedSampler configured from
                // App Settings if needed.
                tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)));
            }
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
