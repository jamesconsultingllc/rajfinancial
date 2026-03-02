import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import Index from "../Index";
import {
  setMockAuthState,
  resetMockAuthState,
  createMockUseAuth,
  createTestWrapper,
} from "@/test/msal-mocks";

// Mock the useAuth hook
const mockUseAuth = createMockUseAuth();
vi.mock("@/auth/useAuth", () => ({
  useAuth: () => mockUseAuth(),
}));

// Track navigation
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe("Index page", () => {
  beforeEach(() => {
    resetMockAuthState();
    mockNavigate.mockClear();
  });

  it("renders landing page for unauthenticated users", () => {
    setMockAuthState({ isAuthenticated: false });

    render(<Index />, { wrapper: createTestWrapper() });

    // Landing page content should be visible (from HeroSection)
    expect(
      screen.getByText(/take control of your/i)
    ).toBeInTheDocument();
  });

  it("shows loading spinner while auth is being checked", () => {
    setMockAuthState({ isAuthenticated: false, inProgress: "login" });

    render(<Index />, { wrapper: createTestWrapper() });

    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it("redirects Administrator to /admin/dashboard", () => {
    setMockAuthState({ isAuthenticated: true, roles: ["Administrator"] });

    render(<Index />, { wrapper: createTestWrapper() });

    expect(mockNavigate).toHaveBeenCalledWith("/admin/dashboard", {
      replace: true,
    });
  });

  it("redirects AdminAdvisor to /admin/dashboard", () => {
    setMockAuthState({ isAuthenticated: true, roles: ["AdminAdvisor"] });

    render(<Index />, { wrapper: createTestWrapper() });

    expect(mockNavigate).toHaveBeenCalledWith("/admin/dashboard", {
      replace: true,
    });
  });

  it("redirects Advisor to /advisor/clients", () => {
    setMockAuthState({ isAuthenticated: true, roles: ["Advisor"] });

    render(<Index />, { wrapper: createTestWrapper() });

    expect(mockNavigate).toHaveBeenCalledWith("/advisor/clients", {
      replace: true,
    });
  });

  it("redirects Client role to /dashboard", () => {
    setMockAuthState({ isAuthenticated: true, roles: ["Client"] });

    render(<Index />, { wrapper: createTestWrapper() });

    expect(mockNavigate).toHaveBeenCalledWith("/dashboard", {
      replace: true,
    });
  });

  it("redirects user with no role (implicit client) to /dashboard", () => {
    setMockAuthState({ isAuthenticated: true, roles: [] });

    render(<Index />, { wrapper: createTestWrapper() });

    expect(mockNavigate).toHaveBeenCalledWith("/dashboard", {
      replace: true,
    });
  });
});
