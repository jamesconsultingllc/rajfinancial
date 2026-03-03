# 11 — Budgeting & Debt Payoff

> Budget tracking, debt payoff strategies (Avalanche vs Snowball), payoff timeline projections, and monthly spending analysis.

**ADO Tracking:** [Epic #542 — 11 - Budgeting & Debt Payoff](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/542)

| # | Feature | State |
|---|---------|-------|
| [416](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/416) | Debt Payoff Analysis | New |

---

## Overview

RAJ Financial provides tools to help users take control of their debt and build healthy financial habits. The Budgeting & Debt Payoff module includes:

1. **Debt Payoff Calculator** — Compare Avalanche (highest interest first) vs Snowball (lowest balance first) strategies
2. **Payoff Timeline** — Visualize the path to debt freedom with month-by-month projections
3. **Strategy Comparison** — Side-by-side analysis showing total interest paid and payoff dates
4. **Budget Tracking** — (Future) Monthly spending budgets by category with alerts

> Budgeting features use `DataCategory.Accounts` for data sharing grants (same as transactions).

---

## Design Goals

| Goal | Description |
|------|-------------|
| **Calculator-first** | Works without linked accounts — users enter debts manually |
| **Visual payoff path** | Timeline chart showing debt reduction over time |
| **Actionable comparison** | Clear winner between strategies with savings highlighted |
| **Mobile-friendly** | Swipeable cards and collapsible debt list on mobile |
| **Tier-gated** | Basic calculator for all; saved scenarios and integration with accounts is Premium |

---

## Debt Payoff Calculator

### How It Works

Users enter their debts (name, balance, interest rate, minimum payment) and an optional extra monthly payment. The calculator computes:

1. **Avalanche Method** — Pay minimums on all debts, apply extra payment to highest interest rate debt first. Mathematically optimal.
2. **Snowball Method** — Pay minimums on all debts, apply extra payment to lowest balance debt first. Psychologically motivating (quick wins).
3. **Cash Flow Index (CFI) Method** — Pay minimums on all debts, apply extra payment to debt with lowest CFI first. CFI = Balance ÷ Minimum Payment. Optimizes for freeing up cash flow fastest.

All methods show:
- Total months to payoff
- Total interest paid
- Payoff date
- Month-by-month schedule

### DebtItem

```typescript
/** A single debt entered by the user. */
interface DebtItem {
  id: string;
  name: string;
  balance: number;
  minimumPayment: number;
  /** Optional — links to a LinkedAccount for Premium users. */
  linkedAccountId?: string;
  /**
   * Interest rate segments. Supports introductory/promotional rates.
   * For simple debts, this is a single segment with no expiration.
   * For promo rates (e.g., PayPal "0% for 6 months"), multiple segments
   * can specify different rates that expire at different times.
   */
  rateSegments: RateSegment[];
}

/**
 * A time-bound interest rate segment.
 * Enables modeling of introductory rates like "0% APR for 6 months,
 * then 24.99% APR" or multiple purchases on one account with
 * different promo expirations.
 */
interface RateSegment {
  id: string;
  /** Annual APR as decimal (e.g., 0.1899 for 18.99%, 0 for 0%) */
  interestRate: number;
  /** Balance portion this rate applies to (for split-rate accounts) */
  balance: number;
  /** Optional description (e.g., "TV Purchase", "Balance Transfer") */
  description?: string;
  /** 
   * When this rate expires and reverts to standard rate.
   * Null means permanent (standard rate, no expiration).
   */
  expirationDate?: string;  // ISO date string
  /** 
   * Rate to use after expiration. If null, uses the account's
   * default/standard rate (highest rate segment with no expiration).
   */
  postExpirationRate?: number;
}
```

> **Example**: PayPal account with multiple 0% purchases:
> ```typescript
> {
>   id: "paypal-1",
>   name: "PayPal Credit",
>   balance: 2500,  // Total balance
>   minimumPayment: 50,
>   rateSegments: [
>     { id: "tv", interestRate: 0, balance: 800, description: "TV Purchase", 
>       expirationDate: "2026-06-01", postExpirationRate: 0.2499 },
>     { id: "laptop", interestRate: 0, balance: 1200, description: "Laptop", 
>       expirationDate: "2026-09-01", postExpirationRate: 0.2499 },
>     { id: "standard", interestRate: 0.2499, balance: 500, description: "Standard" }
>   ]
> }
> ```

### PayoffRequest

```typescript
/** Request to calculate debt payoff strategies. */
interface PayoffRequest {
  debts: DebtItem[];
  extraMonthlyPayment: number;
}
```

### PayoffResult

```typescript
/** Result of payoff calculation for a single strategy. */
interface PayoffResult {
  strategy: 'avalanche' | 'snowball';
  totalMonths: number;
  totalInterestPaid: number;
  payoffDate: string;           // ISO date string
  schedule: PayoffMonth[];
}

/** A single month in the payoff schedule. */
interface PayoffMonth {
  month: number;
  date: string;                 // ISO date string (first of month)
  payments: DebtPayment[];
  totalPaid: number;
  totalRemainingBalance: number;
}

/** Payment applied to a single debt in a month. */
interface DebtPayment {
  debtId: string;
  debtName: string;
  payment: number;
  principal: number;
  interest: number;
  remainingBalance: number;
  isPaidOff: boolean;
}
```

### StrategyComparison

```typescript
/** Payoff strategy types. */
type PayoffStrategy = 'avalanche' | 'snowball' | 'cashFlowIndex' | 'custom';

/** Result of payoff calculation for a single strategy. */
interface PayoffResult {
  strategy: PayoffStrategy;
  totalMonths: number;
  totalInterestPaid: number;
  payoffDate: string;           // ISO date string
  schedule: PayoffMonth[];
}

/** 
 * Custom strategy configuration.
 * User defines their own payoff order by dragging debts into priority order.
 */
interface CustomStrategyConfig {
  /** Debt IDs in priority order (first = pay off first). */
  payoffOrder: string[];
}

/** Side-by-side comparison of all strategies. */
interface StrategyComparison {
  avalanche: PayoffResult;
  snowball: PayoffResult;
  cashFlowIndex: PayoffResult;
  /** Custom strategy result (only present if user configured one). */
  custom?: PayoffResult;
  /** Best strategy based on total interest paid. */
  lowestInterest: PayoffStrategy;
  /** Best strategy based on time to payoff. */
  fastestPayoff: PayoffStrategy;
  /** Best strategy for freeing up cash flow quickly. */
  bestCashFlow: PayoffStrategy;
  /** Interest savings of best vs worst strategy. */
  maxInterestSavings: number;
  /** Recommendation with reasoning. */
  recommendedStrategy: PayoffStrategy;
  recommendation: string;        // Human-readable recommendation
}
```

---

## Calculation Logic

### Avalanche Method

```typescript
/**
 * Avalanche: Pay off highest interest rate first.
 * Mathematically optimal — minimizes total interest paid.
 * 
 * For debts with rate segments, uses the effective rate (weighted average
 * of current rates, accounting for promo expirations).
 */
function calculateAvalanche(debts: DebtItem[], extraPayment: number): PayoffResult {
  // Sort by effective interest rate descending
  const sorted = [...debts].sort((a, b) => 
    getEffectiveRate(b) - getEffectiveRate(a)
  );
  return calculatePayoff(sorted, extraPayment, 'avalanche');
}

/** 
 * Gets the effective (weighted average) interest rate for a debt.
 * Accounts for multiple rate segments with different balances.
 */
function getEffectiveRate(debt: DebtItem): number {
  if (debt.rateSegments.length === 1) {
    return debt.rateSegments[0].interestRate;
  }
  const totalBalance = debt.rateSegments.reduce((sum, s) => sum + s.balance, 0);
  if (totalBalance === 0) return 0;
  return debt.rateSegments.reduce(
    (sum, s) => sum + (s.interestRate * s.balance / totalBalance), 0
  );
}
```

### Snowball Method

```typescript
/**
 * Snowball: Pay off lowest balance first.
 * Psychologically motivating — quick wins build momentum.
 */
function calculateSnowball(debts: DebtItem[], extraPayment: number): PayoffResult {
  // Sort by balance ascending
  const sorted = [...debts].sort((a, b) => a.balance - b.balance);
  return calculatePayoff(sorted, extraPayment, 'snowball');
}
```

### Cash Flow Index Method

```typescript
/**
 * Cash Flow Index (CFI): Pay off lowest CFI first.
 * CFI = Balance ÷ Minimum Payment
 * 
 * Lower CFI = debt is "hogging" more cash flow relative to its size.
 * Paying these off first frees up cash flow faster.
 * 
 * Example:
 *   Debt A: $5,000 balance, $200 min payment → CFI = 25
 *   Debt B: $10,000 balance, $150 min payment → CFI = 66.7
 *   Pay Debt A first — it frees up $200/month with only $5K to pay.
 */
function calculateCashFlowIndex(debts: DebtItem[], extraPayment: number): PayoffResult {
  // Sort by CFI ascending (lowest CFI first)
  const sorted = [...debts].sort((a, b) => 
    getCashFlowIndex(a) - getCashFlowIndex(b)
  );
  return calculatePayoff(sorted, extraPayment, 'cashFlowIndex');
}

function getCashFlowIndex(debt: DebtItem): number {
  if (debt.minimumPayment === 0) return Infinity;
  return debt.balance / debt.minimumPayment;
}
```

### Custom Strategy Method

```typescript
/**
 * Custom: User defines their own payoff order.
 * Useful when user has personal reasons to prioritize certain debts
 * (e.g., pay off family loan first for relationship reasons,
 * or clear a specific card to close the account).
 */
function calculateCustom(
  debts: DebtItem[], 
  extraPayment: number,
  customOrder: string[]
): PayoffResult {
  // Sort debts by user's custom order
  const debtMap = new Map(debts.map(d => [d.id, d]));
  const sorted: DebtItem[] = [];
  
  // First, add debts in user's specified order
  for (const id of customOrder) {
    const debt = debtMap.get(id);
    if (debt) {
      sorted.push(debt);
      debtMap.delete(id);
    }
  }
  
  // Then, add any remaining debts not in the custom order (shouldn't happen, but safety)
  for (const debt of debtMap.values()) {
    sorted.push(debt);
  }
  
  return calculatePayoff(sorted, extraPayment, 'custom');
}
```

### Handling Promotional Rate Expirations

```typescript
/**
 * Calculates monthly interest for a debt, accounting for rate segments
 * and promotional rate expirations.
 * 
 * When a promo rate expires mid-calculation:
 * 1. The segment's rate changes to postExpirationRate
 * 2. For avalanche strategy, debt priority may need re-sorting
 */
function calculateMonthlyInterest(
  debt: DebtItem, 
  currentDate: Date
): { interest: number; segmentInterests: Map<string, number> } {
  const segmentInterests = new Map<string, number>();
  let totalInterest = 0;

  for (const segment of debt.rateSegments) {
    // Check if promo has expired
    let effectiveRate = segment.interestRate;
    if (segment.expirationDate) {
      const expDate = new Date(segment.expirationDate);
      if (currentDate >= expDate && segment.postExpirationRate !== undefined) {
        effectiveRate = segment.postExpirationRate;
      }
    }

    const monthlyRate = effectiveRate / 12;
    const segmentInterest = segment.balance * monthlyRate;
    segmentInterests.set(segment.id, segmentInterest);
    totalInterest += segmentInterest;
  }

  return { interest: totalInterest, segmentInterests };
}
```

### Promo Expiration Warning

```typescript
/**
 * Identifies debts with promos expiring soon that should be prioritized
 * to avoid deferred interest charges.
 */
function getExpiringPromos(debts: DebtItem[], withinMonths: number = 3): PromoWarning[] {
  const warnings: PromoWarning[] = [];
  const cutoffDate = new Date();
  cutoffDate.setMonth(cutoffDate.getMonth() + withinMonths);

  for (const debt of debts) {
    for (const segment of debt.rateSegments) {
      if (segment.expirationDate && segment.interestRate === 0) {
        const expDate = new Date(segment.expirationDate);
        if (expDate <= cutoffDate) {
          warnings.push({
            debtId: debt.id,
            debtName: debt.name,
            segmentId: segment.id,
            segmentDescription: segment.description,
            balance: segment.balance,
            expirationDate: segment.expirationDate,
            postExpirationRate: segment.postExpirationRate ?? 0,
            monthsUntilExpiration: Math.ceil(
              (expDate.getTime() - Date.now()) / (30 * 24 * 60 * 60 * 1000)
            ),
          });
        }
      }
    }
  }

  return warnings.sort((a, b) => 
    new Date(a.expirationDate).getTime() - new Date(b.expirationDate).getTime()
  );
}

interface PromoWarning {
  debtId: string;
  debtName: string;
  segmentId: string;
  segmentDescription?: string;
  balance: number;
  expirationDate: string;
  postExpirationRate: number;
  monthsUntilExpiration: number;
}
```

### Core Payoff Algorithm

```typescript
function calculatePayoff(
  sortedDebts: DebtItem[],
  extraPayment: number,
  strategy: PayoffStrategy
): PayoffResult {
  // Deep clone debts to track segment balances independently
  const debtState = sortedDebts.map(d => ({
    ...d,
    rateSegments: d.rateSegments.map(s => ({ ...s })),
  }));
  
  const schedule: PayoffMonth[] = [];
  let month = 0;
  let totalInterest = 0;
  const startDate = new Date();
  startDate.setDate(1); // First of current month

  while (hasRemainingBalance(debtState)) {
    month++;
    const monthDate = new Date(startDate);
    monthDate.setMonth(monthDate.getMonth() + month);

    // For avalanche, re-sort each month to account for rate changes from promo expirations
    if (strategy === 'avalanche') {
      debtState.sort((a, b) => getEffectiveRate(b, monthDate) - getEffectiveRate(a, monthDate));
    }

    let availableExtra = extraPayment;
    const payments: DebtPayment[] = [];

    for (const debt of debtState) {
      const balance = getTotalBalance(debt);
      if (balance <= 0) continue;

      // Calculate monthly interest accounting for rate segments and expirations
      const { interest } = calculateMonthlyInterest(debt, monthDate);
      totalInterest += interest;

      // Determine payment amount
      let payment = debt.minimumPayment;

      // Apply extra payment to the priority debt (first with balance)
      if (availableExtra > 0 && isFirstWithBalance(debt, debtState)) {
        payment += availableExtra;
        availableExtra = 0;
      }

      // Don't overpay
      const maxPayment = balance + interest;
      payment = Math.min(payment, maxPayment);

      const principal = payment - interest;
      
      // Apply principal to segments (highest rate first within this debt)
      applyPrincipalToSegments(debt, principal, monthDate);
      
      const newBalance = getTotalBalance(debt);

      payments.push({
        debtId: debt.id,
        debtName: debt.name,
        payment,
        principal,
        interest,
        remainingBalance: newBalance,
        isPaidOff: newBalance === 0,
      });
    }

    schedule.push({
      month,
      date: monthDate.toISOString().slice(0, 10),
      payments,
      totalPaid: payments.reduce((sum, p) => sum + p.payment, 0),
      totalRemainingBalance: debtState.reduce((sum, d) => sum + getTotalBalance(d), 0),
    });

    // Safety: prevent infinite loop (max 360 months = 30 years)
    if (month > 360) break;
  }

  const payoffDate = schedule[schedule.length - 1]?.date ?? startDate.toISOString().slice(0, 10);

  return {
    strategy,
    totalMonths: month,
    totalInterestPaid: Math.round(totalInterest * 100) / 100,
    payoffDate,
    schedule,
  };
}
```

---

## API Endpoints

### Debt Payoff Calculator (Stateless)

```
POST   /api/debt-payoff/calculate         → Calculate payoff strategies
```

This endpoint is **stateless** — it doesn't save anything. Users pass debts in the request body and get back the comparison.

### Query Parameters for `POST /api/debt-payoff/calculate`

Request body: `PayoffRequest`

Response: `StrategyComparison`

### Example Request

```json
{
  "debts": [
    { "id": "1", "name": "Credit Card", "balance": 5000, "interestRate": 0.1899, "minimumPayment": 150 },
    { "id": "2", "name": "Car Loan", "balance": 12000, "interestRate": 0.0649, "minimumPayment": 300 },
    { "id": "3", "name": "Student Loan", "balance": 25000, "interestRate": 0.0499, "minimumPayment": 250 }
  ],
  "extraMonthlyPayment": 200
}
```

### Example Response

```json
{
  "avalanche": {
    "strategy": "avalanche",
    "totalMonths": 38,
    "totalInterestPaid": 4823.45,
    "payoffDate": "2029-05-01",
    "schedule": [...]
  },
  "snowball": {
    "strategy": "snowball",
    "totalMonths": 40,
    "totalInterestPaid": 5234.12,
    "payoffDate": "2029-07-01",
    "schedule": [...]
  },
  "interestSavings": 410.67,
  "monthsSaved": 2,
  "recommendedStrategy": "avalanche",
  "recommendation": "Avalanche saves you $410.67 in interest and pays off debt 2 months faster."
}
```

---

## Service Interface

```csharp
/// <summary>
/// Debt payoff calculation service.
/// Stateless — no database storage. Calculations are done in-memory.
/// </summary>
public interface IDebtPayoffService
{
    /// <summary>
    /// Calculates both Avalanche and Snowball payoff strategies
    /// and returns a comparison.
    /// </summary>
    StrategyComparisonDto Calculate(PayoffRequestDto request);
}
```

---

## UI Design

### Route & Layout

> **Route**: `/tools/debt-payoff`
> **Auth Policy**: `RequireClient`
> **Layout**: `DashboardLayout` (sidebar)

### Page Structure

```
┌──────────────────────────────────────────────────────────┐
│  Debt Payoff Calculator                                   │
│  Find the fastest path to debt freedom                    │
├──────────────────────────────────────────────────────────┤
│  ⚠️ PROMO ALERT: PayPal TV Purchase ($800) 0% expires    │
│     in 3 months! Pay it off to avoid 24.99% interest.    │
├──────────────────────────────────────────────────────────┤
│  Your Debts                              [+ Add Debt]     │
│  ┌────────────────────────────────────────────────────┐  │
│  │ ≡ 💳 Credit Card        $5,000   18.99% APR  $150  │  │
│  │ ≡ 🚗 Car Loan          $12,000    6.49% APR  $300  │  │
│  │ ≡ 🎓 Student Loan      $25,000    4.99% APR  $250  │  │
│  │ ≡ 🅿️ PayPal Credit      $2,500   Mixed rates  $50  │  │
│  │      ├─ TV Purchase      $800    0% → 24.99% Jun   │  │
│  │      ├─ Laptop         $1,200    0% → 24.99% Sep   │  │
│  │      └─ Standard         $500   24.99% APR         │  │
│  └────────────────────────────────────────────────────┘  │
│  ≡ = drag handle for custom order                        │
│                                                           │
│  Extra Monthly Payment                                    │
│  ┌─────────────────────────────────────────────┐         │
│  │  $200                                       │         │
│  └─────────────────────────────────────────────┘         │
│                                                           │
│  [Calculate Payoff Strategies]                            │
├──────────────────────────────────────────────────────────┤
│  Strategy Comparison                                      │
│  ┌─────────────┐┌─────────────┐┌─────────────┐┌─────────┐│
│  │⚡ AVALANCHE ││❄️ SNOWBALL  ││💸 CASH FLOW ││🎯 CUSTOM││
│  │Best Interest││             ││   INDEX     ││         ││
│  │             ││             ││             ││Your     ││
│  │ 38 months   ││ 40 months   ││ 39 months   ││Order    ││
│  │ $4,823 int  ││ $5,234 int  ││ $5,012 int  ││         ││
│  │ May 2029    ││ July 2029   ││ June 2029   ││41 months││
│  │             ││             ││Frees $350/mo││$5,456int││
│  │             ││             ││fastest      ││Aug 2029 ││
│  └─────────────┘└─────────────┘└─────────────┘└─────────┘│
│                                                           │
│  💰 Avalanche saves you $410.67 vs Snowball!             │
│  💸 Cash Flow Index frees up cash flow 4 months faster!  │
│  🎯 Custom order costs $633 more than Avalanche          │
├──────────────────────────────────────────────────────────┤
│  Payoff Timeline                           [Avalanche ▾]  │
│  ┌────────────────────────────────────────────────────┐  │
│  │  📊 Stacked area chart showing debt reduction      │  │
│  │     over time. Each debt is a different color.     │  │
│  │     X-axis: months, Y-axis: total balance          │  │
│  └────────────────────────────────────────────────────┘  │
├──────────────────────────────────────────────────────────┤
│  Payment Schedule                         [View All →]    │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Month 1 (Apr 2026)          $900 total            │  │
│  │    Credit Card: $350 → $4,729 remaining            │  │
│  │    PayPal (TV): $200 → $600 remaining ⚠️ promo     │  │
│  │    Car Loan: $300 → $11,765 remaining              │  │
│  │    Student Loan: $250 → $24,854 remaining          │  │
│  │                                                     │  │
│  │  Month 2 (May 2026)          $900 total            │  │
│  │    ...                                              │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

### Mobile Layout

On mobile (< md breakpoint):
1. Debt list as swipeable cards (swipe to delete)
2. Strategy comparison as horizontal scroll cards
3. Timeline chart with horizontal scroll
4. Collapsible payment schedule accordion

### Components

| Component | File | Description |
|-----------|------|-------------|
| `DebtPayoffPage` | `pages/tools/DebtPayoff.tsx` | Main page with form and results |
| `DebtForm` | `components/debt/DebtForm.tsx` | Add/edit debt dialog with rate segments |
| `RateSegmentForm` | `components/debt/RateSegmentForm.tsx` | Add promo/intro rate segments |
| `DebtListItem` | `components/debt/DebtListItem.tsx` | Draggable debt card with segments |
| `DebtList` | `components/debt/DebtList.tsx` | Drag-and-drop sortable list for custom order |
| `PromoWarningBanner` | `components/debt/PromoWarningBanner.tsx` | Expiring promo alert |
| `StrategyCard` | `components/debt/StrategyCard.tsx` | Strategy result card |
| `StrategyComparison` | `components/debt/StrategyComparison.tsx` | All 4 strategy cards with insights |
| `PayoffTimeline` | `components/debt/PayoffTimeline.tsx` | Stacked area chart |
| `PayoffSchedule` | `components/debt/PayoffSchedule.tsx` | Month-by-month payment table |

---

## React Query Hooks

```typescript
import { useMutation } from '@tanstack/react-query';

/**
 * Calculates debt payoff strategies.
 * Uses mutation (not query) because it's a calculation request, not cached data.
 */
export function useCalculatePayoff() {
  return useMutation({
    mutationFn: (request: PayoffRequest) =>
      api.post<StrategyComparison>('/debt-payoff/calculate', request),
  });
}
```

### Local State for Debts

Since the calculator is stateless, debts are stored in React state (not server):

```typescript
const [debts, setDebts] = useState<DebtItem[]>([]);
const [extraPayment, setExtraPayment] = useState(0);
const calculatePayoff = useCalculatePayoff();

function handleCalculate() {
  calculatePayoff.mutate({ debts, extraMonthlyPayment: extraPayment });
}
```

---

## Tier Gating

| Feature | Free | Premium |
|---------|------|---------|
| Basic calculator (manual entry) | ✅ | ✅ |
| Strategy comparison | ✅ | ✅ |
| Payoff timeline chart | ✅ | ✅ |
| Payment schedule (first 6 months) | ✅ | ✅ |
| Full payment schedule | ❌ | ✅ |
| Save scenarios | ❌ | ✅ |
| Link to actual accounts | ❌ | ✅ |
| Export schedule (CSV/PDF) | ❌ | ✅ |

---

## Validation Rules

| Rule | Level | Message |
|------|-------|---------|
| At least one debt required | Error | Add at least one debt to calculate |
| Balance must be > 0 | Error | Balance must be greater than zero |
| Interest rate must be 0–100% | Error | Interest rate must be between 0% and 100% |
| Minimum payment must be > 0 | Error | Minimum payment must be greater than zero |
| Minimum payment should cover interest | Warning | Minimum payment may not cover monthly interest |
| Extra payment must be >= 0 | Error | Extra payment cannot be negative |
| Max 20 debts | Error | Maximum 20 debts allowed |

---

## Error Codes

```typescript
const DebtPayoffErrorCodes = {
  NO_DEBTS: 'DEBT_PAYOFF_NO_DEBTS',
  INVALID_BALANCE: 'DEBT_PAYOFF_INVALID_BALANCE',
  INVALID_RATE: 'DEBT_PAYOFF_INVALID_RATE',
  INVALID_PAYMENT: 'DEBT_PAYOFF_INVALID_PAYMENT',
  TOO_MANY_DEBTS: 'DEBT_PAYOFF_TOO_MANY_DEBTS',
  CALCULATION_FAILED: 'DEBT_PAYOFF_CALCULATION_FAILED',
} as const;
```

---

## Future Considerations

| Feature | Priority | Notes |
|---------|----------|-------|
| Budget categories | P2 | Set monthly budgets per spending category |
| Budget alerts | P2 | Notify when approaching/exceeding budget |
| Saved debt scenarios | P2 | Save and compare multiple payoff plans |
| Link to LinkedAccounts | P2 | Auto-populate debts from credit cards/loans |
| What-if analysis | P3 | "What if I add $100/month extra?" slider |
| Debt-free celebration | P3 | Confetti animation when projected payoff reached |
| Recurring debt tracking | P3 | Track actual payments vs projected |

---

## Navigation

Add to `DashboardLayout` sidebar under "Planning Tools":

```typescript
{ label: "Debt Payoff", icon: TrendingDown, href: "/tools/debt-payoff" },
```

Add route to `App.tsx`:

```tsx
<Route path="/tools/debt-payoff" element={
  <ProtectedRoute policy="RequireClient"><DebtPayoffPage /></ProtectedRoute>
} />
```

---

## Cross-References

- Linked accounts (for Premium debt linking): [06-accounts-transactions.md](06-accounts-transactions.md)
- Transaction categories (for future budgeting): [06-accounts-transactions.md](06-accounts-transactions.md)
- Dashboard spending widget: [07-dashboard-reporting.md](07-dashboard-reporting.md)
- Tier model & gating: [01-platform-infrastructure.md](01-platform-infrastructure.md)

---

*Last Updated: March 2026*
