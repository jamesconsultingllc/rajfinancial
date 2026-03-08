import { describe, it, expect, vi, beforeAll, beforeEach } from "vitest";
import { render, screen, within, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { HelmetProvider } from "react-helmet-async";
import { ThemeProvider } from "@/hooks/use-theme";
import { TooltipProvider } from "@/components/ui/tooltip";
import type { AssetContactLinkDto } from "@/types/contacts";
import type { ContactDto } from "@/types/contacts";
import type { AssetDto } from "@/types/assets";

/* ---- jsdom polyfills for Radix UI APIs ---- */
beforeAll(() => {
  if (!Element.prototype.hasPointerCapture) {
    Element.prototype.hasPointerCapture = vi.fn(() => false);
  }
  if (!Element.prototype.setPointerCapture) {
    Element.prototype.setPointerCapture = vi.fn();
  }
  if (!Element.prototype.releasePointerCapture) {
    Element.prototype.releasePointerCapture = vi.fn();
  }
  if (!Element.prototype.scrollIntoView) {
    Element.prototype.scrollIntoView = vi.fn();
  }
});

/* ------------------------------------------------------------------ */
/*  Mock data                                                          */
/* ------------------------------------------------------------------ */

const mockAsset: AssetDto = {
  id: "asset-001",
  name: "Primary Residence",
  type: "RealEstate",
  currentValue: 500000,
  isDepreciable: false,
  isDisposed: false,
  hasBeneficiaries: true,
  createdAt: "2024-01-01T00:00:00Z",
};

const mockContacts: ContactDto[] = [
  {
    id: "contact-001",
    contactType: "Individual",
    displayName: "Priya Patel",
    firstName: "Priya",
    lastName: "Patel",
    relationship: "Spouse",
    assetLinkCount: 1,
    createdAt: "2024-01-01T00:00:00Z",
    updatedAt: "2024-01-01T00:00:00Z",
  },
  {
    id: "contact-002",
    contactType: "Individual",
    displayName: "Arjun Patel",
    firstName: "Arjun",
    lastName: "Patel",
    relationship: "Child",
    assetLinkCount: 0,
    createdAt: "2024-01-01T00:00:00Z",
    updatedAt: "2024-01-01T00:00:00Z",
  },
  {
    id: "contact-003",
    contactType: "Trust",
    displayName: "Patel Family Trust",
    trustName: "Patel Family Trust",
    assetLinkCount: 0,
    createdAt: "2024-01-01T00:00:00Z",
    updatedAt: "2024-01-01T00:00:00Z",
  },
];

const mockLinks: AssetContactLinkDto[] = [
  {
    id: "lnk-001",
    assetId: "asset-001",
    assetName: "Primary Residence",
    contactId: "contact-001",
    contactDisplayName: "Priya Patel",
    role: "Beneficiary",
    designation: "Primary",
    allocationPercent: 50,
    perStirpes: false,
  },
];

/* ------------------------------------------------------------------ */
/*  Mutable mock state (reset per test)                                */
/* ------------------------------------------------------------------ */

let linksData: AssetContactLinkDto[] = [];
const mockCreateMutate = vi.fn();
const mockUpdateMutate = vi.fn();
const mockDeleteMutate = vi.fn();

vi.mock("@/auth/useAuth", () => ({
  useAuth: () => ({
    isAuthenticated: true,
    user: { name: "Test User", email: "test@example.com", initials: "TU", roles: ["Client"] },
    login: vi.fn(),
    logout: vi.fn(),
    hasRole: () => false,
    isAdmin: false,
    isClient: true,
  }),
}));

vi.mock("@/services/contact-service", () => ({
  useContacts: () => ({
    data: mockContacts,
    isLoading: false,
  }),
}));

vi.mock("@/services/beneficiary-service", () => ({
  useAssetLinks: () => ({
    data: linksData,
    isLoading: false,
  }),
  useCreateAssetLink: () => ({
    mutate: mockCreateMutate,
    isPending: false,
  }),
  useUpdateAssetLink: () => ({
    mutate: mockUpdateMutate,
    isPending: false,
  }),
  useDeleteAssetLink: () => ({
    mutate: mockDeleteMutate,
    isPending: false,
  }),
}));

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

import { BeneficiaryAssignmentDialog } from "../BeneficiaryAssignmentDialog";

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

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

function renderDialog(overrides: Partial<{ open: boolean; links: AssetContactLinkDto[] }> = {}) {
  linksData = overrides.links ?? [];
  const onOpenChange = vi.fn();
  const result = render(
    <BeneficiaryAssignmentDialog
      open={overrides.open ?? true}
      onOpenChange={onOpenChange}
      asset={mockAsset}
    />,
    { wrapper: createTestWrapper() },
  );
  return { ...result, onOpenChange };
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe("BeneficiaryAssignmentDialog — Rendering", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    linksData = [];
  });

  it("renders dialog title and description", () => {
    renderDialog();
    expect(screen.getByText("Manage Beneficiaries")).toBeInTheDocument();
    expect(screen.getByText(/Assign contacts to Primary Residence/)).toBeInTheDocument();
  });

  it("does not render when open is false", () => {
    renderDialog({ open: false });
    expect(screen.queryByText("Manage Beneficiaries")).not.toBeInTheDocument();
  });

  it("shows empty state when no links exist", () => {
    renderDialog({ links: [] });
    expect(screen.getByText("No beneficiaries assigned yet.")).toBeInTheDocument();
  });

  it("renders the Add Assignment button", () => {
    renderDialog();
    expect(screen.getByRole("button", { name: /add assignment/i })).toBeInTheDocument();
  });

  it("renders a Close button in the footer", () => {
    renderDialog();
    // Both the Radix X close button and the footer Close button exist
    const closeButtons = screen.getAllByRole("button", { name: /close/i });
    expect(closeButtons.length).toBeGreaterThanOrEqual(2);
  });

  it("calls onOpenChange when Close is clicked", async () => {
    const user = userEvent.setup({ pointerEventsCheck: 0 });
    const { onOpenChange } = renderDialog();
    // Multiple Close buttons exist (Radix X + footer); target the footer one
    const closeButtons = screen.getAllByRole("button", { name: /close/i });
    await user.click(closeButtons[closeButtons.length - 1]);
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });
});

describe("BeneficiaryAssignmentDialog — Existing Assignments Table", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders existing links in the table", () => {
    renderDialog({ links: mockLinks });
    expect(screen.getByText("Priya Patel")).toBeInTheDocument();
    expect(screen.getByText("Beneficiary")).toBeInTheDocument();
    // "50%" appears in both the table cell and allocation badge; scope to table
    expect(within(screen.getByRole("table")).getByText("50%")).toBeInTheDocument();
  });

  it("shows designation badge for beneficiary links", () => {
    renderDialog({ links: mockLinks });
    expect(screen.getByText("Primary")).toBeInTheDocument();
  });

  it("shows per stirpes as Yes when true", () => {
    renderDialog({
      links: [{ ...mockLinks[0], perStirpes: true }],
    });
    expect(screen.getByText("Yes")).toBeInTheDocument();
  });

  it("shows dash for per stirpes when false", () => {
    renderDialog({ links: mockLinks });
    // Per stirpes false shows "—"
    const cells = screen.getAllByText("—");
    expect(cells.length).toBeGreaterThanOrEqual(1);
  });

  it("renders edit and delete buttons for each link", () => {
    renderDialog({ links: mockLinks });
    const table = screen.getByRole("table");
    const tableButtons = within(table).getAllByRole("button");
    // Each link row has an edit button and a delete button (with destructive class)
    const deleteButtons = tableButtons.filter((btn) => btn.className.includes("destructive"));
    const editButtons = tableButtons.filter((btn) => !btn.className.includes("destructive"));
    expect(editButtons).toHaveLength(1);
    expect(deleteButtons).toHaveLength(1);
  });
});

describe("BeneficiaryAssignmentDialog — Allocation Badges", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows 0% for Primary and Contingent when no links", () => {
    renderDialog({ links: [] });
    expect(screen.getByText("Primary:")).toBeInTheDocument();
    expect(screen.getByText("Contingent:")).toBeInTheDocument();
    const zeroPcts = screen.getAllByText("0%");
    expect(zeroPcts).toHaveLength(2);
  });

  it("computes Primary allocation total correctly", () => {
    renderDialog({
      links: [
        { ...mockLinks[0], allocationPercent: 60 },
        {
          id: "lnk-002",
          assetId: "asset-001",
          assetName: "Primary Residence",
          contactId: "contact-002",
          contactDisplayName: "Arjun Patel",
          role: "Beneficiary",
          designation: "Primary",
          allocationPercent: 40,
          perStirpes: true,
        },
      ],
    });
    expect(screen.getByText("100%")).toBeInTheDocument();
  });

  it("computes Contingent allocation total correctly", () => {
    renderDialog({
      links: [
        {
          id: "lnk-003",
          assetId: "asset-001",
          assetName: "Primary Residence",
          contactId: "contact-003",
          contactDisplayName: "Patel Family Trust",
          role: "Beneficiary",
          designation: "Contingent",
          allocationPercent: 75,
          perStirpes: false,
        },
      ],
    });
    // Primary should be 0%, Contingent should be 75% (appears in both badge and table)
    const pctElements = screen.getAllByText("75%");
    expect(pctElements.length).toBeGreaterThanOrEqual(1);
  });

  it("shows checkmark when allocation equals 100%", () => {
    renderDialog({
      links: [
        { ...mockLinks[0], allocationPercent: 100, designation: "Primary" },
      ],
    });
    expect(screen.getByText("✓")).toBeInTheDocument();
  });
});

describe("BeneficiaryAssignmentDialog — Add Assignment Flow", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows add form when Add Assignment button is clicked", async () => {
    const user = userEvent.setup();
    renderDialog();
    await user.click(screen.getByRole("button", { name: /add assignment/i }));
    expect(screen.getByText("Add Assignment")).toBeInTheDocument();
    expect(screen.getByText("Contact")).toBeInTheDocument();
    expect(screen.getByText("Role")).toBeInTheDocument();
  });

  it("shows designation and allocation fields for Beneficiary role", async () => {
    const user = userEvent.setup();
    renderDialog();
    await user.click(screen.getByRole("button", { name: /add assignment/i }));
    // Beneficiary is the default role, so designation/allocation visible
    expect(screen.getByText("Designation")).toBeInTheDocument();
    expect(screen.getByText("Allocation %")).toBeInTheDocument();
  });

  it("shows per stirpes checkbox in add form", async () => {
    const user = userEvent.setup();
    renderDialog();
    await user.click(screen.getByRole("button", { name: /add assignment/i }));
    expect(screen.getByText(/Per Stirpes/)).toBeInTheDocument();
  });

  it("hides add form when Cancel is clicked", async () => {
    const user = userEvent.setup({ pointerEventsCheck: 0 });
    renderDialog();
    await user.click(screen.getByRole("button", { name: /add assignment/i }));
    // Form fields should be visible
    expect(screen.getByText("Select contact\u2026")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /cancel/i }));
    // Form fields should be gone; the "Add Assignment" button reappears
    expect(screen.queryByText("Select contact\u2026")).not.toBeInTheDocument();
  });

  it("filters out already-assigned contacts from dropdown", async () => {
    const user = userEvent.setup({ pointerEventsCheck: 0 });
    // Priya (contact-001) is already assigned
    renderDialog({ links: mockLinks });
    await user.click(screen.getByRole("button", { name: /add assignment/i }));

    // Open the contact dropdown
    const contactTrigger = screen.getByText("Select contact…");
    await user.click(contactTrigger);

    // Priya should NOT be available; Arjun and Trust should be
    await waitFor(() => {
      expect(screen.queryByRole("option", { name: "Priya Patel" })).not.toBeInTheDocument();
      expect(screen.getByText("Arjun Patel")).toBeInTheDocument();
      expect(screen.getByText("Patel Family Trust")).toBeInTheDocument();
    });
  });

  it("Add button is disabled when no contact is selected", async () => {
    const user = userEvent.setup();
    renderDialog();
    await user.click(screen.getByRole("button", { name: /add assignment/i }));

    const addBtn = screen.getByRole("button", { name: /^add$/i });
    expect(addBtn).toBeDisabled();
  });
});

describe("BeneficiaryAssignmentDialog — Delete Assignment", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls deleteLink.mutate with correct args when delete button is clicked", async () => {
    const user = userEvent.setup({ pointerEventsCheck: 0 });
    renderDialog({ links: mockLinks });

    const table = screen.getByRole("table");
    const deleteButton = within(table).getAllByRole("button").find(
      (btn) => btn.className.includes("destructive"),
    )!;
    await user.click(deleteButton);

    expect(mockDeleteMutate).toHaveBeenCalledWith(
      { linkId: "lnk-001", assetId: "asset-001" },
      expect.objectContaining({ onSuccess: expect.any(Function), onError: expect.any(Function) }),
    );
  });
});

describe("BeneficiaryAssignmentDialog — Edit Assignment", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("enters edit mode when edit button is clicked", async () => {
    const user = userEvent.setup();
    renderDialog({ links: mockLinks });

    const editButton = screen.getAllByRole("button").find(
      (btn) => btn.querySelector("svg.lucide-pencil") !== null,
    )!;
    await user.click(editButton);

    // Edit mode should show Save and Cancel buttons
    expect(screen.getByRole("button", { name: /save/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /cancel/i })).toBeInTheDocument();
  });

  it("exits edit mode when Cancel is clicked", async () => {
    const user = userEvent.setup();
    renderDialog({ links: mockLinks });

    const editButton = screen.getAllByRole("button").find(
      (btn) => btn.querySelector("svg.lucide-pencil") !== null,
    )!;
    await user.click(editButton);

    await user.click(screen.getByRole("button", { name: /cancel/i }));

    // Should be back to view mode — Save button gone
    expect(screen.queryByRole("button", { name: /save/i })).not.toBeInTheDocument();
  });

  it("calls updateLink.mutate when Save is clicked", async () => {
    const user = userEvent.setup();
    renderDialog({ links: mockLinks });

    const editButton = screen.getAllByRole("button").find(
      (btn) => btn.querySelector("svg.lucide-pencil") !== null,
    )!;
    await user.click(editButton);
    await user.click(screen.getByRole("button", { name: /save/i }));

    expect(mockUpdateMutate).toHaveBeenCalledWith(
      expect.objectContaining({
        linkId: "lnk-001",
        assetId: "asset-001",
        data: expect.objectContaining({ role: "Beneficiary" }),
      }),
      expect.objectContaining({ onSuccess: expect.any(Function), onError: expect.any(Function) }),
    );
  });
});

describe("BeneficiaryAssignmentDialog — Multiple Links", () => {
  const multipleLinks: AssetContactLinkDto[] = [
    {
      id: "lnk-001",
      assetId: "asset-001",
      assetName: "Primary Residence",
      contactId: "contact-001",
      contactDisplayName: "Priya Patel",
      role: "Beneficiary",
      designation: "Primary",
      allocationPercent: 50,
      perStirpes: false,
    },
    {
      id: "lnk-002",
      assetId: "asset-001",
      assetName: "Primary Residence",
      contactId: "contact-002",
      contactDisplayName: "Arjun Patel",
      role: "Beneficiary",
      designation: "Primary",
      allocationPercent: 50,
      perStirpes: true,
    },
    {
      id: "lnk-003",
      assetId: "asset-001",
      assetName: "Primary Residence",
      contactId: "contact-003",
      contactDisplayName: "Patel Family Trust",
      role: "Beneficiary",
      designation: "Contingent",
      allocationPercent: 100,
      perStirpes: false,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders all links in the table", () => {
    renderDialog({ links: multipleLinks });
    expect(screen.getByText("Priya Patel")).toBeInTheDocument();
    expect(screen.getByText("Arjun Patel")).toBeInTheDocument();
    expect(screen.getByText("Patel Family Trust")).toBeInTheDocument();
  });

  it("correctly totals Primary and Contingent allocations", () => {
    renderDialog({ links: multipleLinks });
    // Primary = 50 + 50 = 100, Contingent = 100
    const checkmarks = screen.getAllByText("✓");
    expect(checkmarks).toHaveLength(2); // both at 100%
  });

  it("shows no available contacts in add form when all are assigned", async () => {
    const user = userEvent.setup({ pointerEventsCheck: 0 });
    renderDialog({ links: multipleLinks });
    await user.click(screen.getByRole("button", { name: /add assignment/i }));

    const contactTrigger = screen.getByText("Select contact…");
    await user.click(contactTrigger);

    await waitFor(() => {
      expect(screen.getByText("No available contacts")).toBeInTheDocument();
    });
  });
});

describe("BeneficiaryAssignmentDialog — Non-Beneficiary Roles", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders non-beneficiary links without designation or allocation", () => {
    renderDialog({
      links: [
        {
          id: "lnk-010",
          assetId: "asset-001",
          assetName: "Primary Residence",
          contactId: "contact-001",
          contactDisplayName: "Priya Patel",
          role: "CoOwner",
          perStirpes: false,
        },
      ],
    });
    expect(screen.getByText("Priya Patel")).toBeInTheDocument();
    expect(screen.getByText("Co-Owner")).toBeInTheDocument();
    // Should show dashes for designation and allocation
    const dashes = screen.getAllByText("—");
    expect(dashes.length).toBeGreaterThanOrEqual(2);
  });
});
