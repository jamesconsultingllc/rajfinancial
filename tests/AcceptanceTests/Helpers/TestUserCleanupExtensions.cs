namespace RajFinancial.AcceptanceTests.Helpers;

/// <summary>
///     Extension methods for running cleanup as a scheduled job.
/// </summary>
public static class TestUserCleanupExtensions
{
    /// <summary>
    ///     Example: Run cleanup as a scheduled task (can be called from Azure Function).
    /// </summary>
    public static async Task RunScheduledCleanup()
    {
        try
        {
            var cleanup = new TestUserCleanupHelper
            {
                TestUserEmailPattern = "test-e2e-" // Match test emails
            };

            if (!cleanup.IsConfigured())
            {
                Console.WriteLine("⚠ Cleanup skipped: AZURE_TENANT_ID and AZURE_CLIENT_ID not configured");
                return;
            }

            // Delete test users older than 24 hours
            var deletedCount = await cleanup.DeleteAllTestUsers();
            Console.WriteLine($"Cleanup complete. Deleted {deletedCount} test users.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cleanup failed: {ex.Message}");
            throw;
        }
    }
}