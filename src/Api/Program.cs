using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RajFinancial.Api;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Authorization;
using RajFinancial.Api.Middleware.Content;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.Auth;

// ============================================================================
// Security guard — must run BEFORE anything else.
// AUTH__USE_UNSIGNED_LOCAL_VALIDATOR replaces the production JWT validator with
// one that skips signature verification. It is intended for local integration
// tests only. Fail loudly if the flag is ever left on in App Service.
// ============================================================================
var useUnsignedLocalValidator = string.Equals(
    Environment.GetEnvironmentVariable(EnvironmentVariableNames.UseUnsignedLocalValidator),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (useUnsignedLocalValidator && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariableNames.WebsiteSiteName)))
{
    await Console.Error.WriteLineAsync(
        $"FATAL: {EnvironmentVariableNames.UseUnsignedLocalValidator}=true detected while {EnvironmentVariableNames.WebsiteSiteName} is set " +
        "(running on Azure App Service). The unsigned JWT validator must never run outside local development.");
    throw new InvalidOperationException(
        $"Refusing to start: {EnvironmentVariableNames.UseUnsignedLocalValidator} is enabled on Azure App Service.");
}

var builder = FunctionsApplication.CreateBuilder(args);

// ============================================================================
// Security guard (post-builder) — also fail fast when the unsigned validator
// is requested outside Development environments. This catches non-App-Service
// deployments (e.g., container hosts, AKS) where WEBSITE_SITE_NAME is absent
// but the host is still serving production traffic.
// ============================================================================
if (useUnsignedLocalValidator && !builder.Environment.IsDevelopment())
{
    await Console.Error.WriteLineAsync(
        $"FATAL: {EnvironmentVariableNames.UseUnsignedLocalValidator}=true is only permitted when " +
        $"the Functions environment is Development. Current environment: {builder.Environment.EnvironmentName}.");
    throw new InvalidOperationException(
        $"Refusing to start: {EnvironmentVariableNames.UseUnsignedLocalValidator} requires Development environment " +
        $"(was '{builder.Environment.EnvironmentName}').");
}

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

// ============================================================================
// Service registrations (grouped in ServiceCollectionExtensions)
// ============================================================================
builder.Services.AddApplicationObservability(builder.Environment, builder.Configuration);
builder.Services.AddApplicationHealthChecks();
builder.Services.AddSingleton<ISerializationFactory, SerializationFactory>();
builder.Services.Configure<AppRoleOptions>(
    builder.Configuration.GetSection(AppRoleOptions.SectionName));
builder.Services.AddSingleton(builder.Configuration);

// ---------------------------------------------------------------------------
// Authentication / JWT bearer validator
// ---------------------------------------------------------------------------
// - Bind EntraExternalIdOptions (Instance, TenantId, ClientId, ValidAudiences).
// - Register a singleton ConfigurationManager<OpenIdConnectConfiguration> so
//   the OIDC discovery document + signing keys are cached and auto-rotated.
// - Register IJwtBearerValidator as a singleton. In normal operation this is
//   JwtBearerValidator; the unsigned local-only validator is swapped in when
//   AUTH__USE_UNSIGNED_LOCAL_VALIDATOR=true (gated above).
// ---------------------------------------------------------------------------
builder.Services.Configure<EntraExternalIdOptions>(
    builder.Configuration.GetSection(EntraExternalIdOptions.SectionName));

builder.Services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EntraExternalIdOptions>>().Value;
    var metadataAddress = OidcMetadataAddress.Build(options);
    return new ConfigurationManager<OpenIdConnectConfiguration>(
        metadataAddress,
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever());
});

if (useUnsignedLocalValidator)
{
    builder.Services.AddSingleton<IJwtBearerValidator, LocalUnsignedJwtValidator>();
}
else
{
    builder.Services.AddSingleton<IJwtBearerValidator, JwtBearerValidator>();
}

builder.Services.AddApplicationDatabase(builder.Configuration, builder.Environment);
builder.Services.AddContactResolver(builder.Environment);
builder.Services.AddApplicationServices();
builder.Services.AddApplicationValidators();

var host = builder.Build();

// Surface validator selection in the host log so operators can spot accidental
// unsigned-validator usage immediately after startup.
var startupLogger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(StartupLog.Category);
if (useUnsignedLocalValidator)
{
    StartupLog.UsingUnsignedValidator(startupLogger);
}
else
{
    StartupLog.UsingProductionValidator(startupLogger);
}

await host.RunAsync();

namespace RajFinancial.Api
{
    /// <summary>
    ///     Source-generated logger for the startup banner so we honour CA1848 without
    ///     polluting regular product code with new partial types.
    /// </summary>
    internal static partial class StartupLog
    {
        /// <summary>
        ///     Logger category used for the startup banner. Centralised so tests and log
        ///     queries can reference the same constant instead of a magic string literal.
        /// </summary>
        public const string Category = "RajFinancial.Api.Startup";

        [LoggerMessage(EventId = 1105, Level = LogLevel.Information,
            Message = "Auth validator: JwtBearerValidator (production)")]
        public static partial void UsingProductionValidator(ILogger logger);

        [LoggerMessage(EventId = 1108, Level = LogLevel.Warning,
            Message = "Auth validator: LocalUnsignedJwtValidator (UNSIGNED — DEV ONLY)")]
        public static partial void UsingUnsignedValidator(ILogger logger);
    }
}
