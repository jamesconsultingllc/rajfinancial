import {
  InteractionRequiredAuthError,
  type SilentRequest,
} from "@azure/msal-browser";
import { msalInstance } from "@/auth/AuthProvider";
import { apiRequest } from "@/auth/authConfig";
import { ApiError } from "@/types/api";

/**
 * Base URL for the Azure Functions API.
 *
 * @description Defaults to "/api" which is the SWA managed proxy path.
 * Override with VITE_API_BASE_URL for local development against a
 * standalone Functions host (e.g., "http://localhost:7071/api").
 */
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "/api";

/**
 * Acquires a bearer token silently via MSAL.
 * Falls back to interactive redirect if silent acquisition fails
 * (e.g., expired refresh token, consent required).
 */
async function acquireToken(): Promise<string> {
  const account = msalInstance.getActiveAccount();
  if (!account) {
    throw new ApiError("AUTH_NO_ACCOUNT", "No active account. Please sign in.", 401);
  }

  const request: SilentRequest = {
    scopes: apiRequest.scopes,
    account,
  };

  try {
    const response = await msalInstance.acquireTokenSilent(request);
    return response.accessToken;
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      await msalInstance.acquireTokenRedirect(request);
      // Redirect will navigate away — this line is not reached
      throw new ApiError("AUTH_REDIRECT", "Redirecting to sign in.", 401);
    }
    throw error;
  }
}

/**
 * Makes authenticated API calls to the Azure Functions backend.
 *
 * @description Acquires a bearer token via MSAL, injects it into the
 * Authorization header, and handles standard error responses.
 *
 * @param endpoint - API path relative to the base URL (e.g., "/auth/me")
 * @param options - Standard fetch RequestInit options
 * @returns Parsed JSON response typed as T
 * @throws {ApiError} On non-2xx responses or network failures
 *
 * @example
 * ```ts
 * const profile = await apiClient<UserProfileResponse>("/auth/me");
 * ```
 */
export async function apiClient<T>(
  endpoint: string,
  options: RequestInit = {},
): Promise<T> {
  const accessToken = await acquireToken();

  const url = `${API_BASE_URL}${endpoint}`;

  let response: Response;
  try {
    response = await fetch(url, {
      ...options,
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
        ...options.headers,
      },
    });
  } catch {
    throw new ApiError(
      "NETWORK_ERROR",
      "Unable to reach the server. Please check your connection.",
      0,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  if (!response.ok) {
    let code = "UNKNOWN_ERROR";
    let message = `Request failed with status ${response.status}`;

    try {
      const body = await response.json();
      if (body.code) code = body.code;
      if (body.message) message = body.message;
    } catch {
      // Response body is not JSON — use defaults
    }

    throw new ApiError(code, message, response.status);
  }

  return response.json() as Promise<T>;
}
