# Lovable Prompt — Debt Payoff Calculator Page

## Context

Build the **Debt Payoff Calculator** page, scoped per entity. Debt payoff analyzes only that entity's debts and income for disposable income calculation. This is a stateless calculator (no database storage) that lets users enter their debts, compare 4 payoff strategies (Avalanche, Snowball, Cash Flow Index, Custom), and view month-by-month payment schedules with timeline projections. Supports promotional/introductory rate tracking with expiration warnings. Follow the same layout patterns as the existing **Assets** page. Use Recharts for the payoff timeline chart.

## Route & Nav

- **Route**: `/:entityType/:slug/debt-payoff` (e.g., `/personal/debt-payoff`, `/business/acme-llc/debt-payoff`). Renders inside an `EntityProvider` layout — do not add a standalone route in `App.tsx`.
- **Nav**: Sub-item under each entity section, within the Planning sub-group. Icon: `Calculator` from lucide-react. Label: "Debt Payoff"
- **Page file**: `src/pages/DebtPayoff.tsx`

## Entity Context

This page renders inside an `EntityProvider`:
```tsx
const { entityId, entityType, entitySlug } = useEntityContext();
```
All data fetching uses `entityId` to scope to the current entity's debts and income.

## Data Types

Create `src/types/debt-payoff.ts`:

```typescript
type PayoffStrategy = "Avalanche" | "Snowball" | "CashFlowIndex" | "Custom";

interface DebtEntry {
  id: string; // client-generated UUID
  name: string;
  balance: number;
  interestRate: number; // Annual APR as percentage (e.g., 24.99)
  minimumPayment: number;
  rateSegments?: RateSegment[]; // For promo rates
  customOrder?: number; // For Custom strategy drag ordering
}

interface RateSegment {
  id: string;
  rate: number;        // APR percentage for this segment
  balance: number;     // Balance at this rate
  expiresAt?: string;  // ISO date when promo expires
  postExpirationRate?: number; // Rate after promo ends
  label?: string;      // e.g., "0% Promo - PayPal Purchase"
}

interface PayoffRequest {
  debts: DebtEntry[];
  extraMonthlyPayment: number; // Additional amount above minimums
  strategy: PayoffStrategy;
}

interface PayoffResult {
  strategy: PayoffStrategy;
  totalInterestPaid: number;
  totalPaid: number;
  payoffDate: string;
  totalMonths: number;
  monthlySchedule: MonthlyPayment[];
  debtPayoffOrder: DebtPayoffSummary[];
}

interface MonthlyPayment {
  month: number;
  date: string;
  payments: DebtMonthlyDetail[];
  totalPayment: number;
  totalRemainingBalance: number;
}

interface DebtMonthlyDetail {
  debtId: string;
  debtName: string;
  payment: number;
  principal: number;
  interest: number;
  remainingBalance: number;
  isPaidOff: boolean;
}

interface DebtPayoffSummary {
  debtId: string;
  debtName: string;
  originalBalance: number;
  totalInterestPaid: number;
  payoffMonth: number;
  payoffDate: string;
}

interface StrategyComparison {
  strategies: PayoffResult[];
  bestStrategy: PayoffStrategy;
  savingsVsMinimum: number; // Interest saved vs minimum-only payments
}
```

## Calculation Logic

Create `src/lib/debt-calculator.ts` — **pure client-side calculation** (no API call):

The calculator runs entirely in the browser. For each strategy:
1. Sort debts according to strategy order
2. Apply minimum payments to all debts
3. Apply extra payment to the priority debt
4. When a debt is paid off, roll its payment into the next debt (snowball/avalanche cascade)
5. Track interest per month, handle promo rate expirations
6. Generate month-by-month schedule until all debts are $0

Sorting order by strategy:
- **Avalanche**: Highest interest rate first
- **Snowball**: Lowest balance first
- **Cash Flow Index**: Lowest CFI first (CFI = Balance / MinimumPayment)
- **Custom**: User-defined order (via drag-and-drop)

## Auto-Population from Bills

When the entity has bills with debt categories (CreditCard, Loan, Mortgage, AutoLoan, StudentLoan, PersonalLoan), auto-populate the debt list from `useBills(entityId)` — only this entity's debt-category bills:

- Map each debt-category bill to a `DebtEntry`: `name` = bill name, `balance` = `currentBalance`, `interestRate` = `interestRate`, `minimumPayment` = `minimumPayment`
- Show info banner at top:
  ```tsx
  <div className="flex items-center gap-2 rounded-md border border-blue-200 bg-blue-50 dark:bg-blue-950/20 px-3 py-2 text-sm">
    <Info className="h-4 w-4 text-blue-500 shrink-0" />
    <span>
      Debts auto-filled from your <a href={`/${entityType}/${entitySlug}/bills`} className="underline font-medium">Bills</a>.
      Changes here won't affect your bill records. <button className="underline font-medium">Load example data instead</button>
    </span>
  </div>
  ```
- User can edit/remove auto-populated debts (changes are local to calculator, don't persist back to bills)
- If no bills exist, fall back to the mock example data below
- **Extra Monthly Payment** can be informed by `useIncomeSummary(entityId).totalMonthlyNet - useBillsSummary(entityId).totalMonthlyBills` to show disposable income hint: "This entity's estimated disposable income is $X/mo after bills"

## Mock Data (Pre-filled Example)

Pre-populate the form with example debts when no Bills data exists. Mock data varies by entity type:

### Personal Entity (default)

| Debt | Balance | APR | Min Payment |
|------|---------|-----|-------------|
| Chase Sapphire | $8,500 | 24.99% | $170 |
| Student Loan | $22,000 | 5.50% | $250 |
| Auto Loan | $15,200 | 6.99% | $380 |
| PayPal Credit | $3,200 | 0% (promo, expires Aug 2026, then 29.99%) | $65 |
| Medical Bill | $1,800 | 0% | $150 |

Extra monthly payment: $500

### Business Entity

| Debt | Balance | APR | Min Payment |
|------|---------|-----|-------------|
| Business Line of Credit | $45,000 | 9.50% | $900 |
| Equipment Loan | $28,000 | 7.25% | $520 |
| Vendor Financing | $12,500 | 0% (promo, expires Jun 2026, then 18.99%) | $350 |
| Business Credit Card | $6,800 | 21.99% | $136 |

Extra monthly payment: $1,000

### Trust Entity

Trusts rarely carry debt. Show minimal mock data:

| Debt | Balance | APR | Min Payment |
|------|---------|-----|-------------|
| Property Mortgage | $180,000 | 4.25% | $1,200 |

Extra monthly payment: $300

## Page Layout

### Top Section — Debt Entry Form

**"Your Debts" Card** with editable debt list:

Each debt row (responsive — horizontal on desktop, stacked on mobile):
- **Name** (text input, required)
- **Balance** (currency input, required)
- **Interest Rate** (% input, required)
- **Min Payment** (currency input, required)
- **Expand arrow** to show rate segments
- **Delete button** (trash icon)
- **Drag handle** (visible only when Custom strategy selected, `GripVertical` icon)

**Rate Segments Sub-form** (expandable per debt):
- "Add Promotional Rate" button
- Each segment row: Rate %, Balance at this rate, Expiration date, Post-expiration rate
- Promo expiration warning badge when expiration < 3 months away

**Extra Monthly Payment Input**:
- Currency input below the debt list
- Helper text: "Amount above your total minimum payments ($1,015/mo)"

**Action Buttons**:
- "Add Debt" button (Plus icon)
- "Calculate" primary button
- "Reset" ghost button
- "Load Example" text button (fills in mock data)

### Strategy Comparison Section

After calculation, show 4 strategy result cards in a responsive grid (`grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4`):

Each strategy card:
- **Strategy name** as header with icon:
  - Avalanche: `TrendingDown` — "Lowest Interest"
  - Snowball: `Snowflake` — "Quick Wins"
  - CFI: `Banknote` — "Free Cash Flow"
  - Custom: `GripVertical` — "Your Order"
- **Total Interest**: Large number (mono font, red)
- **Payoff Date**: Month + Year
- **Total Months**: Duration
- **Total Paid**: Sum of all payments
- **"Best" badge**: Green badge on the strategy that saves the most interest
- **"Selected" ring**: Highlight border on the currently selected strategy

Clicking a card selects that strategy and updates the timeline chart and schedule below.

### Payoff Timeline Chart

**Recharts `<AreaChart>`** showing remaining balance over time:
- X-axis: Months (or dates)
- Y-axis: Dollar amount
- Stacked areas: One color per debt, showing how each debt's balance decreases
- Vertical markers where each debt reaches $0 (labeled with debt name)
- Promo expiration markers (dashed vertical line with label)
- Tooltip on hover: month, each debt's remaining balance, total payment that month

Chart is responsive — full width, `h-64 md:h-80 lg:h-96`

### Payment Schedule Table

Expandable accordion showing month-by-month details:

**Summary View** (default): Collapsed, showing:
- "Month 1 (Apr 2026)": Total payment, Total remaining balance, progress bar

**Expanded View**: Table per month showing:

| Debt | Payment | Principal | Interest | Remaining |
|------|---------|-----------|----------|-----------|
| Chase Sapphire | $670 | $493 | $177 | $8,007 |
| Student Loan | $250 | $149 | $101 | $21,851 |
| ... | ... | ... | ... | ... |
| **Total** | **$1,515** | | | **$47,058** |

- Paid-off debts show "PAID OFF" badge with strikethrough
- Mobile: Card layout per debt per month (only show months with activity)

### Debt Payoff Order Timeline

Visual timeline showing when each debt gets paid off:
- Horizontal timeline bar per debt
- Bar length proportional to payoff duration
- Color-coded by debt
- Shows payoff month label at the end of each bar
- Sorted by payoff order

## Promo Rate Warnings

When a debt has a promotional rate expiring within 3 months:
```tsx
<div className="flex items-center gap-2 rounded-md border border-amber-200 bg-amber-50 dark:bg-amber-950/20 px-3 py-2 text-sm">
  <AlertTriangle className="h-4 w-4 text-amber-500 shrink-0" />
  <span>
    <strong>PayPal Credit</strong>: 0% promo rate expires Aug 2026.
    Rate increases to 29.99% — consider paying off before expiration.
  </span>
</div>
```

## Premium Tier Gating

- **Free tier**: Basic calculator with all 4 strategies, up to 5 debts
- **Premium**: Unlimited debts, saved scenarios (future), export to PDF/CSV, account integration
- Show limit message when Free user tries to add 6th debt
- Export buttons show Premium lock icon for Free users

## Drag-and-Drop (Custom Strategy)

When "Custom" strategy is selected:
- Show `GripVertical` drag handles on each debt row
- Use a drag-and-drop library pattern (or simple up/down arrow buttons as fallback)
- Reordering updates `customOrder` on each `DebtEntry`
- Recalculate automatically on reorder

## Skeleton Loading

Not needed (client-side calculation is instant). Show a brief calculation animation (0.5s fade-in) for the results section when "Calculate" is clicked.

## Empty State

When no debts are entered:
- Calculator icon
- "Add your debts to get started"
- "Enter your debts to compare payoff strategies and see your path to debt freedom"
- "Load Example" button + "Add Debt" button

## Accessibility

- Debt form inputs have proper `<label>` associations
- Currency inputs use `inputMode="decimal"` for mobile keyboards
- Drag-and-drop has keyboard alternative (up/down arrow buttons)
- Strategy comparison cards are keyboard-selectable (`role="radiogroup"` / `role="radio"`)
- Chart has `aria-label` describing the trend
- Payment schedule accordion uses `aria-expanded` and `aria-controls`
- Promo warning uses `role="alert"`
- All interactive elements have minimum 44x44px touch targets

## i18n

- All text uses `useTranslation()` with `t("debtPayoff.strategy.avalanche")` pattern
- Create translation keys in `src/locales/en/debt-payoff.json`
- Currency/number formatting via `Intl.NumberFormat`
- Date formatting via `Intl.DateTimeFormat`
- Strategy descriptions should be translatable
