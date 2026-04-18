import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { I18nextProvider } from "react-i18next";
import type { ReactElement } from "react";
import type { AccountInfo, AuthenticationResult } from "@azure/msal-browser";
import i18n from "@/lib/i18n";
import { ApiError } from "@/types/api";

// Mock MSAL - use factory pattern to avoid hoisting issues
vi.mock("@/auth/AuthProvider", () => ({
  msalInstance: {
    loginRedirect: vi.fn(),
    logoutRedirect: vi.fn(),
    acquireTokenSilent: vi.fn(),
    acquireTokenRedirect: vi.fn(),
    getActiveAccount: vi.fn(),
  },
}));

vi.mock("@/auth/authConfig", () => ({
  apiRequest: { scopes: ["api://test/.default"] },
  loginRequest: { scopes: ["openid", "profile"] },
}));

import { ApiErrorBoundary } from "../ApiErrorBoundary";
import { msalInstance } from "@/auth/AuthProvider";

// Helper to render with i18n provider (boundary sub-components use useTranslation)
function renderWithI18n(ui: ReactElement) {
  return render(<I18nextProvider i18n={i18n}>{ui}</I18nextProvider>);
}

// Get typed mock references after import
const mockLoginRedirect = vi.mocked(msalInstance.loginRedirect);
const mockLogoutRedirect = vi.mocked(msalInstance.logoutRedirect);
const mockAcquireTokenSilent = vi.mocked(msalInstance.acquireTokenSilent);
const mockAcquireTokenRedirect = vi.mocked(msalInstance.acquireTokenRedirect);
const mockGetActiveAccount = vi.mocked(msalInstance.getActiveAccount);

// Test component that throws errors
function ThrowError({ error }: { error: Error }): ReactElement {
  throw error;
}

// Suppress console.error for cleaner test output
const originalConsoleError = console.error;
beforeEach(() => {
  console.error = vi.fn();
  vi.clearAllMocks();
});
afterEach(() => {
  console.error = originalConsoleError;
});

describe("ApiErrorBoundary", () => {
  describe("when no error occurs", () => {
    it("renders children normally", () => {
      renderWithI18n(
        <ApiErrorBoundary>
          <div data-testid="child">Hello World</div>
        </ApiErrorBoundary>
      );

      expect(screen.getByTestId("child")).toHaveTextContent("Hello World");
    });
  });

  describe("when 401 Unauthorized error occurs", () => {
    it("shows authenticating fallback", () => {
      const error = new ApiError("UNAUTHORIZED", "Unauthorized", 401);

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      expect(screen.getByText(/refreshing authentication/i)).toBeInTheDocument();
    });

    it("attempts silent token refresh when account exists", async () => {
      mockGetActiveAccount.mockReturnValue({ username: "test@example.com" } as AccountInfo);
      mockAcquireTokenSilent.mockResolvedValue({ accessToken: "new-token" } as AuthenticationResult);

      const error = new ApiError("UNAUTHORIZED", "Unauthorized", 401);

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      // Wait for async handleUnauthorized
      await vi.waitFor(() => {
        expect(mockAcquireTokenSilent).toHaveBeenCalledWith({
          scopes: ["api://test/.default"],
          account: { username: "test@example.com" },
        });
      });
    });

    it("redirects to login when no account exists", async () => {
      mockGetActiveAccount.mockReturnValue(null);

      const error = new ApiError("UNAUTHORIZED", "Unauthorized", 401);

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      await vi.waitFor(() => {
        expect(mockLoginRedirect).toHaveBeenCalled();
      });
    });

    it("falls back to interactive login when silent refresh fails", async () => {
      const { InteractionRequiredAuthError } = await import("@azure/msal-browser");
      
      mockGetActiveAccount.mockReturnValue({ username: "test@example.com" } as AccountInfo);
      mockAcquireTokenSilent.mockRejectedValue(
        new InteractionRequiredAuthError("interaction_required")
      );

      const error = new ApiError("UNAUTHORIZED", "Unauthorized", 401);

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      await vi.waitFor(() => {
        expect(mockAcquireTokenRedirect).toHaveBeenCalled();
      });
    });
  });

  describe("when 403 Forbidden error occurs", () => {
    it("shows Access Denied UI", () => {
      const error = new ApiError("FORBIDDEN", "Forbidden", 403);

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      expect(screen.getByText("Access Denied")).toBeInTheDocument();
      expect(screen.getByText(/don't have permission/i)).toBeInTheDocument();
    });

    it("has Go Back button that navigates back", () => {
      const mockHistoryBack = vi.spyOn(window.history, "back").mockImplementation(() => {});
      const error = new ApiError("FORBIDDEN", "Forbidden", 403);

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      fireEvent.click(screen.getByRole("button", { name: /go back/i }));

      expect(mockHistoryBack).toHaveBeenCalled();
      mockHistoryBack.mockRestore();
    });

    it("has Sign Out button that triggers logout", async () => {
      const error = new ApiError("FORBIDDEN", "Forbidden", 403);

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      fireEvent.click(screen.getByRole("button", { name: /sign out/i }));

      await vi.waitFor(() => {
        expect(mockLogoutRedirect).toHaveBeenCalled();
      });
    });

    it("does not redirect in a loop", () => {
      const error = new ApiError("FORBIDDEN", "Forbidden", 403);

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      // Should show UI, not redirect
      expect(screen.getByText("Access Denied")).toBeInTheDocument();
      expect(mockLoginRedirect).not.toHaveBeenCalled();
    });
  });

  describe("when network error occurs (API unreachable)", () => {
    it("shows offline/connection error UI", () => {
      const error = new ApiError(
        "NETWORK_ERROR",
        "Unable to reach the server.",
        0
      );

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      expect(screen.getByText("Connection Problem")).toBeInTheDocument();
      // Exact-string match targets the raw `error.message` paragraph only:
      // the translated description is "Unable to reach the server. Please
      // check your internet connection." — a strict superset — so this
      // assertion fails if the raw message stops rendering.
      expect(
        screen.getByText("Unable to reach the server.")
      ).toBeInTheDocument();
    });

    it("has Try Again button", () => {
      const error = new ApiError(
        "NETWORK_ERROR",
        "Unable to reach the server.",
        0
      );

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      expect(screen.getByText("Connection Problem")).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /try again/i })).toBeInTheDocument();
    });
  });

  describe("when generic API error occurs", () => {
    it("shows generic error UI with error details", () => {
      const error = new ApiError(
        "VALIDATION_ERROR",
        "Invalid request data",
        400
      );

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      expect(screen.getByText("Something Went Wrong")).toBeInTheDocument();
      expect(screen.getByText("Invalid request data")).toBeInTheDocument();
      expect(screen.getByText(/error 400/i)).toBeInTheDocument();
    });

    it("shows 500 server errors appropriately", () => {
      const error = new ApiError(
        "INTERNAL_ERROR",
        "Internal server error",
        500
      );

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      expect(screen.getByText("Something Went Wrong")).toBeInTheDocument();
      expect(screen.getByText("Internal server error")).toBeInTheDocument();
    });
  });

  describe("when non-ApiError is thrown", () => {
    it("shows generic error UI", () => {
      const error = new Error("Something unexpected happened");

      renderWithI18n(
        <ApiErrorBoundary>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      expect(screen.getByText("Something Went Wrong")).toBeInTheDocument();
      expect(screen.getByText("Something unexpected happened")).toBeInTheDocument();
    });
  });

  describe("custom fallback", () => {
    it("renders custom fallback when provided", () => {
      const error = new ApiError("ERROR", "Error", 500);

      renderWithI18n(
        <ApiErrorBoundary fallback={<div data-testid="custom-fallback">Custom Error</div>}>
          <ThrowError error={error} />
        </ApiErrorBoundary>
      );

      expect(screen.getByTestId("custom-fallback")).toHaveTextContent("Custom Error");
    });
  });
});
