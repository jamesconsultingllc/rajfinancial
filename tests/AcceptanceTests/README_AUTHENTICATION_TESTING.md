# Authentication E2E Testing Guide

This guide explains how to run and maintain E2E tests for the authentication flow (signup, login, logout).

---

## Table of Contents

1. [Overview](#overview)
2. [Email Verification Strategies](#email-verification-strategies)
3. [Test User Cleanup](#test-user-cleanup)
4. [Configuration](#configuration)
5. [Running Tests](#running-tests)
6. [Scheduled Cleanup](#scheduled-cleanup)

---

## Overview

The authentication E2E tests verify:
- ✅ User signup flow through Entra External ID
- ✅ User login with existing credentials
- ✅ User logout
- ✅ Protected page access control
- ✅ Role-based authorization

**Test Naming Convention:**
```
test-e2e-{timestamp}-{guid}@{domain}
Example: test-e2e-20241224120530-a1b2c3d4@rajfinancialdev.onmicrosoft.com
```

This naming pattern allows:
- Easy identification of test users
- Automated cleanup via scheduled jobs
- Unique emails for each test run

---

## Email Verification Strategies

Entra External ID may require email verification during signup. Choose one strategy:

### Option 1: Disable Email Verification (Easiest)

**For test environments only:**

1. Go to **Azure Portal** → **Entra External ID** → **User flows**
2. Select your user flow
3. Go to **Page layouts** → **Sign up page**
4. Uncheck **Email verification required**
5. Save

✅ **Pros**: Fast, simple tests
❌ **Cons**: Doesn't test production flow

---

### Option 2: Use Mailosaur (Recommended for .NET)

**Mailosaur** provides test email inboxes with API access.

#### Setup:

1. **Sign up** at [mailosaur.com](https://mailosaur.com) (free tier available)

2. **Create a server** and note the Server ID (e.g., `abc123`)

3. **Get API key** from Settings

4. **Install NuGet package:**
   ```bash
   dotnet add tests/AcceptanceTests package Mailosaur
   ```

5. **Configure environment variables:**
   ```bash
   export TEST_EMAIL_PROVIDER=mailosaur
   export MAILOSAUR_API_KEY=your-api-key
   export MAILOSAUR_SERVER_ID=abc123
   ```

6. **Update TestEmailHelper.cs** to uncomment Mailosaur integration code

#### Usage:

```csharp
var emailHelper = new TestEmailHelper();
var testEmail = emailHelper.GenerateTestEmail();
// Returns: test-e2e-20241224-a1b2c3d4@abc123.mailosaur.net

// After signup, retrieve verification code
var code = await emailHelper.GetVerificationCodeFromEmail(testEmail);
```

✅ **Pros**: Reliable, private, .NET SDK, production-like
❌ **Cons**: Requires paid account for high volume

---

### Option 3: Use Mailinator (Public, Free)

**Mailinator** is a public email service - **DO NOT use for sensitive data.**

#### Setup:

1. **Configure environment variable:**
   ```bash
   export TEST_EMAIL_PROVIDER=mailinator
   ```

2. **Test emails** will use pattern:
   ```
   test-e2e-{timestamp}-{guid}@mailinator.com
   ```

3. **View emails** at: `https://www.mailinator.com/v4/public/inboxes.jsp?to={inbox}`

⚠️ **WARNING**: Mailinator inboxes are **publicly accessible**. Only use for testing in non-production environments.

✅ **Pros**: Free, no setup
❌ **Cons**: Public (anyone can read), unreliable, no API on free tier

---

### Option 4: Custom Test Email Domain

Set up a test subdomain with email forwarding:

1. **Create subdomain**: `test.rajfinancial.com`
2. **Configure MX records** to forward to a test inbox
3. **Use catch-all** forwarding: `*@test.rajfinancial.com` → test inbox
4. **Access via IMAP/API**

✅ **Pros**: Full control, secure
❌ **Cons**: Requires infrastructure setup

---

## Test User Cleanup

Test users are cleaned up automatically or manually.

### Naming Convention

All test users follow the pattern:
```
test-e2e-{timestamp}-{guid}@{domain}
```

This allows automated cleanup by filtering users with the `test-e2e-` prefix.

### Cleanup Strategies

#### Strategy 1: After Each Test (Immediate Cleanup)

Use Reqnroll hooks to delete test users after each test:

**Create `CleanupHooks.cs`:**

```csharp
[Binding]
public class CleanupHooks
{
    [AfterScenario("@cleanup")]
    public static async Task CleanupTestUsers()
    {
        var testUsers = AuthenticationSteps.GetTestUsersForCleanup();
        if (testUsers.Count > 0)
        {
            var cleanup = new TestUserCleanupHelper();
            await cleanup.DeleteTestUsers(testUsers);
            AuthenticationSteps.ClearCleanupList();
        }
    }
}
```

✅ **Pros**: Clean state after each test
❌ **Cons**: Tests take longer

---

#### Strategy 2: Scheduled Cleanup (Recommended)

Delete old test users periodically (e.g., daily).

**Azure Function (Timer Trigger):**

```csharp
[FunctionName("CleanupTestUsers")]
public static async Task Run(
    [TimerTrigger("0 0 2 * * *")] TimerInfo timer, // Daily at 2 AM
    ILogger log)
{
    log.LogInformation("Starting test user cleanup...");

    var cleanup = new TestUserCleanupHelper
    {
        TestUserEmailPattern = "test-e2e-"
    };

    // Delete test users older than 24 hours
    var deletedCount = await cleanup.DeleteAllTestUsers(olderThanHours: 24);

    log.LogInformation($"Deleted {deletedCount} test users.");
}
```

**Or use a GitHub Actions workflow:**

```yaml
name: Cleanup Test Users

on:
  schedule:
    - cron: '0 2 * * *' # Daily at 2 AM UTC

jobs:
  cleanup:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
      - name: Run cleanup
        env:
          AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
        run: |
          dotnet run --project tests/AcceptanceTests -- cleanup-users
```

✅ **Pros**: Fast tests, batch cleanup, less API calls
❌ **Cons**: Test users accumulate between cleanups

---

## Configuration

### Environment Variables

Set these in your test environment:

```bash
# Email verification (choose one provider)
export TEST_EMAIL_PROVIDER=mailosaur  # or "mailinator", "none"
export MAILOSAUR_API_KEY=your-api-key
export MAILOSAUR_SERVER_ID=abc123

# Microsoft Graph API (for user cleanup)
export AZURE_TENANT_ID=your-tenant-id
export AZURE_CLIENT_ID=your-app-client-id
export AZURE_CLIENT_SECRET=your-app-secret

# Test user credentials (existing users)
export TEST_CLIENT_PASSWORD=SecurePassword123!
export TEST_ADVISOR_PASSWORD=SecurePassword123!
export TEST_ADMINISTRATOR_PASSWORD=SecurePassword123!
```

### App Registration for Cleanup

To use Microsoft Graph API for user cleanup, create an app registration:

1. **Azure Portal** → **Entra ID** → **App registrations** → **New registration**

2. **Name**: `RAJ Financial E2E Test Cleanup`

3. **Add API permissions**:
   - Microsoft Graph → Application permissions
   - `User.ReadWrite.All` (to delete users)
   - **Grant admin consent**

4. **Create client secret**:
   - Certificates & secrets → New client secret
   - Copy the secret value → Save as `AZURE_CLIENT_SECRET`

5. **Note Client ID and Tenant ID** from the Overview page

---

## Running Tests

### Run All Authentication Tests

```bash
cd tests/AcceptanceTests
dotnet test --filter "Category=authentication"
```

### Run Signup Test Only

```bash
dotnet test --filter "FullyQualifiedName~SignupFlow"
```

### Run with Email Verification

```bash
export TEST_EMAIL_PROVIDER=mailosaur
export MAILOSAUR_API_KEY=your-key
export MAILOSAUR_SERVER_ID=your-server-id

dotnet test --filter "Category=authentication"
```

### Run Without Email Verification

Ensure email verification is disabled in Entra External ID:

```bash
export TEST_EMAIL_PROVIDER=none
dotnet test --filter "Category=authentication"
```

---

## Scheduled Cleanup

### Manual Cleanup

Delete all test users older than 24 hours:

```bash
cd tests/AcceptanceTests
dotnet run -- cleanup-users --older-than 24
```

### Azure Function Cleanup

Deploy the cleanup function to Azure:

```bash
# Deploy the function app
cd cleanup-function
func azure functionapp publish YourFunctionAppName

# Configure app settings
az functionapp config appsettings set \
  --name YourFunctionAppName \
  --resource-group YourResourceGroup \
  --settings \
    AZURE_TENANT_ID=your-tenant-id \
    AZURE_CLIENT_ID=your-client-id \
    AZURE_CLIENT_SECRET=your-secret
```

---

## Troubleshooting

### Issue: Email Verification Required

**Error**: Test stuck on "Verify your email" page

**Solution**:
- Disable email verification in Entra External ID user flow (test environments only)
- OR configure `TEST_EMAIL_PROVIDER` to use Mailosaur/Mailinator

---

### Issue: Graph API Permission Denied

**Error**: `Insufficient privileges to complete the operation`

**Solution**:
- Ensure app registration has `User.ReadWrite.All` permission
- Grant admin consent in Azure Portal
- Wait 5-10 minutes for permission propagation

---

### Issue: Test Users Not Deleted

**Error**: Cleanup script doesn't find users

**Solution**:
- Verify `TestUserEmailPattern` matches your test emails
- Check that `AZURE_CLIENT_ID` and `AZURE_CLIENT_SECRET` are correct
- Ensure app registration has admin consent for Graph API

---

## Best Practices

1. ✅ **Use unique test emails** for each test run (timestamp + GUID)
2. ✅ **Tag tests with `@cleanup`** to enable automatic cleanup
3. ✅ **Run cleanup daily** via scheduled job (Azure Function or GitHub Actions)
4. ✅ **Use Mailosaur** for production-like email verification testing
5. ✅ **Disable email verification** in test environments if email testing isn't required
6. ❌ **Never use Mailinator** for sensitive data (public inboxes)
7. ❌ **Don't delete test users immediately** in CI - batch cleanup is faster

---

## Summary

| Aspect | Recommended Approach |
|--------|---------------------|
| **Email Verification** | Mailosaur (production-like) or Disabled (fast tests) |
| **Test Email Pattern** | `test-e2e-{timestamp}-{guid}@{domain}` |
| **Cleanup Strategy** | Scheduled daily cleanup (Azure Function or GitHub Actions) |
| **Cleanup API** | Microsoft Graph API with app registration |
| **Minimum Cleanup Age** | 24 hours (to avoid deleting active test runs) |

---

For questions or issues, contact the DevOps team or open an issue in the repository.
