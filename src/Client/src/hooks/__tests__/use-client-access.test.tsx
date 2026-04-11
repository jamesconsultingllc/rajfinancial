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

import {
  useClientList,
  useAssignClient,
  useRemoveClient,
  CLIENT_LIST_QUERY_KEY,
} from "../use-client-access";
import { apiClient } from "@/services/api-client";
import { useAuth } from "@/auth/useAuth";
import type { ClientAssignmentResponse } from "@/generated/memorypack/ClientAssignmentResponse";

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

const mockClientList: ClientAssignmentResponse[] = [
  {
    grantId: "grant-1",
    clientEmail: "client1@example.com",
    accessType: "Full",
    categories: ["Assets", "Accounts"],
    relationshipLabel: "Spouse",
    status: "Active",
    createdAt: new Date("2026-01-01"),
  } as ClientAssignmentResponse,
  {
    grantId: "grant-2",
    clientEmail: "client2@example.com",
    accessType: "ReadOnly",
    categories: null,
    relationshipLabel: null,
    status: "Pending",
    createdAt: new Date("2026-02-01"),
  } as ClientAssignmentResponse,
];

function createMockUseAuth(overrides: Partial<ReturnType<typeof useAuth>> = {}) {
  return {
    isAuthenticated: true,
    isLoading: false,
    user: {
      name: "Test Advisor",
      email: "advisor@example.com",
      initials: "TA",
      roles: ["Advisor"],
    },
    login: vi.fn(),
    logout: vi.fn(),
    hasRole: vi.fn((role: string) => overrides.user?.roles?.includes(role) ?? false),
    isAdmin: false,
    isClient: false,
    ...overrides,
  };
}

describe("useClientList", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = createTestQueryClient();
    vi.clearAllMocks();
  });

  afterEach(() => {
    queryClient.clear();
  });

  describe("when user is an Advisor", () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(
        createMockUseAuth({
          user: {
            name: "Test Advisor",
            email: "advisor@example.com",
            initials: "TA",
            roles: ["Advisor"],
          },
          hasRole: (role: string) => role === "Advisor",
        })
      );
    });

    it("fetches client list from GET /api/auth/clients", async () => {
      mockApiClient.mockResolvedValueOnce(mockClientList);

      const { result } = renderHook(() => useClientList(), {
        wrapper: createWrapper(queryClient),
      });

      await waitFor(() => expect(result.current.isLoading).toBe(false));

      expect(mockApiClient).toHaveBeenCalledWith("/auth/clients", expect.objectContaining({
        deserialize: expect.any(Function),
      }));
      expect(result.current.clients).toEqual(mockClientList);
    });

    it("returns loading state while fetching", () => {
      mockApiClient.mockImplementation(() => new Promise(() => {}));

      const { result } = renderHook(() => useClientList(), {
        wrapper: createWrapper(queryClient),
      });

      expect(result.current.isLoading).toBe(true);
      expect(result.current.clients).toBeUndefined();
    });

    it("returns error state on API failure", async () => {
      const error = new Error("API Error");
      mockApiClient.mockRejectedValueOnce(error);

      const { result } = renderHook(() => useClientList(), {
        wrapper: createWrapper(queryClient),
      });

      await waitFor(() => expect(result.current.isError).toBe(true));

      expect(result.current.error).toBe(error);
    });
  });

  describe("when user is an Administrator", () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(
        createMockUseAuth({
          user: {
            name: "Test Admin",
            email: "admin@example.com",
            initials: "TA",
            roles: ["Administrator"],
          },
          hasRole: (role: string) => role === "Administrator",
          isAdmin: true,
        })
      );
    });

    it("fetches client list", async () => {
      mockApiClient.mockResolvedValueOnce(mockClientList);

      const { result } = renderHook(() => useClientList(), {
        wrapper: createWrapper(queryClient),
      });

      await waitFor(() => expect(result.current.isLoading).toBe(false));

      expect(mockApiClient).toHaveBeenCalledWith("/auth/clients", expect.objectContaining({
        deserialize: expect.any(Function),
      }));
    });
  });

  describe("when user is a Client (no access)", () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(
        createMockUseAuth({
          user: {
            name: "Test Client",
            email: "client@example.com",
            initials: "TC",
            roles: ["Client"],
          },
          hasRole: (role: string) => role === "Client",
          isClient: true,
        })
      );
    });

    it("does not fetch client list", async () => {
      const { result } = renderHook(() => useClientList(), {
        wrapper: createWrapper(queryClient),
      });

      // Wait a bit to ensure no fetch is triggered
      await new Promise((r) => setTimeout(r, 50));

      expect(mockApiClient).not.toHaveBeenCalled();
      expect(result.current.clients).toBeUndefined();
    });
  });

  describe("when user is not authenticated", () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue(
        createMockUseAuth({
          isAuthenticated: false,
          user: null,
        })
      );
    });

    it("does not fetch client list", async () => {
      const { result } = renderHook(() => useClientList(), {
        wrapper: createWrapper(queryClient),
      });

      await new Promise((r) => setTimeout(r, 50));

      expect(mockApiClient).not.toHaveBeenCalled();
      expect(result.current.clients).toBeUndefined();
    });
  });
});

describe("useAssignClient", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = createTestQueryClient();
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue(
      createMockUseAuth({
        hasRole: (role: string) => role === "Advisor",
      })
    );
  });

  afterEach(() => {
    queryClient.clear();
  });

  it("calls POST /api/auth/clients with request body", async () => {
    const newClient: ClientAssignmentResponse = {
      grantId: "new-grant",
      clientEmail: "new@example.com",
      accessType: "Full",
      categories: null,
      relationshipLabel: null,
      status: "Active",
      createdAt: new Date(),
    } as ClientAssignmentResponse;

    mockApiClient.mockResolvedValueOnce(newClient);

    const { result } = renderHook(() => useAssignClient(), {
      wrapper: createWrapper(queryClient),
    });

    await result.current.mutateAsync({
      clientEmail: "new@example.com",
      accessType: "Full",
      categories: null,
      relationshipLabel: null,
    });

    expect(mockApiClient).toHaveBeenCalledWith("/auth/clients", expect.objectContaining({
      method: "POST",
      body: expect.any(Uint8Array),
      deserialize: expect.any(Function),
    }));
  });

  it("invalidates client list query on success", async () => {
    mockApiClient.mockResolvedValueOnce({} as ClientAssignmentResponse);

    const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");

    const { result } = renderHook(() => useAssignClient(), {
      wrapper: createWrapper(queryClient),
    });

    await result.current.mutateAsync({
      clientEmail: "new@example.com",
      accessType: "Full",
      categories: null,
      relationshipLabel: null,
    });

    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: CLIENT_LIST_QUERY_KEY,
    });
  });
});

describe("useRemoveClient", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = createTestQueryClient();
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue(
      createMockUseAuth({
        hasRole: (role: string) => role === "Advisor",
      })
    );
  });

  afterEach(() => {
    queryClient.clear();
  });

  it("calls DELETE /api/auth/clients/{clientId}", async () => {
    mockApiClient.mockResolvedValueOnce(undefined);

    const { result } = renderHook(() => useRemoveClient(), {
      wrapper: createWrapper(queryClient),
    });

    await result.current.mutateAsync("grant-123");

    expect(mockApiClient).toHaveBeenCalledWith("/auth/clients/grant-123", {
      method: "DELETE",
    });
  });

  it("invalidates client list query on success", async () => {
    mockApiClient.mockResolvedValueOnce(undefined);

    const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");

    const { result } = renderHook(() => useRemoveClient(), {
      wrapper: createWrapper(queryClient),
    });

    await result.current.mutateAsync("grant-123");

    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: CLIENT_LIST_QUERY_KEY,
    });
  });
});

describe("query key", () => {
  it("exports query key for cache invalidation", () => {
    expect(CLIENT_LIST_QUERY_KEY).toEqual(["auth", "clients"]);
  });
});
