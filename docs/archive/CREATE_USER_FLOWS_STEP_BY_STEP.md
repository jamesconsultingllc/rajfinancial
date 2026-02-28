# Create Entra External ID User Flows
## Step-by-Step Guide for Dev and Prod Tenants

---

## What Are User Flows?

User flows define the **sign-up and sign-in experience** for your users:
- What identity providers they can use (email, Google, Microsoft, etc.)
- What information to collect during signup (name, email, etc.)
- Whether to require MFA
- Custom branding and language
- API Connectors (for automatic role assignment)

---

## Part 1: Create Dev User Flow (No MFA)

### Step 1: Navigate to Entra Portal

1. Go to https://entra.microsoft.com
2. Click the tenant dropdown (top right)
3. Select: **rajfinancialdev.onmicrosoft.com** (Dev tenant)
4. Wait for the portal to switch tenants

### Step 2: Navigate to User Flows

1. In the left sidebar, expand **External Identities**
2. Click **User flows**
3. You should see "No user flows found" or an empty list

### Step 3: Create New User Flow

1. Click **+ New user flow**
2. You'll see the user flow creation wizard

### Step 4: Choose User Flow Type

Select:
- **Sign up and sign in** (this is the most common)
- Click **Next**

### Step 5: Configure Basic Settings

Fill in:
- **Name**: `signup_signin`
  - This will become `B2C_1_signup_signin` automatically
- **Identity providers**:
  - ☑️ **Email accounts** (Email with password)
  - You can add social providers later (Google, Microsoft, etc.)

Click **Next**

### Step 6: Configure User Attributes

Choose what information to collect during signup:

**Required (always collect):**
- ☑️ Email Address
- ☑️ Display Name
- ☑️ Given Name
- ☑️ Surname

**Optional (you can skip for now):**
- ☐ City
- ☐ Country/Region
- ☐ Postal Code
- ☐ State/Province

**Custom attribute for role assignment:**
- ☑️ **RequestedRole** (if you created this custom attribute)
  - If you haven't created it yet, skip it - we can add it later

Click **Next**

### Step 7: Configure MFA

**⚠️ IMPORTANT**: Entra External ID does NOT have MFA settings in user flows like B2C does.

MFA for Entra External ID is configured **tenant-wide**, not per user flow:
- Go to **Protection** → **Authentication methods** to enable/disable MFA
- Or use **Conditional Access** (requires Premium P1)

**For Dev tenant**: Leave MFA disabled tenant-wide (default setting).

**No action needed here** - just click **Next**

### Step 8: Configure Branding (Optional)

You can customize later, so for now:
- **Page layout**: Default
- **Language customization**: English (US) - default

Click **Next**

### Step 9: Review and Create

Review your settings:
```
Name: B2C_1_signup_signin
Identity providers: Email accounts
MFA: Off
User attributes: Email, Display Name, Given Name, Surname
```

Click **Create**

### Step 10: Verify Creation

You should now see your user flow in the list!

---

## Part 2: Test the User Flow

Before configuring the API connector, let's test the basic flow:

### Step 1: Get the Test URL

1. Click on your user flow: **B2C_1_signup_signin**
2. At the top, click **Run user flow**
3. You'll see a panel on the right with:
   - **Application**: Select your app (RajFinancial.Client.Dev)
   - **Reply URL**: Should show your app's redirect URI
4. Click **Run user flow** button

### Step 2: Test Sign-Up

1. A new tab will open with the sign-up page
2. Click **Sign up now**
3. Enter:
   - Email address (use a test email)
   - Password (make it strong)
   - Display name
   - Given name
   - Surname
4. Click **Create**
5. Verify email if prompted

**Expected Result:** ✅ You should be signed in without MFA prompt

### Step 3: Test Sign-In

1. Sign out
2. Go back to the user flow test page
3. Click **Run user flow** again
4. This time, enter your email and password
5. Click **Sign in**

**Expected Result:** ✅ You should be signed in without MFA prompt

---

## Part 3: Add API Connector (For Automatic Role Assignment)

Now that the basic flow works, let's add the API connector to assign roles automatically.

### Step 1: Create API Connector

1. Still in your user flow, go to **API connectors**
2. Click **+ Add API connector**
3. Configure:

**General:**
- **Display name**: `Assign Role During Signup`
- **Endpoint URL**: `https://func-rajfinancial-dev.azurewebsites.net/api/auth/assign-role-signup`
  - ⚠️ **NOTE**: You need to deploy your Function App first!
  - For local testing, you can use ngrok or skip this step for now

**Authentication:**
- **Authentication type**: **Basic authentication**
- **Username**: Can be anything (e.g., `apiconnector`)
- **Password**: Your Function App's function key
  - Get this from: Azure Portal → Function App → Functions → AssignRoleDuringSignup → Function Keys

Click **Save**

### Step 2: Attach API Connector to User Flow

1. Go back to your user flow: **B2C_1_signup_signin**
2. Scroll down to **API connectors**
3. Click on the step: **Before creating the user**
4. Select: **Assign Role During Signup** (the one you just created)
5. Click **Save**

### Step 3: Test Role Assignment

1. Run the user flow again
2. Sign up with a NEW email address
3. After signup, check if the role was assigned:

```powershell
# Get the user's app role assignments
$email = "testuser@example.com"
$user = Get-MgUser -Filter "mail eq '$email'"
Get-MgUserAppRoleAssignment -UserId $user.Id | Format-Table AppRoleId, ResourceDisplayName
```

You should see the role assignment!

---

## Part 4: Configure MFA for Dev and Prod Tenants

### Dev Tenant - Disable MFA (Default)

1. Go to https://entra.microsoft.com
2. Switch to: `rajfinancialdev.onmicrosoft.com`
3. Navigate to: **Protection** → **Authentication methods**
4. Click **Settings**
5. **Leave defaults** (Email OTP disabled, SMS disabled)
6. **Result**: ✅ No MFA for any dev users

### Prod Tenant - Enable MFA (Later)

**Option A: Tenant-Wide MFA (Free, No Exceptions)**

1. Go to https://entra.microsoft.com
2. Switch to: `rajfinancialprod.onmicrosoft.com`
3. Navigate to: **Protection** → **Authentication methods**
4. Enable **Email OTP** or **SMS**
5. **Result**: ✅ MFA for ALL users (no test user exceptions)

**Option B: Conditional Access (Premium P1, Can Exclude Test Users)**

See `docs/MFA_CONFIGURATION_WITH_TEST_EXCEPTIONS.md` for details.

**Recommendation**: Use Option A (tenant-wide) unless you need test user exceptions.

## Part 5: Create Prod User Flow

**Do this LATER** when you're ready to deploy to production. The steps are identical to Dev, except:

### Key Differences:

1. **Tenant**: Use `rajfinancialprod.onmicrosoft.com` (Prod tenant)
2. **MFA**: Configured tenant-wide (see Part 4 above)
3. **API Connector URL**: `https://func-rajfinancial-prod.azurewebsites.net/api/auth/assign-role-signup`
4. **Branding**: Apply custom RAJ Financial branding (gold theme, logo)

---

## Part 5: Update Your Blazor App Configuration

Once your user flow is created, update your app configuration:

### appsettings.Development.json

```json
{
  "AzureAdB2C": {
    "Instance": "https://rajfinancialdev.b2clogin.com",
    "Domain": "rajfinancialdev.onmicrosoft.com",
    "TenantId": "496527a2-41f8-4297-a979-c916e7255a22",
    "ClientId": "<YOUR-DEV-APP-CLIENT-ID>",
    "SignUpSignInPolicyId": "B2C_1_signup_signin",
    "CallbackPath": "/authentication/login-callback"
  }
}
```

### Test in Your App

1. Run your Blazor app locally: `dotnet run`
2. Click "Sign Up as Client" or "Sign Up as Advisor"
3. Complete the signup flow
4. Verify you're signed in with the correct role

---

## Troubleshooting

### "Application not found" when running user flow

**Fix**:
1. Go to user flow → **Applications**
2. Click **+ Add application**
3. Select your app: `RajFinancial.Client.Dev`
4. Save

### API Connector returns error

**Check Function App logs:**
```bash
az functionapp logs tail \
  --name func-rajfinancial-dev \
  --resource-group rg-rajfinancial-dev
```

### User flow not showing in app

**Fix**: Make sure `SignUpSignInPolicyId` in appsettings matches exactly:
- In portal: `B2C_1_signup_signin`
- In config: `"SignUpSignInPolicyId": "B2C_1_signup_signin"`

---

## Summary Checklist

### Dev Tenant Setup
- [ ] Create user flow: `B2C_1_signup_signin`
- [ ] Set MFA: **Off**
- [ ] Add user attributes: Email, Display Name, Given Name, Surname
- [ ] Test basic sign-up/sign-in
- [ ] Deploy Function App to Azure
- [ ] Create API connector
- [ ] Attach API connector to "Before creating user" step
- [ ] Test role assignment
- [ ] Update Blazor app configuration

### Prod Tenant Setup (Later)
- [ ] Create user flow: `B2C_1_signup_signin`
- [ ] Set MFA: **Email/SMS, Always on**
- [ ] Add user attributes (same as dev)
- [ ] Apply custom branding
- [ ] Create API connector (prod URL)
- [ ] Test end-to-end

---

## Next Steps

After creating the user flow:
1. **Deploy Function App** to Azure (so API connector can reach it)
2. **Get function key** from Azure Portal
3. **Configure API connector** with the function key
4. **Test the full flow** end-to-end
5. **Update execution plan** to mark user flow tasks as complete

---

*Last Updated: December 24, 2024*
