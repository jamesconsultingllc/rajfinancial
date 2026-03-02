import {
  Home, Car, TrendingUp, Landmark, Wallet, Shield, Briefcase,
  Package, Gem, Coins, Lightbulb, CircleDot, type LucideIcon,
} from "lucide-react";

export type AssetType =
  | "RealEstate" | "Vehicle" | "Investment" | "Retirement"
  | "BankAccount" | "Insurance" | "Business" | "PersonalProperty"
  | "Collectible" | "Cryptocurrency" | "IntellectualProperty" | "Other";

export type DepreciationMethod = "None" | "StraightLine" | "DecliningBalance" | "Macrs";

/* ------------------------------------------------------------------ */
/*  Type-specific metadata interfaces                                  */
/* ------------------------------------------------------------------ */

/** Vehicle-specific fields */
export interface VehicleMetadata {
  vin?: string;
  make: string;
  model: string;
  year: number;
  mileage?: number;
  color?: string;
  licensePlate?: string;
}

/** Real Estate-specific fields */
export interface RealEstateMetadata {
  address: string;
  city: string;
  state: string;
  zipCode: string;
  propertyType: "SingleFamily" | "Condo" | "Townhouse" | "MultiFamily" | "Land" | "Commercial" | "Other";
  squareFeet?: number;
  yearBuilt?: number;
  lotSize?: string;
  bedrooms?: number;
  bathrooms?: number;
}

/** Investment-specific fields */
export interface InvestmentMetadata {
  ticker?: string;
  shares?: number;
  costBasis?: number;
  investmentType: "Stocks" | "Bonds" | "MutualFunds" | "ETF" | "Options" | "Other";
  brokerageName?: string;
}

/** Retirement account fields */
export interface RetirementMetadata {
  accountType: "401k" | "IRA" | "RothIRA" | "SEP_IRA" | "Pension" | "403b" | "Other";
  employerMatch?: number;
  vestingPercent?: number;
  projectedAnnualContribution?: number;
}

/** Bank account fields */
export interface BankAccountMetadata {
  bankAccountType: "Checking" | "Savings" | "MoneyMarket" | "CD" | "Other";
  routingNumber?: string;
  interestRate?: number;
  maturityDate?: string;
}

/** Insurance policy fields */
export interface InsuranceMetadata {
  policyNumber: string;
  policyType: "WholeLife" | "UniversalLife" | "TermLife" | "Annuity" | "Other";
  cashValue?: number;
  deathBenefit?: number;
  premiumAmount?: number;
  premiumFrequency?: "Monthly" | "Quarterly" | "Annually";
}

/** Business ownership fields */
export interface BusinessMetadata {
  entityType: "SoleProprietorship" | "Partnership" | "LLC" | "Corporation" | "SCorp" | "Other";
  ownershipPercent: number;
  ein?: string;
  industry?: string;
  annualRevenue?: number;
}

/** Cryptocurrency fields */
export interface CryptoMetadata {
  coinSymbol: string;
  quantity: number;
  walletAddress?: string;
  exchange?: string;
  stakingApy?: number;
}

/** Collectible / Personal Property fields */
export interface CollectibleMetadata {
  category: string;
  condition?: "Mint" | "Excellent" | "Good" | "Fair" | "Poor";
  provenance?: string;
  appraiserName?: string;
  lastAppraisalDate?: string;
}

/** Intellectual Property fields */
export interface IntellectualPropertyMetadata {
  ipType: "Patent" | "Trademark" | "Copyright" | "TradeSecret" | "Other";
  registrationNumber?: string;
  filingDate?: string;
  expirationDate?: string;
  licensee?: string;
  royaltyRate?: number;
}

/**
 * Maps each AssetType to its metadata interface.
 * "Other" and "PersonalProperty" share CollectibleMetadata for simplicity.
 */
export interface AssetMetadataMap {
  RealEstate: RealEstateMetadata;
  Vehicle: VehicleMetadata;
  Investment: InvestmentMetadata;
  Retirement: RetirementMetadata;
  BankAccount: BankAccountMetadata;
  Insurance: InsuranceMetadata;
  Business: BusinessMetadata;
  PersonalProperty: CollectibleMetadata;
  Collectible: CollectibleMetadata;
  Cryptocurrency: CryptoMetadata;
  IntellectualProperty: IntellectualPropertyMetadata;
  Other: Record<string, unknown>;
}

/* ------------------------------------------------------------------ */
/*  Core DTOs                                                          */
/* ------------------------------------------------------------------ */

export interface AssetDto {
  id: string;
  name: string;
  type: AssetType;
  currentValue: number;
  purchasePrice?: number;
  purchaseDate?: string;
  description?: string;
  location?: string;
  accountNumber?: string;
  institutionName?: string;
  isDepreciable: boolean;
  isDisposed: boolean;
  hasBeneficiaries: boolean;
  createdAt: string;
  updatedAt?: string;
  /** Type-specific metadata (keyed by AssetType) */
  metadata?: AssetMetadataMap[AssetType];
}

export interface BeneficiaryAssignmentDto {
  beneficiaryId: string;
  beneficiaryName: string;
  relationship: string;
  allocationPercent: number;
  type: string;
}

export interface AssetDetailDto extends AssetDto {
  depreciationMethod?: DepreciationMethod;
  salvageValue?: number;
  usefulLifeMonths?: number;
  inServiceDate?: string;
  accumulatedDepreciation?: number;
  bookValue?: number;
  monthlyDepreciation?: number;
  depreciationPercentComplete?: number;
  disposalDate?: string;
  disposalPrice?: number;
  disposalNotes?: string;
  marketValue?: number;
  lastValuationDate?: string;
  beneficiaries: BeneficiaryAssignmentDto[];
}

export interface CreateAssetRequest {
  name: string;
  type: AssetType;
  currentValue: number;
  purchasePrice?: number;
  purchaseDate?: string;
  description?: string;
  location?: string;
  accountNumber?: string;
  institutionName?: string;
  depreciationMethod?: DepreciationMethod;
  salvageValue?: number;
  usefulLifeMonths?: number;
  inServiceDate?: string;
  marketValue?: number;
  lastValuationDate?: string;
  /** Type-specific metadata */
  metadata?: AssetMetadataMap[AssetType];
}

export type UpdateAssetRequest = CreateAssetRequest;

export interface ApiErrorResponse {
  code: string;
  message: string;
  details?: Record<string, unknown>;
}

export const ASSET_TYPE_LABELS: Record<AssetType, string> = {
  RealEstate: "Real Estate",
  Vehicle: "Vehicle",
  Investment: "Investment",
  Retirement: "Retirement",
  BankAccount: "Bank Account",
  Insurance: "Insurance",
  Business: "Business",
  PersonalProperty: "Personal Property",
  Collectible: "Collectible",
  Cryptocurrency: "Cryptocurrency",
  IntellectualProperty: "Intellectual Property",
  Other: "Other",
};

export const ASSET_TYPE_ICONS: Record<AssetType, LucideIcon> = {
  RealEstate: Home,
  Vehicle: Car,
  Investment: TrendingUp,
  Retirement: Landmark,
  BankAccount: Wallet,
  Insurance: Shield,
  Business: Briefcase,
  PersonalProperty: Package,
  Collectible: Gem,
  Cryptocurrency: Coins,
  IntellectualProperty: Lightbulb,
  Other: CircleDot,
};

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(value);
}
