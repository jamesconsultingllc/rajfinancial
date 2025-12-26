# Mailinator Setup Guide for E2E Tests

Quick setup guide for using Mailinator with RAJ Financial E2E authentication tests.

---

## 🚀 Quick Start (5 minutes)

### Step 1: Get Your Mailinator API Token

1. **Sign up** at [mailinator.com](https://www.mailinator.com) (free account available)

2. **Log in** and go to your API token page:
   - Direct link: https://www.mailinator.com/v4/index.jsp#/#apitoken
   - Or navigate: Settings → API Token

3. **Copy your API token** (looks like: `abc123def456...`)

---

### Step 2: Configure Environment Variable

**Windows (PowerShell):**
```powershell
$env:MAILINATOR_API_TOKEN="your-token-here"
```

**Windows (CMD):**
```cmd
set MAILINATOR_API_TOKEN=your-token-here
```

**macOS/Linux:**
```bash
export MAILINATOR_API_TOKEN=your-token-here
```

**Or add to your shell profile** (e.g., `~/.bashrc`, `~/.zshrc`):
```bash
echo 'export MAILINATOR_API_TOKEN="your-token-here"' >> ~/.bashrc
source ~/.bashrc
```

---

### Step 3: Run the Tests

```bash
cd tests/AcceptanceTests
dotnet test --filter "Category=authentication&Category=signup"
```

**That's it!** ✅

---

## 📧 How It Works

### Test Email Pattern

The tests generate unique Mailinator emails:
```
test-e2e-20241224120530-a1b2c3d4@mailinator.com
         ^^^^^^^^^^^^^^ ^^^^^^^^
         timestamp      unique ID
```

### Viewing Emails Manually

If you need to check emails during test development:

1. **Note the test email** from console output:
   ```
   📧 Test email: test-e2e-20241224120530-a1b2c3d4@mailinator.com
   📬 View inbox: https://www.mailinator.com/v4/public/inboxes.jsp?to=test-e2e-20241224120530-a1b2c3d4
   ```

2. **Click the inbox URL** or visit:
   ```
   https://www.mailinator.com/v4/public/inboxes.jsp?to={inbox-name}
   ```

3. **View the verification email** and extract the code if needed

---

## 🧪 Test Flow

Here's what happens during a signup test:

1. ✅ Test clicks "Sign In / Sign Up" button
2. ✅ Redirected to Entra External ID
3. ✅ Clicks "Create account"
4. ✅ Generates unique Mailinator email: `test-e2e-{timestamp}-{guid}@mailinator.com`
5. ✅ Fills in signup form with email + password
6. ✅ Submits the form
7. ⏳ **Email verification** (if enabled):
   - Waits for verification email (up to 60 seconds)
   - Fetches email via Mailinator API
   - Extracts verification code
   - Enters code to verify account
8. ✅ Redirected back to RAJ Financial
9. ✅ Verified: User is logged in
10. 🧹 **Cleanup**: User marked for deletion

---

## ⚙️ Configuration Options

### Option 1: Disable Email Verification (Fastest)

If email verification is not critical for your tests:

1. **Azure Portal** → **Entra External ID** → **User flows**
2. Select your user flow
3. **Page layouts** → **Sign up page**
4. **Uncheck**: "Email verification required"
5. Save

**Tests will run faster** without waiting for emails.

---

### Option 2: Keep Email Verification (Production-like)

To test the full signup flow including email verification:

1. **Keep email verification enabled** in Entra
2. **Set the API token** (as shown above)
3. **Run tests** - they'll automatically:
   - Wait for verification email
   - Extract the code
   - Complete verification

---

## 🧹 Test User Cleanup

Test users follow the pattern: `test-e2e-*@mailinator.com`

### Manual Cleanup

Delete test users via Microsoft Graph API:

```bash
cd tests/AcceptanceTests

# Set cleanup credentials
export AZURE_TENANT_ID=your-tenant-id
export AZURE_CLIENT_ID=your-app-client-id
export AZURE_CLIENT_SECRET=your-app-secret

# Run cleanup (deletes users older than 24 hours)
dotnet run -- cleanup-users --older-than 24
```

### Automated Cleanup (Recommended)

Set up a **daily cleanup job** using:
- **Azure Function** (timer trigger)
- **GitHub Actions** (scheduled workflow)

See `README_AUTHENTICATION_TESTING.md` for complete setup instructions.

---

## 🐛 Troubleshooting

### Issue: "Mailinator API token not configured"

**Error:**
```
Mailinator API token not configured.
Get your token from https://www.mailinator.com/v4/index.jsp#/#apitoken
and set MAILINATOR_API_TOKEN environment variable.
```

**Solution:**
- Verify you've set `MAILINATOR_API_TOKEN` environment variable
- Restart your terminal/IDE after setting the variable
- Check token is correct (no extra spaces/quotes)

---

### Issue: "Email not received within 60 seconds"

**Error:**
```
Email not received within 60 seconds.
Check inbox manually at: https://www.mailinator.com/v4/public/inboxes.jsp?to={inbox}
```

**Possible causes:**
1. **Email verification disabled** in Entra → No email will be sent
2. **Entra not configured for test domain** → Email may be blocked
3. **Network issues** → Mailinator API unreachable

**Solutions:**
- Click the inbox URL to verify if email arrived
- Check Entra External ID settings
- Increase timeout in test if needed:
  ```csharp
  await emailHelper.GetVerificationCodeFromEmail(testEmail, timeoutSeconds: 120);
  ```

---

### Issue: "Could not find verification code in email"

**Error:**
```
Could not find verification code in email
```

**Solution:**
1. **View the email manually** using the inbox URL
2. **Check the email format** - verification code may use a different pattern
3. **Update the regex patterns** in `TestEmailHelper.cs` → `ExtractVerificationCode()` method

Example patterns supported:
- `Verification code: 123456`
- `Your code is: 123456`
- `Enter code: 123456`
- `OTP: 123456`
- Links: `?code=abc123` or `&token=abc123`

---

### Issue: API Rate Limiting

**Free tier limits:**
- 50 API calls per hour
- Public inboxes only

**Solution:**
- Upgrade to **Mailinator Team/Pro** for higher limits
- Or reduce test frequency
- Or batch cleanup instead of per-test cleanup

---

## 📊 Mailinator Plans

| Plan | Price | API Calls | Private Inboxes | Best For |
|------|-------|-----------|-----------------|----------|
| **Free** | $0 | 50/hour | ❌ (public only) | Development |
| **Team** | $49/mo | 10,000/month | ✅ | Small teams |
| **Pro** | $149/mo | 50,000/month | ✅ | CI/CD pipelines |

**For RAJ Financial**: Free tier is fine for local development. Consider Team plan for CI/CD.

---

## ✅ Checklist

Before running tests, ensure:

- [ ] Mailinator account created
- [ ] API token obtained from mailinator.com
- [ ] `MAILINATOR_API_TOKEN` environment variable set
- [ ] Entra External ID configured (email verification enabled/disabled as needed)
- [ ] Microsoft Graph API configured for cleanup (optional)

---

## 🔗 Useful Links

- **Mailinator Home**: https://www.mailinator.com
- **API Documentation**: https://www.mailinator.com/api/docs
- **Get API Token**: https://www.mailinator.com/v4/index.jsp#/#apitoken
- **View Public Inboxes**: https://www.mailinator.com/v4/public/inboxes.jsp

---

## 💡 Tips

1. **Use descriptive inbox names** for easier debugging:
   ```csharp
   test-e2e-signup-20241224-abc123@mailinator.com
            ^^^^^^
            feature name
   ```

2. **Keep test emails short** - Mailinator limits inbox name length

3. **Delete old emails** - Free tier has storage limits

4. **Bookmark inbox URLs** during test development for quick access

5. **Check spam folder** if emails don't arrive (rare with Mailinator)

---

For more details, see the full documentation:
- **Authentication Testing**: `README_AUTHENTICATION_TESTING.md`
- **Test User Cleanup**: `TestUserCleanupHelper.cs`
- **Email Helper**: `TestEmailHelper.cs`
