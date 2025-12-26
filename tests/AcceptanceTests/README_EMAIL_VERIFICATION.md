# Email Verification Setup for E2E Tests

This guide explains how to configure email verification for acceptance tests that test the signup flow with email verification codes.

## Overview

The E2E tests use **MailKit** to connect to an IMAP server and retrieve verification codes from emails sent during the signup process. This allows the tests to complete the full email verification flow automatically.

## Setup Instructions

### 1. Configure Email Account

You'll need an email account that supports IMAP access. The tests are configured to use **@rajlegacy.org** emails.

**For Yandex Mail (recommended):**
- Host: `imap.yandex.com`
- Port: `993`
- SSL/TLS: Required
- Create an app-specific password (not your main password)

**For other email providers:**
- Gmail: `imap.gmail.com:993` (requires app password with 2FA enabled)
- Outlook: `outlook.office365.com:993`
- Custom domain: Check your email provider's IMAP settings

### 2. Create Configuration File

Create `appsettings.local.json` in the `tests/AcceptanceTests` folder:

```json
{
  "TestSettings": {
    "BaseUrl": "https://localhost:7161",
    "TestUsers": {
      "Client": { "Password": "your-test-client-password" },
      "Advisor": { "Password": "your-test-advisor-password" },
      "Administrator": { "Password": "your-test-admin-password" }
    },
    "ImapHost": "imap.yandex.com",
    "ImapPort": 993,
    "ImapUsername": "test@rajlegacy.org",
    "ImapPassword": "your-app-specific-password"
  }
}
```

**Important:** `appsettings.local.json` is in `.gitignore` and will NOT be committed to the repository. This keeps your credentials safe.

### 3. Alternative: Environment Variables

Instead of `appsettings.local.json`, you can set environment variables:

```bash
# PowerShell
$env:TEST_IMAP_HOST="imap.yandex.com"
$env:TEST_IMAP_PORT="993"
$env:TEST_IMAP_USERNAME="test@rajlegacy.org"
$env:TEST_IMAP_PASSWORD="your-app-specific-password"

# Bash / Linux
export TEST_IMAP_HOST="imap.yandex.com"
export TEST_IMAP_PORT="993"
export TEST_IMAP_USERNAME="test@rajlegacy.org"
export TEST_IMAP_PASSWORD="your-app-specific-password"
```

Environment variables take precedence over `appsettings.local.json`.

## How It Works

1. **Generate Test Email**: Tests generate unique email addresses like `test-e2e-20241224152030-abc123@rajlegacy.org`
2. **Signup Flow**: Test fills in signup form with the generated email
3. **Email Verification**: Entra External ID sends a verification code to the email
4. **Retrieve Code**: Test connects to IMAP server, searches for recent emails, and extracts the 6-digit code
5. **Complete Verification**: Test enters the code to complete signup

## Code Structure

- **TestEmailHelper.cs**: Handles IMAP connection and code extraction
- **AuthenticationSteps.cs**: Step definitions that use the email helper
- **TestConfiguration.cs**: Loads IMAP settings from configuration

## Verification Code Patterns

The email helper recognizes these common patterns in verification emails:

```
Verification code: 123456
Your code is: 123456
Code: 123456
OTP: 123456
```

It specifically looks for 6-digit numeric codes, which is the standard format for Entra External ID verification codes.

## Troubleshooting

### "IMAP settings not configured" error
- Ensure `appsettings.local.json` exists with all IMAP fields filled
- OR ensure all 4 environment variables are set

### "Email not received within 60 seconds" error
- Check that the IMAP credentials are correct
- Verify the email account can receive emails
- Check your email provider's IMAP settings (host, port)
- Ensure the Entra External ID tenant is configured to send verification emails

### "Could not connect to IMAP server" error
- Verify IMAP is enabled on the email account
- Check firewall settings (port 993 must be open)
- For Gmail: Enable "Less secure app access" or use app-specific password
- For Yandex: Create app-specific password in account settings

### Tests still use Mailinator
- The old `MailinatorApiToken` setting is obsolete and no longer used
- All tests now use MailKit with IMAP

## Security Notes

- **Never commit credentials**: `appsettings.local.json` is gitignored
- **Use app-specific passwords**: Don't use your main email password
- **Rotate passwords regularly**: Change test account passwords periodically
- **Limit access**: Use a dedicated test email account, not a production account

## CI/CD Integration

For GitHub Actions or Azure Pipelines, set the environment variables as secrets:

```yaml
# GitHub Actions
env:
  TEST_IMAP_HOST: ${{ secrets.TEST_IMAP_HOST }}
  TEST_IMAP_PORT: ${{ secrets.TEST_IMAP_PORT }}
  TEST_IMAP_USERNAME: ${{ secrets.TEST_IMAP_USERNAME }}
  TEST_IMAP_PASSWORD: ${{ secrets.TEST_IMAP_PASSWORD }}
```

```yaml
# Azure Pipelines
variables:
- name: TEST_IMAP_HOST
  value: $(TestImapHost)
- name: TEST_IMAP_USERNAME
  value: $(TestImapUsername)
# etc.
```
