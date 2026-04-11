import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/services/api-client";
import { useAuth } from "@/auth/useAuth";
import { UserProfileResponse } from "@/generated/memorypack/UserProfileResponse";

/**
 * Query key for the auth profile query.
 * Export for cache invalidation on logout.
 */
export const AUTH_PROFILE_QUERY_KEY = ["auth", "profile"] as const;

/**
 * Fetches the authenticated user's profile from GET /api/auth/me.
 */
async function fetchAuthProfile(): Promise<UserProfileResponse> {
  return apiClient<UserProfileResponse>("/auth/me", {
    deserialize: (buf) => UserProfileResponse.deserialize(buf),
  });
}

/**
 * Return type for the useAuthProfile hook.
 */
export interface UseAuthProfileResult {
  /** The user's profile from the API, or undefined if not loaded */
  profile: UserProfileResponse | undefined;
  /** Whether the profile is currently being fetched */
  isLoading: boolean;
  /** Whether the fetch failed */
  isError: boolean;
  /** The error if the fetch failed */
  error: Error | null;
  /** Function to manually refetch the profile */
  refetch: () => void;
}

/**
 * Custom hook to fetch the authenticated user's profile from the API.
 *
 * @description Uses TanStack Query to fetch GET /api/auth/me when authenticated.
 * Caches the result with a 5-minute stale time and auto-refetches on window focus.
 * Supplements the existing useAuth hook with richer profile data from the API.
 *
 * @returns Profile data, loading/error states, and refetch function
 *
 * @example
 * ```tsx
 * const { profile, isLoading, isError } = useAuthProfile();
 *
 * if (isLoading) return <Spinner />;
 * if (isError) return <ErrorMessage />;
 *
 * return <div>Welcome, {profile?.displayName}</div>;
 * ```
 */
export function useAuthProfile(): UseAuthProfileResult {
  const { isAuthenticated, isLoading: isAuthLoading } = useAuth();

  const {
    data: profile,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: AUTH_PROFILE_QUERY_KEY,
    queryFn: fetchAuthProfile,
    // Only enable when auth is fully settled
    enabled: isAuthenticated && !isAuthLoading,
  });

  return {
    profile,
    isLoading: isLoading && isAuthenticated,
    isError,
    error: error as Error | null,
    refetch,
  };
}
