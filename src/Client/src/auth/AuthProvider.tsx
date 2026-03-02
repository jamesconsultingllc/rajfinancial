import { type ReactNode } from "react";
import {
  PublicClientApplication,
  EventType,
  type AuthenticationResult,
} from "@azure/msal-browser";
import { MsalProvider } from "@azure/msal-react";
import { msalConfig } from "./authConfig";

/**
 * Singleton MSAL PublicClientApplication instance.
 *
 * @description Initialized once at module scope so that React re-renders
 * do not create duplicate instances. The active account is set from
 * the LOGIN_SUCCESS event or from the first cached account.
 */
export const msalInstance = new PublicClientApplication(msalConfig);

// Set the first cached account as active (if any)
const cachedAccounts = msalInstance.getAllAccounts();
if (cachedAccounts.length > 0) {
  msalInstance.setActiveAccount(cachedAccounts[0]);
}

// Listen for login success to set the active account
msalInstance.addEventCallback((event) => {
  if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
    const result = event.payload as AuthenticationResult;
    if (result.account) {
      msalInstance.setActiveAccount(result.account);
    }
  }
});

/**
 * AuthProvider wraps the application with MSAL authentication context.
 *
 * @description Provides the MsalProvider from @azure/msal-react with
 * a pre-configured PublicClientApplication instance. All child components
 * can then use useAuth() or the raw MSAL hooks.
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
  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}
