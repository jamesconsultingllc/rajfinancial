import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { HelmetProvider } from "react-helmet-async";
import { ThemeProvider } from "@/hooks/use-theme";
import { TooltipProvider } from "@/components/ui/tooltip";

// Mock the auth hook
vi.mock("@/auth/useAuth", () => ({
  useAuth: () => ({
    isAuthenticated: true,
    user: {
      name: "Test User",
      email: "test@example.com",
      initials: "TU",
      roles: ["Client"],
    },
    login: vi.fn(),
    logout: vi.fn(),
    hasRole: () => false,
    isAdmin: false,
    isClient: true,
  }),
}));

// Mock the asset service
vi.mock("@/services/asset-service", () => ({
  useAssets: () => ({
    data: [
      {
        id: "1",
        name: "123 Main Street",
        type: "RealEstate",
        currentValue: 500000,
        purchasePrice: 400000,
        hasBeneficiaries: true,
      },
      {
        id: "2",
        name: "Tesla Model 3",
        type: "Vehicle",
        currentValue: 35000,
        purchasePrice: 45000,
        hasBeneficiaries: false,
      },
      {
        id: "3",
        name: "Vanguard 401k",
        type: "Retirement",
        currentValue: 150000,
        purchasePrice: null,
        hasBeneficiaries: true,
      },
    ],
    isLoading: false,
  }),
  useCreateAsset: () => ({
    mutate: vi.fn(),
    isPending: false,
  }),
  useUpdateAsset: () => ({
    mutate: vi.fn(),
    isPending: false,
  }),
  useDeleteAsset: () => ({
    mutate: vi.fn(),
    isPending: false,
  }),
}));

// Mock sonner toast
vi.mock("sonner", () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

import Assets from "@/pages/Assets";

function createTestWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return function TestWrapper({ children }: { children: React.ReactNode }) {
    return (
      <HelmetProvider>
        <ThemeProvider>
          <QueryClientProvider client={queryClient}>
            <TooltipProvider>
              <MemoryRouter>{children}</MemoryRouter>
            </TooltipProvider>
          </QueryClientProvider>
        </ThemeProvider>
      </HelmetProvider>
    );
  };
}

describe("Assets Page — View Toggle", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it("renders card view by default", () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    // Card view should show grid layout - check for asset cards
    expect(screen.getByText("123 Main Street")).toBeInTheDocument();
    
    // Table should not be visible (no table headers)
    expect(screen.queryByRole("columnheader", { name: /purchase price/i })).not.toBeInTheDocument();
  });

  it("renders view toggle buttons with correct aria-labels", () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    const cardViewButton = screen.getByLabelText("Card view");
    const tableViewButton = screen.getByLabelText("Table view");

    expect(cardViewButton).toBeInTheDocument();
    expect(tableViewButton).toBeInTheDocument();
  });

  it("switches to table view when table button is clicked", async () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    const tableViewButton = screen.getByLabelText("Table view");
    fireEvent.click(tableViewButton);

    await waitFor(() => {
      // Table should have column headers
      expect(screen.getByRole("columnheader", { name: /name/i })).toBeInTheDocument();
      expect(screen.getByRole("columnheader", { name: /current value/i })).toBeInTheDocument();
    });
  });

  it("switches back to card view when card button is clicked", async () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    // Switch to table first
    const tableViewButton = screen.getByLabelText("Table view");
    fireEvent.click(tableViewButton);

    await waitFor(() => {
      expect(screen.getByRole("columnheader", { name: /name/i })).toBeInTheDocument();
    });

    // Switch back to card
    const cardViewButton = screen.getByLabelText("Card view");
    fireEvent.click(cardViewButton);

    await waitFor(() => {
      // Table headers should be gone
      expect(screen.queryByRole("columnheader", { name: /purchase price/i })).not.toBeInTheDocument();
    });
  });

  it("persists view preference to localStorage", async () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    // Switch to table view
    const tableViewButton = screen.getByLabelText("Table view");
    fireEvent.click(tableViewButton);

    await waitFor(() => {
      expect(localStorage.getItem("assets-view-mode")).toBe("table");
    });

    // Switch to card view
    const cardViewButton = screen.getByLabelText("Card view");
    fireEvent.click(cardViewButton);

    await waitFor(() => {
      expect(localStorage.getItem("assets-view-mode")).toBe("card");
    });
  });

  it("restores view preference from localStorage on mount", () => {
    // Set preference before rendering
    localStorage.setItem("assets-view-mode", "table");

    render(<Assets />, { wrapper: createTestWrapper() });

    // Should render table view (has column headers)
    expect(screen.getByRole("columnheader", { name: /name/i })).toBeInTheDocument();
  });

  it("highlights the active view button", () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    const cardViewButton = screen.getByLabelText("Card view");
    const tableViewButton = screen.getByLabelText("Table view");

    // Card should be active by default
    expect(cardViewButton).toHaveClass("bg-primary");
    expect(tableViewButton).not.toHaveClass("bg-primary");

    // Switch to table
    fireEvent.click(tableViewButton);

    expect(tableViewButton).toHaveClass("bg-primary");
    expect(cardViewButton).not.toHaveClass("bg-primary");
  });

  it("displays all assets in card view", () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    expect(screen.getByText("123 Main Street")).toBeInTheDocument();
    expect(screen.getByText("Tesla Model 3")).toBeInTheDocument();
    expect(screen.getByText("Vanguard 401k")).toBeInTheDocument();
  });

  it("displays all assets in table view", async () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    const tableViewButton = screen.getByLabelText("Table view");
    fireEvent.click(tableViewButton);

    await waitFor(() => {
      expect(screen.getByText("123 Main Street")).toBeInTheDocument();
      expect(screen.getByText("Tesla Model 3")).toBeInTheDocument();
      expect(screen.getByText("Vanguard 401k")).toBeInTheDocument();
    });
  });
});

describe("Assets Page — Filter Tabs", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("renders all filter tabs", () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    // Use getAllByRole for buttons that are filter tabs
    const filterButtons = screen.getAllByRole("button").filter(
      btn => btn.classList.contains("rounded-full")
    );
    
    expect(filterButtons.length).toBeGreaterThanOrEqual(8); // All, Real Estate, Vehicles, etc.
  });

  it("highlights 'All' filter by default", () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    // Find the All filter tab button (rounded-full class and contains "All" text)
    const allTab = screen.getAllByRole("button").find(
      btn => btn.classList.contains("rounded-full") && btn.textContent === "All"
    );
    
    expect(allTab).toHaveClass("bg-primary");
  });

  it("changes active filter when tab is clicked", async () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    // Find filter tab buttons
    const filterButtons = screen.getAllByRole("button").filter(
      btn => btn.classList.contains("rounded-full")
    );
    
    // Find the Vehicles tab
    const vehiclesTab = filterButtons.find(btn => btn.textContent === "Vehicles");
    expect(vehiclesTab).toBeDefined();
    
    fireEvent.click(vehiclesTab!);

    await waitFor(() => {
      expect(vehiclesTab).toHaveClass("bg-primary");
    });
  });
});

describe("Assets Page — Summary Cards", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("displays summary cards when assets exist", () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    expect(screen.getByText("Total Assets Value")).toBeInTheDocument();
    expect(screen.getByText("Number of Assets")).toBeInTheDocument();
    expect(screen.getByText("Top Category")).toBeInTheDocument();
    expect(screen.getByText("Needs Attention")).toBeInTheDocument();
  });

  it("shows correct asset count", () => {
    render(<Assets />, { wrapper: createTestWrapper() });

    // We have 3 assets in mock data - find the count in summary cards
    const summarySection = screen.getByText("Number of Assets").closest("div");
    expect(summarySection).toHaveTextContent("3");
  });
});

describe("Assets Page — Delete Functionality", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it("shows delete option in actions menu (table view)", async () => {
    const user = userEvent.setup();
    localStorage.setItem("assets-view-mode", "table");
    render(<Assets />, { wrapper: createTestWrapper() });

    // Find the row containing "123 Main Street" and click its action button
    const rows = screen.getAllByRole("row");
    const assetRow = rows.find(row => row.textContent?.includes("123 Main Street"));
    expect(assetRow).toBeDefined();
    
    const actionButton = assetRow!.querySelector("button");
    expect(actionButton).toBeDefined();
    
    await user.click(actionButton!);

    await waitFor(() => {
      expect(screen.getByText("Delete")).toBeInTheDocument();
    });
  });

  it("opens delete dialog from actions menu in table view", async () => {
    const user = userEvent.setup();
    localStorage.setItem("assets-view-mode", "table");
    render(<Assets />, { wrapper: createTestWrapper() });

    const rows = screen.getAllByRole("row");
    const assetRow = rows.find(row => row.textContent?.includes("123 Main Street"));
    const actionButton = assetRow!.querySelector("button");
    
    await user.click(actionButton!);

    await waitFor(() => {
      expect(screen.getByText("Delete")).toBeInTheDocument();
    });

    await user.click(screen.getByText("Delete"));

    await waitFor(() => {
      expect(screen.getByRole("alertdialog")).toBeInTheDocument();
      expect(screen.getByText("Delete Asset")).toBeInTheDocument();
    });
  });

  it("shows asset name in delete confirmation dialog", async () => {
    const user = userEvent.setup();
    localStorage.setItem("assets-view-mode", "table");
    render(<Assets />, { wrapper: createTestWrapper() });

    const rows = screen.getAllByRole("row");
    const assetRow = rows.find(row => row.textContent?.includes("123 Main Street"));
    const actionButton = assetRow!.querySelector("button");
    
    await user.click(actionButton!);

    await waitFor(() => {
      expect(screen.getByText("Delete")).toBeInTheDocument();
    });

    await user.click(screen.getByText("Delete"));

    await waitFor(() => {
      const dialog = screen.getByRole("alertdialog");
      expect(dialog).toHaveTextContent("123 Main Street");
    });
  });

  it("closes delete dialog when Cancel is clicked", async () => {
    const user = userEvent.setup();
    localStorage.setItem("assets-view-mode", "table");
    render(<Assets />, { wrapper: createTestWrapper() });

    const rows = screen.getAllByRole("row");
    const assetRow = rows.find(row => row.textContent?.includes("123 Main Street"));
    const actionButton = assetRow!.querySelector("button");
    
    await user.click(actionButton!);

    await waitFor(() => {
      expect(screen.getByText("Delete")).toBeInTheDocument();
    });

    await user.click(screen.getByText("Delete"));

    await waitFor(() => {
      expect(screen.getByRole("alertdialog")).toBeInTheDocument();
    });

    const cancelButton = screen.getByRole("button", { name: /cancel/i });
    await user.click(cancelButton);

    await waitFor(() => {
      expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });
  });

  it("displays warning about beneficiary assignments in delete dialog", async () => {
    const user = userEvent.setup();
    localStorage.setItem("assets-view-mode", "table");
    render(<Assets />, { wrapper: createTestWrapper() });

    const rows = screen.getAllByRole("row");
    const assetRow = rows.find(row => row.textContent?.includes("123 Main Street"));
    const actionButton = assetRow!.querySelector("button");
    
    await user.click(actionButton!);

    await waitFor(() => {
      expect(screen.getByText("Delete")).toBeInTheDocument();
    });

    await user.click(screen.getByText("Delete"));

    await waitFor(() => {
      expect(screen.getByText(/beneficiary assignments will also be removed/i)).toBeInTheDocument();
    });
  });

  it("shows Delete button with destructive styling in confirmation dialog", async () => {
    const user = userEvent.setup();
    localStorage.setItem("assets-view-mode", "table");
    render(<Assets />, { wrapper: createTestWrapper() });

    const rows = screen.getAllByRole("row");
    const assetRow = rows.find(row => row.textContent?.includes("123 Main Street"));
    const actionButton = assetRow!.querySelector("button");
    
    await user.click(actionButton!);

    await waitFor(() => {
      expect(screen.getByText("Delete")).toBeInTheDocument();
    });

    await user.click(screen.getByText("Delete"));

    await waitFor(() => {
      expect(screen.getByRole("alertdialog")).toBeInTheDocument();
    });

    const dialog = screen.getByRole("alertdialog");
    const deleteButton = dialog.querySelector("button.bg-destructive");
    expect(deleteButton).toBeInTheDocument();
  });
});
