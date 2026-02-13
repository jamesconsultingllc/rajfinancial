using System.Data.Common;
using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RajFinancial.Api.Data;

/// <summary>
/// EF Core connection interceptor that adds Azure AD access tokens to SQL connections.
/// Enables Managed Identity authentication for Azure SQL Database.
/// </summary>
/// <remarks>
/// This interceptor acquires tokens using DefaultAzureCredential, which supports:
/// - Managed Identity (in Azure)
/// - Azure CLI credentials (local development)
/// - Visual Studio credentials (local development)
/// - Environment variables (CI/CD)
/// </remarks>
public class ManagedIdentityConnectionInterceptor : DbConnectionInterceptor
{
    private readonly TokenCredential credential;
    private readonly string[] scopes = ["https://database.windows.net/.default"];

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedIdentityConnectionInterceptor"/> class.
    /// </summary>
    /// <param name="credential">The token credential to use for authentication. Defaults to DefaultAzureCredential.</param>
    public ManagedIdentityConnectionInterceptor(TokenCredential? credential = null)
    {
        this.credential = credential ?? new DefaultAzureCredential();
    }

    /// <inheritdoc/>
    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        if (connection is SqlConnection sqlConnection)
        {
            SetAccessToken(sqlConnection);
        }

        return base.ConnectionOpening(connection, eventData, result);
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (connection is SqlConnection sqlConnection)
        {
            await SetAccessTokenAsync(sqlConnection, cancellationToken);
        }

        return await base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
    }

    private void SetAccessToken(SqlConnection connection)
    {
        // Only set token if not already using integrated authentication
        if (string.IsNullOrEmpty(connection.AccessToken) && !UsesIntegratedSecurity(connection))
        {
            var tokenRequestContext = new TokenRequestContext(scopes);
            var token = credential.GetToken(tokenRequestContext, CancellationToken.None);
            connection.AccessToken = token.Token;
        }
    }

    private async Task SetAccessTokenAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // Only set token if not already using integrated authentication
        if (string.IsNullOrEmpty(connection.AccessToken) && !UsesIntegratedSecurity(connection))
        {
            var tokenRequestContext = new TokenRequestContext(scopes);
            var token = await credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            connection.AccessToken = token.Token;
        }
    }

    private static bool UsesIntegratedSecurity(SqlConnection connection)
    {
        var connectionString = connection.ConnectionString;
        return connectionString.Contains("Integrated Security=", StringComparison.OrdinalIgnoreCase) ||
               connectionString.Contains("Trusted_Connection=", StringComparison.OrdinalIgnoreCase) ||
               connectionString.Contains("Authentication=", StringComparison.OrdinalIgnoreCase);
    }
}
