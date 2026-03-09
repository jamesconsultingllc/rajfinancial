/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_AZURE_AD_AUTHORITY?: string;
  readonly VITE_AZURE_AD_CLIENT_ID?: string;
  readonly VITE_AZURE_AD_API_SCOPE?: string;
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_MSAL_CACHE_LOCATION?: "sessionStorage" | "localStorage";
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
