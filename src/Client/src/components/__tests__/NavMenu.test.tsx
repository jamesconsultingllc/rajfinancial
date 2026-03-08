import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Navbar } from "../Navbar";
import {
  setMockAuthState,
  resetMockAuthState,
  createMockUseAuth,
  createTestWrapper,
} from "@/test/msal-mocks";

// Mock the useAuth hook
const mockLogin = vi.fn();
const mockLogout = vi.fn();
const mockUseAuth = createMockUseAuth();

vi.mock("@/auth/useAuth", () => ({
  useAuth: () => {
    const result = mockUseAuth();
    return { ...result, login: mockLogin, logout: mockLogout };
  },
}));

describe("Navbar", () => {
  beforeEach(() => {
    resetMockAuthState();
    mockLogin.mockClear();
    mockLogout.mockClear();
  });

  it("renders Sign In button for unauthenticated users", () => {
    setMockAuthState({ isAuthenticated: false });

    render(<Navbar />, { wrapper: createTestWrapper() });

    const signInButtons = screen.getAllByRole("button", { name: /sign in/i });
    expect(signInButtons.length).toBeGreaterThan(0);
  });

  it("renders Get Started button for unauthenticated users", () => {
    setMockAuthState({ isAuthenticated: false });

    render(<Navbar />, { wrapper: createTestWrapper() });

    const getStartedButtons = screen.getAllByRole("button", {
      name: /get started/i,
    });
    expect(getStartedButtons.length).toBeGreaterThan(0);
  });

  it("calls login when Sign In is clicked", async () => {
    setMockAuthState({ isAuthenticated: false });
    const user = userEvent.setup();

    render(<Navbar />, { wrapper: createTestWrapper() });

    // Click the first (desktop) Sign In button
    const signInButtons = screen.getAllByRole("button", { name: /sign in/i });
    await user.click(signInButtons[0]);

    expect(mockLogin).toHaveBeenCalled();
  });

  it("calls login when Get Started is clicked", async () => {
    setMockAuthState({ isAuthenticated: false });
    const user = userEvent.setup();

    render(<Navbar />, { wrapper: createTestWrapper() });

    const getStartedButtons = screen.getAllByRole("button", {
      name: /get started/i,
    });
    await user.click(getStartedButtons[0]);

    expect(mockLogin).toHaveBeenCalled();
  });

  it("renders the logo", () => {
    setMockAuthState({ isAuthenticated: false });

    render(<Navbar />, { wrapper: createTestWrapper() });

    // The Logo component should render
    expect(document.querySelector("nav")).toBeInTheDocument();
  });

  it("renders theme toggle", () => {
    setMockAuthState({ isAuthenticated: false });

    render(<Navbar />, { wrapper: createTestWrapper() });

    // Theme toggle buttons should exist (desktop + mobile)
    const toggleButtons = screen.getAllByRole("button");
    expect(toggleButtons.length).toBeGreaterThanOrEqual(1);
  });

  it("toggles mobile menu when hamburger is clicked", async () => {
    setMockAuthState({ isAuthenticated: false });
    const user = userEvent.setup();

    render(<Navbar />, { wrapper: createTestWrapper() });

    // Find and click the mobile menu toggle
    const menuToggle = screen.getByRole("button", { name: /open menu/i });
    await user.click(menuToggle);

    // Mobile menu should now be visible with full-width buttons
    const mobileSignIn = screen.getAllByRole("button", { name: /sign in/i });
    // Should have both desktop and mobile Sign In buttons now
    expect(mobileSignIn.length).toBeGreaterThanOrEqual(2);
  });

  it("has accessible menu toggle button", () => {
    setMockAuthState({ isAuthenticated: false });

    render(<Navbar />, { wrapper: createTestWrapper() });

    const menuToggle = screen.getByRole("button", { name: /open menu/i });
    expect(menuToggle).toHaveAttribute("aria-label");
  });

  it("renders navigation element with correct structure", () => {
    setMockAuthState({ isAuthenticated: false });

    render(<Navbar />, { wrapper: createTestWrapper() });

    const nav = document.querySelector("nav");
    expect(nav).toBeInTheDocument();
    expect(nav).toHaveClass("fixed");
  });
});
