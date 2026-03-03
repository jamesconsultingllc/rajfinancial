# Design: MSAL Redirect Loop Fix

**ADO Task:** #540 — Add MSAL handleRedirectPromise gate and event callback for token acquisition failures
**Parent Feature:** #267 — Entra User Flows Configuration
**Priority:** P1
**Date:** 2026-03-02

---

## Problem

When a user returns to the app after their MSAL refresh token has expired (typically 24h for SPAs), the following sequence occurs:

1. App loads a protected route (e.g., `/dashboard`)
2. MSAL finds a cached account in localStorage with an expired access token
3. MSAL sends a `refresh_token` grant to the CIAM token endpoint
4. CIAM returns **400 `invalid_grant`** (refresh token expired/revoked)
5. MSAL clears the stale tokens from the cache
6. `UnauthenticatedTemplate` renders → `RedirectToLogin` fires `loginRedirect()`
7. Entra issues new tokens → redirects back to the app
8. **`handleRedirectPromise()` is never awaited** → MSAL hasn't processed the auth response from the URL hash yet
9. `UnauthenticatedTemplate` renders again → `loginRedirect()` fires again → **redirect loop**

The user cannot break out of the loop without manually clearing browser storage (localStorage/cookies).

## Root Cause

`AuthProvider.tsx` does not call `msalInstance.handleRedirectPromise()` before rendering children. The `ProtectedRoute` component renders `RedirectToLogin`, which immediately fires `instance.loginRedirect()` before MSAL has finished processing the authentication response from the URL hash. This creates a race condition that results in an infinite redirect loop.

## Solution: Approach B — Gate + Loop Breaker + Event Callback

Three changes working together:

### 1. `handleRedirectPromise` Gate (AuthProvider.tsx)

**What:** Gate the entire app behind `handleRedirectPromise()` resolution. Nothing renders until MSAL has finished processing any pending redirect response.

**How:**

- Add a `msalReady` state initialized to `false`
- In a `useEffect`, call `await msalInstance.handleRedirectPromise()`
- On success (non-null result): set the active account, reset the redirect counter in `sessionStorage`, set `msalReady = true`
- On success (null result — no pending redirect): set `msalReady = true`
- On error: log the error, set `msalReady = true` (let the app render so the user can manually sign in)
- While `msalReady` is `false`: render a full-screen loading spinner with localized "Authenticating..." text
- When `msalReady` is `true`: render `<MsalProvider>` with children

**Important:** The existing `LOGIN_SUCCESS` event callback and `setActiveAccount` logic from the module-scope initialization should be preserved. The `handleRedirectPromise` gate is additive.

```tsx
// Pseudocode — not final implementation
export function AuthProvider({ children }: { children: ReactNode }) {
  const [msalReady, setMsalReady] = useState(false);

  useEffect(() => {
    msalInstance
      .handleRedirectPromise()
      .then((response) => {
        if (response?.account) {
          msalInstance.setActiveAccount(response.account);
          sessionStorage.removeItem("msal_redirect_count");
        }
      })
      .catch((error) => {
        console.error("MSAL redirect error:", error);
      })
      .finally(() => {
        setMsalReady(true);
      });
  }, []);

  if (!msalReady) {
    return <LoadingSpinner label={t("auth.authenticating")} />;
  }

  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}
```

### 2. Redirect Loop Breaker (ProtectedRoute.tsx)

**What:** Prevent infinite redirect loops by tracking redirect attempts. After 2 failed attempts, stop auto-redirecting and show a manual "Sign In" button.

**How:**

Modify the `RedirectToLogin` component:

- Before calling `loginRedirect()`, read `msal_redirect_count` from `sessionStorage`
- If count > 2: render a "Session expired" card with a manual Sign In button
  - The Sign In button resets the counter to 0 and then calls `loginRedirect()`
- If count <= 2: increment the counter and call `loginRedirect()`

Counter reset points:
- `handleRedirectPromise` resolves with a non-null result (in AuthProvider)
- `LOGIN_SUCCESS` event fires (in AuthProvider's existing event callback)
- User clicks the manual "Sign In" button (in RedirectToLogin)

```tsx
// Pseudocode — not final implementation
const REDIRECT_COUNT_KEY = "msal_redirect_count";
const MAX_REDIRECTS = 2;

function RedirectToLogin() {
  const { instance, inProgress } = useMsal();
  const { t } = useTranslation();

  const redirectCount = parseInt(
    sessionStorage.getItem(REDIRECT_COUNT_KEY) ?? "0",
    10
  );

  if (redirectCount > MAX_REDIRECTS) {
    return (
      <SessionExpiredCard
        onSignIn={() => {
          sessionStorage.setItem(REDIRECT_COUNT_KEY, "0");
          instance.loginRedirect(loginRequest);
        }}
      />
    );
  }

  if (inProgress === InteractionStatus.None) {
    sessionStorage.setItem(
      REDIRECT_COUNT_KEY,
      String(redirectCount + 1)
    );
    instance.loginRedirect(loginRequest);
  }

  return <LoadingSpinner label={t("auth.redirecting")} />;
}
```

The "Session expired" card should include:
- A shield or lock icon
- Heading: "Session Expired" (localized)
- Description: "Your session has expired. Please sign in again." (localized)
- A primary "Sign In" button
- Styling consistent with the existing `AccessDenied` component

### 3. MSAL Event Callback (AuthProvider.tsx)

**What:** Add an event callback for `ACQUIRE_TOKEN_FAILURE` to provide observability and user feedback.

**How:**

Add a second `addEventCallback` (alongside the existing `LOGIN_SUCCESS` one) at module scope:

```tsx
msalInstance.addEventCallback((event) => {
  if (event.eventType === EventType.ACQUIRE_TOKEN_FAILURE) {
    const error = event.error as { errorCode?: string } | undefined;

    if (error?.errorCode === "invalid_grant") {
      // Expected: refresh token expired
      console.warn("MSAL: Refresh token expired, user will need to re-authenticate");
    } else {
      // Unexpected auth failure
      console.error("MSAL: Token acquisition failed", error);
    }
  }
});
```

**Note:** We intentionally do NOT show a toast from the module-scope callback because the redirect gate and loop breaker handle the UX. The event callback is purely for observability (console logging). A toast could be added later if telemetry (e.g., App Insights) is wired up.

### 4. Counter Reset in LOGIN_SUCCESS

Update the existing `LOGIN_SUCCESS` event callback to also reset the redirect counter:

```tsx
msalInstance.addEventCallback((event) => {
  if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
    const result = event.payload as AuthenticationResult;
    if (result.account) {
      msalInstance.setActiveAccount(result.account);
      sessionStorage.removeItem("msal_redirect_count");  // ← ADD THIS
    }
  }
});
```

## i18n Strings

Add the following keys. If `en/common.json` already exists and has an `auth` section, add there. Otherwise, add an `auth` section to `en/common.json` or create it if needed. Register the namespace in `src/Client/src/lib/i18n.ts` if not already registered.

```json
{
  "auth": {
    "authenticating": "Authenticating...",
    "redirecting": "Redirecting to sign in...",
    "sessionExpired": "Session Expired",
    "sessionExpiredDescription": "Your session has expired. Please sign in again.",
    "signIn": "Sign In"
  }
}
```

## Files to Modify

| File | Change |
|------|--------|
| `src/Client/src/auth/AuthProvider.tsx` | Add `handleRedirectPromise()` gate with loading state, add `ACQUIRE_TOKEN_FAILURE` event callback, add counter reset in `LOGIN_SUCCESS` callback |
| `src/Client/src/auth/ProtectedRoute.tsx` | Add redirect loop breaker in `RedirectToLogin`, add `SessionExpiredCard` component |
| `src/Client/src/locales/en/common.json` | Add `auth.*` i18n keys (create file if needed) |
| `src/Client/src/lib/i18n.ts` | Register `common` namespace if not already registered |

## Files NOT Modified

| File | Reason |
|------|--------|
| `src/Client/src/auth/authConfig.ts` | No changes needed — config is correct |
| `src/Client/src/auth/useAuth.ts` | No changes needed — hook API stays the same |
| `src/Client/src/test/msal-mocks.tsx` | May need updates if tests exercise the new gate, but no functional changes |

## Testing

### Unit Tests (add to `authorization.test.ts` or create new file)

1. **Redirect counter logic:**
   - When `msal_redirect_count` is 0, `RedirectToLogin` should call `loginRedirect`
   - When `msal_redirect_count` is 3, `RedirectToLogin` should render the Session Expired card
   - Clicking "Sign In" resets counter and calls `loginRedirect`

2. **handleRedirectPromise gate:**
   - While promise is pending, loading spinner is shown
   - After promise resolves, children are rendered
   - After promise rejects, children are still rendered (graceful degradation)

### Manual Testing

1. Sign in, navigate to `/dashboard`
2. Open DevTools → Application → localStorage
3. Find the MSAL refresh token entry and corrupt its value (change a few characters)
4. Refresh the page
5. **Expected:** App shows loading spinner → detects invalid token → shows "Session Expired" card after 2 redirect attempts → user clicks "Sign In" → successfully authenticates

## What We Are NOT Doing

- **No proactive token refresh** — MSAL handles silent renewal automatically once `handleRedirectPromise` is properly gated
- **No `tokenRenewalOffsetSeconds` tuning** — Default MSAL behavior is sufficient
- **No `acquireTokenSilent` interceptor** — Not needed for this fix
- **No telemetry integration** — Console logging only for now; App Insights can be wired up later
- **No changes to cache location** — `sessionStorage` default remains correct
