using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RajFinancial.Api.Data;
using RajFinancial.Api.Data.Interceptors;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     DI registration for <see cref="ApplicationDbContext"/>. Uses SQL Server with
///     optional Managed Identity when <c>SqlConnectionString</c> is configured; falls
///     back to an in-memory database for local dev.
/// </summary>
internal static class DatabaseRegistration
{
    private const string SQL_CONNECTION_STRING_KEY = "SqlConnectionString";
    private const string USE_MANAGED_IDENTITY_KEY = "UseManagedIdentity";
    private const string IN_MEMORY_DATABASE_NAME = "RajFinancial_Dev";
    private const int SQL_MAX_RETRY_COUNT = 3;
    private const int SQL_MAX_RETRY_DELAY_SECONDS = 10;
    private const int SQL_COMMAND_TIMEOUT_SECONDS = 30;

    public static IServiceCollection AddApplicationDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var sqlConnectionString = configuration[SQL_CONNECTION_STRING_KEY]
                                  ?? configuration.GetConnectionString(SQL_CONNECTION_STRING_KEY);

        if (string.IsNullOrEmpty(sqlConnectionString))
        {
            AddInMemoryDatabase(services, environment);
            return services;
        }

        var useManagedIdentity = configuration.GetValue(USE_MANAGED_IDENTITY_KEY, defaultValue: true);
        if (useManagedIdentity)
            services.AddSingleton<ManagedIdentityConnectionInterceptor>();

        services.AddScoped<BusinessEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            ConfigureSqlServer(options, sp, environment, sqlConnectionString, useManagedIdentity));

        return services;
    }

    private static void AddInMemoryDatabase(IServiceCollection services, IHostEnvironment environment)
    {
        services.AddScoped<BusinessEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(IN_MEMORY_DATABASE_NAME);
            options.AddInterceptors(sp.GetRequiredService<BusinessEventsInterceptor>());
            ApplyDevelopmentOptions(options, environment);
        });
    }

    private static void ConfigureSqlServer(
        DbContextOptionsBuilder options,
        IServiceProvider serviceProvider,
        IHostEnvironment environment,
        string sqlConnectionString,
        bool useManagedIdentity)
    {
        options.UseSqlServer(sqlConnectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: SQL_MAX_RETRY_COUNT,
                maxRetryDelay: TimeSpan.FromSeconds(SQL_MAX_RETRY_DELAY_SECONDS),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(SQL_COMMAND_TIMEOUT_SECONDS);
        });

        if (useManagedIdentity)
        {
            var interceptor = serviceProvider.GetRequiredService<ManagedIdentityConnectionInterceptor>();
            options.AddInterceptors(interceptor);
        }

        options.AddInterceptors(serviceProvider.GetRequiredService<BusinessEventsInterceptor>());

        ApplyDevelopmentOptions(options, environment);
    }

    private static void ApplyDevelopmentOptions(DbContextOptionsBuilder options, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return;

        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
}
