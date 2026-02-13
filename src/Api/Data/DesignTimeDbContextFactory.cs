using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RajFinancial.Api.Data;

/// <summary>
/// Factory for creating ApplicationDbContext at design time for EF Core migrations.
/// </summary>
/// <remarks>
/// This factory is used by EF Core tools (dotnet ef migrations add, etc.) when running
/// outside the Azure Functions runtime. It reads configuration from local.settings.json
/// to use the same connection string as the running application.
/// </remarks>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <inheritdoc/>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Determine environment (default to Development for design-time)
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")
            ?? "Development";

        // Build configuration from multiple sources (order matters - later sources override earlier)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            // Standard .NET configuration files
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            // Azure Functions local development file
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)
            // Environment variables (highest priority, used in Azure)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Try to get connection string from configuration
        // Azure Functions uses ConnectionStrings:SqlConnectionString or Values:SqlConnectionString
        var connectionString = configuration.GetConnectionString("SqlConnectionString")
            ?? configuration["Values:SqlConnectionString"]
            ?? configuration["SqlConnectionString"];

        // Fall back to LocalDB for local development if no connection string configured
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Server=(localdb)\\mssqllocaldb;Database=RajFinancial_Dev;Trusted_Connection=True;MultipleActiveResultSets=true";
            Console.WriteLine("Warning: No SqlConnectionString found in local.settings.json, using LocalDB.");
        }

        Console.WriteLine($"Using database: {GetDatabaseName(connectionString)}");

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            // Enable retry on failure for transient errors
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);

            sqlOptions.CommandTimeout(60);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Extracts the database name from a connection string for logging purposes.
    /// </summary>
    private static string GetDatabaseName(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return $"{builder.DataSource}/{builder.InitialCatalog}";
        }
        catch
        {
            return "(unknown)";
        }
    }
}
