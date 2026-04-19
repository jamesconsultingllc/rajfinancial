using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RajFinancial.Api.Data;
using RajFinancial.Api.Middleware.Authorization;

namespace RajFinancial.Api.Configuration;

/// <summary>
///     DI registration for <see cref="ApplicationDbContext"/>. Uses SQL Server with
///     optional Managed Identity when <c>SqlConnectionString</c> is configured; falls
///     back to an in-memory database for local dev.
/// </summary>
internal static class DatabaseRegistration
{
    private const string SqlConnectionStringKey = "SqlConnectionString";
    private const string UseManagedIdentityKey = "UseManagedIdentity";
    private const string InMemoryDatabaseName = "RajFinancial_Dev";
    private const int SqlMaxRetryCount = 3;
    private const int SqlMaxRetryDelaySeconds = 10;
    private const int SqlCommandTimeoutSeconds = 30;

    public static IServiceCollection AddApplicationDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var sqlConnectionString = configuration[SqlConnectionStringKey]
                                  ?? configuration.GetConnectionString(SqlConnectionStringKey);

        if (string.IsNullOrEmpty(sqlConnectionString))
        {
            AddInMemoryDatabase(services, environment);
            return services;
        }

        var useManagedIdentity = configuration.GetValue(UseManagedIdentityKey, defaultValue: true);
        if (useManagedIdentity)
            services.AddSingleton<ManagedIdentityConnectionInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            ConfigureSqlServer(options, sp, environment, sqlConnectionString, useManagedIdentity));

        return services;
    }

    private static void AddInMemoryDatabase(IServiceCollection services, IHostEnvironment environment)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase(InMemoryDatabaseName);
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
                maxRetryCount: SqlMaxRetryCount,
                maxRetryDelay: TimeSpan.FromSeconds(SqlMaxRetryDelaySeconds),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(SqlCommandTimeoutSeconds);
        });

        if (useManagedIdentity)
        {
            var interceptor = serviceProvider.GetRequiredService<ManagedIdentityConnectionInterceptor>();
            options.AddInterceptors(interceptor);
        }

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
