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

const MEMORYPACK_CONTENT_TYPE = "application/x-memorypack";

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
 * Options for apiClient beyond standard RequestInit.
 */
export interface ApiClientOptions<T> extends Omit<RequestInit, "body"> {
  /** MemoryPack-serialized binary body (for POST/PUT). When provided, Content-Type is set to application/x-memorypack. */
  body?: Uint8Array | string;
  /** MemoryPack deserializer for the response. When provided, responses are deserialized from binary. Falls back to JSON when server responds with JSON (dev mode). */
  deserialize?: (buffer: ArrayBuffer) => T | null;
}

/**
 * Makes authenticated API calls to the Azure Functions backend using MemoryPack.
 *
 * @description Always requests MemoryPack via Accept header. The server's
 * ContentNegotiationMiddleware responds with MemoryPack in production and
 * JSON in development. When a `deserialize` function is provided, binary
 * responses are deserialized with it; otherwise falls back to JSON.parse.
 *
 * For request bodies, pass a `Uint8Array` (MemoryPack-serialized via
 * generated TypeScript classes) and Content-Type is set automatically.
 * String bodies are sent as JSON for backward compatibility.
 *
 * @param endpoint - API path relative to the base URL (e.g., "/auth/me")
 * @param options - Request options including optional MemoryPack deserializer
 * @returns Deserialized response typed as T
 * @throws {ApiError} On non-2xx responses or network failures
 *
 * @example
 * ```ts
 * // GET with MemoryPack deserialization
 * const profile = await apiClient("/auth/me", {
 *   deserialize: (buf) => UserProfileResponse.deserialize(buf),
 * });
 *
 * // PUT with MemoryPack request + response
 * const updated = await apiClient("/profile/me", {
 *   method: "PUT",
 *   body: UpdateProfileRequest.serialize(request),
 *   deserialize: (buf) => UserProfileResponse.deserialize(buf),
 * });
 *
 * // Legacy JSON body (still supported)
 * const result = await apiClient("/auth/clients", {
 *   method: "POST",
 *   body: JSON.stringify(request),
 * });
 * ```
 */
export async function apiClient<T>(
  endpoint: string,
  options: ApiClientOptions<T> = {},
): Promise<T> {
  const accessToken = await acquireToken();
  const url = `${API_BASE_URL}${endpoint}`;

  const { body, deserialize, ...fetchOptions } = options;

  // Determine content type and fetch body based on body type
  const isBinaryBody = body instanceof Uint8Array;
  const contentType = isBinaryBody ? MEMORYPACK_CONTENT_TYPE : "application/json";
  const fetchBody = isBinaryBody
    ? (body.buffer.slice(body.byteOffset, body.byteOffset + body.byteLength) as ArrayBuffer)
    : body;

  // Only set Content-Type when a request body is present (avoids unnecessary CORS preflights on GET)
  const headers: Record<string, string> = {
    Accept: MEMORYPACK_CONTENT_TYPE,
    Authorization: `Bearer ${accessToken}`,
  };
  if (body != null) {
    headers["Content-Type"] = contentType;
  }

  let response: Response;
  try {
    response = await fetch(url, {
      ...fetchOptions,
      body: fetchBody,
      headers: {
        ...headers,
        ...fetchOptions.headers,
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
      const errorBody = await response.json();
      if (errorBody.code) code = errorBody.code;
      if (errorBody.message) message = errorBody.message;
    } catch {
      // Response body is not JSON — use defaults
    }

    throw new ApiError(code, message, response.status);
  }

  // Deserialize response based on Content-Type and available deserializer
  const responseContentType = response.headers.get("Content-Type") ?? "";

  if (deserialize && responseContentType.includes(MEMORYPACK_CONTENT_TYPE)) {
    const buffer = await response.arrayBuffer();
    const result = deserialize(buffer);
    if (result === null) {
      throw new ApiError("DESERIALIZE_ERROR", "Failed to deserialize MemoryPack response", 0);
    }
    return result;
  }

  // Fallback: JSON response (development mode or no deserializer provided)
  return response.json() as Promise<T>;
}
