using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Data;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Content;

var builder = FunctionsApplication.CreateBuilder(args);

// ============================================================================
// Configuration Sources (order matters - later sources override earlier)
// ============================================================================
// Azure Functions automatically loads:
// - local.settings.json (local dev)
// - Environment variables (Azure App Settings in production)
// We add appsettings.json for additional flexibility
// ============================================================================
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

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
//builder.Services.AddApplicationInsightsTelemetryWorkerService();
//builder.Services.ConfigureFunctionsApplicationInsights();

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
// Database (Entity Framework Core with Managed Identity)
// ============================================================================

var sqlConnectionString = builder.Configuration.GetConnectionString("SqlConnectionString");
var useManagedIdentity = builder.Configuration.GetValue("UseManagedIdentity", defaultValue: true);

if (!string.IsNullOrEmpty(sqlConnectionString))
{
    // Register the Managed Identity interceptor for Azure AD authentication
    // Uses DefaultAzureCredential which works with:
    // - Managed Identity (in Azure)
    // - Azure CLI (local development)
    // - Visual Studio credentials (local development)
    if (useManagedIdentity)
    {
        builder.Services.AddSingleton<ManagedIdentityConnectionInterceptor>();
    }

    builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
    {
        options.UseSqlServer(sqlConnectionString, sqlOptions =>
        {
            // Enable retry on failure for transient errors (Azure SQL best practice)
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);

            // Command timeout for long-running queries
            sqlOptions.CommandTimeout(30);
        });

        // Add the Managed Identity interceptor if enabled
        if (useManagedIdentity)
        {
            var interceptor = serviceProvider.GetRequiredService<ManagedIdentityConnectionInterceptor>();
            options.AddInterceptors(interceptor);
        }

        // Enable detailed errors and sensitive data logging in development only
        if (builder.Environment.IsDevelopment())
        {
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        }
    });
}
else
{
    // For local development without Azure SQL, use in-memory database
    // This should only happen during local development
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseInMemoryDatabase("RajFinancial_Dev");

        if (builder.Environment.IsDevelopment())
        {
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        }
    });
}

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