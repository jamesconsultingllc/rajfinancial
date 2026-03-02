/**
 * Contact & Beneficiary type definitions.
 *
 * @description Implements the polymorphic Contact model from
 * 04-contacts-beneficiaries.md. "Beneficiary" is a role a contact plays
 * when linked to an asset — not a separate entity type.
 *
 * Entity hierarchy:
 *   Contact (abstract base)
 *   ├── IndividualContact (people)
 *   ├── TrustContact (trusts)
 *   └── OrganizationContact (charities, businesses, etc.)
 */

/* ------------------------------------------------------------------ */
/*  Enums                                                              */
/* ------------------------------------------------------------------ */

/** Discriminator for the three contact subtypes. */
export type ContactType = "Individual" | "Trust" | "Organization";

/** Relationship of an IndividualContact to the account holder. */
export type RelationshipType =
  | "Spouse"
  | "Child"
  | "Parent"
  | "Sibling"
  | "Grandchild"
  | "Grandparent"
  | "InLaw"
  | "Friend"
  | "Attorney"
  | "Accountant"
  | "FinancialAdvisor"
  | "Other";

/** Broad trust classification. */
export type TrustCategory =
  | "RevocableLiving"
  | "Irrevocable"
  | "Testamentary";

/** Purpose / intent of the trust. */
export type TrustPurpose =
  | "General"
  | "Marital"
  | "AssetProtection"
  | "SpecialNeeds"
  | "Charitable"
  | "Insurance"
  | "TaxPlanning"
  | "Dynasty"
  | "Business"
  | "Other";

/** Organization sub-classification. */
export type OrganizationType =
  | "Charity"
  | "Business"
  | "Government"
  | "NonProfit"
  | "EducationalInstitution"
  | "ReligiousOrganization"
  | "Other";

/** Role a contact plays when linked to an asset. */
export type AssetContactRole =
  | "Beneficiary"
  | "CoOwner"
  | "Trustee"
  | "Custodian"
  | "PowerOfAttorney"
  | "EmergencyContact"
  | "Insured"
  | "Other";

/** Beneficiary designation — only applicable when role is "Beneficiary". */
export type DesignationType = "Primary" | "Contingent";

/** Role a contact (person) plays within a TrustContact. */
export type TrustRoleType =
  | "Grantor"
  | "Trustee"
  | "SuccessorTrustee"
  | "Beneficiary"
  | "RemainderBeneficiary"
  | "TrustProtector"
  | "InvestmentAdvisor"
  | "DistributionAdvisor";

/* ------------------------------------------------------------------ */
/*  Human-readable label maps                                          */
/* ------------------------------------------------------------------ */

export const CONTACT_TYPE_LABELS: Record<ContactType, string> = {
  Individual: "Individual",
  Trust: "Trust",
  Organization: "Organization",
};

export const RELATIONSHIP_LABELS: Record<RelationshipType, string> = {
  Spouse: "Spouse",
  Child: "Child",
  Parent: "Parent",
  Sibling: "Sibling",
  Grandchild: "Grandchild",
  Grandparent: "Grandparent",
  InLaw: "In-Law",
  Friend: "Friend",
  Attorney: "Attorney",
  Accountant: "Accountant",
  FinancialAdvisor: "Financial Advisor",
  Other: "Other",
};

export const TRUST_CATEGORY_LABELS: Record<TrustCategory, string> = {
  RevocableLiving: "Revocable Living",
  Irrevocable: "Irrevocable",
  Testamentary: "Testamentary",
};

export const TRUST_PURPOSE_LABELS: Record<TrustPurpose, string> = {
  General: "General",
  Marital: "Marital (QTIP, Bypass, AB)",
  AssetProtection: "Asset Protection",
  SpecialNeeds: "Special Needs",
  Charitable: "Charitable (CRT, CLT)",
  Insurance: "Insurance (ILIT)",
  TaxPlanning: "Tax Planning (GRAT, QPRT)",
  Dynasty: "Dynasty / Multi-Generational",
  Business: "Business (ESBT, QSST)",
  Other: "Other",
};

export const ORGANIZATION_TYPE_LABELS: Record<OrganizationType, string> = {
  Charity: "Charity",
  Business: "Business",
  Government: "Government",
  NonProfit: "Non-Profit",
  EducationalInstitution: "Educational Institution",
  ReligiousOrganization: "Religious Organization",
  Other: "Other",
};

export const ASSET_CONTACT_ROLE_LABELS: Record<AssetContactRole, string> = {
  Beneficiary: "Beneficiary",
  CoOwner: "Co-Owner",
  Trustee: "Trustee",
  Custodian: "Custodian",
  PowerOfAttorney: "Power of Attorney",
  EmergencyContact: "Emergency Contact",
  Insured: "Insured",
  Other: "Other",
};

export const DESIGNATION_LABELS: Record<DesignationType, string> = {
  Primary: "Primary",
  Contingent: "Contingent",
};

export const TRUST_ROLE_TYPE_LABELS: Record<TrustRoleType, string> = {
  Grantor: "Grantor",
  Trustee: "Trustee",
  SuccessorTrustee: "Successor Trustee",
  Beneficiary: "Beneficiary",
  RemainderBeneficiary: "Remainder Beneficiary",
  TrustProtector: "Trust Protector",
  InvestmentAdvisor: "Investment Advisor",
  DistributionAdvisor: "Distribution Advisor",
};

/* ------------------------------------------------------------------ */
/*  DTOs — API response shapes                                         */
/* ------------------------------------------------------------------ */

/**
 * Address owned entity.
 *
 * @description Maps to EF Core owned entity `ContactAddress`.
 * Stored as columns on the Contact table.
 */
export interface ContactAddressDto {
  street1: string;
  street2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

/**
 * Polymorphic contact DTO — discriminated by `contactType`.
 *
 * @description Base fields are always present. Subtype-specific
 * fields are null/undefined for non-matching contact types.
 * This mirrors the C# `ContactDto` sealed record.
 */
export interface ContactDto {
  id: string;
  contactType: ContactType;
  displayName: string;
  email?: string;
  phone?: string;
  address?: ContactAddressDto;
  notes?: string;
  assetLinkCount: number;
  createdAt: string;
  updatedAt: string;

  // Individual-specific (null for other types)
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string;
  ssn?: string;          // Masked: "••••1234"
  ssnMasked?: boolean;
  relationship?: RelationshipType;

  // Trust-specific (null for other types)
  trustName?: string;
  ein?: string;           // Masked: "••••5678"
  einMasked?: boolean;
  category?: TrustCategory;
  purpose?: TrustPurpose;
  specificType?: string;
  trustDate?: string;
  stateOfFormation?: string;
  isGrantorTrust?: boolean;
  hasCrummeyProvisions?: boolean;
  isGstExempt?: boolean;

  // Organization-specific (null for other types)
  organizationName?: string;
  organizationType?: OrganizationType;
  is501C3?: boolean;
}

/**
 * Asset-Contact link DTO.
 *
 * @description Represents the relationship between a contact and an asset.
 * Designation, allocation, and perStirpes live HERE — not on the contact.
 */
export interface AssetContactLinkDto {
  id: string;
  assetId: string;
  assetName: string;
  contactId: string;
  contactDisplayName: string;
  role: AssetContactRole;
  designation?: DesignationType;
  allocationPercent?: number;
  perStirpes: boolean;
  notes?: string;
}

/**
 * Trust role DTO — links a contact to a trust in a specific role.
 */
export interface TrustRoleDto {
  id: string;
  trustContactId: string;
  contactId: string;
  contactDisplayName: string;
  roleType: TrustRoleType;
  successionOrder?: number;
  notes?: string;
}

/**
 * Beneficiary coverage summary across all user assets.
 */
export interface BeneficiaryCoverageDto {
  totalAssets: number;
  assetsWithBeneficiaries: number;
  assetsWithoutBeneficiaries: number;
  coveragePercent: number;
  assets: AssetCoverageItem[];
}

/** Per-asset beneficiary coverage breakdown. */
export interface AssetCoverageItem {
  assetId: string;
  assetName: string;
  assetType: string;
  currentValue?: number;
  primaryAllocationTotal: number;
  contingentAllocationTotal: number;
  hasPrimaryBeneficiary: boolean;
  hasContingentBeneficiary: boolean;
  allocationWarning: boolean;
}

/* ------------------------------------------------------------------ */
/*  Request shapes — discriminated union for creates                    */
/* ------------------------------------------------------------------ */

/** Shared optional fields for all contact types. */
interface ContactBaseFields {
  email?: string;
  phone?: string;
  address?: ContactAddressDto;
  notes?: string;
}

/** Create request for IndividualContact. */
export interface CreateIndividualContactRequest extends ContactBaseFields {
  contactType: "Individual";
  firstName: string;
  lastName: string;
  dateOfBirth?: string;
  ssn?: string;
  relationship?: RelationshipType;
}

/** Create request for TrustContact. */
export interface CreateTrustContactRequest extends ContactBaseFields {
  contactType: "Trust";
  trustName: string;
  ein?: string;
  category: TrustCategory;
  purpose: TrustPurpose;
  specificType?: string;
  trustDate?: string;
  stateOfFormation?: string;
  isGrantorTrust?: boolean;
  hasCrummeyProvisions?: boolean;
  isGstExempt?: boolean;
}

/** Create request for OrganizationContact. */
export interface CreateOrganizationContactRequest extends ContactBaseFields {
  contactType: "Organization";
  organizationName: string;
  ein?: string;
  organizationType: OrganizationType;
  is501C3?: boolean;
}

/**
 * Discriminated union for creating a contact.
 *
 * @description Validators enforce correct fields per type.
 * The `contactType` field acts as the discriminator.
 */
export type CreateContactRequest =
  | CreateIndividualContactRequest
  | CreateTrustContactRequest
  | CreateOrganizationContactRequest;

/**
 * Update request — all fields optional, contactType cannot be changed.
 *
 * @description Partial update. The server ignores fields irrelevant
 * to the contact's existing type.
 */
export interface UpdateContactRequest {
  email?: string;
  phone?: string;
  address?: ContactAddressDto;
  notes?: string;

  // Individual fields
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string;
  ssn?: string;
  relationship?: RelationshipType;

  // Trust fields
  trustName?: string;
  ein?: string;
  category?: TrustCategory;
  purpose?: TrustPurpose;
  specificType?: string;
  trustDate?: string;
  stateOfFormation?: string;
  isGrantorTrust?: boolean;
  hasCrummeyProvisions?: boolean;
  isGstExempt?: boolean;

  // Organization fields
  organizationName?: string;
  organizationType?: OrganizationType;
  is501C3?: boolean;
}

/** Request to link a contact to an asset. */
export interface CreateAssetContactLinkRequest {
  contactId: string;
  role: AssetContactRole;
  designation?: DesignationType;
  allocationPercent?: number;
  perStirpes?: boolean;
  notes?: string;
}

/** Request to update an existing asset-contact link. */
export interface UpdateAssetContactLinkRequest {
  role?: AssetContactRole;
  designation?: DesignationType;
  allocationPercent?: number;
  perStirpes?: boolean;
  notes?: string;
}

/** Request to add a trust role. */
export interface CreateTrustRoleRequest {
  contactId: string;
  roleType: TrustRoleType;
  successionOrder?: number;
  notes?: string;
}

/** Request to update a trust role. */
export interface UpdateTrustRoleRequest {
  roleType?: TrustRoleType;
  successionOrder?: number;
  notes?: string;
}
