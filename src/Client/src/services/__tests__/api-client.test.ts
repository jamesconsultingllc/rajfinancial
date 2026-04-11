/**
 * Tests for the authenticated API client service.
 *
 * @description Covers MSAL token acquisition, bearer header injection,
 * error handling (401, 403, network), and 204 no-content responses.
 */
import { describe, it, expect, vi, beforeEach, type Mock } from "vitest";
import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { ApiError } from "@/types/api";

/* ------------------------------------------------------------------ */
/*  Mocks                                                              */
/* ------------------------------------------------------------------ */

const mockGetActiveAccount = vi.fn();
const mockAcquireTokenSilent = vi.fn();
const mockAcquireTokenRedirect = vi.fn();

vi.mock("@/auth/AuthProvider", () => ({
  msalInstance: {
    getActiveAccount: (...args: unknown[]) => mockGetActiveAccount(...args),
    acquireTokenSilent: (...args: unknown[]) => mockAcquireTokenSilent(...args),
    acquireTokenRedirect: (...args: unknown[]) => mockAcquireTokenRedirect(...args),
  },
}));

vi.mock("@/auth/authConfig", () => ({
  apiRequest: { scopes: ["api://test-api/access_as_user"] },
}));

const mockFetch = vi.fn() as Mock;
global.fetch = mockFetch;

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

const TEST_TOKEN = "eyJ0ZXN0LXRva2VuIn0";
const TEST_ACCOUNT = { username: "user@test.com", homeAccountId: "123" };

/** Creates a mock Response with headers.get() support. */
function mockResponse(overrides: Record<string, unknown> = {}) {
  return {
    headers: new Headers({ "Content-Type": "application/json" }),
    ...overrides,
  };
}

function setupSuccessfulAuth() {
  mockGetActiveAccount.mockReturnValue(TEST_ACCOUNT);
  mockAcquireTokenSilent.mockResolvedValue({ accessToken: TEST_TOKEN });
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe("apiClient", () => {
  let apiClient: typeof import("@/services/api-client").apiClient;

  beforeEach(async () => {
    vi.clearAllMocks();
    const mod = await import("@/services/api-client");
    apiClient = mod.apiClient;
  });

  describe("token acquisition", () => {
    it("injects bearer token into Authorization header", async () => {
      setupSuccessfulAuth();
      mockFetch.mockResolvedValue(
        mockResponse({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ data: "test" }),
        }),
      );

      await apiClient("/auth/me");

      expect(mockAcquireTokenSilent).toHaveBeenCalledWith({
        scopes: ["api://test-api/access_as_user"],
        account: TEST_ACCOUNT,
      });
      expect(mockFetch).toHaveBeenCalledWith(
        "/api/auth/me",
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: `Bearer ${TEST_TOKEN}`,
            "Content-Type": "application/json",
          }),
        }),
      );
    });

    it("throws ApiError when no active account", async () => {
      mockGetActiveAccount.mockReturnValue(null);

      await expect(apiClient("/auth/me")).rejects.toThrow(ApiError);
      await expect(apiClient("/auth/me")).rejects.toMatchObject({
        code: "AUTH_NO_ACCOUNT",
        status: 401,
      });
    });

    it("redirects to interactive login on InteractionRequiredAuthError", async () => {
      mockGetActiveAccount.mockReturnValue(TEST_ACCOUNT);
      mockAcquireTokenSilent.mockRejectedValue(
        new InteractionRequiredAuthError("interaction_required"),
      );
      mockAcquireTokenRedirect.mockResolvedValue(undefined);

      await expect(apiClient("/auth/me")).rejects.toThrow(ApiError);
      expect(mockAcquireTokenRedirect).toHaveBeenCalledWith({
        scopes: ["api://test-api/access_as_user"],
        account: TEST_ACCOUNT,
      });
    });
  });

  describe("response handling", () => {
    beforeEach(() => setupSuccessfulAuth());

    it("returns parsed JSON for 200 responses", async () => {
      const payload = { userId: "abc", email: "test@test.com" };
      mockFetch.mockResolvedValue(
        mockResponse({
          ok: true,
          status: 200,
          json: () => Promise.resolve(payload),
        }),
      );

      const result = await apiClient("/auth/me");
      expect(result).toEqual(payload);
    });

    it("returns undefined for 204 No Content", async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        status: 204,
      });

      const result = await apiClient("/auth/clients/abc");
      expect(result).toBeUndefined();
    });

    it("passes custom headers and method through", async () => {
      mockFetch.mockResolvedValue(
        mockResponse({
          ok: true,
          status: 200,
          json: () => Promise.resolve({}),
        }),
      );

      await apiClient("/auth/clients", {
        method: "POST",
        body: JSON.stringify({ clientEmail: "c@test.com" }),
      });

      expect(mockFetch).toHaveBeenCalledWith(
        "/api/auth/clients",
        expect.objectContaining({ method: "POST" }),
      );
    });
  });

  describe("error handling", () => {
    beforeEach(() => setupSuccessfulAuth());

    it("throws ApiError with server error code on 400", async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 400,
        json: () =>
          Promise.resolve({
            code: "VALIDATION_ERROR",
            message: "Invalid email",
          }),
      });

      await expect(apiClient("/auth/clients")).rejects.toMatchObject({
        code: "VALIDATION_ERROR",
        message: "Invalid email",
        status: 400,
      });
    });

    it("throws ApiError with server error code on 401", async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 401,
        json: () =>
          Promise.resolve({
            code: "AUTH_REQUIRED",
            message: "Authentication required",
          }),
      });

      await expect(apiClient("/auth/me")).rejects.toMatchObject({
        code: "AUTH_REQUIRED",
        status: 401,
      });
    });

    it("throws ApiError with server error code on 403", async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 403,
        json: () =>
          Promise.resolve({
            code: "AUTH_FORBIDDEN",
            message: "Insufficient permissions",
          }),
      });

      await expect(apiClient("/auth/clients")).rejects.toMatchObject({
        code: "AUTH_FORBIDDEN",
        status: 403,
      });
    });

    it("handles non-JSON error responses gracefully", async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 500,
        json: () => Promise.reject(new Error("not json")),
      });

      await expect(apiClient("/auth/me")).rejects.toMatchObject({
        code: "UNKNOWN_ERROR",
        status: 500,
      });
    });

    it("throws NETWORK_ERROR when fetch fails", async () => {
      mockFetch.mockRejectedValue(new TypeError("Failed to fetch"));

      await expect(apiClient("/auth/me")).rejects.toMatchObject({
        code: "NETWORK_ERROR",
        status: 0,
      });
    });
  });
});
