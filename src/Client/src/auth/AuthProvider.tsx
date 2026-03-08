import { type ReactNode, useEffect, useState } from "react";
import {
  PublicClientApplication,
  EventType,
  type AuthenticationResult,
} from "@azure/msal-browser";
import { MsalProvider } from "@azure/msal-react";
import { useTranslation } from "react-i18next";
import { msalConfig } from "./authConfig";

/** Session storage key for tracking redirect attempts to prevent infinite loops */
export const REDIRECT_COUNT_KEY = "msal_redirect_count";

/**
 * Singleton MSAL PublicClientApplication instance.
 *
 * @description Initialized once at module scope so that React re-renders
 * do not create duplicate instances. The active account is set from
 * the LOGIN_SUCCESS event or from the first cached account.
 */
export const msalInstance = new PublicClientApplication(msalConfig);

// Listen for login success to set the active account and reset redirect counter
msalInstance.addEventCallback((event) => {
  if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
    const result = event.payload as AuthenticationResult;
    if (result.account) {
      msalInstance.setActiveAccount(result.account);
      sessionStorage.removeItem(REDIRECT_COUNT_KEY);
    }
  }
});

// Listen for token acquisition failures for observability
msalInstance.addEventCallback((event) => {
  if (event.eventType === EventType.ACQUIRE_TOKEN_FAILURE) {
    const error = event.error as { errorCode?: string } | undefined;

    if (error?.errorCode === "invalid_grant") {
      // Expected: refresh token expired
      console.warn(
        "MSAL: Refresh token expired, user will need to re-authenticate"
      );
    } else {
      // Unexpected auth failure
      console.error("MSAL: Token acquisition failed", error);
    }
  }
});

/**
 * AuthProvider wraps the application with MSAL authentication context.
 *
 * @description Provides the MsalProvider from @azure/msal-react with
 * a pre-configured PublicClientApplication instance. Gates rendering
 * behind initialize() and handleRedirectPromise() to prevent redirect
 * loops when processing authentication responses from the URL hash.
 *
 * @param children - React children to wrap with auth context
 *
 * @example
 * ```tsx
 * <AuthProvider>
 *   <App />
 * </AuthProvider>
 * ```
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [msalReady, setMsalReady] = useState(false);
  const { t } = useTranslation();

  useEffect(() => {
    // MSAL v3 requires initialize() before any other API calls
    msalInstance
      .initialize()
      .then(() => {
        // Set active account from cache after initialization
        const accounts = msalInstance.getAllAccounts();
        if (accounts.length > 0) {
          msalInstance.setActiveAccount(accounts[0]);
        }
        return msalInstance.handleRedirectPromise();
      })
      .then((response) => {
        if (response?.account) {
          msalInstance.setActiveAccount(response.account);
          sessionStorage.removeItem(REDIRECT_COUNT_KEY);
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
    return (
      <div className="flex items-center justify-center min-h-screen bg-background">
        <div className="text-center">
          <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="text-muted-foreground text-sm">
            {t("auth.authenticating")}
          </p>
        </div>
      </div>
    );
  }

  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}
