# Lovable Prompt — Insurance Needs Calculator Page

## Context

Build the **Insurance Needs Calculator** page scoped per entity. Insurance recommendations adapt to entity type — a Personal entity calculates life, disability, property, and liability insurance needs; a Business entity recommends E&O, workers' comp, general liability, and cyber liability (driven by `Industry` and `BusinessFormationType` from entity metadata); a Trust entity covers trustee liability and trust property insurance. This is a stateless calculator (no database storage) that estimates coverage needs based on entity-specific inputs, compares them to existing policies, and shows a visual coverage gauge with a breakdown chart. Follow the same layout patterns as the existing **Assets** page. Use Recharts for the breakdown donut chart.

**ADO Work Item**: #415 — Insurance Needs Calculator (Epic #414 — Estate Planning)

## Route & Nav

- **Route**: `/:entityType/:slug/insurance` (e.g., `/personal/insurance`, `/business/acme-llc/insurance`, `/trust/family-trust/insurance`)
- **Nav**: Sub-item under each entity section in the sidebar. Icon: `Shield` from lucide-react. Label: "Insurance"
- **Page file**: `src/pages/InsuranceCalculator.tsx`
- The page renders inside an `EntityProvider` layout that supplies entity context via route params. Do not add explicit route or layout wiring — the entity layout handles it.

## Entity Context

This page renders inside an `EntityProvider` that supplies entity context:

```tsx
const { entityId, entityType, entitySlug } = useEntityContext();
```

Insurance recommendations adapt to entity type. The same `InsuranceCalculator.tsx` component renders for all entity types, but the calculator inputs, breakdown categories, and recommendation logic change based on `entityType`. For Business entities, recommendations are additionally driven by `Industry` and `BusinessFormationType` from entity metadata.

## Data Types

Create `src/types/insurance-calculator.ts`:

```typescript
interface InsuranceInputs {
  annualIncome: number;
  incomeYearsToReplace: number; // 1–20, default 10
  dependents: number;           // 0–5+
  mortgageBalance: number;
  otherDebts: number;
  includeEducation: boolean;    // $50K per dependent
  educationPerDependent: number; // default 50000
  finalExpenses: number;         // default 25000 (funeral, medical, legal)
  emergencyFundMonths: number;   // default 6
  currentCoverage: number;       // existing life insurance total
}

interface InsuranceResult {
  calculatedNeed: number;
  currentCoverage: number;
  coverageGap: number;         // max(0, need - coverage)
  coverageRatio: number;       // coverage / need (0 to 1+)
  coverageStatus: CoverageStatus;
  breakdown: InsuranceBreakdownItem[];
}

type CoverageStatus = "Adequate" | "Partial" | "Insufficient";

interface InsuranceBreakdownItem {
  category: string;
  amount: number;
  description: string;
  percentage: number;
  color: string;
}

interface ExistingPolicy {
  id: string;
  name: string;
  policyType: "WholeLife" | "UniversalLife" | "TermLife" | "Employer" | "Other";
  deathBenefit: number;
  premiumAmount?: number;
  premiumFrequency?: "Monthly" | "Quarterly" | "SemiAnnually" | "Annually";
  expirationDate?: string;  // For term policies
  cashValue?: number;        // For whole/universal
  notes?: string;
}
```

## Calculation Logic

Create `src/lib/insurance-calculator.ts` — **pure client-side calculation** (no API call):

```typescript
function calculateInsuranceNeed(inputs: InsuranceInputs): InsuranceResult {
  const incomeReplacement = inputs.annualIncome * inputs.incomeYearsToReplace;
  const debtPayoff = inputs.mortgageBalance + inputs.otherDebts;
  const educationFunding = inputs.includeEducation
    ? inputs.dependents * inputs.educationPerDependent
    : 0;
  const finalExpenses = inputs.finalExpenses;
  const emergencyFund = inputs.annualIncome * (inputs.emergencyFundMonths / 12);

  const calculatedNeed = incomeReplacement + debtPayoff + educationFunding + finalExpenses + emergencyFund;
  const coverageGap = Math.max(0, calculatedNeed - inputs.currentCoverage);
  const coverageRatio = calculatedNeed > 0 ? inputs.currentCoverage / calculatedNeed : 1;

  const coverageStatus: CoverageStatus =
    coverageRatio >= 1 ? "Adequate" :
    coverageRatio >= 0.7 ? "Partial" : "Insufficient";

  const breakdown: InsuranceBreakdownItem[] = [
    { category: "Income Replacement", amount: incomeReplacement, description: `${inputs.incomeYearsToReplace} years x $${inputs.annualIncome.toLocaleString()}`, percentage: 0, color: "#ebbb10" },
    { category: "Debt Payoff", amount: debtPayoff, description: "Mortgage + other debts", percentage: 0, color: "#c3922e" },
    { category: "Education", amount: educationFunding, description: `${inputs.dependents} dependents x $${inputs.educationPerDependent.toLocaleString()}`, percentage: 0, color: "#22c55e" },
    { category: "Final Expenses", amount: finalExpenses, description: "Funeral, medical, legal", percentage: 0, color: "#3b82f6" },
    { category: "Emergency Fund", amount: emergencyFund, description: `${inputs.emergencyFundMonths} months income`, percentage: 0, color: "#8b5cf6" },
  ].map(item => ({ ...item, percentage: calculatedNeed > 0 ? item.amount / calculatedNeed : 0 }));

  return { calculatedNeed, currentCoverage: inputs.currentCoverage, coverageGap, coverageRatio, coverageStatus, breakdown };
}
```

## Page Layout

Two-column layout on desktop, stacked on mobile:

```
Desktop (lg+):                                Mobile (< lg):
┌──────────────┬────────────────────────┐     [Income & Family Card]
│ Income &     │ Coverage Gauge         │     [Debts & Expenses Card]
│ Family       │ + Summary Stats        │     [Current Coverage Card]
│              │ + Status Message       │     [Existing Policies List]
├──────────────┤                        │     [Coverage Gauge]
│ Debts &      ├────────────────────────┤     [Breakdown Chart]
│ Expenses     │ Breakdown Donut Chart  │     [Considerations]
│              │ + Category Legend       │
├──────────────┤                        │
│ Current      ├────────────────────────┤
│ Coverage     │ Considerations Card    │
│              │                        │
├──────────────┤                        │
│ Existing     │                        │
│ Policies     │                        │
└──────────────┴────────────────────────┘
```

Use: `grid lg:grid-cols-5 gap-6` with inputs in `lg:col-span-2` and results in `lg:col-span-3`

## Input Section (Left Column)

### Card 1: Income & Family

- **Annual Household Income** — Currency input (default: $75,000)
  - **Auto-filled from Income Profile** if the entity has income sources. Show info banner:
    ```tsx
    <div className="flex items-center gap-2 rounded-md border border-blue-200 bg-blue-50 dark:bg-blue-950/20 px-3 py-2 text-sm">
      <Info className="h-4 w-4 text-blue-500 shrink-0" />
      <span>
        Auto-filled from your <a href={`/${entityType}/${entitySlug}/income`} className="underline font-medium">Income Profile</a>
        (Annual Gross: $180,000). Edit to override.
      </span>
    </div>
    ```
  - Uses `useIncomeSummary(entityId).totalAnnualGross` when available; user can manually override
  - If income profile changes, recommendation updates automatically on next page load
- **Years of Income to Replace** — Slider 1–20 with visible value label (default: 10)
- **Number of Dependents** — Row of 6 toggle buttons: 0, 1, 2, 3, 4, 5+ (default: 2)

### Card 2: Debts & Future Expenses

- **Mortgage Balance** — Currency input (default: $250,000)
  - **Auto-filled from Bills** if the entity has Mortgage-category bills. Uses `useBillsSummary(entityId)` to pull total mortgage balance.
- **Other Debts** — Currency input (default: $25,000)
  - **Auto-filled from Bills** — sums `currentBalance` from all non-mortgage debt bills (CreditCard, StudentLoan, AutoLoan, PersonalLoan, Loan) for this entity.
  - Show info banner when auto-filled:
    ```tsx
    <div className="flex items-center gap-2 rounded-md border border-blue-200 bg-blue-50 dark:bg-blue-950/20 px-3 py-2 text-sm">
      <Info className="h-4 w-4 text-blue-500 shrink-0" />
      <span>
        Debts auto-filled from your <a href={`/${entityType}/${entitySlug}/bills`} className="underline font-medium">Bills</a>
        (Mortgage: $287,000 + Other: $45,700). Edit to override.
      </span>
    </div>
    ```
  - If bills change (new debt added, balance updated via Plaid sync), insurance recommendation updates automatically
- **Include Education Funding** — Switch toggle with helper text "$50,000 per dependent"
  - When enabled, shows computed amount (e.g., "2 dependents = $100,000")

### Card 3: Current Life Insurance

- **Existing Coverage Amount** — Currency input (default: $250,000)
- Helper text: "Include employer-provided, term, and whole life policies"

### Card 4: Existing Policies (Optional Detail)

Optional expandable section to itemize policies:
- "Add Policy" button
- Each policy row: Name, Type (dropdown), Death Benefit (currency), Premium, Frequency, Expiration (for term)
- Total coverage auto-sums from policies if entered (overrides the single input above)
- Policies are client-side only (not persisted)
- Expiration warning for term policies expiring within 2 years (amber banner)

## Results Section (Right Column)

All results update **live** as inputs change (no "Calculate" button needed — reactive computation).

### Coverage Gauge Component (`CoverageGauge.tsx`)

- **SVG circular gauge** (arc/donut style, 180px x 180px)
- Arc fills proportionally to `coverageRatio` (0% to 100%+)
- Color gradient:
  - Adequate (>=100%): green gradient
  - Partial (70-99%): amber gradient
  - Insufficient (<70%): red gradient
- Center content: Large percentage number + "Covered" label
- Status icon at bottom of gauge (check for adequate, warning for partial, alert for insufficient)

### Summary Stats

Below the gauge, three stat boxes:
- **Calculated Need**: Large bold number (e.g., "$1,075,000")
- **Current Coverage**: Medium number (e.g., "$250,000")
- **Coverage Gap**: Medium number, red if > 0, green "None" if covered (e.g., "$825,000")

### Status Message Banner

Conditional banner based on `coverageStatus`:

**Adequate** (green):
> "Coverage Looks Good — Your current coverage meets the calculated need."

**Partial** (amber):
> "Partial Coverage — Consider reviewing your coverage to close the $X gap."

**Insufficient** (red):
> "Coverage Gap Detected — A $X gap exists between your coverage and calculated need."

### Breakdown Donut Chart

- **Recharts `<PieChart>`** with `<Pie>` (innerRadius for donut effect)
- 5 segments: Income Replacement, Debt Payoff, Education, Final Expenses, Emergency Fund
- Center label: "Total Need" + formatted dollar amount
- **Legend/Breakdown List** (right side on desktop, below on mobile):
  - Each item: colored dot + category name + amount + description + percentage bar

### Considerations Card

Educational disclaimer card (left gold border accent):
- "Things to Consider" header with info icon
- Bullet list:
  - "Social Security survivor benefits may reduce your need"
  - "Spouse's income and earning potential affect calculations"
  - "Consider future expenses like healthcare and long-term care"
  - "Employer-provided coverage typically ends when employment ends"
- Footer disclaimer: "This calculator provides estimates for educational purposes only. Consult a licensed insurance professional for personalized advice."

## Entity-Type-Specific Recommendations

The calculator adapts its inputs, breakdown categories, and recommendations based on `entityType`:

### Personal Entity

Uses the current calculator logic described above — life insurance needs based on income replacement, dependents, debts, education funding, final expenses, and emergency fund. Breakdown categories: Income Replacement, Debt Payoff, Education, Final Expenses, Emergency Fund.

Additional recommendation cards for:
- **Disability Insurance**: Suggest 60–70% of annual income as coverage target
- **Property Insurance**: Link to assets page for property valuations
- **Personal Liability (Umbrella)**: Recommend $1M+ if net worth exceeds $500K

### Business Entity

Different calculator entirely. Pulls `Industry`, `AnnualRevenue`, `NumberOfEmployees`, and `BusinessFormationType` from entity metadata via `useEntityContext()`.

**Inputs (replaces the personal inputs):**
- **Annual Revenue** — Currency input, auto-filled from entity metadata `AnnualRevenue`
- **Number of Employees** — Numeric input, auto-filled from entity metadata `NumberOfEmployees`
- **Industry** — Read-only display from entity metadata (e.g., "Restaurant", "Technology", "Construction")
- **Business Formation** — Read-only display from entity metadata (e.g., "LLC", "S-Corp", "Sole Proprietorship")
- **Current General Liability Coverage** — Currency input
- **Current Workers' Comp Coverage** — Currency input
- **Current Professional Liability Coverage** — Currency input

**Recommended coverage types (shown as cards with calculated minimums):**
- **General Liability**: Based on revenue tier ($1M for <$500K revenue, $2M for $500K–$2M, $5M for $2M+)
- **Professional Liability / E&O**: Based on industry + revenue
- **Workers' Compensation**: Required if `NumberOfEmployees > 0`, amount based on industry risk class
- **Cyber Liability**: Recommended for all businesses, minimum $1M
- **Commercial Property**: If entity has business property assets
- **Industry-Specific**: e.g., Liquor Liability for restaurants, Malpractice for healthcare

**Breakdown chart** shows recommended coverage by type instead of needs breakdown.

### Trust Entity

Simpler calculator focused on fiduciary protection.

**Inputs:**
- **Trust Asset Value** — Currency input, auto-filled from entity's total asset value
- **Number of Beneficiaries** — Numeric input
- **Trust Type** — Read-only display from entity metadata (e.g., "Revocable Living Trust", "Irrevocable Trust")
- **Current Trustee Liability Coverage** — Currency input

**Recommended coverage types:**
- **Trustee Liability Insurance**: Based on trust asset value (typically 1–2% of assets, minimum $1M)
- **Trust Property Insurance**: For real property held in trust
- **Fiduciary Liability**: If trust has investment authority

**Breakdown chart** shows coverage allocation by type.

## Mock Default Values

Pre-populate form with realistic defaults per entity type:

### Personal Entity (default mock data)
- Annual Income: $75,000
- Years to Replace: 10
- Dependents: 2
- Mortgage Balance: $250,000
- Other Debts: $25,000
- Education: enabled ($100,000 for 2 dependents)
- Final Expenses: $25,000
- Emergency Fund: 6 months ($37,500)
- Current Coverage: $250,000
- **Calculated Need**: $1,087,500
- **Coverage Gap**: $837,500
- **Coverage Ratio**: 23%

### Business Entity (mock data)
- Industry: Restaurant
- Business Formation: LLC
- Annual Revenue: $500,000
- Number of Employees: 15
- Current General Liability: $500,000
- Current Workers' Comp: $0
- Current Professional Liability: $0
- **Recommended General Liability**: $1,000,000 (gap: $500,000)
- **Recommended Workers' Comp**: $500,000 (gap: $500,000)
- **Recommended Liquor Liability**: $1,000,000 (industry-specific, gap: $1,000,000)
- **Recommended Cyber Liability**: $1,000,000 (gap: $1,000,000)
- **Total Coverage Gap**: $3,000,000

### Trust Entity (mock data)
- Trust Asset Value: $2,000,000
- Number of Beneficiaries: 3
- Trust Type: Revocable Living Trust
- Current Trustee Liability: $0
- **Recommended Trustee Liability**: $1,000,000 (gap: $1,000,000)
- **Recommended Trust Property Insurance**: $1,500,000 (based on real property in trust)
- **Total Coverage Gap**: $2,500,000

## Premium Tier Gating

- **Free tier**: Full calculator available (no restrictions — it's a stateless tool)
- **Premium**: Future enhancement — auto-pull existing insurance assets and account data to pre-fill inputs

## Action Buttons

- **"Reset to Defaults"** — Ghost button, resets all inputs
- **"Share Results"** — Copy summary to clipboard (Premium: export PDF)
- **"View Insurance Assets"** — Link to `/${entityType}/${entitySlug}/assets` filtered by Insurance type

## Skeleton Loading

Not needed (client-side calculation, instant render). Show a brief fade-in animation on first load.

## Empty State

Not applicable — form is always pre-populated with defaults.

## Accessibility

- All form inputs have proper `<label>` associations and `id` attributes
- Currency inputs use `inputMode="decimal"` for mobile keyboards
- Slider uses `role="slider"` with `aria-valuemin`, `aria-valuemax`, `aria-valuenow`
- Dependent toggle buttons use `role="radiogroup"` / `role="radio"` with `aria-checked`
- Coverage gauge SVG has `role="img"` with `aria-label` describing the coverage ratio
- Status message banner uses `role="status"` with `aria-live="polite"` for reactive updates
- Color is never the sole indicator — always paired with text/icon
- All interactive elements have minimum 44x44px touch targets
- Switch toggle has `role="switch"` with `aria-checked`

## i18n

- All text uses `useTranslation()` with `t("insurance.input.annualIncome")` pattern
- Create translation keys in `src/locales/en/insurance.json`
- Currency formatting via `Intl.NumberFormat`
- Percentage formatting via `Intl.NumberFormat` with `style: "percent"`
- Disclaimer text must be translatable
