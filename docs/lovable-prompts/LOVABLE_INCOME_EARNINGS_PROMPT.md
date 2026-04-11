# Lovable Prompt — Income & Earnings Page

## Context

Build the **Income & Earnings** page scoped per entity. Income sources belong to a specific entity — a Personal entity has employment, rental, and investment income; a Business entity has business revenue and freelance income; a Trust entity might have investment income and royalties. The total annual income is computed from all sources within that entity and is consumed by other features (insurance calculator, dashboard cash flow, AI insights) so recommendations auto-update when income changes. Follow the same layout patterns as the existing **Assets** page (summary cards, filter tabs, view toggle, form sheet).

## Route & Nav

- **Route**: `/:entityType/:slug/income` (e.g., `/personal/income`, `/business/acme-llc/income`, `/trust/family-trust/income`)
- **Nav**: Sub-item under each entity section in the sidebar. Icon: `DollarSign` from lucide-react. Label: "Income"
- **Page file**: `src/pages/Income.tsx`
- The page renders inside an `EntityProvider` layout that supplies entity context via route params. Do not add explicit route or layout wiring — the entity layout handles it.

## Entity Context

This page renders inside an `EntityProvider` that supplies entity context:

```tsx
const { entityId, entityType, entitySlug } = useEntityContext();
```

All data fetching and mutations use `entityId`. The same `Income.tsx` component renders for all entity types, but income type suggestions adapt to entity type (see "Entity-Type-Specific Income Categories" below).

## Data Types

Create `src/types/income.ts`:

```typescript
type IncomeSourceType =
  | "Employment"
  | "SelfEmployment"
  | "RentalProperty"
  | "InvestmentDividends"
  | "InterestIncome"
  | "CapitalGains"
  | "Retirement"
  | "SocialSecurity"
  | "Pension"
  | "Annuity"
  | "Business"
  | "Freelance"
  | "Alimony"
  | "ChildSupport"
  | "Royalties"
  | "Other";

type PayFrequency =
  | "Weekly"
  | "BiWeekly"
  | "SemiMonthly"
  | "Monthly"
  | "Quarterly"
  | "Annually"
  | "Irregular";

interface IncomeSourceDto {
  id: string;
  entityId: string;
  name: string;                    // e.g., "Acme Corp Salary", "123 Main St Rental"
  type: IncomeSourceType;
  grossAmount: number;             // Per-period gross amount
  netAmount?: number;              // Per-period net (after deductions)
  frequency: PayFrequency;
  annualizedGross: number;         // Computed: grossAmount * periods per year
  annualizedNet?: number;          // Computed: netAmount * periods per year
  isActive: boolean;               // Currently receiving this income
  startDate?: string;              // When this income started
  endDate?: string;                // For temporary/contract income
  notes?: string;
  metadata?: IncomeMetadata;       // Type-specific details
  createdAt: string;
  updatedAt: string;
}

// Discriminated metadata by IncomeSourceType
interface IncomeMetadata {
  employment?: EmploymentIncomeDetail;
  rental?: RentalIncomeDetail;
  investment?: InvestmentIncomeDetail;
  business?: BusinessIncomeDetail;
  retirement?: RetirementIncomeDetail;
}

interface EmploymentIncomeDetail {
  employer: string;
  jobTitle?: string;
  // Paystub breakdown (per pay period)
  federalTax?: number;
  stateTax?: number;
  socialSecurity?: number;        // FICA - Social Security
  medicare?: number;              // FICA - Medicare
  retirement401k?: number;        // 401k / 403b contribution
  healthInsurance?: number;       // Health/dental/vision premiums
  hsa?: number;                   // HSA contributions
  otherDeductions?: number;       // Union dues, garnishments, etc.
  otherDeductionsLabel?: string;
  employerMatch401k?: number;     // Employer 401k match (not deducted, but tracked)
}

interface RentalIncomeDetail {
  propertyAddress?: string;
  linkedAssetId?: string;         // Link to RealEstate asset
  monthlyRent: number;
  vacancyRatePercent?: number;    // e.g., 5 = 5% vacancy assumed
  monthlyExpenses?: number;       // Property tax, insurance, maintenance, HOA
  netOperatingIncome?: number;    // Computed: rent * (1 - vacancy) - expenses
}

interface InvestmentIncomeDetail {
  linkedAssetId?: string;         // Link to Investment/Retirement asset
  dividendYield?: number;         // Annual yield percentage
  isQualifiedDividend?: boolean;  // Tax treatment
  isReinvested?: boolean;         // DRIP
}

interface BusinessIncomeDetail {
  businessName?: string;
  linkedAssetId?: string;         // Link to Business asset
  estimatedTaxRate?: number;      // Self-employment tax + income tax rate
  quarterlyEstimatedTax?: number;
}

interface RetirementIncomeDetail {
  source: "SocialSecurity" | "Pension" | "Annuity" | "IRA" | "401k" | "Other";
  isTaxable?: boolean;
  colaAdjustment?: boolean;       // Cost of living adjustment
  survivorBenefit?: boolean;
  startAge?: number;              // Age when benefit starts
}

// Summary DTO consumed by other features
interface IncomeSummaryDto {
  entityId: string;
  totalAnnualGross: number;
  totalAnnualNet: number;
  totalMonthlyGross: number;
  totalMonthlyNet: number;
  sourceCount: number;
  activeSourceCount: number;
  byType: IncomeByTypeItem[];
  primarySource?: IncomeSourceDto; // Largest income source
}

interface IncomeByTypeItem {
  type: IncomeSourceType;
  label: string;
  annualGross: number;
  annualNet: number;
  percentage: number;             // % of total gross
  sourceCount: number;
}

interface CreateIncomeSourceRequest {
  entityId: string;
  name: string;
  type: IncomeSourceType;
  grossAmount: number;
  netAmount?: number;
  frequency: PayFrequency;
  isActive?: boolean;
  startDate?: string;
  endDate?: string;
  notes?: string;
  metadata?: IncomeMetadata;
}
```

## API Service

Create `src/services/income-service.ts` following the same TanStack Query pattern as `asset-service.ts`. All endpoints are entity-scoped:

- `GET /api/entities/{entityId}/income` → `useIncomeSources(entityId)` — list income sources for entity
- `GET /api/entities/{entityId}/income/summary` → `useIncomeSummary(entityId)` — aggregated totals for entity
- `POST /api/entities/{entityId}/income` → `useCreateIncomeSource(entityId, request)` — add income source to entity
- `PUT /api/entities/{entityId}/income/{id}` → `useUpdateIncomeSource(entityId, id, request)` — update
- `DELETE /api/entities/{entityId}/income/{id}` → `useDeleteIncomeSource(entityId, id)` — delete
- Use mock data initially.

## Entity-Type-Specific Income Categories

Filter the income type dropdown based on `entityType` from the entity context:

- **Personal**: Employment, SelfEmployment, RentalProperty, InvestmentDividends, InterestIncome, CapitalGains, Retirement, SocialSecurity, Pension, Annuity, Alimony, ChildSupport, Royalties, Other
- **Business**: Business, SelfEmployment, Freelance, InvestmentDividends, InterestIncome, CapitalGains, Other
- **Trust**: InvestmentDividends, InterestIncome, CapitalGains, RentalProperty, Royalties, Other

The full `IncomeSourceType` union type remains unchanged in `income.ts`, but the UI form only presents the subset relevant to the current entity type.

## Mock Data

Mock data varies by entity type. Pre-populate with entity-scoped examples:

### Personal Entity Mock

| Source | Type | Gross | Net | Frequency |
|--------|------|-------|-----|-----------|
| Acme Corp — Software Engineer | Employment | $5,769.23 | $3,846.15 | BiWeekly |
| 123 Main St Duplex | RentalProperty | $2,800 | $1,950 | Monthly |
| Fidelity Brokerage Dividends | InvestmentDividends | $1,200 | $1,020 | Quarterly |
| Freelance Consulting | Freelance | $2,500 | $1,875 | Irregular |

**Employment paystub detail** (Acme Corp, per bi-weekly check):
- Gross: $5,769.23
- Federal Tax: -$865.38
- State Tax: -$288.46
- Social Security: -$357.69
- Medicare: -$83.65
- 401(k): -$461.54 (8%)
- Health Insurance: -$192.31
- HSA: -$96.15
- Net: $3,424.05
- Employer 401(k) Match: $288.46 (5%)

**Personal entity summary totals**:
- Annual Gross: ~$180,000
- Annual Net: ~$120,000
- Monthly Gross: ~$15,000
- Monthly Net: ~$10,000

### Business Entity Mock (e.g., "Acme LLC")

| Source | Type | Gross | Net | Frequency |
|--------|------|-------|-----|-----------|
| Acme LLC Revenue | Business | $45,000 | $31,500 | Monthly |
| Business Savings Interest | InterestIncome | $500 | $500 | Monthly |

**Business entity summary totals**:
- Annual Gross: ~$546,000
- Annual Net: ~$384,000

> **Note:** Mock data varies by entity type. When switching entity context, the displayed income sources change accordingly.

## Summary Cards Row

Top of page, 4 summary cards (`grid grid-cols-2 lg:grid-cols-4 gap-4`):

| Card | Content |
|------|---------|
| Annual Gross | "$180,000" with "Monthly: $15,000" subtitle |
| Annual Net | "$120,000" with "Monthly: $10,000" subtitle |
| Active Sources | "4 sources" with breakdown by type |
| Effective Tax Rate | "33%" computed from (gross - net) / gross |

## Filter Tabs

Horizontal filter tabs:
`All | Employment | Rental | Investment | Business | Retirement | Other`

"Other" tab combines: Freelance, Alimony, ChildSupport, Royalties, Other.

## View Toggle (Table + Card Grid)

Same dual-view pattern as Assets page.

### Table View (default on desktop)

| Column | Content |
|--------|---------|
| Source | Name + type icon |
| Type | Badge with income type |
| Gross (per period) | Amount + frequency label (e.g., "$5,769/bi-weekly") |
| Net (per period) | Amount (muted if not provided) |
| Annualized | Annual gross (mono font, green) |
| Status | "Active" (green) or "Inactive" (muted) badge |
| Actions | 3-dot menu: Edit, View Paystub (Employment only), Deactivate, Delete |

### Card Grid (default on mobile)

Each card shows:
- **Type icon** (colored circle): `Briefcase` (Employment), `Home` (Rental), `TrendingUp` (Investment), `Building2` (Business), `Clock` (Retirement), `CircleDollarSign` (Other)
- **Source name** (truncated)
- **Gross per period** + frequency label
- **Annualized amount** (large, green, mono font)
- **Active/Inactive** badge
- **3-dot menu**

## Add/Edit Income Source Sheet

`Sheet` (same pattern as `AssetFormSheet`) with fields:

### Common Fields (all types)
- **Source Name** (required, text)
- **Income Type** (required, select dropdown — changes the metadata section below)
- **Gross Amount** (required, currency — per period)
- **Net Amount** (optional, currency — per period)
- **Pay Frequency** (required, select: Weekly, BiWeekly, SemiMonthly, Monthly, Quarterly, Annually, Irregular)
- **Currently Active** (switch, default on)
- **Start Date** (optional, date picker)
- **End Date** (optional, date picker — shown only if not active or for contract work)
- **Notes** (optional, textarea)

### Employment Metadata Section (shown when type = Employment)
- **Employer Name** (text)
- **Job Title** (text)
- Divider: "Paystub Deductions (per pay period)"
- **Federal Tax** (currency)
- **State Tax** (currency)
- **Social Security (FICA)** (currency)
- **Medicare** (currency)
- **401(k) / 403(b)** (currency)
- **Health Insurance** (currency)
- **HSA** (currency)
- **Other Deductions** (currency) + **Label** (text, e.g., "Union Dues")
- **Employer 401(k) Match** (currency, labeled as "not deducted — informational")
- Auto-computed **Net Pay** shown at bottom = Gross - all deductions (validates against entered Net Amount)

### Rental Property Metadata Section (shown when type = RentalProperty)
- **Property Address** (text)
- **Link to Asset** (optional, select from user's RealEstate assets)
- **Monthly Rent** (currency)
- **Vacancy Rate** (%, default 5%)
- **Monthly Expenses** (currency — tax, insurance, maintenance, HOA)
- Auto-computed **Net Operating Income** = Rent × (1 - vacancy%) - Expenses

### Investment Metadata Section (shown when type = InvestmentDividends, InterestIncome, CapitalGains)
- **Link to Asset** (optional, select from user's Investment/Retirement assets)
- **Dividend Yield** (%, optional)
- **Qualified Dividends** (switch)
- **Reinvested (DRIP)** (switch)

### Business Metadata Section (shown when type = Business, SelfEmployment, Freelance)
- **Business Name** (text)
- **Link to Asset** (optional, select from user's Business assets)
- **Estimated Tax Rate** (%, default 30%)
- **Quarterly Estimated Tax** (currency, auto-computed from gross × tax rate / 4)

### Retirement Metadata Section (shown when type = Retirement, SocialSecurity, Pension, Annuity)
- **Benefit Source** (select: Social Security, Pension, Annuity, IRA, 401k, Other)
- **Taxable** (switch, default yes)
- **COLA Adjustment** (switch — cost of living increases)
- **Survivor Benefit** (switch)
- **Start Age** (number, optional)

## Income Breakdown Chart

Below the list, show a **Recharts donut chart** (`<PieChart>` with inner radius):
- Segments by income type, color-coded
- Center label: Total Annual Gross
- Legend showing each type with annual amount and percentage
- Only shown when 2+ income sources exist

## Integration Points (How Other Features Consume This Data)

The `useIncomeSummary(entityId)` hook is consumed by other features, always scoped to the current entity:

1. **Insurance Calculator** — `useIncomeSummary(entityId)` auto-populates the "Annual Household Income" field scoped to the entity's income. If the entity has income sources entered, show an info badge: "Based on your income profile" with link to the entity-relative income path. Insurance recommendations update automatically when income changes.

2. **Dashboard** — `useIncomeSummary(entityId)` feeds `totalMonthlyNet` into the entity overview spending summary (income vs. expenses). `byType` breakdown shown in income composition widget.

3. **Debt Payoff Calculator** — `useIncomeSummary(entityId)` provides the entity's `totalMonthlyNet` to compute how much extra payment is feasible.

4. **AI Insights** — Entity-scoped income profile available for analysis and recommendations.

Show a banner on the Insurance Calculator page when income data exists:
```tsx
const { entityType, entitySlug } = useEntityContext();
const incomeHref = `/${entityType}/${entitySlug}/income`;

<div className="flex items-center gap-2 rounded-md border border-blue-200 bg-blue-50 dark:bg-blue-950/20 px-3 py-2 text-sm">
  <Info className="h-4 w-4 text-blue-500 shrink-0" />
  <span>
    Income auto-filled from your <a href={incomeHref} className="underline font-medium">Income Profile</a>
    (Annual Gross: $180,000). Edit here to override for this calculation.
  </span>
</div>
```

## Delete Confirmation

AlertDialog: "Deleting this income source will update your total income across all tools (insurance calculator, dashboard, etc.). This cannot be undone."

## Empty State

- DollarSign icon
- "No income sources yet"
- "Add your income sources to get accurate insurance recommendations, spending analysis, and financial insights"
- "Add Income Source" primary button

## Skeleton Loading

Card-shaped skeletons while loading (same pattern as Assets page).

## Premium Tier Gating

- **Free tier**: Up to 3 income sources, basic types (Employment, Rental, Other)
- **Premium**: Unlimited sources, all types, paystub detail, asset linking, tax projections

## Accessibility

- All form inputs have proper `<label>` associations
- Currency inputs use `inputMode="decimal"` for mobile keyboards
- Filter tabs use `role="tablist"` / `role="tab"`
- Cards are focusable with keyboard navigation
- Switch toggles use `role="switch"` with `aria-checked`
- Dynamic metadata sections use `aria-live="polite"` when they appear/disappear
- All interactive elements have minimum 44x44px touch targets

## i18n

- All text uses `useTranslation()` with `t("income.summary.annualGross")` pattern
- Create translation keys in `src/locales/en/income.json`
- Currency/number formatting via `Intl.NumberFormat`
- Frequency labels translatable: `t("income.frequency.biWeekly")`
