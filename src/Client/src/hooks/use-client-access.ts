import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/services/api-client";
import { useAuth } from "@/auth/useAuth";
import { ClientAssignmentResponse } from "@/generated/memorypack/ClientAssignmentResponse";
import { AssignClientRequest } from "@/generated/memorypack/AssignClientRequest";

/**
 * Query key for the client list query.
 * Export for cache invalidation.
 */
export const CLIENT_LIST_QUERY_KEY = ["auth", "clients"] as const;

/**
 * Fetches the list of assigned clients from GET /api/auth/clients.
 */
async function fetchClientList(): Promise<ClientAssignmentResponse[]> {
  return apiClient<ClientAssignmentResponse[]>("/auth/clients", {
    deserialize: (buf) => ClientAssignmentResponse.deserializeArray(buf),
  });
}

/**
 * Assigns a new client via POST /api/auth/clients.
 */
async function assignClient(
  request: AssignClientRequest
): Promise<ClientAssignmentResponse> {
  return apiClient<ClientAssignmentResponse>("/auth/clients", {
    method: "POST",
    body: AssignClientRequest.serialize(request),
    deserialize: (buf) => ClientAssignmentResponse.deserialize(buf),
  });
}

/**
 * Removes a client assignment via DELETE /api/auth/clients/{grantId}.
 */
async function removeClient(grantId: string): Promise<void> {
  return apiClient<void>(`/auth/clients/${grantId}`, {
    method: "DELETE",
  });
}

/**
 * Return type for the useClientList hook.
 */
export interface UseClientListResult {
  /** List of assigned clients, or undefined if not loaded */
  clients: ClientAssignmentResponse[] | undefined;
  /** Whether the list is currently being fetched */
  isLoading: boolean;
  /** Whether the fetch failed */
  isError: boolean;
  /** The error if the fetch failed */
  error: Error | null;
  /** Function to manually refetch the list */
  refetch: () => void;
}

/**
 * Custom hook to fetch the list of clients assigned to the current user.
 *
 * @description Uses TanStack Query to fetch GET /api/auth/clients.
 * Only enabled for users with Advisor or Administrator roles.
 * Clients cannot view this data.
 *
 * @returns Client list, loading/error states, and refetch function
 *
 * @example
 * ```tsx
 * const { clients, isLoading, isError } = useClientList();
 *
 * if (isLoading) return <Spinner />;
 * if (isError) return <ErrorMessage />;
 *
 * return clients?.map(c => <ClientCard key={c.grantId} client={c} />);
 * ```
 */
export function useClientList(): UseClientListResult {
  const { isAuthenticated, hasRole, isAdmin } = useAuth();

  // Only Advisors and Administrators can view client lists
  const canViewClients =
    isAuthenticated && (hasRole("Advisor") || isAdmin);

  const {
    data: clients,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: CLIENT_LIST_QUERY_KEY,
    queryFn: fetchClientList,
    enabled: canViewClients,
  });

  return {
    clients,
    isLoading: isLoading && canViewClients,
    isError,
    error: error as Error | null,
    refetch,
  };
}

/**
 * Custom hook to assign a new client.
 *
 * @description Uses TanStack Query mutation to POST /api/auth/clients.
 * Automatically invalidates the client list query on success.
 *
 * @returns Mutation object with mutate/mutateAsync functions
 *
 * @example
 * ```tsx
 * const { mutateAsync: assignClient, isPending } = useAssignClient();
 *
 * const handleAssign = async () => {
 *   await assignClient({
 *     clientEmail: "client@example.com",
 *     accessType: "Full",
 *     categories: null,
 *     relationshipLabel: "Spouse",
 *   });
 * };
 * ```
 */
export function useAssignClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: assignClient,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CLIENT_LIST_QUERY_KEY });
    },
  });
}

/**
 * Custom hook to remove a client assignment.
 *
 * @description Uses TanStack Query mutation to DELETE /api/auth/clients/{grantId}.
 * Automatically invalidates the client list query on success.
 *
 * @returns Mutation object with mutate/mutateAsync functions
 *
 * @example
 * ```tsx
 * const { mutateAsync: removeClient, isPending } = useRemoveClient();
 *
 * const handleRemove = async (grantId: string) => {
 *   await removeClient(grantId);
 * };
 * ```
 */
export function useRemoveClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: removeClient,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CLIENT_LIST_QUERY_KEY });
    },
  });
}
