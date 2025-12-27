// ============================================================================
// RAJ Financial - Test Email Helper
// ============================================================================
// Helper for handling email verification in E2E tests using MailKit (IMAP)
// Connects to email server to retrieve verification codes from test emails
// ============================================================================

using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;

namespace RajFinancial.AcceptanceTests.Helpers;

/// <summary>
/// Helper for handling email verification in E2E tests using MailKit (IMAP).
///
/// IMAP Setup (for rajlegacy.org emails):
/// 1. Configure IMAP settings in appsettings.local.json:
///    - ImapHost (e.g., "imap.yandex.com")
///    - ImapPort (e.g., 993)
///    - ImapUsername (e.g., "test@rajlegacy.org")
///    - ImapPassword (app-specific password)
/// 2. Alternatively, set environment variables:
///    - TEST_IMAP_HOST
///    - TEST_IMAP_PORT
///    - TEST_IMAP_USERNAME
///    - TEST_IMAP_PASSWORD
/// </summary>
public class TestEmailHelper
{
    private readonly string? imapHost;
    private readonly int imapPort;
    private readonly string? imapUsername;
    private readonly string? imapPassword;
    private const string TestEmailDomain = "rajlegacy.org";

    public TestEmailHelper()
    {
        // Load from configuration (can be overridden by environment variables)
        var config = TestConfiguration.Instance;

        imapHost = Environment.GetEnvironmentVariable("TEST_IMAP_HOST")
                   ?? config.ImapHost;

        var portString = Environment.GetEnvironmentVariable("TEST_IMAP_PORT");
        imapPort = int.TryParse(portString, out var port)
                   ? port
                   : config.ImapPort;

        imapUsername = Environment.GetEnvironmentVariable("TEST_IMAP_USERNAME")
                       ?? config.ImapUsername;

        imapPassword = Environment.GetEnvironmentVariable("TEST_IMAP_PASSWORD")
                       ?? config.ImapPassword;
    }

    /// <summary>
    /// Generates a unique test email address for rajlegacy.org.
    /// Pattern: test-e2e-{timestamp}-{guid}@rajlegacy.org
    /// </summary>
    /// <returns>Test email address that can receive verification emails</returns>
    public string GenerateTestEmail()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"test-e2e-{timestamp}-{guid}@{TestEmailDomain}";
    }

    /// <summary>
    /// Extracts the local part (before @) from an email address.
    /// Example: "test-e2e-20241224-abc123@rajlegacy.org" → "test-e2e-20241224-abc123"
    /// </summary>
    public static string GetLocalPart(string emailAddress)
    {
        return emailAddress.Split('@')[0];
    }

    /// <summary>
    /// Retrieves the verification code from the most recent email using IMAP.
    /// </summary>
    /// <param name="emailAddress">Email address to check (filters emails sent to this address)</param>
    /// <param name="timeoutSeconds">Maximum time to wait for email (default: 60 seconds)</param>
    /// <param name="searchSubject">Optional subject filter (e.g., "Verification Code")</param>
    /// <returns>Verification code or link extracted from the email</returns>
    public async Task<string> GetVerificationCodeFromEmail(
        string emailAddress,
        int timeoutSeconds = 60,
        string? searchSubject = null)
    {
        if (!IsConfigured())
        {
            throw new InvalidOperationException(
                "IMAP settings not configured. " +
                "Set TEST_IMAP_HOST, TEST_IMAP_PORT, TEST_IMAP_USERNAME, and TEST_IMAP_PASSWORD " +
                "environment variables or configure in appsettings.local.json");
        }

        using var client = new ImapClient();
        var startTime = DateTime.UtcNow;
        var lastException = new Exception("No email found");

        Console.WriteLine($"📧 Waiting for verification email...");
        Console.WriteLine($"📧 Connecting to IMAP server: {imapHost}:{imapPort}");
        await client.ConnectAsync(imapHost, imapPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(imapUsername, imapPassword);

        try
        {
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            while ((DateTime.UtcNow - startTime).TotalSeconds < timeoutSeconds)
            {
                try
                {
                    var verificationCode = await TryFindVerificationCodeAsync(inbox, emailAddress, searchSubject);
                    if (!string.IsNullOrEmpty(verificationCode))
                    {
                        Console.WriteLine($"✓ Found verification code: {verificationCode}");
                        return verificationCode;
                    }

                    Console.WriteLine($"⏳ Waiting for email to {emailAddress}... ({(int)(DateTime.UtcNow - startTime).TotalSeconds}s elapsed)");
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"⚠ Error checking email: {ex.Message}");
                }

                await Task.Delay(3000); // Check every 3 seconds
            }
        }
        finally
        {
            await client.DisconnectAsync(true);
        }

        throw new TimeoutException(
            $"Email not received within {timeoutSeconds} seconds. Last error: {lastException.Message}");
    }

    private static async Task<string?> TryFindVerificationCodeAsync(IMailFolder inbox, string recipientEmail, string? searchSubject)
    {
        // Search for recent messages (last 5 minutes to avoid old test emails)
        var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
        var uids = await inbox.SearchAsync(SearchQuery.DeliveredAfter(cutoffTime));

        if (uids.Count == 0)
        {
            return null;
        }

        // Iterate through emails newest first
        foreach (var uid in uids.Reverse())
        {
            var message = await inbox.GetMessageAsync(uid);

            // CRITICAL: Filter by recipient email address to avoid getting codes from other test runs
            bool isForThisRecipient = false;
            if (message.To != null)
            {
                foreach (var recipient in message.To.Mailboxes)
                {
                    if (recipient.Address.Equals(recipientEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        isForThisRecipient = true;
                        break;
                    }
                }
            }

            if (!isForThisRecipient)
            {
                // This email is for a different test email address, skip it
                continue;
            }

            // Filter by subject if specified
            if (searchSubject != null &&
                !message.Subject.Contains(searchSubject, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Console.WriteLine($"📧 Found email: {message.Subject} ({message.Date})");

            // Extract body (prefer text, fallback to HTML)
            var body = message.TextBody ?? message.HtmlBody ?? "";

            // Try to extract verification code
            var verificationCode = ExtractVerificationCode(body);
            if (!string.IsNullOrEmpty(verificationCode))
            {
                return verificationCode;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts verification code from email body using common patterns.
    /// Specifically designed for Microsoft Entra External ID verification codes.
    /// </summary>
    private static string? ExtractVerificationCode(string emailBody)
    {
        // Common patterns for verification codes in emails:
        // 1. "Verification code: 123456" or "Verification code: 12345678" (Entra External ID format - 6 or 8 digits)
        // 2. "Your code is: 123456"
        // 3. "Enter code: 123456"
        // 4. "OTP: 123456"
        // 5. 6-8 digit numbers in isolation
        // 6. Links with ?code=abc123 or &token=abc123

        var patterns = new[]
        {
            @"(?:verification\s*code|your\s*code|code|token|otp)[\s:=]+([0-9]{6,8})",  // 6-8 digit numeric codes
            @"\b([0-9]{8})\b",                                                          // Any 8-digit number (try first)
            @"\b([0-9]{6})\b",                                                          // Any 6-digit number (fallback)
            @"[?&]code=([A-Za-z0-9_-]+)",                                              // URL query parameter ?code=
            @"[?&]token=([A-Za-z0-9_-]+)",                                             // URL query parameter ?token=
            @"(?:enter|use)\s*(?:this|the)?\s*code[\s:]+([0-9]{6,8})"                // "Enter this code: 123456"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(emailBody, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var code = match.Groups[1].Value;
                // Verify it's 6-8 digits (Entra verification codes can be either length)
                if (Regex.IsMatch(code, @"^[0-9]{6,8}$"))
                {
                    Console.WriteLine($"✓ Extracted verification code: {code} ({code.Length} digits)");
                    return code;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if IMAP is properly configured.
    /// </summary>
    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(imapHost) &&
               imapPort > 0 &&
               !string.IsNullOrEmpty(imapUsername) &&
               !string.IsNullOrEmpty(imapPassword);
    }

    /// <summary>
    /// Gets a user-friendly description of the IMAP connection for debugging.
    /// Does not expose sensitive credentials.
    /// </summary>
    public string GetConnectionInfo()
    {
        if (!IsConfigured())
        {
            return "IMAP not configured";
        }

        return $"IMAP: {imapUsername} @ {imapHost}:{imapPort}";
    }
}
