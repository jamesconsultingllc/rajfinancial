using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Reqnroll;

namespace RajFinancial.IntegrationTests.Support;

/// <summary>
/// Reqnroll hooks that keep asset-related tests isolated across runs by
/// wiping residual <c>Assets</c> rows for known test users before each
/// scenario tagged <c>@assets</c>.
/// </summary>
[Binding]
public sealed class AssetTestHooks(FunctionsHostFixture fixture)
{
    [BeforeScenario(Order = 10)]
    [Scope(Tag = "assets")]
    public async Task CleanupBeforeAssetScenarioAsync()
    {
        var connectionString = fixture.Configuration.GetConnectionString("SqlConnectionString")
                               ?? throw new InvalidOperationException(
                                   "SqlConnectionString is not configured. Set it in appsettings.local.json under " +
                                   "ConnectionStrings:SqlConnectionString. AssetTestHooks issues destructive DELETE " +
                                   "statements and must never silently skip cleanup.");

        var userIds = EntityTestDataCleanup.KnownTestEmails
            .Select(email => Guid.Parse(TestClaimsBuilder.DeterministicUserId(email)))
            .ToArray();

        EntityTestDataCleanup.EnsureConnectionStringIsNonProduction(connectionString);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var paramNames = userIds.Select((_, i) => $"@u{i}").ToArray();
        var inClause = string.Join(", ", paramNames);

        await using var cmd = new SqlCommand(
            $"DELETE FROM [Assets] WHERE [UserId] IN ({inClause})",
            connection);

        for (var i = 0; i < userIds.Length; i++)
        {
            cmd.Parameters.Add(new SqlParameter(paramNames[i], userIds[i]));
        }

        await cmd.ExecuteNonQueryAsync();
    }
}
