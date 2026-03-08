/**
 * Mock contact data covering all three contact types.
 *
 * @description Provides realistic seed data for development before
 * the API is ready. Covers Individual, Trust, and Organization contacts
 * with varied relationships and asset link counts.
 */
import type { ContactDto, AssetContactLinkDto } from "@/types/contacts";

export const mockContacts: ContactDto[] = [
  // ── Individuals ──────────────────────────────────────────────
  {
    id: "c001-1111-2222-3333-444444444401",
    contactType: "Individual",
    displayName: "Priya Patel",
    firstName: "Priya",
    lastName: "Patel",
    relationship: "Spouse",
    email: "priya.patel@email.com",
    phone: "(512) 555-0101",
    dateOfBirth: "1988-03-15",
    ssn: "••••6789",
    ssnMasked: true,
    address: {
      street1: "4200 Lakewood Dr",
      city: "Austin",
      state: "TX",
      postalCode: "78731",
      country: "US",
    },
    assetLinkCount: 3,
    createdAt: "2024-01-15T10:00:00Z",
    updatedAt: "2025-01-05T14:30:00Z",
  },
  {
    id: "c001-1111-2222-3333-444444444402",
    contactType: "Individual",
    displayName: "Arjun Patel",
    firstName: "Arjun",
    lastName: "Patel",
    relationship: "Child",
    email: "arjun.patel@email.com",
    phone: "(512) 555-0102",
    dateOfBirth: "2010-07-22",
    assetLinkCount: 1,
    createdAt: "2024-01-15T10:05:00Z",
    updatedAt: "2024-12-20T09:15:00Z",
  },
  {
    id: "c001-1111-2222-3333-444444444403",
    contactType: "Individual",
    displayName: "Anaya Patel",
    firstName: "Anaya",
    lastName: "Patel",
    relationship: "Child",
    dateOfBirth: "2013-11-08",
    assetLinkCount: 1,
    createdAt: "2024-01-15T10:10:00Z",
    updatedAt: "2024-12-20T09:20:00Z",
  },
  {
    id: "c001-1111-2222-3333-444444444404",
    contactType: "Individual",
    displayName: "Raj Patel Sr.",
    firstName: "Raj",
    lastName: "Patel",
    relationship: "Parent",
    email: "raj.sr@email.com",
    phone: "(512) 555-0104",
    assetLinkCount: 0,
    createdAt: "2024-06-01T08:00:00Z",
    updatedAt: "2024-06-01T08:00:00Z",
  },
  {
    id: "c001-1111-2222-3333-444444444405",
    contactType: "Individual",
    displayName: "Sarah Chen",
    firstName: "Sarah",
    lastName: "Chen",
    relationship: "FinancialAdvisor",
    email: "sarah.chen@advisory.com",
    phone: "(512) 555-0200",
    address: {
      street1: "100 Congress Ave",
      street2: "Suite 1400",
      city: "Austin",
      state: "TX",
      postalCode: "78701",
      country: "US",
    },
    assetLinkCount: 0,
    createdAt: "2024-03-10T14:00:00Z",
    updatedAt: "2024-11-01T16:45:00Z",
  },

  // ── Trusts ───────────────────────────────────────────────────
  {
    id: "c001-1111-2222-3333-444444444410",
    contactType: "Trust",
    displayName: "Patel Family Revocable Trust",
    trustName: "Patel Family Revocable Trust",
    ein: "••••4321",
    einMasked: true,
    category: "RevocableLiving",
    purpose: "General",
    trustDate: "2020-09-15",
    stateOfFormation: "TX",
    isGrantorTrust: true,
    hasCrummeyProvisions: false,
    isGstExempt: false,
    email: "trust@patelfamily.com",
    assetLinkCount: 2,
    createdAt: "2024-02-01T09:00:00Z",
    updatedAt: "2025-01-10T11:00:00Z",
  },
  {
    id: "c001-1111-2222-3333-444444444411",
    contactType: "Trust",
    displayName: "Patel Children's Irrevocable Trust",
    trustName: "Patel Children's Irrevocable Trust",
    category: "Irrevocable",
    purpose: "TaxPlanning",
    specificType: "GRAT",
    trustDate: "2022-01-10",
    stateOfFormation: "TX",
    isGrantorTrust: false,
    hasCrummeyProvisions: true,
    isGstExempt: true,
    assetLinkCount: 1,
    createdAt: "2024-04-15T10:30:00Z",
    updatedAt: "2024-12-15T08:45:00Z",
  },

  // ── Organizations ────────────────────────────────────────────
  {
    id: "c001-1111-2222-3333-444444444420",
    contactType: "Organization",
    displayName: "Austin Community Foundation",
    organizationName: "Austin Community Foundation",
    organizationType: "Charity",
    is501C3: true,
    ein: "••••9876",
    einMasked: true,
    email: "giving@austincf.org",
    phone: "(512) 555-0300",
    address: {
      street1: "4315 Guadalupe St",
      street2: "Suite 300",
      city: "Austin",
      state: "TX",
      postalCode: "78751",
      country: "US",
    },
    assetLinkCount: 1,
    createdAt: "2024-05-20T13:00:00Z",
    updatedAt: "2024-11-30T10:00:00Z",
  },
  {
    id: "c001-1111-2222-3333-444444444421",
    contactType: "Organization",
    displayName: "University of Texas at Austin",
    organizationName: "University of Texas at Austin",
    organizationType: "EducationalInstitution",
    is501C3: true,
    email: "development@utexas.edu",
    assetLinkCount: 0,
    createdAt: "2024-08-10T09:30:00Z",
    updatedAt: "2024-08-10T09:30:00Z",
  },
];

/* ------------------------------------------------------------------ */
/*  Mock asset-contact links (for linked-assets dialog)                */
/* ------------------------------------------------------------------ */

const mockAssetContactLinks: Record<string, AssetContactLinkDto[]> = {
  // Priya Patel — linked to 3 assets as beneficiary
  "c001-1111-2222-3333-444444444401": [
    {
      id: "lnk-0001",
      assetId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      assetName: "Primary Residence",
      contactId: "c001-1111-2222-3333-444444444401",
      contactDisplayName: "Priya Patel",
      role: "Beneficiary",
      designation: "Primary",
      allocationPercent: 50,
      perStirpes: false,
    },
    {
      id: "lnk-0002",
      assetId: "c3d4e5f6-a7b8-9012-cdef-123456789012",
      assetName: "Fidelity 401(k)",
      contactId: "c001-1111-2222-3333-444444444401",
      contactDisplayName: "Priya Patel",
      role: "Beneficiary",
      designation: "Primary",
      allocationPercent: 100,
      perStirpes: false,
    },
    {
      id: "lnk-0003",
      assetId: "d4e5f6a7-b8c9-0123-defa-234567890123",
      assetName: "High-Yield Savings",
      contactId: "c001-1111-2222-3333-444444444401",
      contactDisplayName: "Priya Patel",
      role: "CoOwner",
      perStirpes: false,
    },
  ],
  // Arjun Patel — linked to 1 asset
  "c001-1111-2222-3333-444444444402": [
    {
      id: "lnk-0004",
      assetId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      assetName: "Primary Residence",
      contactId: "c001-1111-2222-3333-444444444402",
      contactDisplayName: "Arjun Patel",
      role: "Beneficiary",
      designation: "Primary",
      allocationPercent: 25,
      perStirpes: true,
    },
  ],
  // Anaya Patel — linked to 1 asset
  "c001-1111-2222-3333-444444444403": [
    {
      id: "lnk-0005",
      assetId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      assetName: "Primary Residence",
      contactId: "c001-1111-2222-3333-444444444403",
      contactDisplayName: "Anaya Patel",
      role: "Beneficiary",
      designation: "Primary",
      allocationPercent: 25,
      perStirpes: true,
    },
  ],
  // Patel Family Trust — linked to 2 assets
  "c001-1111-2222-3333-444444444410": [
    {
      id: "lnk-0006",
      assetId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      assetName: "Primary Residence",
      contactId: "c001-1111-2222-3333-444444444410",
      contactDisplayName: "Patel Family Revocable Trust",
      role: "Beneficiary",
      designation: "Contingent",
      allocationPercent: 100,
      perStirpes: false,
    },
    {
      id: "lnk-0007",
      assetId: "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      assetName: "2022 Tesla Model 3",
      contactId: "c001-1111-2222-3333-444444444410",
      contactDisplayName: "Patel Family Revocable Trust",
      role: "Trustee",
      perStirpes: false,
    },
  ],
  // Austin Community Foundation — linked to 1 asset
  "c001-1111-2222-3333-444444444420": [
    {
      id: "lnk-0008",
      assetId: "c3d4e5f6-a7b8-9012-cdef-123456789012",
      assetName: "Fidelity 401(k)",
      contactId: "c001-1111-2222-3333-444444444420",
      contactDisplayName: "Austin Community Foundation",
      role: "Beneficiary",
      designation: "Contingent",
      allocationPercent: 100,
      perStirpes: false,
    },
  ],
};

/**
 * Returns asset-contact links for a given contact.
 *
 * @param contactId - The contact's unique identifier.
 * @returns Array of asset-contact link DTOs, or empty array.
 */
export function getContactAssetLinks(contactId: string): AssetContactLinkDto[] {
  return mockAssetContactLinks[contactId] ?? [];
}
