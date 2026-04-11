# Lovable Prompt — Accounts, Liabilities & Transactions

## Context

Build two new pages: **Accounts** and **Transactions**. These pages render within an **entity context** (e.g., a personal profile, a business, or a trust) and show only that entity's accounts and transactions. Routes are entity-scoped: `/:entityType/:slug/accounts` and `/:entityType/:slug/transactions`. They unify bank accounts AND liabilities (loans, credit cards, mortgages) under each entity's navigation section. Liabilities are account types with negative balances that can optionally link to an existing asset (e.g., mortgage → real estate, auto loan → vehicle). Follow the exact same layout patterns as the existing **Assets** page — filter tabs, view toggle, summary cards, 3-dot actions menu, delete dialog, empty state, skeleton loading.

## Route & Nav

### Routes

- **`/:entityType/:slug/accounts`** — Accounts list page for a specific entity (e.g., `/personal/accounts`, `/business/acme-llc/accounts`, `/trust/family-trust/accounts`)
- **`/:entityType/:slug/transactions`** — Transactions list page for a specific entity (e.g., `/personal/transactions`, `/business/acme-llc/transactions`)

Both pages render inside an `EntityProvider` layout that resolves the entity from the URL params. Navigation items appear as sub-items under each entity section in the sidebar (e.g., under "Personal" or under "Acme LLC"). All links within these pages use entity-relative paths derived from the current `entityType` and `entitySlug`.

---

## Entity Context

Both pages render inside an `EntityProvider`:

```tsx
const { entityId, entityType, entitySlug } = useEntityContext();
```

All data fetching and mutations use `entityId` to scope to the current entity. For example:

```tsx
const { data: accounts } = useAccounts(entityId);
const createAccount = useCreateAccount(entityId);
```

The entity context also provides helper functions for building entity-relative navigation paths:

```tsx
const { entityPath } = useEntityContext();
// entityPath("accounts") → "/personal/accounts" or "/business/acme-llc/accounts"
// entityPath("transactions") → "/personal/transactions"
// entityPath("assets") → "/personal/assets"
```

---

## Data Types — Accounts

Create `src/types/accounts.ts`:

```typescript
export type AccountType = "Checking" | "Savings" | "CreditCard" | "Investment" | "Loan" | "Mortgage" | "Other";

export type AccountSubtype =
  | "Regular" | "MoneyMarket" | "CertificateOfDeposit" | "HighYield"       // Savings subtypes
  | "Brokerage" | "Ira" | "Roth" | "FiveZeroOneK"                         // Investment subtypes
  | "Auto" | "Personal" | "Student" | "HELOC" | "PolicyLoan"              // Loan subtypes
  | "Rewards" | "Business"                                                 // CreditCard subtypes
  | "Other";

export type AccountSource = "Plaid" | "Manual";

export type LiabilityType = "Mortgage" | "AutoLoan" | "StudentLoan" | "CreditCard" | "PersonalLoan" | "HELOC" | "PolicyLoan" | "Other";

export interface LinkedAccountDto {
  id: string;
  entityId: string;
  name: string;
  officialName?: string;
  institutionName: string;
  type: AccountType;
  subtype?: AccountSubtype;
  mask?: string;                    // last 4 digits, e.g. "4829"
  currentBalance?: number;          // positive for assets, negative for liabilities
  availableBalance?: number;
  isoCurrencyCode: string;
  source: AccountSource;
  lastSyncAt?: string;
  createdAt: string;
  // Liability-specific fields (populated when type is CreditCard, Loan, or Mortgage)
  interestRate?: number;            // APR as decimal, e.g. 0.0649 for 6.49%
  monthlyPayment?: number;
  maturityDate?: string;
  linkedAssetId?: string;           // FK to asset (mortgage → RealEstate, auto loan → Vehicle)
  linkedAssetName?: string;         // denormalized for display
}

export interface CreateAccountRequest {
  entityId: string;
  name: string;
  institutionName: string;
  type: AccountType;
  subtype?: AccountSubtype;
  mask?: string;
  currentBalance?: number;
  isoCurrencyCode?: string;         // defaults to "USD"
  interestRate?: number;
  monthlyPayment?: number;
  maturityDate?: string;
  linkedAssetId?: string;
}

export type UpdateAccountRequest = Partial<CreateAccountRequest>;

export const ACCOUNT_TYPE_LABELS: Record<AccountType, string> = {
  Checking: "Checking",
  Savings: "Savings",
  CreditCard: "Credit Card",
  Investment: "Investment",
  Loan: "Loan",
  Mortgage: "Mortgage",
  Other: "Other",
};

export const ACCOUNT_SUBTYPE_LABELS: Record<AccountSubtype, string> = {
  Regular: "Regular",
  MoneyMarket: "Money Market",
  CertificateOfDeposit: "Certificate of Deposit",
  HighYield: "High Yield",
  Brokerage: "Brokerage",
  Ira: "Traditional IRA",
  Roth: "Roth IRA",
  FiveZeroOneK: "401(k)",
  Auto: "Auto Loan",
  Personal: "Personal Loan",
  Student: "Student Loan",
  HELOC: "HELOC",
  PolicyLoan: "Policy Loan",
  Rewards: "Rewards",
  Business: "Business",
  Other: "Other",
};

/** Which account types represent liabilities (negative balance expected) */
export const LIABILITY_TYPES: AccountType[] = ["CreditCard", "Loan", "Mortgage"];

/** Which asset types can be linked from each liability subtype */
export const ASSET_LINK_RULES: Record<string, string[]> = {
  Mortgage: ["RealEstate"],
  HELOC: ["RealEstate"],
  Auto: ["Vehicle"],
  PolicyLoan: ["Insurance"],
  // Personal, Student, Rewards, Business, Other — no asset link available
};

import {
  Landmark, PiggyBank, CreditCard, TrendingUp, HandCoins, Home, HelpCircle,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";

export const ACCOUNT_TYPE_ICONS: Record<AccountType, LucideIcon> = {
  Checking: Landmark,
  Savings: PiggyBank,
  CreditCard: CreditCard,
  Investment: TrendingUp,
  Loan: HandCoins,
  Mortgage: Home,
  Other: HelpCircle,
};
```

---

## Data Types — Transactions

Create `src/types/transactions.ts`:

```typescript
export type TransactionType = "Place" | "Digital" | "Special" | "Unresolved";

export type TaxCategory =
  | "NotTaxRelated"
  | "BusinessExpense"
  | "MedicalDental"
  | "CharitableDonation"
  | "HomeOffice"
  | "Education"
  | "InvestmentExpense"
  | "RentalProperty"
  | "SelfEmployment"
  | "OtherDeduction";

export const TAX_CATEGORY_LABELS: Record<TaxCategory, string> = {
  NotTaxRelated: "Not Tax Related",
  BusinessExpense: "Business Expense",
  MedicalDental: "Medical / Dental",
  CharitableDonation: "Charitable Donation",
  HomeOffice: "Home Office",
  Education: "Education",
  InvestmentExpense: "Investment Expense",
  RentalProperty: "Rental Property",
  SelfEmployment: "Self-Employment",
  OtherDeduction: "Other Deduction",
};

export interface TransactionDto {
  id: string;
  entityId: string;
  linkedAccountId: string;
  accountName: string;
  amount: number;                   // positive = debit (money out), negative = credit (money in)
  isoCurrencyCode: string;
  date: string;                     // ISO date
  authorizedDate?: string;
  name: string;
  merchantName?: string;
  primaryCategory?: string;
  detailedCategory?: string;
  categoryIconUrl?: string;
  userCategoryOverride?: string;
  paymentChannel: string;           // "online" | "in store" | "other"
  transactionType: TransactionType;
  isPending: boolean;
  userNotes?: string;
  userTags?: string[];              // e.g. ["tax-deductible", "business"]
  isHidden: boolean;
  // Tax tracking
  userTaxCategory?: TaxCategory;
  // Receipt attachments
  receiptUrls?: string[];           // Azure Blob URLs for receipt images
  // Promo financing (for credit cards)
  hasPromoFinancing: boolean;
  promoRate?: number;
  promoExpirationDate?: string;
  postPromoRate?: number;
}

export interface TransactionSummaryDto {
  entityId: string;
  totalIncome: number;
  totalExpenses: number;
  netCashFlow: number;
  transactionCount: number;
  periodStart: string;
  periodEnd: string;
  spendingByCategory: CategoryBreakdownDto[];
  topMerchants: MerchantBreakdownDto[];
}

export interface CategoryBreakdownDto {
  category: string;
  amount: number;
  percentage: number;
  transactionCount: number;
}

export interface MerchantBreakdownDto {
  merchantName: string;
  amount: number;
  transactionCount: number;
}

export interface PaginatedTransactionsDto {
  entityId: string;
  transactions: TransactionDto[];
  totalCount: number;
  nextCursor?: string;
  hasMore: boolean;
}

export interface TransactionQueryParams {
  entityId: string;
  accountId?: string;
  from?: string;
  to?: string;
  category?: string;
  search?: string;
  taxCategory?: TaxCategory;
  limit?: number;
  cursor?: string;
}

export interface UpdateTransactionRequest {
  entityId: string;
  userCategoryOverride?: string;
  userNotes?: string;
  userTags?: string[];
  userTaxCategory?: TaxCategory;
  receiptUrls?: string[];
  isHidden?: boolean;
}

/** Colors for the spending-by-category donut chart */
export const CATEGORY_COLORS: Record<string, string> = {
  "Food & Drink": "#f97316",
  "Shopping": "#8b5cf6",
  "Transportation": "#3b82f6",
  "Housing": "#10b981",
  "Entertainment": "#ec4899",
  "Healthcare": "#ef4444",
  "Personal Care": "#14b8a6",
  "Education": "#6366f1",
  "Travel": "#f59e0b",
  "Income": "#22c55e",
  "Transfer": "#6b7280",
  default: "#94a3b8",
};
```

---

## Mock Data — Accounts

Create `src/data/mock-accounts.ts`. Include a mix of positive-balance accounts and liabilities (negative balance). Some liabilities link to existing mock assets by ID. All mock accounts include an `entityId` field.

> **Entity-type variation**: In real usage, different entity types have different account profiles:
> - **Personal**: Personal checking, savings, credit cards, retirement accounts (IRA, 401k)
> - **Business**: Business checking, business savings, merchant account, business credit card
> - **Trust**: Trust checking, trust investment account
>
> The mock data below represents a personal entity. When building for real data, the accounts shown will vary based on the entity selected in the sidebar.

```typescript
import type { LinkedAccountDto } from "@/types/accounts";

const MOCK_ENTITY_ID = "entity-personal-001"; // All mock data scoped to this entity

export const mockAccounts: LinkedAccountDto[] = [
  {
    id: "acct-001",
    entityId: MOCK_ENTITY_ID,
    name: "Everyday Checking",
    institutionName: "Chase",
    type: "Checking",
    subtype: "Regular",
    mask: "4829",
    currentBalance: 15420,
    availableBalance: 15220,
    isoCurrencyCode: "USD",
    source: "Plaid",
    lastSyncAt: "2026-03-08T09:00:00Z",
    createdAt: "2025-01-15T08:00:00Z",
  },
  {
    id: "acct-002",
    entityId: MOCK_ENTITY_ID,
    name: "High-Yield Savings",
    institutionName: "Marcus by Goldman Sachs",
    type: "Savings",
    subtype: "HighYield",
    mask: "7731",
    currentBalance: 45000,
    isoCurrencyCode: "USD",
    source: "Manual",
    createdAt: "2025-02-01T10:00:00Z",
  },
  {
    id: "acct-003",
    entityId: MOCK_ENTITY_ID,
    name: "Brokerage Account",
    institutionName: "Fidelity",
    type: "Investment",
    subtype: "Brokerage",
    mask: "3388",
    currentBalance: 312000,
    isoCurrencyCode: "USD",
    source: "Plaid",
    lastSyncAt: "2026-03-08T09:00:00Z",
    createdAt: "2025-01-20T12:00:00Z",
  },
  {
    id: "acct-004",
    entityId: MOCK_ENTITY_ID,
    name: "Amex Platinum",
    institutionName: "American Express",
    type: "CreditCard",
    subtype: "Rewards",
    mask: "1004",
    currentBalance: -2340,
    isoCurrencyCode: "USD",
    source: "Manual",
    interestRate: 0.2499,
    monthlyPayment: 250,
    createdAt: "2025-03-10T14:00:00Z",
  },
  {
    id: "acct-005",
    entityId: MOCK_ENTITY_ID,
    name: "Home Mortgage",
    officialName: "30-Year Fixed Mortgage",
    institutionName: "Wells Fargo",
    type: "Mortgage",
    mask: "9201",
    currentBalance: -287000,
    isoCurrencyCode: "USD",
    source: "Manual",
    interestRate: 0.0399,
    monthlyPayment: 1850,
    maturityDate: "2053-06-01",
    linkedAssetId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",  // → Primary Residence
    linkedAssetName: "Primary Residence",
    createdAt: "2025-01-15T08:30:00Z",
  },
  {
    id: "acct-006",
    entityId: MOCK_ENTITY_ID,
    name: "Auto Loan",
    institutionName: "Toyota Financial",
    type: "Loan",
    subtype: "Auto",
    currentBalance: -18500,
    isoCurrencyCode: "USD",
    source: "Manual",
    interestRate: 0.0449,
    monthlyPayment: 485,
    maturityDate: "2027-03-01",
    linkedAssetId: "b2c3d4e5-f6a7-8901-bcde-f12345678901",  // → 2022 Tesla Model 3
    linkedAssetName: "2022 Tesla Model 3",
    createdAt: "2025-04-01T09:00:00Z",
  },
  {
    id: "acct-007",
    entityId: MOCK_ENTITY_ID,
    name: "Student Loan",
    institutionName: "SoFi",
    type: "Loan",
    subtype: "Student",
    currentBalance: -32000,
    isoCurrencyCode: "USD",
    source: "Manual",
    interestRate: 0.0525,
    monthlyPayment: 380,
    maturityDate: "2033-09-01",
    createdAt: "2025-05-12T11:00:00Z",
  },
  {
    id: "acct-008",
    entityId: MOCK_ENTITY_ID,
    name: "Personal Line of Credit",
    institutionName: "Discover",
    type: "Loan",
    subtype: "Personal",
    currentBalance: -5200,
    isoCurrencyCode: "USD",
    source: "Manual",
    interestRate: 0.1199,
    monthlyPayment: 200,
    createdAt: "2025-08-20T16:00:00Z",
  },
];
```

---

## Mock Data — Transactions

Create `src/data/mock-transactions.ts`. Include 25+ transactions across accounts with varied categories, amounts, and dates in the last 30 days. Mix of income (negative amounts = money in) and expenses (positive amounts = money out) following Plaid convention. All mock transactions include an `entityId` field matching the mock entity.

```typescript
import type { TransactionDto, TransactionSummaryDto, CategoryBreakdownDto } from "@/types/transactions";

const MOCK_ENTITY_ID = "entity-personal-001"; // Must match mock-accounts.ts

export const mockTransactions: TransactionDto[] = [
  {
    id: "tx-001", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: -6400, isoCurrencyCode: "USD", date: "2026-03-01",
    name: "Direct Deposit - Employer", merchantName: "Acme Corp",
    primaryCategory: "Income", detailedCategory: "Payroll",
    paymentChannel: "other", transactionType: "Special",
    isPending: false, isHidden: false, hasPromoFinancing: false,
    userTaxCategory: "NotTaxRelated",
  },
  {
    id: "tx-002", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 2150, isoCurrencyCode: "USD", date: "2026-03-01",
    name: "Wells Fargo Mortgage", merchantName: "Wells Fargo",
    primaryCategory: "Housing", detailedCategory: "Mortgage Payment",
    paymentChannel: "other", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
    userTaxCategory: "NotTaxRelated",
  },
  {
    id: "tx-003", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-004", accountName: "Amex Platinum",
    amount: 47.99, isoCurrencyCode: "USD", date: "2026-03-07",
    name: "AMZN Mktp US*AB1CD2EF3", merchantName: "Amazon",
    primaryCategory: "Shopping", detailedCategory: "Online Marketplace",
    paymentChannel: "online", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
    userNotes: "Office supplies for Q1",
    userTags: ["tax-deductible", "business"],
    userTaxCategory: "BusinessExpense",
    receiptUrls: ["https://placeholder.blob.core.windows.net/receipts/amazon-mar7.jpg"],
  },
  {
    id: "tx-004", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 185.00, isoCurrencyCode: "USD", date: "2026-03-05",
    name: "State Farm Insurance", merchantName: "State Farm",
    primaryCategory: "Housing", detailedCategory: "Insurance",
    paymentChannel: "other", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-005", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 62.38, isoCurrencyCode: "USD", date: "2026-03-06",
    name: "Whole Foods Market #10234", merchantName: "Whole Foods",
    primaryCategory: "Food & Drink", detailedCategory: "Groceries",
    paymentChannel: "in store", transactionType: "Place",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-006", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-004", accountName: "Amex Platinum",
    amount: 14.99, isoCurrencyCode: "USD", date: "2026-03-06",
    name: "Netflix", merchantName: "Netflix",
    primaryCategory: "Entertainment", detailedCategory: "Streaming",
    paymentChannel: "online", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-007", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 45.00, isoCurrencyCode: "USD", date: "2026-03-04",
    name: "Shell Oil #12847", merchantName: "Shell",
    primaryCategory: "Transportation", detailedCategory: "Gas",
    paymentChannel: "in store", transactionType: "Place",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-008", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 485.00, isoCurrencyCode: "USD", date: "2026-03-01",
    name: "Toyota Financial Services", merchantName: "Toyota Financial",
    primaryCategory: "Transportation", detailedCategory: "Car Payment",
    paymentChannel: "other", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-009", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 380.00, isoCurrencyCode: "USD", date: "2026-03-01",
    name: "SoFi Student Loan Payment", merchantName: "SoFi",
    primaryCategory: "Education", detailedCategory: "Student Loan Payment",
    paymentChannel: "other", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
    userTaxCategory: "Education",
  },
  {
    id: "tx-010", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-004", accountName: "Amex Platinum",
    amount: 89.50, isoCurrencyCode: "USD", date: "2026-03-03",
    name: "CVS Pharmacy #4521", merchantName: "CVS Pharmacy",
    primaryCategory: "Healthcare", detailedCategory: "Pharmacy",
    paymentChannel: "in store", transactionType: "Place",
    isPending: false, isHidden: false, hasPromoFinancing: false,
    userTaxCategory: "MedicalDental",
    receiptUrls: ["https://placeholder.blob.core.windows.net/receipts/cvs-mar3.jpg"],
  },
  {
    id: "tx-011", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: -1200, isoCurrencyCode: "USD", date: "2026-03-05",
    name: "Freelance Payment - Client XYZ",
    primaryCategory: "Income", detailedCategory: "Freelance",
    paymentChannel: "other", transactionType: "Special",
    isPending: false, isHidden: false, hasPromoFinancing: false,
    userTaxCategory: "SelfEmployment",
  },
  {
    id: "tx-012", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 125.00, isoCurrencyCode: "USD", date: "2026-03-02",
    name: "AT&T Wireless", merchantName: "AT&T",
    primaryCategory: "Housing", detailedCategory: "Telecommunications",
    paymentChannel: "online", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-013", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 85.00, isoCurrencyCode: "USD", date: "2026-03-04",
    name: "Austin Energy", merchantName: "Austin Energy",
    primaryCategory: "Housing", detailedCategory: "Utilities",
    paymentChannel: "other", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-014", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-004", accountName: "Amex Platinum",
    amount: 32.50, isoCurrencyCode: "USD", date: "2026-03-07",
    name: "Uber *Trip", merchantName: "Uber",
    primaryCategory: "Transportation", detailedCategory: "Rideshare",
    paymentChannel: "online", transactionType: "Digital",
    isPending: true, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-015", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-003", accountName: "Brokerage Account",
    amount: -340, isoCurrencyCode: "USD", date: "2026-03-03",
    name: "Dividend - VTSAX", merchantName: "Vanguard",
    primaryCategory: "Income", detailedCategory: "Dividend",
    paymentChannel: "other", transactionType: "Special",
    isPending: false, isHidden: false, hasPromoFinancing: false,
    userTaxCategory: "InvestmentExpense",
  },
  {
    id: "tx-016", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 250.00, isoCurrencyCode: "USD", date: "2026-03-02",
    name: "Habitat for Humanity", merchantName: "Habitat for Humanity",
    primaryCategory: "Shopping", detailedCategory: "Donation",
    paymentChannel: "online", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
    userCategoryOverride: "Charitable Donation",
    userTaxCategory: "CharitableDonation",
    receiptUrls: ["https://placeholder.blob.core.windows.net/receipts/habitat-mar2.pdf"],
  },
  {
    id: "tx-017", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-004", accountName: "Amex Platinum",
    amount: 156.80, isoCurrencyCode: "USD", date: "2026-02-28",
    name: "H-E-B Grocery #312", merchantName: "H-E-B",
    primaryCategory: "Food & Drink", detailedCategory: "Groceries",
    paymentChannel: "in store", transactionType: "Place",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-018", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 200.00, isoCurrencyCode: "USD", date: "2026-03-01",
    name: "Discover Personal Loan", merchantName: "Discover",
    primaryCategory: "Transfer", detailedCategory: "Loan Payment",
    paymentChannel: "other", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-019", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 38.45, isoCurrencyCode: "USD", date: "2026-03-06",
    name: "Starbucks #8842", merchantName: "Starbucks",
    primaryCategory: "Food & Drink", detailedCategory: "Coffee Shop",
    paymentChannel: "in store", transactionType: "Place",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-020", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: -6400, isoCurrencyCode: "USD", date: "2026-02-15",
    name: "Direct Deposit - Employer", merchantName: "Acme Corp",
    primaryCategory: "Income", detailedCategory: "Payroll",
    paymentChannel: "other", transactionType: "Special",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-021", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-004", accountName: "Amex Platinum",
    amount: 75.00, isoCurrencyCode: "USD", date: "2026-03-08",
    name: "Target #1234", merchantName: "Target",
    primaryCategory: "Shopping", detailedCategory: "Department Store",
    paymentChannel: "in store", transactionType: "Place",
    isPending: true, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-022", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-002", accountName: "High-Yield Savings",
    amount: -93.75, isoCurrencyCode: "USD", date: "2026-03-01",
    name: "Interest Payment", merchantName: "Marcus by Goldman Sachs",
    primaryCategory: "Income", detailedCategory: "Interest",
    paymentChannel: "other", transactionType: "Special",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-023", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 55.00, isoCurrencyCode: "USD", date: "2026-03-05",
    name: "Planet Fitness", merchantName: "Planet Fitness",
    primaryCategory: "Personal Care", detailedCategory: "Gym",
    paymentChannel: "other", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
  {
    id: "tx-024", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-001", accountName: "Everyday Checking",
    amount: 1500, isoCurrencyCode: "USD", date: "2026-03-01",
    name: "Transfer to Savings", merchantName: "Marcus by Goldman Sachs",
    primaryCategory: "Transfer", detailedCategory: "Savings Transfer",
    paymentChannel: "other", transactionType: "Special",
    isPending: false, isHidden: true, hasPromoFinancing: false,
  },
  {
    id: "tx-025", entityId: MOCK_ENTITY_ID, linkedAccountId: "acct-004", accountName: "Amex Platinum",
    amount: 22.99, isoCurrencyCode: "USD", date: "2026-03-04",
    name: "Spotify Premium", merchantName: "Spotify",
    primaryCategory: "Entertainment", detailedCategory: "Streaming",
    paymentChannel: "online", transactionType: "Digital",
    isPending: false, isHidden: false, hasPromoFinancing: false,
  },
];

export const mockTransactionSummary: TransactionSummaryDto = {
  entityId: MOCK_ENTITY_ID,
  totalIncome: 14433.75,
  totalExpenses: 6800.61,
  netCashFlow: 7633.14,
  transactionCount: 25,
  periodStart: "2026-02-15",
  periodEnd: "2026-03-08",
  spendingByCategory: [
    { category: "Housing", amount: 2360, percentage: 34.7, transactionCount: 3 },
    { category: "Transportation", amount: 562.50, percentage: 8.3, transactionCount: 3 },
    { category: "Food & Drink", amount: 257.63, percentage: 3.8, transactionCount: 3 },
    { category: "Shopping", amount: 355.49, percentage: 5.2, transactionCount: 3 },
    { category: "Entertainment", amount: 37.98, percentage: 0.6, transactionCount: 2 },
    { category: "Healthcare", amount: 89.50, percentage: 1.3, transactionCount: 1 },
    { category: "Education", amount: 380, percentage: 5.6, transactionCount: 1 },
    { category: "Transfer", amount: 1700, percentage: 25.0, transactionCount: 2 },
    { category: "Personal Care", amount: 55, percentage: 0.8, transactionCount: 1 },
  ],
  topMerchants: [
    { merchantName: "Wells Fargo", amount: 2150, transactionCount: 1 },
    { merchantName: "Toyota Financial", amount: 485, transactionCount: 1 },
    { merchantName: "SoFi", amount: 380, transactionCount: 1 },
    { merchantName: "Habitat for Humanity", amount: 250, transactionCount: 1 },
    { merchantName: "H-E-B", amount: 156.80, transactionCount: 1 },
  ],
};
```

---

## API Services

All API endpoints are entity-scoped. The URL pattern is `/api/entities/{entityId}/accounts` and `/api/entities/{entityId}/transactions`. TanStack Query keys include `entityId` to ensure proper cache isolation between entities.

### Account Service

Create `src/services/account-service.ts` following the exact same pattern as `asset-service.ts` — mock data with simulated delay, swappable to real fetch when API is ready.

**Real API endpoints** (mock for now, swap later):
- `GET /api/entities/{entityId}/accounts` — list accounts for entity
- `GET /api/entities/{entityId}/accounts/{id}` — get single account
- `POST /api/entities/{entityId}/accounts` — create account
- `PUT /api/entities/{entityId}/accounts/{id}` — update account
- `DELETE /api/entities/{entityId}/accounts/{id}` — delete account
- `PUT /api/entities/{entityId}/accounts/{id}/link-asset` — link to asset
- `DELETE /api/entities/{entityId}/accounts/{id}/link-asset` — unlink from asset

```typescript
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type { LinkedAccountDto, CreateAccountRequest, UpdateAccountRequest } from "@/types/accounts";
import { mockAccounts } from "@/data/mock-accounts";

const delay = (ms = 400) => new Promise((r) => setTimeout(r, ms));

export async function getAccounts(entityId: string, params?: { type?: string }): Promise<LinkedAccountDto[]> {
  await delay();
  let result = mockAccounts.filter((a) => a.entityId === entityId);
  if (params?.type) result = result.filter((a) => a.type === params.type);
  return result;
}

export async function getAccount(entityId: string, id: string): Promise<LinkedAccountDto> {
  await delay();
  const account = mockAccounts.find((a) => a.id === id && a.entityId === entityId);
  if (!account) throw new Error("ACCOUNT_NOT_FOUND");
  return account;
}

export async function createAccount(entityId: string, data: CreateAccountRequest): Promise<LinkedAccountDto> {
  await delay(600);
  const newAccount: LinkedAccountDto = {
    ...data,
    id: crypto.randomUUID(),
    entityId,
    isoCurrencyCode: data.isoCurrencyCode ?? "USD",
    source: "Manual",
    createdAt: new Date().toISOString(),
  };
  mockAccounts.push(newAccount);
  return newAccount;
}

export async function updateAccount(entityId: string, id: string, data: UpdateAccountRequest): Promise<LinkedAccountDto> {
  await delay(600);
  const idx = mockAccounts.findIndex((a) => a.id === id && a.entityId === entityId);
  if (idx === -1) throw new Error("ACCOUNT_NOT_FOUND");
  const updated = { ...mockAccounts[idx], ...data };
  mockAccounts[idx] = updated;
  return updated;
}

export async function deleteAccount(entityId: string, id: string): Promise<void> {
  await delay(400);
  const idx = mockAccounts.findIndex((a) => a.id === id && a.entityId === entityId);
  if (idx === -1) throw new Error("ACCOUNT_NOT_FOUND");
  mockAccounts.splice(idx, 1);
}

export async function linkAccountToAsset(entityId: string, accountId: string, assetId: string, assetName: string): Promise<LinkedAccountDto> {
  await delay(400);
  const idx = mockAccounts.findIndex((a) => a.id === accountId && a.entityId === entityId);
  if (idx === -1) throw new Error("ACCOUNT_NOT_FOUND");
  mockAccounts[idx] = { ...mockAccounts[idx], linkedAssetId: assetId, linkedAssetName: assetName };
  return mockAccounts[idx];
}

export async function unlinkAccountFromAsset(entityId: string, accountId: string): Promise<LinkedAccountDto> {
  await delay(400);
  const idx = mockAccounts.findIndex((a) => a.id === accountId && a.entityId === entityId);
  if (idx === -1) throw new Error("ACCOUNT_NOT_FOUND");
  const { linkedAssetId, linkedAssetName, ...rest } = mockAccounts[idx];
  mockAccounts[idx] = rest as LinkedAccountDto;
  return mockAccounts[idx];
}

// --- TanStack Query hooks (entityId-scoped) ---

export function useAccounts(entityId: string, params?: { type?: string }) {
  return useQuery({
    queryKey: ["entities", entityId, "accounts", params],
    queryFn: () => getAccounts(entityId, params),
    enabled: !!entityId,
  });
}

export function useAccount(entityId: string, id: string) {
  return useQuery({
    queryKey: ["entities", entityId, "accounts", id],
    queryFn: () => getAccount(entityId, id),
    enabled: !!entityId && !!id,
  });
}

export function useCreateAccount(entityId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateAccountRequest) => createAccount(entityId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["entities", entityId, "accounts"] }),
  });
}

export function useUpdateAccount(entityId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAccountRequest }) => updateAccount(entityId, id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["entities", entityId, "accounts"] }),
  });
}

export function useDeleteAccount(entityId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteAccount(entityId, id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["entities", entityId, "accounts"] });
      qc.invalidateQueries({ queryKey: ["entities", entityId, "transactions"] });
    },
  });
}

export function useLinkAccountToAsset(entityId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ accountId, assetId, assetName }: { accountId: string; assetId: string; assetName: string }) =>
      linkAccountToAsset(entityId, accountId, assetId, assetName),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["entities", entityId, "accounts"] }),
  });
}
```

### Transaction Service

Create `src/services/transaction-service.ts`:

**Real API endpoints** (mock for now, swap later):
- `GET /api/entities/{entityId}/transactions` — list transactions for entity (with query params)
- `GET /api/entities/{entityId}/transactions/summary` — get transaction summary
- `PUT /api/entities/{entityId}/transactions/{id}` — update transaction annotations

```typescript
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type {
  TransactionDto, TransactionSummaryDto, PaginatedTransactionsDto,
  TransactionQueryParams, UpdateTransactionRequest,
} from "@/types/transactions";
import { mockTransactions, mockTransactionSummary } from "@/data/mock-transactions";

const delay = (ms = 400) => new Promise((r) => setTimeout(r, ms));

export async function getTransactions(entityId: string, params?: Omit<TransactionQueryParams, "entityId">): Promise<PaginatedTransactionsDto> {
  await delay();
  let result = mockTransactions.filter((t) => t.entityId === entityId);
  if (params?.accountId && params.accountId !== "all")
    result = result.filter((t) => t.linkedAccountId === params.accountId);
  if (params?.category)
    result = result.filter((t) => (t.userCategoryOverride ?? t.primaryCategory) === params.category);
  if (params?.taxCategory)
    result = result.filter((t) => t.userTaxCategory === params.taxCategory);
  if (params?.search) {
    const q = params.search.toLowerCase();
    result = result.filter((t) =>
      t.name.toLowerCase().includes(q) ||
      t.merchantName?.toLowerCase().includes(q) ||
      t.userNotes?.toLowerCase().includes(q)
    );
  }
  if (params?.from) result = result.filter((t) => t.date >= params.from!);
  if (params?.to) result = result.filter((t) => t.date <= params.to!);
  // Exclude hidden by default
  result = result.filter((t) => !t.isHidden);
  // Sort by date descending
  result.sort((a, b) => b.date.localeCompare(a.date));
  return { entityId, transactions: result, totalCount: result.length, hasMore: false };
}

export async function getTransactionSummary(entityId: string, params?: { from?: string; to?: string; accountId?: string }): Promise<TransactionSummaryDto> {
  await delay();
  return { ...mockTransactionSummary, entityId };
}

export async function updateTransaction(entityId: string, id: string, data: Omit<UpdateTransactionRequest, "entityId">): Promise<TransactionDto> {
  await delay(600);
  const idx = mockTransactions.findIndex((t) => t.id === id && t.entityId === entityId);
  if (idx === -1) throw new Error("TRANSACTION_NOT_FOUND");
  mockTransactions[idx] = {
    ...mockTransactions[idx],
    ...data,
    userTaxCategory: data.userTaxCategory ?? mockTransactions[idx].userTaxCategory,
    receiptUrls: data.receiptUrls ?? mockTransactions[idx].receiptUrls,
  };
  return mockTransactions[idx];
}

// --- TanStack Query hooks (entityId-scoped) ---

export function useTransactions(entityId: string, params?: Omit<TransactionQueryParams, "entityId">) {
  return useQuery({
    queryKey: ["entities", entityId, "transactions", params],
    queryFn: () => getTransactions(entityId, params),
    enabled: !!entityId,
  });
}

export function useTransactionSummary(entityId: string, params?: { from?: string; to?: string; accountId?: string }) {
  return useQuery({
    queryKey: ["entities", entityId, "transactions", "summary", params],
    queryFn: () => getTransactionSummary(entityId, params),
    enabled: !!entityId,
  });
}

export function useUpdateTransaction(entityId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: Omit<UpdateTransactionRequest, "entityId"> & { id: string }) =>
      updateTransaction(entityId, id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["entities", entityId, "transactions"] }),
  });
}
```

---

## Accounts Page (`/:entityType/:slug/accounts`)

Create `src/pages/Accounts.tsx`. Follow the **exact same patterns** as the existing `Assets.tsx` page — same component structure, same styling, same state management. The page uses `useEntityContext()` to get `entityId` for all data fetching.

### Page Structure

```
┌─────────────────────────────────────────────────────────────────┐
│  Accounts                                    [+ Add Account]   │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐          │
│  │ Total    │ │ Total    │ │ Net      │ │ Accounts │          │
│  │ Balance  │ │ Debt     │ │ Position │ │ Count    │          │
│  │ $372,420 │ │ -$345,040│ │ $27,380  │ │ 8        │          │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘          │
├─────────────────────────────────────────────────────────────────┤
│  (All) (Checking) (Savings) (Credit Card) (Loan) (Mortgage)   │
│                                                  [≡] [⊞]      │
├─────────────────────────────────────────────────────────────────┤
│  TABLE VIEW or CARD GRID (toggled)                             │
└─────────────────────────────────────────────────────────────────┘
```

### Summary Cards

Same pattern as Assets `SummaryCards`. Compute from the accounts array:

- **Total Balance**: sum of all accounts where `currentBalance > 0` → accent (primary color)
- **Total Debt**: sum of all accounts where `currentBalance < 0` → displayed as negative, destructive color
- **Net Position**: Total Balance + Total Debt → green if positive, red if negative
- **Accounts**: count of all accounts

### Filter Tabs

Same radio-button pattern as Assets. Filter values:

```typescript
const FILTER_VALUES: (AccountType | "All")[] = [
  "All", "Checking", "Savings", "CreditCard", "Investment", "Loan", "Mortgage", "Other",
];
```

### View Toggle

Same `card` / `table` toggle with `localStorage.getItem("accounts-view-mode")` persistence.

### Table View

Same component pattern as `AssetTable`:

| Column | Content |
|--------|---------|
| Institution | Colored circle (first letter) + institution name |
| Account | Name + mask (••••1234 in muted text) |
| Type | Badge with account type label |
| Balance | Mono font, green for positive, red for negative |
| Linked Asset | Asset name as link (or "—" if none) |
| Rate | APR displayed for liabilities (or "—") |
| Source | "Linked" badge (emerald) for Plaid, "Manual" badge (muted) |
| Actions | 3-dot menu |

### Card Grid

Same pattern as `CardGrid` in Assets. Each card shows:

- **Institution logo placeholder** (colored circle with first letter, use `ACCOUNT_TYPE_ICONS` for the icon)
- **Account name** (truncated) + institution name (muted text)
- **Account mask** (••••1234) if available
- **Current balance** (large, mono font) — green for positive, red for negative
- For liabilities: **APR** and **Monthly payment** in small muted text below balance
- **Linked Asset** badge if `linkedAssetName` exists (clickable, navigates to `entityPath("assets")`)
- **Source badge**: "Linked" (emerald) or "Manual" (muted)
- **3-dot menu** (same ActionsMenu pattern)

### 3-Dot Actions Menu

```typescript
function AccountActionsMenu({ account, onEdit, onDelete, onLinkAsset }: { ... }) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="h-8 w-8" aria-label={t("actions.moreActions")}>
          <MoreHorizontal className="w-4 h-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => onEdit(account)}>
          <Pencil className="w-4 h-4 mr-2" /> {t("actions.edit")}
        </DropdownMenuItem>
        {/* Only show "Link to Asset" for liability types that support it */}
        {LIABILITY_TYPES.includes(account.type) && (
          <DropdownMenuItem onClick={() => onLinkAsset(account)}>
            <Link2 className="w-4 h-4 mr-2" />
            {account.linkedAssetId ? t("actions.changeLinkedAsset") : t("actions.linkToAsset")}
          </DropdownMenuItem>
        )}
        {account.linkedAssetId && (
          <DropdownMenuItem onClick={() => onUnlinkAsset(account)}>
            <Unlink className="w-4 h-4 mr-2" /> {t("actions.unlinkAsset")}
          </DropdownMenuItem>
        )}
        {account.source === "Plaid" && (
          <DropdownMenuItem>
            <RefreshCw className="w-4 h-4 mr-2" /> {t("actions.refresh")}
          </DropdownMenuItem>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem className="text-destructive focus:text-destructive" onClick={() => onDelete(account)}>
          <Trash2 className="w-4 h-4 mr-2" /> {t("actions.delete")}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
```

### Add/Edit Account Sheet

Use a `Sheet` component (same pattern as `AssetFormSheet`). The form adapts based on account type:

**Base fields (all types):**
- Account Name (required, text input)
- Institution Name (required, text input)
- Account Type (required, Select dropdown)
- Account Subtype (Select dropdown, filter options by selected type)
- Last 4 Digits (optional, max 4 chars, text input)
- Current Balance (optional, currency input)
- Currency (Select, default "USD")

**Liability fields (shown when type is CreditCard, Loan, or Mortgage):**
- Interest Rate (number input, displayed as %, stored as decimal)
- Monthly Payment (currency input)
- Maturity Date (date picker)
- **Link to Asset** (Select dropdown):
  - Only shown for subtypes that support asset linking (see `ASSET_LINK_RULES`)
  - Filter the dropdown to show only assets matching the rule (e.g., Mortgage → only RealEstate assets, Auto → only Vehicle assets)
  - Fetch available assets from `useAssets()` hook
  - Show "No linkable assets found" if none match

### Link to Asset Dialog

When triggered from the 3-dot menu, open a `Dialog` with:
- Title: "Link Account to Asset"
- Asset picker dropdown (filtered by `ASSET_LINK_RULES` based on account type/subtype)
- Shows asset name + type + current value in each option
- Cancel / Link buttons

### Delete Confirmation

Same `AlertDialog` pattern as Assets. Warning text should mention that deleting an account **permanently removes all its transaction history**.

### Empty State

```
┌────────────────────────────────┐
│        🏦 (Wallet icon)       │
│                                │
│    No accounts yet             │
│                                │
│    Add your bank accounts,     │
│    credit cards, and loans     │
│    to track your full          │
│    financial picture.          │
│                                │
│    [+ Add Your First Account]  │
└────────────────────────────────┘
```

### Skeleton Loading

Same pattern as Assets — 3 card-shaped skeletons while loading.

---

## Transactions Page (`/:entityType/:slug/transactions`)

Create `src/pages/Transactions.tsx`. This page has a different layout from Assets — it has a filter bar instead of filter tabs, a spending chart, and a transaction detail sheet. The page uses `useEntityContext()` to get `entityId` for all data fetching.

**Install recharts**: `npm install recharts` (for the spending donut chart).

### Page Structure

```
┌─────────────────────────────────────────────────────────────────┐
│  Transactions                                                   │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐          │
│  │ Income   │ │ Spending │ │ Net Cash │ │ Count    │          │
│  │ $14,434  │ │ $6,801   │ │ +$7,633  │ │ 25       │          │
│  │ ↙ green  │ │ ↗ red    │ │ ↑ green  │ │ 🧾       │          │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘          │
├─────────────────────────────────────────────────────────────────┤
│  [Account ▾]  [Date Range]  [Category ▾]  [🔍 Search...]      │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────┐ ┌──────────────────────────┐      │
│  │   TRANSACTION TABLE     │ │  SPENDING BY CATEGORY    │      │
│  │   (desktop: table)      │ │  (donut chart)           │      │
│  │   (mobile: cards)       │ │                          │      │
│  │                         │ │  🍕 Food & Drink   3.8%  │      │
│  │                         │ │  🏠 Housing        34.7% │      │
│  │                         │ │  🛍️ Shopping       5.2%  │      │
│  │                         │ │  ...                     │      │
│  └─────────────────────────┘ └──────────────────────────┘      │
└─────────────────────────────────────────────────────────────────┘
```

On desktop, the transaction list takes 2/3 width and the chart takes 1/3. On mobile, chart goes below the list.

```tsx
<div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
  <div className="lg:col-span-2">
    {/* Transaction table/cards */}
  </div>
  <div>
    {/* Spending by category chart */}
  </div>
</div>
```

### Summary Cards

Same grid pattern as Accounts summary. Data comes from `useTransactionSummary()`:

| Card | Value | Icon | Color |
|------|-------|------|-------|
| Income | `summary.totalIncome` | `ArrowDownLeft` | Green |
| Spending | `summary.totalExpenses` | `ArrowUpRight` | Red/destructive |
| Net Cash Flow | `summary.netCashFlow` | `TrendingUp` or `TrendingDown` | Green if positive, red if negative |
| Transactions | `summary.transactionCount` | `Receipt` | Muted |

### Filter Bar

A responsive row of filter controls — stacks vertically on mobile, horizontal on desktop:

```tsx
<div className="flex flex-col md:flex-row gap-3" role="search" aria-label={t("filters.label")}>
  {/* Account selector */}
  <Select value={accountId} onValueChange={setAccountId}>
    <SelectTrigger className="w-full md:w-48" aria-label={t("filters.account")}>
      <SelectValue placeholder={t("filters.allAccounts")} />
    </SelectTrigger>
    <SelectContent>
      <SelectItem value="all">{t("filters.allAccounts")}</SelectItem>
      {accounts?.map(a => <SelectItem key={a.id} value={a.id}>{a.name}</SelectItem>)}
    </SelectContent>
  </Select>

  {/* Category filter */}
  <Select value={category} onValueChange={setCategory}>
    <SelectTrigger className="w-full md:w-48" aria-label={t("filters.category")}>
      <SelectValue placeholder={t("filters.allCategories")} />
    </SelectTrigger>
    <SelectContent>
      <SelectItem value="all">{t("filters.allCategories")}</SelectItem>
      {CATEGORIES.map(c => <SelectItem key={c} value={c}>{c}</SelectItem>)}
    </SelectContent>
  </Select>

  {/* Tax category filter */}
  <Select value={taxCategory} onValueChange={setTaxCategory}>
    <SelectTrigger className="w-full md:w-48" aria-label={t("filters.taxCategory")}>
      <SelectValue placeholder={t("filters.allTaxCategories")} />
    </SelectTrigger>
    <SelectContent>
      <SelectItem value="all">{t("filters.allTaxCategories")}</SelectItem>
      {Object.entries(TAX_CATEGORY_LABELS).map(([key, label]) =>
        <SelectItem key={key} value={key}>{label}</SelectItem>
      )}
    </SelectContent>
  </Select>

  {/* Search */}
  <div className="relative flex-1">
    <label htmlFor="transaction-search" className="sr-only">{t("filters.search")}</label>
    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" aria-hidden="true" />
    <Input id="transaction-search" placeholder={t("filters.searchPlaceholder")}
           className="pl-9" value={search} onChange={e => setSearch(e.target.value)} />
  </div>
</div>
```

Categories list for the filter dropdown:
```typescript
const CATEGORIES = [
  "Food & Drink", "Shopping", "Transportation", "Housing", "Entertainment",
  "Healthcare", "Personal Care", "Education", "Travel", "Income", "Transfer",
];
```

### Transaction Table (Desktop)

Shown with `className="hidden md:block"`. Same Card + Table pattern:

| Column | Content |
|--------|---------|
| Date | Formatted date, muted text |
| Description | Category icon (colored circle) + merchant name + pending badge if applicable |
| Category | Badge with `userCategoryOverride ?? primaryCategory` |
| Account | Account name in muted text |
| Amount | Mono font, right-aligned. **Negative = green (income), Positive = neutral/red (expense)** — this follows Plaid convention |
| Actions | Small button to open detail sheet |

Pending transactions render at `opacity-60`.

### Transaction Cards (Mobile)

Shown with `className="md:hidden space-y-2"`. Each card:

```tsx
<Card className={tx.isPending ? "opacity-60" : ""}>
  <CardContent className="flex items-center gap-3 p-3">
    <div className="h-10 w-10 rounded-full bg-muted flex items-center justify-center shrink-0">
      <CategoryIcon category={tx.primaryCategory} />
    </div>
    <div className="flex-1 min-w-0">
      <div className="font-medium truncate">{tx.merchantName ?? tx.name}</div>
      <div className="text-xs text-muted-foreground">
        {formatDate(tx.date)} · {tx.accountName}
      </div>
    </div>
    <div className={cn("font-mono font-medium text-sm",
      tx.amount < 0 ? "text-emerald-500" : "text-foreground"
    )}>
      {formatCurrency(tx.amount)}
    </div>
  </CardContent>
</Card>
```

Click on a row/card opens the Transaction Detail Sheet.

### Spending by Category Chart

Use `recharts` `PieChart` in a Card. Include a screen-reader-accessible table alternative:

```tsx
import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from "recharts";
import { CATEGORY_COLORS } from "@/types/transactions";

<Card>
  <CardHeader>
    <CardTitle>{t("charts.spendingByCategory")}</CardTitle>
  </CardHeader>
  <CardContent>
    {/* Screen reader alternative */}
    <div className="sr-only" role="table" aria-label={t("charts.spendingByCategory")}>
      <div role="rowgroup">
        {summary.spendingByCategory.map(entry => (
          <div key={entry.category} role="row">
            <span role="cell">{entry.category}</span>
            <span role="cell">{formatCurrency(entry.amount)} ({entry.percentage}%)</span>
          </div>
        ))}
      </div>
    </div>
    <ResponsiveContainer width="100%" height={300} aria-hidden="true">
      <PieChart>
        <Pie data={summary.spendingByCategory} dataKey="amount" nameKey="category"
             cx="50%" cy="50%" innerRadius={60} outerRadius={100}>
          {summary.spendingByCategory.map((entry, i) => (
            <Cell key={i} fill={CATEGORY_COLORS[entry.category] ?? CATEGORY_COLORS.default} />
          ))}
        </Pie>
        <Tooltip formatter={(value: number) => formatCurrency(value)} />
        <Legend />
      </PieChart>
    </ResponsiveContainer>
  </CardContent>
</Card>
```

### Transaction Detail Sheet

Opens as a `Sheet` from the right side when clicking a transaction row/card. Contains full details and editable annotation fields:

```
┌──────────────────────────────────────┐
│  ← Back                       ⋯     │
│                                      │
│  ┌────────────────────────────────┐  │
│  │  🛒  Amazon.com               │  │
│  │  -$47.99                      │  │
│  │  March 7, 2026                │  │
│  └────────────────────────────────┘  │
│                                      │
│  Details                             │
│  ─────────────────────────────────   │
│  Account:    Amex Platinum           │
│  Category:   Shopping > Online       │
│  Channel:    Online                  │
│  Status:     Posted / Pending        │
│  Auth Date:  March 6, 2026           │
│                                      │
│  Your Annotations                    │
│  ─────────────────────────────────   │
│                                      │
│  Category Override:                  │
│  [Shopping ▾]                        │
│                                      │
│  Tax Category:                       │
│  [Business Expense ▾]               │
│                                      │
│  Notes:                              │
│  ┌────────────────────────────────┐  │
│  │ Office supplies for Q1        │  │
│  └────────────────────────────────┘  │
│                                      │
│  Tags:                               │
│  [tax-deductible] [business] [+]     │
│                                      │
│  Receipts:                           │
│  ┌──────┐                            │
│  │ 📎🖼️ │  amazon-mar7.jpg          │
│  └──────┘                            │
│  [+ Attach Receipt]                  │
│                                      │
│  ☐ Hide from reports                 │
│                                      │
│  [Save Changes]                      │
└──────────────────────────────────────┘
```

**Tax Category dropdown** — `Select` with all values from `TAX_CATEGORY_LABELS`. Default to "Not Tax Related". The label and dropdown have proper `htmlFor`/`id` association.

**Receipt Attachments section:**
- Show existing receipt thumbnails/filenames from `receiptUrls[]`
- "Attach Receipt" button (for now just shows a file input that captures the filename — actual Azure Blob upload is a future API feature)
- Each receipt has a small "×" remove button

**Tags section:**
- Display existing tags as `Badge` components
- Each badge has a small "×" to remove
- "+" button opens a small input to add a new tag
- Max 20 tags

**Save Changes** button calls `useUpdateTransaction()` mutation with all annotation fields.

### Empty State

```
┌────────────────────────────────────┐
│         🧾 (Receipt icon)         │
│                                    │
│    No transactions yet             │
│                                    │
│    Link a bank account or add      │
│    transactions manually to        │
│    start tracking your spending.   │
│                                    │
│    [Go to Accounts]               │
└────────────────────────────────────┘
```

"Go to Accounts" links to `entityPath("accounts")` (entity-relative, e.g., `/personal/accounts`).

---

## Internationalization (i18n)

All user-facing text must use translation keys via `useTranslation()`. No hardcoded strings.

### Accounts Translation File

Create `src/locales/en/accounts.json`:

```json
{
  "page": {
    "title": "Accounts | RAJ Financial",
    "heading": "Accounts",
    "addAccount": "Add Account",
    "viewModeLabel": "View mode",
    "cardView": "Card view",
    "tableView": "Table view",
    "filterLabel": "Filter by account type"
  },
  "filters": {
    "All": "All",
    "Checking": "Checking",
    "Savings": "Savings",
    "CreditCard": "Credit Card",
    "Investment": "Investment",
    "Loan": "Loan",
    "Mortgage": "Mortgage",
    "Other": "Other"
  },
  "summary": {
    "totalBalance": "Total Balance",
    "totalDebt": "Total Debt",
    "netPosition": "Net Position",
    "accountCount": "Accounts"
  },
  "table": {
    "institution": "Institution",
    "account": "Account",
    "type": "Type",
    "balance": "Balance",
    "linkedAsset": "Linked Asset",
    "rate": "APR",
    "source": "Source",
    "linked": "Linked",
    "manual": "Manual",
    "noLink": "—"
  },
  "actions": {
    "moreActions": "More actions",
    "edit": "Edit",
    "delete": "Delete",
    "refresh": "Refresh",
    "linkToAsset": "Link to Asset",
    "changeLinkedAsset": "Change Linked Asset",
    "unlinkAsset": "Unlink Asset"
  },
  "empty": {
    "title": "No accounts yet",
    "description": "Add your bank accounts, credit cards, and loans to track your full financial picture.",
    "addFirst": "Add Your First Account"
  },
  "deleteDialog": {
    "title": "Delete Account",
    "description": "Are you sure you want to delete \"{{name}}\"? This action cannot be undone. All associated transactions will also be permanently removed.",
    "cancel": "Cancel",
    "deleting": "Deleting...",
    "confirm": "Delete"
  },
  "linkDialog": {
    "title": "Link Account to Asset",
    "description": "Select an asset to link to \"{{name}}\".",
    "selectAsset": "Select an asset",
    "noAssets": "No linkable assets found for this account type.",
    "cancel": "Cancel",
    "link": "Link"
  },
  "form": {
    "title": "Add Account",
    "editTitle": "Edit Account",
    "name": "Account Name",
    "namePlaceholder": "e.g. Chase Checking",
    "institution": "Institution Name",
    "institutionPlaceholder": "e.g. Chase Bank",
    "type": "Account Type",
    "subtype": "Account Subtype",
    "mask": "Last 4 Digits",
    "maskPlaceholder": "1234",
    "balance": "Current Balance",
    "currency": "Currency",
    "interestRate": "Interest Rate (APR %)",
    "monthlyPayment": "Monthly Payment",
    "maturityDate": "Maturity Date",
    "linkedAsset": "Link to Asset",
    "linkedAssetPlaceholder": "Select an asset...",
    "noLinkableAssets": "No linkable assets found",
    "save": "Save",
    "saving": "Saving...",
    "cancel": "Cancel"
  },
  "toast": {
    "added": "Account added successfully",
    "addFailed": "Failed to add account",
    "updated": "Account updated successfully",
    "updateFailed": "Failed to update account",
    "deleted": "\"{{name}}\" has been deleted",
    "deleteFailed": "Failed to delete account",
    "linked": "Account linked to {{assetName}}",
    "linkFailed": "Failed to link account to asset",
    "unlinked": "Account unlinked from asset",
    "unlinkFailed": "Failed to unlink account"
  },
  "liability": {
    "interestRate": "APR",
    "monthlyPayment": "Monthly Payment",
    "maturityDate": "Maturity Date",
    "linkedTo": "Linked to"
  }
}
```

### Transactions Translation File

Create `src/locales/en/transactions.json`:

```json
{
  "page": {
    "title": "Transactions | RAJ Financial",
    "heading": "Transactions"
  },
  "summary": {
    "income": "Income",
    "spending": "Spending",
    "netCashFlow": "Net Cash Flow",
    "transactionCount": "Transactions"
  },
  "filters": {
    "label": "Filter transactions",
    "account": "Account",
    "allAccounts": "All Accounts",
    "category": "Category",
    "allCategories": "All Categories",
    "taxCategory": "Tax Category",
    "allTaxCategories": "All Tax Categories",
    "search": "Search transactions",
    "searchPlaceholder": "Search by merchant or description..."
  },
  "table": {
    "label": "Transaction list",
    "date": "Date",
    "description": "Description",
    "category": "Category",
    "account": "Account",
    "amount": "Amount",
    "actions": "Actions"
  },
  "status": {
    "pending": "Pending",
    "posted": "Posted"
  },
  "category": {
    "uncategorized": "Uncategorized"
  },
  "detail": {
    "title": "Transaction Details",
    "account": "Account",
    "category": "Category",
    "channel": "Channel",
    "status": "Status",
    "authDate": "Authorized Date",
    "annotations": "Your Annotations",
    "categoryOverride": "Category Override",
    "categoryOverridePlaceholder": "Override category...",
    "taxCategory": "Tax Category",
    "notes": "Notes",
    "notesPlaceholder": "Add a note about this transaction...",
    "tags": "Tags",
    "addTag": "Add tag",
    "tagPlaceholder": "New tag...",
    "receipts": "Receipts",
    "attachReceipt": "Attach Receipt",
    "removeReceipt": "Remove receipt",
    "hidden": "Hide from reports",
    "save": "Save Changes",
    "saving": "Saving..."
  },
  "taxCategories": {
    "NotTaxRelated": "Not Tax Related",
    "BusinessExpense": "Business Expense",
    "MedicalDental": "Medical / Dental",
    "CharitableDonation": "Charitable Donation",
    "HomeOffice": "Home Office",
    "Education": "Education",
    "InvestmentExpense": "Investment Expense",
    "RentalProperty": "Rental Property",
    "SelfEmployment": "Self-Employment",
    "OtherDeduction": "Other Deduction"
  },
  "charts": {
    "spendingByCategory": "Spending by Category"
  },
  "empty": {
    "title": "No transactions yet",
    "description": "Link a bank account or add transactions manually to start tracking your spending.",
    "goToAccounts": "Go to Accounts"
  },
  "toast": {
    "updated": "Transaction updated",
    "updateFailed": "Failed to update transaction"
  }
}
```

### Register Namespaces

Update `src/lib/i18n.ts` to load both new namespaces:

```typescript
import accountsEn from "@/locales/en/accounts.json";
import transactionsEn from "@/locales/en/transactions.json";

// Add to resources.en:
accounts: accountsEn,
transactions: transactionsEn,
```

---

## Accessibility Requirements

Follow all accessibility patterns established in the existing Assets page:

- **Filter tabs**: `role="radiogroup"` on container, `role="radio"` + `aria-checked` on each button
- **View toggle**: `role="group"` + `aria-label`, `aria-pressed` on each button
- **3-dot menu**: `aria-label` on the trigger button (e.g., "More actions")
- **Table**: Proper `TableHeader` / `TableHead` / `TableBody` / `TableRow` / `TableCell`
- **Cards**: Clickable cards have `role="button"`, `tabIndex={0}`, and `onKeyDown` handling Enter/Space
- **Form labels**: All form inputs have associated `<label>` elements via `htmlFor`/`id`
- **Select triggers**: `aria-label` on all `SelectTrigger` elements
- **Search input**: `sr-only` label + visible placeholder
- **Donut chart**: `sr-only` table alternative with `role="table"` / `role="row"` / `role="cell"`
- **Delete dialog**: Proper `AlertDialog` focus management
- **Sheet**: Proper focus trap via Radix Sheet
- **Icons**: Decorative icons have `aria-hidden="true"`, meaningful icon-only buttons have `aria-label`
- **Pending transactions**: Visual opacity change + `Badge` text indicating "Pending" status
- **Color contrast**: Green/red balance colors meet WCAG 2.1 AA contrast requirements against both light and dark backgrounds

## Verification Checklist

After building, verify:

- [ ] `/:entityType/:slug/accounts` renders with all 8 mock accounts for the current entity
- [ ] Filter tabs correctly filter accounts by type
- [ ] Summary cards compute correct totals: positive balances, negative balances (debt), net position, count
- [ ] View toggle switches between table and card grid, persists in localStorage
- [ ] Liability accounts (CreditCard, Loan, Mortgage) show APR, monthly payment, and linked asset
- [ ] Add Account sheet opens and adapts form fields based on selected type (showing liability fields for CreditCard/Loan/Mortgage)
- [ ] Asset linking dropdown on form is filtered by account type (`ASSET_LINK_RULES`)
- [ ] 3-dot menu "Link to Asset" opens picker dialog (only for linkable liability types)
- [ ] Delete confirmation dialog warns about transaction deletion
- [ ] Empty state renders when no accounts exist
- [ ] Skeleton loading shows while data is loading
- [ ] `/:entityType/:slug/transactions` renders with all 25 mock transactions for the current entity
- [ ] Transaction summary cards show correct totals
- [ ] Filter bar filters by account, category, tax category, and search text
- [ ] Spending donut chart renders with correct category data and colors
- [ ] Transaction rows show merchant name, category badge, account, and colored amount
- [ ] Pending transactions render at 60% opacity with "Pending" badge
- [ ] Clicking a transaction opens the detail sheet
- [ ] Detail sheet shows tax category dropdown with all options
- [ ] Detail sheet shows receipt attachment section
- [ ] Detail sheet shows tags with add/remove functionality
- [ ] "Save Changes" in detail sheet calls the update mutation and shows toast
- [ ] Mobile layout: transactions show as cards, chart moves below list
- [ ] All text uses i18n translation keys (no hardcoded English strings)
- [ ] Sidebar nav shows "Accounts" and "Transactions" as sub-items under each entity section
- [ ] Both routes are entity-scoped and render within `EntityProvider`
- [ ] All data fetching uses `entityId` from `useEntityContext()`
- [ ] All navigation links use entity-relative paths (via `entityPath()`)

