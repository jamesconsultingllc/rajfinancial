import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/services/api-client";
import { UpdateProfileRequest } from "@/generated/memorypack/UpdateProfileRequest";
import { UserProfileResponse } from "@/generated/memorypack/UserProfileResponse";
import { AUTH_PROFILE_QUERY_KEY } from "@/hooks/use-auth-profile";

/**
 * Input for the profile update mutation.
 */
export interface UpdateProfileInput {
  displayName: string;
  locale: string;
  timezone: string;
  currency: string;
}

/**
 * Sends a PUT /api/profile/me request with MemoryPack binary serialization.
 */
async function updateProfile(input: UpdateProfileInput): Promise<UserProfileResponse> {
  const request = new UpdateProfileRequest();
  request.displayName = input.displayName;
  request.locale = input.locale;
  request.timezone = input.timezone;
  request.currency = input.currency;

  return apiClient<UserProfileResponse>("/profile/me", {
    method: "PUT",
    body: UpdateProfileRequest.serialize(request),
    deserialize: (buf) => UserProfileResponse.deserialize(buf),
  });
}

/**
 * Custom hook to update the authenticated user's profile via PUT /api/profile/me.
 *
 * @description Uses TanStack Query mutation with MemoryPack binary serialization.
 * On success, invalidates the auth profile cache so the UI reflects the update.
 *
 * @returns Mutation trigger, loading/error/success states
 *
 * @example
 * ```tsx
 * const { mutate, isPending } = useUpdateProfile();
 *
 * const handleSave = () => {
 *   mutate({ displayName: "Jane", locale: "en-US", timezone: "America/New_York", currency: "USD" });
 * };
 * ```
 */
export function useUpdateProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: updateProfile,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: AUTH_PROFILE_QUERY_KEY });
    },
  });
}
