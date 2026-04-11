import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";

// Mock apiClient before importing the hook
vi.mock("@/services/api-client", () => ({
  apiClient: vi.fn(),
}));

// Mock useAuth to control authentication state
vi.mock("@/auth/useAuth", () => ({
  useAuth: vi.fn(),
}));

import { useAuthProfile, AUTH_PROFILE_QUERY_KEY } from "../use-auth-profile";
import { apiClient } from "@/services/api-client";
import { useAuth } from "@/auth/useAuth";
import type { UserProfileResponse } from "@/generated/memorypack/UserProfileResponse";

const mockApiClient = vi.mocked(apiClient);
const mockUseAuth = vi.mocked(useAuth);

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
    },
  });
}

function createWrapper(queryClient: QueryClient) {
  return function TestWrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  };
}

const mockProfile = {
  userId: "550e8400-e29b-41d4-a716-446655440000",
  displayName: "Jane Advisor",
  locale: "en-US",
  timezone: "America/New_York",
  currency: "USD",
  createdAt: new Date("2026-01-15T00:00:00Z"),
} as UserProfileResponse;

describe("useAuthProfile", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = createTestQueryClient();
    vi.clearAllMocks();
  });

  afterEach(() => {
    queryClient.clear();
  });

  describe("when user is authenticated", () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        isAuthenticated: true,
        isLoading: false,
        user: {
          name: "Jane Advisor",
          email: "advisor@rajfinancial.com",
          initials: "JA",
          roles: ["Advisor"],
        },
        login: vi.fn(),
        logout: vi.fn(),
        hasRole: vi.fn(),
        isAdmin: false,
        isClient: false,
      });
    });

    it("fetches profile from GET /api/auth/me", async () => {
      mockApiClient.mockResolvedValueOnce(mockProfile);

      const { result } = renderHook(() => useAuthProfile(), {
        wrapper: createWrapper(queryClient),
      });

      await waitFor(() => expect(result.current.isLoading).toBe(false));

      expect(mockApiClient).toHaveBeenCalledWith("/auth/me", expect.objectContaining({
        deserialize: expect.any(Function),
      }));
      expect(result.current.profile).toEqual(mockProfile);
    });

    it("returns loading state while fetching", () => {
      mockApiClient.mockImplementation(
        () => new Promise(() => {}) // Never resolves
      );

      const { result } = renderHook(() => useAuthProfile(), {
        wrapper: createWrapper(queryClient),
      });

      expect(result.current.isLoading).toBe(true);
      expect(result.current.profile).toBeUndefined();
    });

    it("returns error state on API failure", async () => {
      const error = new Error("API Error");
      mockApiClient.mockRejectedValueOnce(error);

      const { result } = renderHook(() => useAuthProfile(), {
        wrapper: createWrapper(queryClient),
      });

      await waitFor(() => expect(result.current.isError).toBe(true));

      expect(result.current.error).toBe(error);
      expect(result.current.profile).toBeUndefined();
    });

    it("caches profile data across re-renders", async () => {
      mockApiClient.mockResolvedValue(mockProfile);

      // First render — triggers fetch
      const { result, rerender } = renderHook(() => useAuthProfile(), {
        wrapper: createWrapper(queryClient),
      });

      await waitFor(() => expect(result.current.isLoading).toBe(false));
      expect(result.current.profile).toEqual(mockProfile);

      // Re-render same hook — should use cache
      rerender();

      expect(result.current.profile).toEqual(mockProfile);
      expect(mockApiClient).toHaveBeenCalledTimes(1);
    });

    it("exposes refetch function", async () => {
      mockApiClient.mockResolvedValue(mockProfile);

      const { result } = renderHook(() => useAuthProfile(), {
        wrapper: createWrapper(queryClient),
      });

      await waitFor(() => expect(result.current.isLoading).toBe(false));

      expect(typeof result.current.refetch).toBe("function");
    });
  });

  describe("when user is not authenticated", () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        isAuthenticated: false,
        isLoading: false,
        user: null,
        login: vi.fn(),
        logout: vi.fn(),
        hasRole: vi.fn(),
        isAdmin: false,
        isClient: false,
      });
    });

    it("does not fetch profile", async () => {
      const { result } = renderHook(() => useAuthProfile(), {
        wrapper: createWrapper(queryClient),
      });

      // Wait a bit to ensure no fetch is triggered
      await new Promise((r) => setTimeout(r, 50));

      expect(mockApiClient).not.toHaveBeenCalled();
      expect(result.current.profile).toBeUndefined();
      expect(result.current.isLoading).toBe(false);
    });
  });

  describe("when auth is loading", () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        isAuthenticated: false,
        isLoading: true,
        user: null,
        login: vi.fn(),
        logout: vi.fn(),
        hasRole: vi.fn(),
        isAdmin: false,
        isClient: false,
      });
    });

    it("does not fetch profile while auth is in progress", async () => {
      const { result } = renderHook(() => useAuthProfile(), {
        wrapper: createWrapper(queryClient),
      });

      // Wait a bit to ensure no fetch is triggered
      await new Promise((r) => setTimeout(r, 50));

      expect(mockApiClient).not.toHaveBeenCalled();
      expect(result.current.profile).toBeUndefined();
    });
  });

  describe("query key", () => {
    it("exports query key for cache invalidation", () => {
      expect(AUTH_PROFILE_QUERY_KEY).toEqual(["auth", "profile"]);
    });
  });
});
