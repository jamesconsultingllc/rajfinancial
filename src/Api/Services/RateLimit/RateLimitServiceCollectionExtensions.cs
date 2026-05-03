using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Middleware.RateLimit;
using RajFinancial.Api.Services.RateLimit.Storage;

namespace RajFinancial.Api.Services.RateLimit;

/// <summary>
///     Single entry-point for wiring the rate-limit subsystem into DI: options binding +
///     validation, <see cref="TableServiceClient" />, store, policy resolver, telemetry,
///     and middleware.
/// </summary>
public static class RateLimitServiceCollectionExtensions
{
    public static IServiceCollection AddRajFinancialRateLimit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();

        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<TableServiceClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RateLimitOptions>>().Value;
            var connectionString =
                configuration.GetConnectionString(opts.StorageConnectionName)
                ?? configuration[opts.StorageConnectionName]
                ?? Environment.GetEnvironmentVariable(opts.StorageConnectionName);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Rate-limit storage connection '{opts.StorageConnectionName}' is not configured. " +
                    "Set it via ConnectionStrings, top-level config, or environment variable.");

            return new TableServiceClient(connectionString);
        });

        services.AddSingleton<IRateLimitPolicyResolver, DefaultRateLimitPolicyResolver>();
        services.AddSingleton<IRateLimitStore, TableStorageRateLimitStore>();

        services.AddSingleton<RateLimitMiddleware>();

        return services;
    }
}
