using Azure.Identity;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using RajFinancial.Api.Services;
using RajFinancial.Api.Validators;
using RajFinancial.Shared.Contracts.Auth;

var builder = FunctionsApplication.CreateBuilder(args);

// Configure Functions web application for HTTP triggers
builder.ConfigureFunctionsWebApplication();

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

// Register Microsoft Graph client with Managed Identity
// Uses DefaultAzureCredential which tries: MI -> Azure CLI -> VS -> etc.
builder.Services.AddSingleton<GraphServiceClient>(sp =>
{
    var credential = new DefaultAzureCredential();
    return new GraphServiceClient(credential);
});

// Register Graph client wrapper for testability
builder.Services.AddSingleton<IGraphClientWrapper, GraphClientWrapper>();

// Register FluentValidation validators
builder.Services.AddScoped<IValidator<CompleteRoleRequest>, CompleteRoleRequestValidator>();

// Make configuration available for dependency injection
builder.Services.AddSingleton(builder.Configuration);

var host = builder.Build();
await host.RunAsync();

