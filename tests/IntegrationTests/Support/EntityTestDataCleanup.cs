using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Test-only helper that wipes entity/role data for well-known test users between scenarios.
/// </summary>
/// <remarks>
/// Integration tests cannot tolerate residual rows from prior runs (for example, a duplicate
/// slug assertion or a "cannot create a second Personal" assertion breaks when an earlier run
/// left rows behind). This helper uses direct SQL to wipe
/// <c>EntityRoles</c> and <c>Entities</c> for a fixed set of test users before each scenario.
/// It never touches production data — the connection string must point at a local SQL Server
/// instance or a host explicitly opted-in via the <c>INTEGRATION_TEST_ALLOWED_SQL_HOSTS</c>
/// environment variable, and any host containing <c>"prod"</c> is always rejected.
/// </remarks>
internal static class EntityTestDataCleanup
{
    /// <summary>
    /// Emails of known test users used in <c>Entities.feature</c> and <c>EntityRoles.feature</c>.
    /// </summary>
    public static readonly IReadOnlyList<string> KnownTestEmails =
    [
        "owner@rajfinancial.com",
        "other@rajfinancial.com",
        "admin@rajfinancial.com",
        "attacker@rajfinancial.com",
        "victim@rajfinancial.com"
    ];

    /// <summary>
    /// Deletes all <c>EntityRoles</c> and <c>Entities</c> rows belonging to known test users.
    /// The <c>UserProfileProvisioningMiddleware</c> will re-create the Personal entity
    /// on the next authenticated request.
    /// </summary>
    /// <param name="configuration">Test configuration (reads <c>ConnectionStrings:SqlConnectionString</c>).</param>
    public static async Task CleanupAsync(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SqlConnectionString is not configured. Set it in appsettings.local.json under " +
                "ConnectionStrings:SqlConnectionString, or via the ConnectionStrings__SqlConnectionString " +
                "environment variable. This helper issues destructive DELETE " +
                "statements and must never silently skip cleanup — a misconfigured CI run would " +
                "then corrupt scenarios with stale rows from previous runs.");
        }

        EnsureConnectionStringIsNonProduction(connectionString);

        var userIds = KnownTestEmails
            .Select(email => Guid.Parse(TestClaimsBuilder.DeterministicUserId(email)))
            .ToArray();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Build a parameterised IN clause. SqlClient has no first-class array-param support
        // so we construct one placeholder per user id.
        var paramNames = userIds.Select((_, i) => $"@u{i}").ToArray();
        var inClause = string.Join(", ", paramNames);

        // Order matters: EntityRoles FK to Entities. Delete roles first.
        var commands = new[]
        {
            $"DELETE FROM [EntityRoles] WHERE [EntityId] IN (SELECT [Id] FROM [Entities] WHERE [UserId] IN ({inClause}))",
            $"DELETE FROM [Entities] WHERE [UserId] IN ({inClause})"
        };

        foreach (var sql in commands)
        {
            await using var cmd = new SqlCommand(sql, connection);
            for (var i = 0; i < userIds.Length; i++)
            {
                cmd.Parameters.Add(new SqlParameter(paramNames[i], userIds[i]));
            }

            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Safety guard: refuse to run destructive DELETE statements against a production database.
    /// Parses the connection string and requires the DataSource to be either a local SQL Server
    /// instance or a host explicitly listed in the <c>INTEGRATION_TEST_ALLOWED_SQL_HOSTS</c>
    /// environment variable (semicolon-separated). Any host containing the substring
    /// <c>"prod"</c> is always rejected, regardless of allowlist contents.
    /// </summary>
    internal static void EnsureConnectionStringIsNonProduction(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource?.Trim() ?? string.Empty;

        // Strip named-instance and port qualifiers (e.g., "localhost,1433" or "localhost\\SQLEXPRESS"
        // or "tcp:rajfinancial-dev.database.windows.net,1433").
        var host = dataSource
            .Split(',')[0]
            .Split('\\')[0]
            .Trim();

        if (host.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
        {
            host = host[4..];
        }

        // Hard reject any host that looks like a production server, regardless of allowlist.
        if (host.Contains("prod", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to run destructive test cleanup against suspected production data source '{dataSource}'. " +
                "EntityTestDataCleanup issues unguarded DELETE statements and must never run against production.");
        }

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "localhost",
            "127.0.0.1",
            "(local)",
            ".",
        };

        // Allow opting in additional non-prod hosts (e.g., a CI dev SQL server) via env var.
        var extraHosts = Environment.GetEnvironmentVariable("INTEGRATION_TEST_ALLOWED_SQL_HOSTS");
        if (!string.IsNullOrWhiteSpace(extraHosts))
        {
            foreach (var extra in extraHosts.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                allowed.Add(extra);
            }
        }

        var isAllowed = allowed.Contains(host)
            || host.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase);

        if (!isAllowed)
        {
            throw new InvalidOperationException(
                $"Refusing to run destructive test cleanup against unapproved data source '{dataSource}'. " +
                "EntityTestDataCleanup issues unguarded DELETE statements and is only safe against a " +
                "local or explicitly-allowlisted ephemeral database. " +
                "Allowed hosts: localhost, 127.0.0.1, (local), ., (localdb)\\*. " +
                "Add additional non-prod hosts via the INTEGRATION_TEST_ALLOWED_SQL_HOSTS environment " +
                "variable (semicolon-separated).");
        }
    }
}
