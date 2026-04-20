using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Authorization;
using RajFinancial.Api.Middleware.Content;
using RajFinancial.Api.Middleware.Exception;

var builder = FunctionsApplication.CreateBuilder(args);

// ============================================================================
// Configuration Sources (order matters - later sources override earlier)
// ============================================================================
// Azure Functions automatically loads local.settings.json (local dev) and
// Environment variables (Azure App Settings in production). We add
// appsettings.json for additional flexibility.
// ============================================================================
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.ConfigureFunctionsWebApplication();

// ============================================================================
// Middleware Pipeline (order matters!)
// 1. Exception handling        — catches all errors and formats responses
// 2. Authentication            — extracts user context from JWT
// 3. UserProfile provisioning  — JIT creates/syncs local shadow of Entra user
// 4. Authorization             — enforces [RequireAuthentication] / [RequireRole]
// 5. Content negotiation       — handles JSON/MemoryPack serialization
// 6. Validation                — validates request bodies
// ============================================================================
builder.UseMiddleware<ExceptionMiddleware>();
builder.UseMiddleware<AuthenticationMiddleware>();
builder.UseMiddleware<UserProfileProvisioningMiddleware>();
builder.UseMiddleware<AuthorizationMiddleware>();
builder.UseMiddleware<ContentNegotiationMiddleware>();
builder.UseMiddleware<ValidationMiddleware>();

#pragma warning disable S1135, S125 // Deferred feature — infra is provisioned; enabling requires SDK API verification.
// TODO: Enable Application Insights telemetry once ConfigureFunctionsApplicationInsights API
// is verified for Microsoft.Azure.Functions.Worker.ApplicationInsights v2.50.0.
// The infra already provisions App Insights and injects APPINSIGHTS_INSTRUMENTATIONKEY.
// builder.Services.AddApplicationInsightsTelemetryWorkerService();
// builder.Services.ConfigureFunctionsApplicationInsights();
#pragma warning restore S1135, S125

// ============================================================================
// Service registrations (grouped in ServiceCollectionExtensions)
// ============================================================================
builder.Services.AddSingleton<ISerializationFactory, SerializationFactory>();
builder.Services.Configure<AppRoleOptions>(
    builder.Configuration.GetSection(AppRoleOptions.SectionName));
builder.Services.AddSingleton(builder.Configuration);

builder.Services.AddApplicationDatabase(builder.Configuration, builder.Environment);
builder.Services.AddContactResolver(builder.Environment);
builder.Services.AddApplicationServices();
builder.Services.AddApplicationValidators();

var host = builder.Build();
await host.RunAsync();
