import { type ReactNode } from "react";
import { vi } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { HelmetProvider } from "react-helmet-async";
import { ThemeProvider } from "@/hooks/use-theme";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

/**
 * Mock MSAL state for testing.
 */
export interface MockMsalState {
  isAuthenticated: boolean;
  roles?: string[];
  name?: string;
  email?: string;
  inProgress?: string;
}

const defaultState: MockMsalState = {
  isAuthenticated: false,
  roles: [],
  name: "Test User",
  email: "test@example.com",
  inProgress: "none",
};

let currentMockState: MockMsalState = { ...defaultState };

/**
 * Sets up mock MSAL state for tests.
 * Call before rendering components that use useAuth().
 */
export function setMockAuthState(state: Partial<MockMsalState>) {
  currentMockState = { ...defaultState, ...state };
}

/**
 * Resets mock MSAL state to defaults.
 */
export function resetMockAuthState() {
  currentMockState = { ...defaultState };
}

/**
 * Mock useAuth hook factory.
 * Returns a function that matches the useAuth signature.
 */
export function createMockUseAuth() {
  return () => {
    const roles = currentMockState.roles ?? [];
    const adminRoles = ["Administrator", "AdminAdvisor", "AdminClient"];

    return {
      isAuthenticated: currentMockState.isAuthenticated,
      isLoading: currentMockState.inProgress !== "none",
      user: currentMockState.isAuthenticated
        ? {
            name: currentMockState.name ?? "Test User",
            email: currentMockState.email ?? "test@example.com",
            initials: (currentMockState.name ?? "TU")
              .split(" ")
              .map((p) => p[0])
              .join("")
              .toUpperCase()
              .slice(0, 2),
            roles,
          }
        : null,
      login: vi.fn(),
      logout: vi.fn(),
      hasRole: (role: string) => roles.includes(role),
      isAdmin: adminRoles.some((r) => roles.includes(r)),
      isClient: roles.includes("Client") || roles.length === 0,
    };
  };
}

/**
 * Test wrapper that provides all required React context providers.
 *
 * @param initialRoute - The initial route for MemoryRouter
 */
export function createTestWrapper(initialRoute = "/") {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return function TestWrapper({ children }: { children: ReactNode }) {
    return (
      <HelmetProvider>
        <ThemeProvider>
          <QueryClientProvider client={queryClient}>
            <MemoryRouter initialEntries={[initialRoute]}>
              {children}
            </MemoryRouter>
          </QueryClientProvider>
        </ThemeProvider>
      </HelmetProvider>
    );
  };
}
