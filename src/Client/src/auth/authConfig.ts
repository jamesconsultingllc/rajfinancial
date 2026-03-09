import { type Configuration, LogLevel } from "@azure/msal-browser";

/**
 * MSAL configuration for Entra External ID authentication.
 *
 * @description Uses environment variables for authority and client ID,
 * allowing different Entra tenants for dev/prod environments.
 * Cache location defaults to sessionStorage for production security,
 * but can be overridden to localStorage for E2E testing.
 */
export const msalConfig: Configuration = {
  auth: {
    authority:
      import.meta.env.VITE_AZURE_AD_AUTHORITY ??
      "https://rajfinancialdev.ciamlogin.com/rajfinancialdev.onmicrosoft.com",
    clientId:
      import.meta.env.VITE_AZURE_AD_CLIENT_ID ??
      "2d6a08c7-b142-4d53-a307-9ac75bae75eb",
    knownAuthorities: [
      "rajfinancialdev.ciamlogin.com",
      "rajfinancialprod.ciamlogin.com",
    ],
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation:
      (import.meta.env.VITE_MSAL_CACHE_LOCATION as
        | "sessionStorage"
        | "localStorage") ?? "sessionStorage",
  },
  system: {
    loggerOptions: {
      logLevel: import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error,
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            break;
          case LogLevel.Warning:
            console.warn(message);
            break;
          case LogLevel.Info:
            console.info(message);
            break;
          case LogLevel.Verbose:
            console.debug(message);
            break;
        }
      },
    },
  },
};

/**
 * Scopes requested during login.
 *
 * @description openid, profile, email are standard OIDC scopes.
 * offline_access enables refresh token flow.
 */
export const loginRequest = {
  scopes: ["openid", "profile", "email", "offline_access"],
};

/**
 * Scopes requested when calling the Azure Functions API.
 *
 * @description The API scope is defined on the API app registration in Entra.
 * Uses VITE_AZURE_AD_API_SCOPE env var, falling back to the dev registration.
 */
export const apiRequest = {
  scopes: [
    import.meta.env.VITE_AZURE_AD_API_SCOPE ??
      "api://2d6a08c7-b142-4d53-a307-9ac75bae75eb/access_as_user",
  ],
};
