import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { InteractionStatus } from "@azure/msal-browser";

// Hoisted mock functions
const mockLoginRedirect = vi.hoisted(() => vi.fn());
const mockHandleRedirectPromise = vi.hoisted(() => vi.fn());
const mockSetActiveAccount = vi.hoisted(() => vi.fn());

vi.mock("@azure/msal-react", () => ({
  MsalProvider: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="msal-provider">{children}</div>
  ),
  useMsal: () => ({
    instance: {
      loginRedirect: mockLoginRedirect,
    },
    inProgress: InteractionStatus.None,
  }),
  AuthenticatedTemplate: () => null,
  UnauthenticatedTemplate: ({ children }: { children: React.ReactNode }) => (
    <>{children}</>
  ),
}));

vi.mock("../AuthProvider", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../AuthProvider")>();
  return {
    ...actual,
    msalInstance: {
      handleRedirectPromise: mockHandleRedirectPromise,
      setActiveAccount: mockSetActiveAccount,
      getAllAccounts: () => [],
      addEventCallback: vi.fn(),
    },
  };
});

vi.mock("../useAuth", () => ({
  useAuth: () => ({
    isAuthenticated: false,
    user: null,
    login: vi.fn(),
    logout: vi.fn(),
    hasRole: () => false,
    isAdmin: false,
    isClient: false,
  }),
}));

vi.mock("react-i18next", () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        "auth.authenticating": "Authenticating...",
        "auth.redirecting": "Redirecting to sign in...",
        "auth.sessionExpired": "Session Expired",
        "auth.sessionExpiredDescription":
          "Your session has expired. Please sign in again.",
        "auth.signIn": "Sign In",
      };
      return translations[key] ?? key;
    },
  }),
}));

// Constant for redirect count key - must match AuthProvider
const REDIRECT_COUNT_KEY = "msal_redirect_count";

describe("MSAL Redirect Loop Prevention", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  describe("Redirect counter logic", () => {
    it("calls loginRedirect when redirect count is 0", async () => {
      // Dynamically import to get fresh module with mocks
      const { ProtectedRoute } = await import("../ProtectedRoute");

      render(
        <ProtectedRoute>
          <div>Protected Content</div>
        </ProtectedRoute>
      );

      await waitFor(() => {
        expect(mockLoginRedirect).toHaveBeenCalled();
      });

      expect(sessionStorage.getItem(REDIRECT_COUNT_KEY)).toBe("1");
    });

    it("increments redirect count on each attempt", async () => {
      sessionStorage.setItem(REDIRECT_COUNT_KEY, "1");

      const { ProtectedRoute } = await import("../ProtectedRoute");

      render(
        <ProtectedRoute>
          <div>Protected Content</div>
        </ProtectedRoute>
      );

      await waitFor(() => {
        expect(mockLoginRedirect).toHaveBeenCalled();
      });

      expect(sessionStorage.getItem(REDIRECT_COUNT_KEY)).toBe("2");
    });

    it("shows Session Expired card when redirect count exceeds MAX_REDIRECTS", async () => {
      sessionStorage.setItem(REDIRECT_COUNT_KEY, "3");

      const { ProtectedRoute } = await import("../ProtectedRoute");

      render(
        <ProtectedRoute>
          <div>Protected Content</div>
        </ProtectedRoute>
      );

      expect(screen.getByText("Session Expired")).toBeInTheDocument();
      expect(
        screen.getByText("Your session has expired. Please sign in again.")
      ).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Sign In" })).toBeInTheDocument();
      expect(mockLoginRedirect).not.toHaveBeenCalled();
    });

    it("resets counter and calls loginRedirect when Sign In button is clicked", async () => {
      sessionStorage.setItem(REDIRECT_COUNT_KEY, "3");

      const { ProtectedRoute } = await import("../ProtectedRoute");

      render(
        <ProtectedRoute>
          <div>Protected Content</div>
        </ProtectedRoute>
      );

      const signInButton = screen.getByRole("button", { name: "Sign In" });
      fireEvent.click(signInButton);

      expect(sessionStorage.getItem(REDIRECT_COUNT_KEY)).toBe("0");
      expect(mockLoginRedirect).toHaveBeenCalled();
    });
  });

  describe("handleRedirectPromise gate", () => {
    it("shows loading spinner while promise is pending", async () => {
      // Create a promise that never resolves to test loading state
      mockHandleRedirectPromise.mockReturnValue(new Promise(() => {}));

      const { AuthProvider } = await import("../AuthProvider");

      render(
        <AuthProvider>
          <div data-testid="children">App Content</div>
        </AuthProvider>
      );

      expect(screen.getByText("Authenticating...")).toBeInTheDocument();
      expect(screen.queryByTestId("children")).not.toBeInTheDocument();
    });

    it("renders children after promise resolves with null", async () => {
      mockHandleRedirectPromise.mockResolvedValue(null);

      const { AuthProvider } = await import("../AuthProvider");

      render(
        <AuthProvider>
          <div data-testid="children">App Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId("msal-provider")).toBeInTheDocument();
      });
    });

    it("renders children after promise resolves with account", async () => {
      mockHandleRedirectPromise.mockResolvedValue({
        account: { username: "test@example.com" },
      });

      const { AuthProvider } = await import("../AuthProvider");

      render(
        <AuthProvider>
          <div data-testid="children">App Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId("msal-provider")).toBeInTheDocument();
      });
    });

    it("renders children after promise rejects (graceful degradation)", async () => {
      mockHandleRedirectPromise.mockRejectedValue(new Error("Auth error"));

      const { AuthProvider } = await import("../AuthProvider");

      render(
        <AuthProvider>
          <div data-testid="children">App Content</div>
        </AuthProvider>
      );

      await waitFor(() => {
        expect(screen.getByTestId("msal-provider")).toBeInTheDocument();
      });
    });
  });
});
