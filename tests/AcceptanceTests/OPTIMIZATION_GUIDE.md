# Test Optimization Guide

## ✅ Optimization Complete (2024-12-26)

**Result**: Test speed improved by **29%** (from 2.3 min to 1.64 min)

See [SELECTOR_OPTIMIZATION_RESULTS.md](SELECTOR_OPTIMIZATION_RESULTS.md) for full details.

---

## Selector Logging Updates

The test now logs which selector successfully finds each element. This helps identify and remove unnecessary selectors.

### How to Use the Logs

1. Run the test and look for these log messages:
   ```
   ✓ Found password field using selector: input[data-testid='ipasswordInput']
   ✓ Found Given Name using selector: input#igivenNameInput
   ✓ Found Surname using selector: input#isurnameInput
   ```

2. The **first selector that works** is logged for each field

3. Reorder selector arrays to put working selectors **first**

4. Remove selectors that are never used

### Example Optimization

**Before** (tries 6 selectors, takes ~3 seconds):
```csharp
var passwordSelectors = new[]
{
    EntraSelectors.PasswordInput,         // Try this first (fails - 500ms)
    "input#ipasswordInput",               // Try this second (fails - 500ms)
    "input[name='newPassword']",          // Try this third (fails - 500ms)
    "input#newPassword",                  // Try this fourth (fails - 500ms)
    "input[name='password']",             // Try this fifth (fails - 500ms)
    "input[type='password']:first-of-type"  // ✓ SUCCESS (500ms)
};
```
**Total time**: 6 × 500ms = 3 seconds

**After** (based on logs showing `input[data-testid='ipasswordInput']` works):
```csharp
var passwordSelectors = new[]
{
    "input[data-testid='ipasswordInput']",  // ✓ SUCCESS immediately (500ms)
    "input[type='password']:first-of-type"  // Fallback
};
```
**Total time**: 1 × 500ms = 500ms (6x faster!)

---

## Timeout Optimization

### Current Timeouts

| Step | Old Timeout | New Timeout | Savings |
|------|-------------|-------------|---------|
| Field selector | 2000ms | 500ms | 1500ms per selector |
| Verification button | 2000ms | 500ms | 1500ms per selector |

### Why Shorter Timeouts?

Since we try **multiple selectors**, we don't need to wait 2 seconds for each one to fail. A 500ms timeout is enough to determine if an element exists.

**Before**: 10 selectors × 2000ms = 20 seconds worst case
**After**: 10 selectors × 500ms = 5 seconds worst case

---

## Next Steps for Optimization

### 1. Run Test and Collect Logs

```bash
cd tests/AcceptanceTests
dotnet test --filter "FullyQualifiedName~NewUserCanCreateAnAccountThroughEntraExternalID" --logger "console;verbosity=detailed" > test-output.txt
```

### 2. Extract Working Selectors

Search the output for lines like:
```
✓ Found password field using selector: input[data-testid='ipasswordInput']
```

### 3. Update Selector Arrays

For each field, reorder the selector array to put the **working selector first**:

#### Email Input
```csharp
// If logs show: ✓ Found using selector: input#i0116
var emailSelectors = new[]
{
    "input#i0116",  // PUT THIS FIRST
    // Keep 1-2 fallbacks
    "input[type='email']"
};
```

#### Password Input
```csharp
// If logs show: ✓ Found password field using selector: input[data-testid='ipasswordInput']
var passwordSelectors = new[]
{
    "input[data-testid='ipasswordInput']",  // PUT THIS FIRST
    "input[type='password']:first-of-type"  // Fallback
};
```

#### Given Name
```csharp
// If logs show: ✓ Found Given Name using selector: input#igivenNameInput
var givenNameSelectors = new[]
{
    "input#igivenNameInput",  // PUT THIS FIRST
    "input[name='givenName']" // Fallback
};
```

### 4. Estimated Time Savings

If each field has 6 selectors and only the last one works:
- **Current time per field**: 6 selectors × 500ms = 3 seconds
- **Optimized time per field**: 1 selector × 500ms = 500ms
- **Savings per field**: 2.5 seconds

With 8 input fields in the signup flow:
- **Total savings**: 8 fields × 2.5 seconds = **20 seconds**
- **Current test time**: ~2 minutes
- **Optimized test time**: ~1 minute 40 seconds

---

## Monitoring Selector Changes

Entra ID's UI may change over time. Keep these practices:

### 1. Keep Fallback Selectors

Always keep 2-3 fallback selectors in case Entra changes their UI:

```csharp
var selectors = new[]
{
    "input#primarySelector",      // Primary (works now)
    "input[name='fallback1']",    // Fallback #1
    "input[type='password']"      // Generic fallback #2
};
```

### 2. Monitor Test Logs

If a test suddenly gets slower, check logs to see if selectors are failing:

```bash
# Before optimization
✓ Found password field using selector: input[data-testid='ipasswordInput']  # Fast!

# After Entra UI change
✓ Found password field using selector: input[type='password']  # Slower (fallback used)
```

This indicates the primary selector changed and needs updating.

### 3. Periodic Review

Review selector performance monthly:
1. Run test with detailed logging
2. Check if primary selectors still work first
3. Update based on Entra UI changes

---

## Quick Reference: Common Entra Selectors

Based on current testing, these selectors work:

| Field | Working Selector | Fallback |
|-------|------------------|----------|
| Email | `input#i0116` or `input#idTxtBx_SAOTCC_OTC` | `input[type='email']` |
| Verification Code | `input#idTxtBx_OTC_Password` | `input[type='tel']` |
| Password | `input[data-testid='ipasswordInput']` | `input[type='password']` |
| Password Confirm | `input[data-testid='ipasswordConfirmationInput']` | `input#reenterPassword` |
| Given Name | `input#igivenNameInput` | `input[name='givenName']` |
| Surname | `input#isurnameInput` | `input[name='surname']` |
| Display Name | `input#idisplayNameInput` | `input[name='displayName']` |
| Next Button | `button[name='idSIButton9']` or `input#idSIButton9` | `input[type='submit']` |
| Accept Button | `input[type='submit'][value='Accept']` | `button:has-text('Accept')` |

**Note**: Update this table after running tests to reflect actual working selectors.

---

## Summary

1. ✅ **Logging added** - Shows which selector succeeds for each field
2. ✅ **Timeouts reduced** - From 2000ms to 500ms per selector attempt
3. ⏭️ **Next step** - Run test, review logs, reorder selectors
4. ⏭️ **Goal** - Reduce test time from ~2 minutes to ~1 minute 40 seconds

After collecting logs from the next test run, create a PR with optimized selector arrays!
