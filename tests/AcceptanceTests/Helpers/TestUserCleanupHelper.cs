// ============================================================================
// RAJ Financial - Test User Cleanup Helper
// ============================================================================
// Deletes test users created during E2E tests using Microsoft Graph API
// Can be called manually or via scheduled Azure Function
// ============================================================================

using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;

namespace RajFinancial.AcceptanceTests.Helpers;

/// <summary>
///     Helper for cleaning up test users created during E2E tests.
///     Uses Microsoft Graph API to delete users by email pattern.
///     Authentication methods (in order of priority):
///     1. Client Secret (Local/CI): Set AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET
///     - Uses ClientSecretCredential explicitly
///     - Requires Application permissions on the app registration
///     2. Workload Identity Federation (GitHub Actions): Set AZURE_TENANT_ID, AZURE_CLIENT_ID
///     - Uses OIDC tokens from GitHub Actions (no secret needed)
///     - Requires federated credential configured in app registration
///     3. Managed Identity (Azure): Automatic when running in Azure
///     - No configuration needed, uses Azure VM/Container identity
///     4. Azure CLI (Local): Run 'az login' first
///     - Uses your user credentials
///     - Requires User.DeleteRestore.All delegated permission AND directory role
///     Required permissions:
///     - Application: User.Read.All + User.DeleteRestore.All (for methods 1-3)
///     - Delegated: User.DeleteRestore.All + User Administrator role (for method 4)
/// </summary>
public class TestUserCleanupHelper
{
    private readonly string? clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
    };
    private readonly string? tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

    /// <summary>
    ///     Pattern to identify test users created by E2E tests.
    ///     Default: test-e2e-* emails
    /// </summary>
    public string TestUserEmailPattern { get; set; } = "test-e2e-";

    /// <summary>
    ///     Checks if the helper is properly configured for authentication.
    /// </summary>
    public bool IsConfigured()
    {
        // At minimum, we need tenant ID and client ID for federated credentials
        // DefaultAzureCredential will handle the rest based on the environment
        return !string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(clientId);
    }

    /// <summary>
    ///     Deletes test users by email address.
    /// </summary>
    /// <param name="emails">List of test user email addresses to delete</param>
    /// <returns>Number of users successfully deleted</returns>
    public async Task<int> DeleteTestUsers(IEnumerable<string> emails)
    {
        var accessToken = await GetAccessToken();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var deletedCount = 0;

        foreach (var email in emails)
            try
            {
                // Find user by email
                var userId = await GetUserIdByEmail(email);
                if (userId != null)
                {
                    // Delete user
                    var response = await httpClient.DeleteAsync($"users/{userId}");
                    if (response.IsSuccessStatusCode)
                    {
                        deletedCount++;
                        Console.WriteLine($"✓ Deleted test user: {email}");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Failed to delete {email}: {response.StatusCode}");
                    }
                }
                else
                {
                    Console.WriteLine($"⊘ User not found: {email}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error deleting {email}: {ex.Message}");
            }

        return deletedCount;
    }

    /// <summary>
    ///     Deletes all test users matching the configured email pattern.
    /// </summary>
    /// <returns>Number of users deleted</returns>
    public async Task<int> DeleteAllTestUsers()
    {
        var accessToken = await GetAccessToken();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Find all test users matching the pattern
        var filter =
            $"startswith(mail,'{TestUserEmailPattern}') or startswith(userPrincipalName,'{TestUserEmailPattern}')";
        var response =
            await httpClient.GetAsync(
                $"users?$filter={Uri.EscapeDataString(filter)}&$select=id,mail,userPrincipalName");

        if (!response.IsSuccessStatusCode) throw new Exception($"Failed to query users: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        var users = result.RootElement.GetProperty("value");

        var deletedCount = 0;

        foreach (var user in users.EnumerateArray())
        {
            var userId = user.GetProperty("id").GetString();
            var email = user.TryGetProperty("mail", out var mailProp)
                ? mailProp.GetString()
                : user.GetProperty("userPrincipalName").GetString();

            var deleteResponse = await httpClient.DeleteAsync($"users/{userId}");
            if (deleteResponse.IsSuccessStatusCode)
            {
                deletedCount++;
                Console.WriteLine($"✓ Deleted test user: {email}");
            }
            else
            {
                Console.WriteLine($"✗ Failed to delete {email}: {deleteResponse.StatusCode}");
            }
        }

        return deletedCount;
    }

    /// <summary>
    ///     Gets user ID by email address using Microsoft Graph API.
    /// </summary>
    private async Task<string?> GetUserIdByEmail(string email)
    {
        var filter = $"mail eq '{email}' or userPrincipalName eq '{email}'";
        var response = await httpClient.GetAsync($"users?$filter={Uri.EscapeDataString(filter)}&$select=id");

        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        var users = result.RootElement.GetProperty("value");

        if (users.GetArrayLength() > 0) return users[0].GetProperty("id").GetString();

        return null;
    }

    /// <summary>
    ///     Gets an access token for Microsoft Graph API.
    ///     Supports multiple authentication methods:
    ///     - Client Secret (AZURE_CLIENT_SECRET environment variable)
    ///     - Workload Identity Federation (GitHub Actions OIDC)
    ///     - Managed Identity (Azure)
    ///     - Azure CLI (local development)
    /// </summary>
    private async Task<string> GetAccessToken()
    {
        var tokenRequestContext = new TokenRequestContext(["https://graph.microsoft.com/.default"]);
        TokenCredential credential;

        // Check for explicit client secret first (most common for local dev)
        var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

        if (!string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(clientId))
        {
            // Use ClientSecretCredential for explicit client secret authentication
            Console.WriteLine("ℹ️  Using client secret authentication (app permissions)");
            credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        }
        else
        {
            // Fall back to DefaultAzureCredential for other authentication methods:
            // 1. WorkloadIdentityCredential (GitHub Actions OIDC)
            // 2. ManagedIdentityCredential (Azure-hosted environments)
            // 3. AzureCliCredential (local development with 'az login')
            // 4. VisualStudioCredential
            // 5. VisualStudioCodeCredential

            Console.WriteLine("ℹ️  Using DefaultAzureCredential (federated/managed identity/Azure CLI)");
            var credentialOptions = new DefaultAzureCredentialOptions
            {
                TenantId = tenantId,
                // Exclude interactive browser to avoid prompts in CI/CD
                ExcludeInteractiveBrowserCredential = true
            };

            credential = new DefaultAzureCredential(credentialOptions);
        }

        try
        {
            var token = await credential.GetTokenAsync(tokenRequestContext, CancellationToken.None);
            Console.WriteLine("✓ Successfully obtained access token for Microsoft Graph");
            return token.Token;
        }
        catch (AuthenticationFailedException ex)
        {
            throw new InvalidOperationException(
                "Failed to authenticate with Azure. Ensure one of the following is configured:\n" +
                "  - Client Secret: Set AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET\n" +
                "  - GitHub Actions: Enable OIDC and set AZURE_TENANT_ID, AZURE_CLIENT_ID\n" +
                "  - Azure: Use Managed Identity\n" +
                "  - Local: Run 'az login' (uses your user credentials)\n" +
                $"Error: {ex.Message}", ex);
        }
    }
}