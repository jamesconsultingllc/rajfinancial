import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { InteractionStatus, type AccountInfo } from "@azure/msal-browser";
import { useCallback, useMemo } from "react";
import { loginRequest } from "./authConfig";

/**
 * Represents the authenticated user with role information.
 */
export interface AuthUser {
  /** Display name from the ID token */
  name: string;
  /** Email address from the ID token */
  email: string;
  /** User's initials derived from display name */
  initials: string;
  /** Parsed roles from the ID token claims */
  roles: string[];
}

/**
 * Return type for the useAuth hook.
 */
export interface UseAuthResult {
  /** Whether the user is currently authenticated */
  isAuthenticated: boolean;
  /** Whether MSAL is processing an authentication interaction */
  isLoading: boolean;
  /** The authenticated user's info, or null if unauthenticated */
  user: AuthUser | null;
  /** Initiates MSAL redirect login flow */
  login: () => Promise<void>;
  /** Initiates MSAL redirect logout flow */
  logout: () => Promise<void>;
  /** Checks whether the user has a specific role */
  hasRole: (role: string) => boolean;
  /** Whether the user has an Administrator-level role */
  isAdmin: boolean;
  /** Whether the user has a Client role (or is implicitly a client) */
  isClient: boolean;
}

/**
 * Extracts initials from a display name.
 *
 * @param name - The user's display name
 * @returns Up to 2 uppercase initials
 */
function getInitials(name: string): string {
  return name
    .split(" ")
    .filter(Boolean)
    .map((part) => part[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);
}

/**
 * Parses roles from an MSAL account's ID token claims.
 *
 * @description Handles both string and array formats for the 'roles' claim,
 * matching the logic from the Blazor CustomAccountFactory.
 *
 * @param account - The MSAL account info
 * @returns Array of role strings
 */
export function parseRoles(account: AccountInfo | null): string[] {
  if (!account?.idTokenClaims) return [];

  const rolesClaim = (
    account.idTokenClaims as Record<string, unknown>
  ).roles;

  if (!rolesClaim) return [];

  if (Array.isArray(rolesClaim)) {
    return rolesClaim.filter(
      (r): r is string => typeof r === "string" && r.trim().length > 0
    );
  }

  if (typeof rolesClaim === "string" && rolesClaim.trim().length > 0) {
    return [rolesClaim.trim()];
  }

  return [];
}

/** Roles that grant administrator-level access */
const ADMIN_ROLES = ["Administrator", "AdminAdvisor", "AdminClient"];

/**
 * Custom hook for MSAL authentication with role-based access control.
 *
 * @description Wraps @azure/msal-react hooks and provides a simplified API
 * with role parsing that matches the Blazor CustomAccountFactory behavior.
 * Users with no explicit role are treated as implicit Clients.
 *
 * @returns Authentication state, user info, and auth actions
 *
 * @example
 * ```tsx
 * const { isAuthenticated, user, login, logout, isAdmin } = useAuth();
 * ```
 */
export function useAuth(): UseAuthResult {
  const { instance, inProgress, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const account = accounts[0] ?? null;
  const isLoading = inProgress !== InteractionStatus.None;

  const roles = useMemo(() => parseRoles(account), [account]);

  const user: AuthUser | null = useMemo(() => {
    if (!account) return null;

    const name = account.name ?? account.username ?? "User";
    const email =
      (account.idTokenClaims as Record<string, unknown>)
        ?.email as string ??
      account.username ??
      "";

    return {
      name,
      email,
      initials: getInitials(name),
      roles,
    };
  }, [account, roles]);

  const login = useCallback(async () => {
    await instance.loginRedirect(loginRequest);
  }, [instance]);

  const logout = useCallback(async () => {
    await instance.logoutRedirect({
      postLogoutRedirectUri: window.location.origin,
    });
  }, [instance]);

  const hasRole = useCallback(
    (role: string) => roles.includes(role),
    [roles]
  );

  const isAdmin = useMemo(
    () => ADMIN_ROLES.some((r) => roles.includes(r)),
    [roles]
  );

  // No explicit role = implicit Client (matches Blazor policy)
  const isClient = useMemo(
    () => roles.includes("Client") || roles.length === 0,
    [roles]
  );

  return {
    isAuthenticated,
    isLoading,
    user,
    login,
    logout,
    hasRole,
    isAdmin,
    isClient,
  };
}
