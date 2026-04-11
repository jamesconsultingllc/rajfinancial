import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, waitFor, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";

vi.mock("@/services/api-client", () => ({
  apiClient: vi.fn(),
}));

vi.mock("@/generated/memorypack/UpdateProfileRequest", () => {
  class MockUpdateProfileRequest {
    displayName = "";
    locale = "";
    timezone = "";
    currency = "";
    static serialize = vi.fn(() => new Uint8Array([1, 2, 3]));
  }
  return { UpdateProfileRequest: MockUpdateProfileRequest };
});

vi.mock("@/generated/memorypack/UserProfileResponse", () => ({
  UserProfileResponse: {
    deserialize: vi.fn(),
  },
}));

import { useUpdateProfile } from "../use-update-profile";
import { apiClient } from "@/services/api-client";
import { AUTH_PROFILE_QUERY_KEY } from "../use-auth-profile";

const mockApiClient = vi.mocked(apiClient);

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
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

const mockInput = {
  displayName: "Jane Updated",
  locale: "es-MX",
  timezone: "America/Chicago",
  currency: "EUR",
};

const mockResponse = {
  userId: "550e8400-e29b-41d4-a716-446655440000",
  displayName: "Jane Updated",
  locale: "es-MX",
  timezone: "America/Chicago",
  currency: "EUR",
  createdAt: new Date(),
};

describe("useUpdateProfile", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = createTestQueryClient();
    vi.clearAllMocks();
  });

  afterEach(() => {
    queryClient.clear();
  });

  it("calls apiClient with PUT /profile/me and MemoryPack body", async () => {
    mockApiClient.mockResolvedValueOnce(mockResponse);

    const { result } = renderHook(() => useUpdateProfile(), {
      wrapper: createWrapper(queryClient),
    });

    await act(async () => {
      result.current.mutate(mockInput);
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockApiClient).toHaveBeenCalledWith(
      "/profile/me",
      expect.objectContaining({
        method: "PUT",
        body: expect.any(Uint8Array),
        deserialize: expect.any(Function),
      }),
    );
  });

  it("sets isPending while mutation is in flight", async () => {
    let resolvePromise: (value: unknown) => void;
    mockApiClient.mockReturnValueOnce(
      new Promise((resolve) => {
        resolvePromise = resolve;
      }),
    );

    const { result } = renderHook(() => useUpdateProfile(), {
      wrapper: createWrapper(queryClient),
    });

    act(() => {
      result.current.mutate(mockInput);
    });

    await waitFor(() => expect(result.current.isPending).toBe(true));

    await act(async () => {
      resolvePromise!(mockResponse);
    });

    await waitFor(() => expect(result.current.isPending).toBe(false));
  });

  it("sets isError on failure", async () => {
    mockApiClient.mockRejectedValueOnce(new Error("Network error"));

    const { result } = renderHook(() => useUpdateProfile(), {
      wrapper: createWrapper(queryClient),
    });

    await act(async () => {
      result.current.mutate(mockInput);
    });

    await waitFor(() => expect(result.current.isError).toBe(true));
    expect(result.current.error?.message).toBe("Network error");
  });

  it("invalidates auth profile cache on success", async () => {
    mockApiClient.mockResolvedValueOnce(mockResponse);
    const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");

    const { result } = renderHook(() => useUpdateProfile(), {
      wrapper: createWrapper(queryClient),
    });

    await act(async () => {
      result.current.mutate(mockInput);
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: AUTH_PROFILE_QUERY_KEY,
    });
  });
});
