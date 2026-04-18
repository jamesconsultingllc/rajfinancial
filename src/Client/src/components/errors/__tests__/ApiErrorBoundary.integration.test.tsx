/**
 * Integration tests for ApiErrorBoundary + TanStack Query.
 *
 * @description Verifies that API errors thrown from `useQuery` propagate up
 * to the nearest `ApiErrorBoundary` via `throwOnError`, which is the end-to-end
 * contract for the global error-handling UX.
 */
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { I18nextProvider } from "react-i18next";
import type { ReactElement } from "react";
import { QueryClient, QueryClientProvider, useQuery } from "@tanstack/react-query";
import i18n from "@/lib/i18n";
import { ApiError } from "@/types/api";

// Mock MSAL so the 401 branch doesn't try to redirect during tests
vi.mock("@/auth/AuthProvider", () => ({
  msalInstance: {
    loginRedirect: vi.fn(),
    logoutRedirect: vi.fn(),
    acquireTokenSilent: vi.fn(),
    acquireTokenRedirect: vi.fn(),
    getActiveAccount: vi.fn(() => null),
  },
}));

vi.mock("@/auth/authConfig", () => ({
  apiRequest: { scopes: ["api://test/.default"] },
  loginRequest: { scopes: ["openid", "profile"] },
}));

import { ApiErrorBoundary } from "../ApiErrorBoundary";

/**
 * Predicate identical to production App.tsx — 401/403/0 bubble to boundary.
 */
function shouldThrowApiError(error: unknown): boolean {
  return (
    error instanceof ApiError &&
    (error.status === 401 || error.status === 403 || error.status === 0)
  );
}

function makeClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        throwOnError: shouldThrowApiError,
      },
    },
  });
}

function ProbeComponent({ error }: { error: Error }) {
  useQuery({
    queryKey: ["probe", error.message],
    queryFn: () => {
      throw error;
    },
  });
  return <div data-testid="probe-rendered">probe</div>;
}

function renderWithProviders(ui: ReactElement) {
  const client = makeClient();
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>{ui}</QueryClientProvider>
    </I18nextProvider>,
  );
}

const originalConsoleError = console.error;
beforeEach(() => {
  console.error = vi.fn();
  vi.clearAllMocks();
});
afterEach(() => {
  console.error = originalConsoleError;
});

describe("ApiErrorBoundary + QueryClient integration", () => {
  it("propagates ApiError(403) from useQuery to the boundary's Access Denied UI", async () => {
    const error = new ApiError("FORBIDDEN", "Forbidden", 403);

    renderWithProviders(
      <ApiErrorBoundary>
        <ProbeComponent error={error} />
      </ApiErrorBoundary>,
    );

    expect(await screen.findByText(i18n.t("auth.accessDenied"))).toBeInTheDocument();
    expect(screen.queryByTestId("probe-rendered")).not.toBeInTheDocument();
  });

  it("propagates ApiError(status=0) from useQuery to the Connection Problem UI", async () => {
    const error = new ApiError("NETWORK_ERROR", "Unable to reach server.", 0);

    renderWithProviders(
      <ApiErrorBoundary>
        <ProbeComponent error={error} />
      </ApiErrorBoundary>,
    );

    expect(
      await screen.findByText(i18n.t("errors.connectionProblem")),
    ).toBeInTheDocument();
  });

  it("does NOT propagate non-matching errors (e.g., ApiError(400) validation)", async () => {
    const error = new ApiError("VALIDATION_ERROR", "Invalid input", 400);

    renderWithProviders(
      <ApiErrorBoundary>
        <ProbeComponent error={error} />
      </ApiErrorBoundary>,
    );

    // Query errors but boundary should NOT render; the probe renders normally
    // because throwOnError returns false for status 400.
    expect(await screen.findByTestId("probe-rendered")).toBeInTheDocument();
    expect(screen.queryByText(i18n.t("auth.accessDenied"))).not.toBeInTheDocument();
    expect(
      screen.queryByText(i18n.t("errors.somethingWentWrong")),
    ).not.toBeInTheDocument();
  });
});



