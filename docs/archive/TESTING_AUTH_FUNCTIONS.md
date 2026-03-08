# Testing Auth Functions

This document provides comprehensive testing guidance for the authentication functions in RAJ Financial.

## Test Coverage Summary

| Test Type | Coverage | Status |
|-----------|----------|--------|
| Unit Tests | 95%+ | ✅ Complete |
| Integration Tests | Manual | 📝 Documented below |
| E2E Tests | Manual | 📝 Documented below |

---

## Unit Tests

### Location
- `tests/UnitTests/Api/Functions/Auth/AssignRoleDuringSignupTests.cs`
- `tests/UnitTests/Api/Functions/Auth/CompleteRoleAssignmentTests.cs`

### Running Unit Tests

```bash
# Run all unit tests
cd tests/UnitTests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~AssignRoleDuringSignupTests"

# Run tests in watch mode
dotnet watch test
```

### Unit Test Coverage

**AssignRoleDuringSignupTests.cs** (12 tests):
- ✅ Valid Client role assignment
- ✅ Valid Advisor role assignment
- ✅ Administrator role blocked (security test)
- ✅ Invalid role defaults to Client
- ✅ Missing UiLocales defaults to Client
- ✅ Missing email returns block page
- ✅ Empty request body returns block page
- ✅ Logging for valid roles (theory test)
- ✅ Exception handling continues signup
- ✅ API Connector response format validation

**CompleteRoleAssignmentTests.cs** (11 tests):
- ✅ Valid role assignment for Client, Advisor, Administrator
- ✅ Missing UserId returns 400
- ✅ Missing Role returns 400
- ✅ Invalid role returns 400
- ✅ Missing ServicePrincipalId config returns 500
- ✅ Role already assigned returns success
- ✅ Insufficient permissions returns 403
- ✅ Graph API exception returns 500
- ✅ Success logging verification

**Total: 23 unit tests**

---

## Integration Tests

Integration tests verify the functions work correctly when deployed to Azure or running locally with the Functions runtime.

### Prerequisites

1. **Azure Functions Core Tools** installed:
   ```bash
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```

2. **Local Settings Configured**:
   - Copy `src/Api/local.settings.json.example` to `local.settings.json`
   - Fill in actual values:
     ```json
     {
       "EntraExternalId:TenantId": "496527a2-41f8-4297-a979-c916e7255a22",
       "EntraExternalId:ClientId": "<YOUR-APP-CLIENT-ID>",
       "EntraExternalId:ServicePrincipalId": "<YOUR-SERVICE-PRINCIPAL-ID>"
     }
     ```

3. **Azure CLI Authenticated**:
   ```bash
   az login --tenant 496527a2-41f8-4297-a979-c916e7255a22
   ```

### Running Integration Tests Locally

#### Test 1: AssignRoleDuringSignup (API Connector)

```bash
# 1. Start the Functions runtime
cd src/Api
func start

# 2. In another terminal, test the endpoint
curl -X POST http://localhost:7071/api/auth/assign-role-signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "displayName": "Test User",
    "givenName": "Test",
    "surname": "User",
    "ui_locales": "Client"
  }'

# Expected response:
# {
#   "version": "1.0.0",
#   "action": "Continue",
#   "extension_RequestedRole": "Client"
# }
```

**Test Cases:**

| Test | Request Body | Expected Result |
|------|-------------|-----------------|
| Valid Client | `{ "email": "...", "ui_locales": "Client" }` | 200 OK, role=Client |
| Valid Advisor | `{ "email": "...", "ui_locales": "Advisor" }` | 200 OK, role=Advisor |
| Admin blocked | `{ "email": "...", "ui_locales": "Administrator" }` | 200 OK, role=Client (default) |
| Invalid role | `{ "email": "...", "ui_locales": "SuperUser" }` | 200 OK, role=Client (default) |
| No ui_locales | `{ "email": "..." }` | 200 OK, role=Client (default) |
| Missing email | `{ "displayName": "..." }` | 200 OK, action=ShowBlockPage |

#### Test 2: CompleteRoleAssignment

**Prerequisites:**
1. Create a test user in Entra portal
2. Copy the user's Object ID

```bash
# Test role assignment
curl -X POST http://localhost:7071/api/auth/complete-role \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "<TEST-USER-OBJECT-ID>",
    "role": "Client"
  }'

# Expected response:
# {
#   "success": true,
#   "role": "Client"
# }
```

**Verify Assignment:**

```powershell
# Connect to Graph
Connect-MgGraph -Scopes "AppRoleAssignment.Read.All"

# Get user's role assignments
Get-MgUserAppRoleAssignment -UserId "<TEST-USER-OBJECT-ID>" | Format-Table
```

**Test Cases:**

| Test | Request Body | Expected Result |
|------|-------------|-----------------|
| Valid Client | `{ "userId": "...", "role": "Client" }` | 200 OK, success=true |
| Valid Advisor | `{ "userId": "...", "role": "Advisor" }` | 200 OK, success=true |
| Already assigned | Call same request twice | 200 OK, message="already assigned" |
| Invalid role | `{ "userId": "...", "role": "SuperUser" }` | 400 Bad Request |
| Missing userId | `{ "role": "Client" }` | 400 Bad Request |
| Non-existent user | `{ "userId": "00000000-...", "role": "Client" }` | 500 Internal Server Error |

### Running Integration Tests in Azure

After deploying to Azure:

```bash
# Get function URL
FUNCTION_URL=$(az functionapp function show \
  --name func-rajfinancial-dev \
  --function-name AssignRoleDuringSignup \
  --resource-group rg-rajfinancial-dev \
  --query "invokeUrlTemplate" -o tsv)

# Get function key
FUNCTION_KEY=$(az functionapp keys list \
  --name func-rajfinancial-dev \
  --resource-group rg-rajfinancial-dev \
  --query "functionKeys.default" -o tsv)

# Test deployed function
curl -X POST "$FUNCTION_URL?code=$FUNCTION_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "displayName": "Test User",
    "givenName": "Test",
    "surname": "User",
    "ui_locales": "Client"
  }'
```

---

## End-to-End (E2E) Tests

E2E tests verify the complete signup flow from the Blazor UI through Entra External ID to role assignment.

### E2E Test Scenarios

#### Scenario 1: Client Signup Flow

**Prerequisites:**
- User flow created in Entra portal: `B2C_1_signup_signin`
- API Connector configured to call `AssignRoleDuringSignup`
- Function App deployed to Azure

**Steps:**

1. **Navigate to signup page**
   - Open: `https://nice-hill-00a8f8f10.5.azurestaticapps.net`
   - Click: "Sign Up as Client" button

2. **Complete signup form**
   - Email: `testclient-001@example.com`
   - Password: Strong password (e.g., `Test@123456`)
   - Display Name: `Test Client 001`
   - Given Name: `Test`
   - Surname: `Client`
   - Click: "Create"

3. **Verify email** (if enabled)
   - Check email inbox
   - Click verification link

4. **Verify automatic role assignment**
   - User should be signed in
   - Check JWT token at https://jwt.ms
   - Should contain: `"roles": ["Client"]`

5. **Verify in Entra portal**
   ```powershell
   $user = Get-MgUser -Filter "mail eq 'testclient-001@example.com'"
   Get-MgUserAppRoleAssignment -UserId $user.Id

   # Should show Client role assigned
   ```

**Expected Results:**
- ✅ User account created in Entra
- ✅ User signed in automatically
- ✅ Client role assigned
- ✅ Access token contains `roles: ["Client"]`
- ✅ User can access Client-only pages

#### Scenario 2: Advisor Signup Flow

Repeat Scenario 1, but:
- Click: "Sign Up as Advisor" button
- Email: `testadvisor-001@example.com`
- Expected role: `Advisor`

#### Scenario 3: Admin Role Prevention

**Steps:**

1. Attempt to signup via direct API call with Administrator role:
   ```bash
   curl -X POST "$API_CONNECTOR_URL?code=$KEY" \
     -H "Content-Type: application/json" \
     -d '{
       "email": "hacker@example.com",
       "ui_locales": "Administrator"
     }'
   ```

2. **Verify response:**
   - Should return: `"extension_RequestedRole": "Client"`
   - Should NOT return: `"extension_RequestedRole": "Administrator"`

3. **Verify in logs:**
   - Check Function App logs
   - Should see: "Invalid role requested during signup: Administrator"

**Expected Results:**
- ✅ Administrator role blocked from self-signup
- ✅ User gets Client role instead
- ✅ Warning logged

#### Scenario 4: Error Handling - API Connector Fails

**Simulate:**
- Stop the Function App
- Attempt signup

**Expected Results:**
- ✅ Signup should complete successfully (not blocked)
- ✅ User created in Entra
- ✅ CompleteProfile page shown after first login
- ✅ User can manually select role on CompleteProfile page
- ✅ CompleteRoleAssignment endpoint assigns role

#### Scenario 5: Role Already Assigned

**Steps:**

1. Signup as Client
2. Call CompleteRoleAssignment with same user/role:
   ```bash
   curl -X POST "$COMPLETE_ROLE_URL?code=$KEY" \
     -H "Content-Type: application/json" \
     -d '{
       "userId": "<USER-OBJECT-ID>",
       "role": "Client"
     }'
   ```

**Expected Results:**
- ✅ Returns: `{ "success": true, "message": "Role already assigned" }`
- ✅ No duplicate role assignment created
- ✅ No errors thrown

---

## Performance Tests

### Load Testing AssignRoleDuringSignup

```bash
# Install hey (HTTP load generator)
# macOS: brew install hey
# Windows: https://github.com/rakyll/hey/releases

# Test with 100 concurrent requests
hey -n 1000 -c 100 -m POST \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","ui_locales":"Client"}' \
  "$API_CONNECTOR_URL?code=$KEY"
```

**Performance Targets:**
- ⏱️ P50 latency: < 200ms
- ⏱️ P95 latency: < 500ms
- ⏱️ P99 latency: < 1000ms
- 📈 Throughput: > 50 req/sec
- ❌ Error rate: < 0.1%

---

## Security Tests

### OWASP Security Test Cases

#### A01: Broken Access Control

**Test: Unauthorized API access**
```bash
# Try to call function without authentication
curl -X POST http://localhost:7071/api/auth/assign-role-signup \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com"}'

# Expected: 401 Unauthorized (when deployed to Azure)
```

**Test: Role escalation attempt**
```bash
# Try to assign Administrator role
curl -X POST "$COMPLETE_ROLE_URL?code=$KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "<USER-ID>",
    "role": "Administrator"
  }'

# Expected: Assignment succeeds (Admin CAN be assigned via API for legitimate admin users)
# But: Cannot self-signup as Admin via API Connector
```

#### A05: Injection

**Test: SQL Injection in email field**
```bash
curl -X POST "$API_CONNECTOR_URL?code=$KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com; DROP TABLE Users--",
    "ui_locales": "Client"
  }'

# Expected: No SQL injection (we use Graph API, not raw SQL)
```

**Test: XSS in displayName**
```bash
curl -X POST "$API_CONNECTOR_URL?code=$KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "displayName": "<script>alert(1)</script>",
    "ui_locales": "Client"
  }'

# Expected: Input sanitized by Entra, no XSS
```

#### A09: Logging Failures

**Test: Verify security events logged**

```bash
# Check logs for:
# - Invalid role attempts
# - Failed authentication
# - Permission errors

az monitor activity-log list \
  --resource-group rg-rajfinancial-dev \
  --max-events 50
```

---

## Test Automation with Playwright

For automated E2E testing, use Playwright:

```typescript
// tests/AcceptanceTests/Specs/Auth/SignupFlow.spec.ts

import { test, expect } from '@playwright/test';

test.describe('Client Signup Flow', () => {
  test('should signup and get Client role', async ({ page }) => {
    // 1. Navigate to signup
    await page.goto('https://nice-hill-00a8f8f10.5.azurestaticapps.net');
    await page.click('text=Sign Up as Client');

    // 2. Fill signup form
    await page.fill('input[type="email"]', 'testclient@example.com');
    await page.fill('input[type="password"]', 'Test@123456');
    await page.fill('input[name="displayName"]', 'Test Client');
    await page.fill('input[name="givenName"]', 'Test');
    await page.fill('input[name="surname"]', 'Client');
    await page.click('button:has-text("Create")');

    // 3. Wait for redirect after signup
    await page.waitForURL('**/authentication/login-callback');

    // 4. Verify signed in
    await expect(page.locator('text=Test Client')).toBeVisible();

    // 5. Extract and verify JWT token
    const token = await page.evaluate(() => {
      return localStorage.getItem('access_token');
    });

    expect(token).toBeTruthy();

    // Decode JWT and check roles claim
    const payload = JSON.parse(atob(token!.split('.')[1]));
    expect(payload.roles).toContain('Client');
  });
});
```

---

## Coverage Report

Generate and view coverage report:

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Install ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:"tests/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html

# Open report
open coverage-report/index.html  # macOS
start coverage-report/index.html # Windows
```

**Coverage Targets:**
- ✅ Line coverage: ≥ 90%
- ✅ Branch coverage: ≥ 80%
- ✅ Method coverage: ≥ 95%

---

## Continuous Integration

Tests run automatically on every PR via GitHub Actions:

```yaml
# .github/workflows/test.yml
name: Tests

on: [pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0'
      - run: dotnet test --collect:"XPlat Code Coverage"
      - run: reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage
      - uses: codecov/codecov-action@v4
        with:
          files: coverage/Cobertura.xml
```

**Quality Gates:**
- ✅ All unit tests must pass
- ✅ Coverage ≥ 90%
- ✅ No new security vulnerabilities (Dependabot)
- ✅ No code quality issues (SonarCloud)

---

## Troubleshooting Tests

### Test Fails: "GraphServiceClient is null"

**Fix**: Ensure mock is properly configured:

```csharp
_graphClientMock = new Mock<GraphServiceClient>();
```

### Test Fails: "ServicePrincipalId not configured"

**Fix**: Setup configuration mock:

```csharp
_configurationMock
    .Setup(c => c["EntraExternalId:ServicePrincipalId"])
    .Returns("12345678-1234-1234-1234-123456789012");
```

### Integration Test Fails: "Insufficient permissions"

**Fix**: Grant Graph API permissions to Managed Identity:

```powershell
# See docs/MFA_CONFIGURATION_WITH_TEST_EXCEPTIONS.md Part 6
Connect-MgGraph -Scopes "AppRoleAssignment.ReadWrite.All"
# ... grant permissions
```

---

## Next Steps

1. ✅ Run unit tests: `dotnet test`
2. ✅ Verify 90%+ coverage: `dotnet test --collect:"XPlat Code Coverage"`
3. 📝 Create user flow in Entra portal (follow CREATE_USER_FLOWS_STEP_BY_STEP.md)
4. 🚀 Deploy Function App to Azure
5. 🔗 Configure API Connector in user flow
6. 🧪 Run E2E tests manually
7. 🤖 Automate E2E tests with Playwright

---

*Last Updated: December 24, 2024*
