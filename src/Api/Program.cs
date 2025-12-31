using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Middleware;

var builder = FunctionsApplication.CreateBuilder(args);

// Configure Functions web application for HTTP triggers
builder.ConfigureFunctionsWebApplication();

// ============================================================================
// Middleware Pipeline (order matters!)
// ============================================================================
// 1. Exception handling - catches all errors and formats responses
// 2. Authentication - extracts user context from JWT
// 3. Content negotiation - handles JSON/MemoryPack serialization
// 4. Validation - validates request bodies
// ============================================================================
builder.UseMiddleware<ExceptionMiddleware>();
builder.UseMiddleware<AuthenticationMiddleware>();
builder.UseMiddleware<ContentNegotiationMiddleware>();
builder.UseMiddleware<ValidationMiddleware>();

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

// ============================================================================
// Serialization
// ============================================================================

// Register serialization factory for JSON/MemoryPack content negotiation
// Development: Always JSON for easy debugging
// Production: MemoryPack for 7-8x faster serialization, 60% smaller payloads
builder.Services.AddSingleton<ISerializationFactory, SerializationFactory>();

// ============================================================================
// Configuration
// ============================================================================

// Configure AppRoleOptions from configuration (used for role claim validation)
builder.Services.Configure<AppRoleOptions>(
    builder.Configuration.GetSection(AppRoleOptions.SectionName));

// Make configuration available for dependency injection
builder.Services.AddSingleton(builder.Configuration);

// ============================================================================
// Application Services
// ============================================================================

// Services will be registered as they are implemented:
// builder.Services.AddScoped<IAccountService, AccountService>();
// builder.Services.AddScoped<IAssetService, AssetService>();
// builder.Services.AddScoped<IBeneficiaryService, BeneficiaryService>();

// ============================================================================
// Validators (FluentValidation)
// ============================================================================

// Register validators as they are implemented:
// builder.Services.AddScoped<IValidator<CreateAssetRequest>, CreateAssetRequestValidator>();
// builder.Services.AddScoped<IValidator<CreateBeneficiaryRequest>, CreateBeneficiaryRequestValidator>();
// builder.Services.AddScoped<IValidator<DebtPayoffRequest>, DebtPayoffRequestValidator>();

var host = builder.Build();
await host.RunAsync();