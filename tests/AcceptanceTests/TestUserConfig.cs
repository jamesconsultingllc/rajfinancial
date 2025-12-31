namespace RajFinancial.AcceptanceTests;

/// <summary>
///     Configuration for a test user.
/// </summary>
public class TestUserConfig
{
    /// <summary>
    ///     Password for the test user.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     Optional Playwright storage state file path for the test user.
    /// </summary>
    public string StorageStatePath { get; set; } = string.Empty;
}