# Lovable Prompt — Entity Overview & Household Dashboard

## Context

Build **two** pages that replace the single Dashboard:

1. **Entity Overview** at `/:entityType/:slug/overview` — the financial overview for a **single** entity (Personal, Business, or Trust). Shows the same 8 widgets (net worth, asset allocation, account balances, spending summary, recent transactions, beneficiary coverage, spending trends, alerts) but scoped to one entity. This page renders inside an `EntityProvider` so all data is fetched by `entityId`.

2. **Household Dashboard** at `/household/dashboard` — an **aggregated** view across **all** entities the user owns or has beneficial interest in. Introduces "Your Share" calculations based on ownership percentage, entity quick cards, and combined alerts. This page has no entity context — it calls a household-level API.

Follow the same layout patterns as the existing **Assets** page and **Settings** page. Use Recharts for all charts.

## Route & Nav

### Entity Overview
- **Route**: `/:entityType/:slug/overview` (e.g., `/personal/overview`, `/business/acme-llc/overview`)
- **Nav**: Shown as "Overview" in the entity-scoped sidebar when an entity is selected
- **Page file**: `src/pages/EntityOverview.tsx`

### Household Dashboard
- **Route**: `/household/dashboard`
- **Nav**: Shown as "Household" or "Dashboard" at the top of the global sidebar (above entity list)
- **Page file**: `src/pages/HouseholdDashboard.tsx`

Both routes require `ProtectedRoute policy="RequireClient"`.

## Entity Context

**Entity Overview** renders inside an `EntityProvider`:
```tsx
const { entityId, entityType, entitySlug, entityName } = useEntityContext();
```
All data fetching is scoped to `entityId`. The page title displays the entity name (e.g., "Acme LLC — Overview").

**Household Dashboard** has no entity context — it aggregates across all entities the authenticated user can access.

## Data Types

Create `src/types/dashboard.ts`:

```typescript
/** Shared types used by both Entity Overview and Household Dashboard */

interface AssetAllocationItem {
  assetType: string;
  label: string;
  value: number;
  percentage: number;
  color: string; // Chart color
}

interface AccountBalanceItem {
  id: string;
  name: string;
  institutionName: string;
  type: string;
  currentBalance: number;
  mask?: string;
  source: "Plaid" | "Manual";
  lastSyncAt?: string;
}

interface RecentTransactionItem {
  id: string;
  date: string;
  name: string;
  merchantName?: string;
  amount: number;
  category?: string;
  accountName: string;
}

interface SpendingSummaryDto {
  totalIncome: number;
  totalExpenses: number;
  netCashFlow: number;
  periodStart: string;
  periodEnd: string;
  topCategories: CategorySpendItem[];
}

interface CategorySpendItem {
  category: string;
  amount: number;
  percentage: number;
  transactionCount: number;
}

interface BeneficiaryCoverageDto {
  totalAssets: number;
  coveredAssets: number;
  coveragePercentage: number;
  level: "Critical" | "NeedsAttention" | "Good" | "Complete";
}

interface MonthlyTrendDto {
  month: string; // "2026-01"
  income: number;
  expenses: number;
  netCashFlow: number;
}

type AlertSeverity = "Critical" | "Warning" | "Info";

interface DashboardAlertDto {
  code: string;
  severity: AlertSeverity;
  title: string;
  description: string;
  actionLabel?: string;
  actionPath?: string;
  entityId?: string;   // Present in household alerts to identify source entity
  entityName?: string; // Present in household alerts for badge display
}

/** Entity Overview — single entity summary */
interface DashboardSummaryDto {
  entityId: string;
  netWorth: number;
  totalAssets: number;
  totalLiabilities: number;
  totalAccountBalance: number;
  assetAllocation: AssetAllocationItem[];
  accountBalances: AccountBalanceItem[];
  recentTransactions: RecentTransactionItem[];
  spendingSummary: SpendingSummaryDto;
  beneficiaryCoverage: BeneficiaryCoverageDto;
  spendingTrends: MonthlyTrendDto[]; // Premium only
  alerts: DashboardAlertDto[];
}

/** Household Dashboard — aggregated across all entities */
interface HouseholdDashboardDto {
  totalNetWorth: number;
  yourShareNetWorth: number;
  totalAssets: number;
  totalLiabilities: number;
  entities: EntityQuickCard[];
  aggregatedAssetAllocation: AssetAllocationItem[];
  aggregatedSpendingSummary: SpendingSummaryDto;
  aggregatedSpendingTrends: MonthlyTrendDto[]; // Premium only
  aggregatedAlerts: DashboardAlertDto[];
}

interface EntityQuickCard {
  entityId: string;
  entityName: string;
  entityType: "Personal" | "Business" | "Trust";
  entitySlug: string;
  netWorth: number;
  yourShare: number;
  ownershipPercent: number;
}
```

## API Service

Create `src/services/dashboard-service.ts` following the same TanStack Query pattern as `asset-service.ts`:

### Entity Overview
- `GET /api/entities/{entityId}/overview` -> `useEntityOverview(entityId)` — fetch single-entity dashboard summary
- Returns `DashboardSummaryDto`

### Household Dashboard
- `GET /api/household/dashboard` -> `useHouseholdDashboard()` — fetch aggregated household dashboard
- Returns `HouseholdDashboardDto`

Use mock data initially (same pattern as assets — return mock from the hooks until the API is ready).

## Mock Data

### Entity Overview Mock (Personal entity)

Same as current single-entity data:
- **Net Worth**: $745,000 (assets $932,000 - liabilities $187,000)
- **Asset Allocation**: RealEstate 45%, Investment 25%, Retirement 15%, Vehicle 8%, Other 7%
- **Account Balances**: 4-5 accounts (Chase Checking $15,420, Marcus Savings $45,000, Fidelity $312,000, Vanguard 401k $198,500, Amex -$2,340)
- **Spending Summary**: Income $12,500, Expenses $8,200, Net $4,300 with top categories (Housing $2,800, Food $1,200, Transport $600, Subscriptions $350)
- **Beneficiary Coverage**: 7/10 assets covered = 70% "Good"
- **Recent Transactions**: 5 recent mock transactions
- **Spending Trends**: 6 months of mock monthly data
- **Alerts**: 2-3 alerts (BENEFICIARY_COVERAGE_LOW warning, NO_ACCOUNTS_LINKED info)

### Household Dashboard Mock

**Entities:**
- **Personal**: Net Worth $745,000 (100% ownership = $745,000 your share)
- **Business "Acme LLC"**: Net Worth $500,000 (40% ownership = $200,000 your share)
- **Trust "Family Trust"**: Net Worth $300,000 (100% beneficial interest = $300,000 your share)

**Aggregated:**
- **Household Total Net Worth**: $1,545,000
- **Your Share Net Worth**: $1,245,000
- **Aggregated Alerts**: Combined from all entities, each tagged with entity name badge
- **Aggregated Asset Allocation**: Merged allocation across all entities (weighted by your share)
- **Aggregated Spending Summary**: Combined income/expenses across all entities

## Page Layout

### Entity Overview Layout

Responsive grid layout using CSS Grid, mobile-first:

```
Mobile (< md):        Tablet (md):              Desktop (lg+):
[Net Worth Card]      [Net Worth] [Spending]    [Net Worth] [Spending] [Beneficiary]
[Spending Summary]    [Allocation][Balances]     [Allocation Chart  ] [Balances     ]
[Beneficiary]         [Transactions          ]   [Recent Transactions] [Trends Chart ]
[Allocation Chart]    [Trends Chart          ]   [Alerts                             ]
[Account Balances]    [Alerts                ]
[Transactions]
[Trends (Premium)]
[Alerts]
```

Use: `grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 md:gap-6`

Page title: `{entityName} — Overview` (e.g., "Acme LLC — Overview")

### Household Dashboard Layout

```
Mobile (< md):            Tablet (md):                    Desktop (lg+):
[Aggregated Net Worth]    [Aggregated Net Worth        ]  [Aggregated Net Worth         ]
[Entity Card: Personal]   [Entity Card] [Entity Card  ]  [Entity Card] [Entity] [Entity]
[Entity Card: Acme LLC]   [Entity Card                 ]  [Agg Allocation] [Agg Spending]
[Entity Card: Family Tr]  [Agg Allocation][Agg Spending]  [Agg Trends Chart             ]
[Agg Allocation]          [Agg Trends Chart            ]  [Aggregated Alerts             ]
[Agg Spending Summary]    [Aggregated Alerts           ]
[Agg Trends (Premium)]
[Aggregated Alerts]
```

Use: `grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 md:gap-6`

Page title: "Household Dashboard"

## Widget Components

Create each widget as a separate component in `src/components/dashboard/`. Widgets are shared between both pages where possible.

### 1. NetWorthCard.tsx (Entity Overview)
- Large hero number: "$745,000" (mono font, formatted with commas)
- Subtitle row: "Assets: $932K | Liabilities: $187K"
- Color: green for positive net worth, red for negative
- Icon: `TrendingUp` from lucide-react

### 2. AggregatedNetWorthCard.tsx (Household Dashboard)
- Two large hero numbers side by side:
  - "Total: $1,545,000" (muted text)
  - "Your Share: $1,245,000" (primary, emphasized)
- Subtitle: "Across 3 entities"
- Icon: `TrendingUp` from lucide-react

### 3. EntityQuickCards.tsx (Household Dashboard only)
- Row of cards, one per entity: `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3`
- Each card shows:
  - Entity name (e.g., "Acme LLC")
  - Entity type badge: "Personal" (blue), "Business" (purple), "Trust" (amber)
  - Net Worth: "$500,000"
  - Your Share: "$200,000" with ownership percentage "(40%)"
  - Click navigates to entity overview: `/:entityType/:slug/overview`

### 4. AssetAllocationChart.tsx (Shared)
- **Recharts** donut/pie chart (`<PieChart>` with `<Pie>` innerRadius for donut)
- Color-coded segments matching asset types
- Legend below chart showing type + percentage + dollar amount
- Center label showing total portfolio value
- In Household Dashboard, values reflect aggregated allocation weighted by ownership

### 5. AccountBalancesGrid.tsx (Entity Overview only)
- Card grid: `grid grid-cols-1 sm:grid-cols-2 gap-3`
- Each mini-card shows: institution logo placeholder (colored circle), account name, mask, balance (green/red), source badge
- "View All" link: entity-relative path using entity context (e.g., `/:entityType/:slug/accounts`)

### 6. SpendingSummaryCard.tsx (Shared)
- Three stat boxes in a row: Income (green), Expenses (red), Net (green/red)
- Below: horizontal bar chart or list of top 5 spending categories with amounts and percentage bars
- Period label: "March 2026" (current month for Free tier)
- In Household Dashboard, shows aggregated totals across all entities

### 7. RecentTransactionsCard.tsx (Entity Overview only)
- List of last 5 transactions
- Each row: date, merchant/name, category badge, amount (green income / red expense), account name (muted)
- "View All" link: entity-relative path using entity context (e.g., `/:entityType/:slug/transactions`)
- Empty state: "No transactions yet"

### 8. BeneficiaryCoverageCard.tsx (Entity Overview only)
- Progress bar (circular or linear) showing coverage percentage
- Color coded: <50% red, 50-74% amber, 75-99% green, 100% emerald
- Text: "7 of 10 assets covered"
- Level badge: "Good" / "Critical" / etc.
- "Review" link: entity-relative path using entity context (e.g., `/:entityType/:slug/beneficiaries`)

### 9. SpendingTrendsChart.tsx (Shared — Premium only)
- **Recharts** stacked area or bar chart (`<AreaChart>` or `<BarChart>`)
- 6-month view: income vs expenses with net cash flow line
- X-axis: month labels, Y-axis: dollar amounts
- Show Premium lock overlay for Free tier users with "Upgrade to Premium" button
- Tooltip on hover showing month details
- In Household Dashboard, shows aggregated trends across all entities

### 10. AlertsBanner.tsx (Shared)
- Stack of alert cards at bottom of page
- Color by severity: Critical (red/destructive), Warning (amber), Info (blue)
- Each alert: icon + title + description + optional action button
- **Household Dashboard**: Each alert also shows an entity name badge (e.g., "Acme LLC") identifying which entity the alert belongs to. Action paths link to the entity-relative route.
- Dismissible (client-side only, no API)
- Alert codes map to icons: `AlertTriangle` (warning), `AlertCircle` (critical), `Info` (info)

## Navigation Links

All "View All" and action links must use entity-relative paths:

### Entity Overview
Links use the current entity context from `useEntityContext()`:
- Accounts: `/${entityType}/${entitySlug}/accounts`
- Transactions: `/${entityType}/${entitySlug}/transactions`
- Beneficiaries: `/${entityType}/${entitySlug}/beneficiaries`

### Household Dashboard
Entity quick cards link to each entity's overview:
- `/${entityType}/${entitySlug}/overview` (e.g., `/business/acme-llc/overview`)

Alert action paths should be entity-relative when the alert belongs to a specific entity.

## Premium Tier Gating

- Free users see all widgets EXCEPT Spending Trends (show locked overlay)
- Spending summary limited to current month for Free tier
- Use a `isPremium` boolean from mock context (hardcode `false` for now)
- Lock overlay pattern:
```tsx
<div className="relative">
  <div className="blur-sm pointer-events-none">{/* Chart content */}</div>
  <div className="absolute inset-0 flex flex-col items-center justify-center bg-background/80">
    <Lock className="h-8 w-8 text-muted-foreground mb-2" />
    <p className="text-sm text-muted-foreground">Premium Feature</p>
    <Button variant="outline" size="sm" className="mt-2">Upgrade</Button>
  </div>
</div>
```

## Skeleton Loading

Show skeleton placeholders while `useEntityOverview()` or `useHouseholdDashboard()` is loading:
- Card-shaped skeletons matching each widget's approximate size
- Use `<Skeleton className="h-[200px] rounded-xl" />` pattern
- Animate with Tailwind's `animate-pulse`

## Empty States

### Entity Overview
- **No assets**: Net worth card shows "$0" with "Add your first asset" CTA
- **No accounts**: Account balances grid shows empty state with "Link Account" button
- **No transactions**: Transaction list shows "No transactions yet"

### Household Dashboard
- **No entities**: Full-page empty state — "Set up your first entity to get started" with CTA to create a Personal entity
- **Single entity**: Still show household view but with one entity card; consider a subtle prompt "Add a business or trust entity"

## Accessibility

- All charts must have `aria-label` descriptions (e.g., "Asset allocation: 45% Real Estate, 25% Investment...")
- Alert banner uses `role="alert"` for screen readers
- Progress bar uses `role="progressbar"` with `aria-valuenow`, `aria-valuemin`, `aria-valuemax`
- Interactive elements have minimum 44x44px touch targets
- Color is never the only indicator — always pair with text/icons
- Entity type badges use `aria-label` (e.g., `aria-label="Entity type: Business"`)
- Entity quick cards are keyboard-navigable and have focus indicators

## i18n

- All user-facing text uses `useTranslation()` with `t("dashboard.netWorth.title")` pattern
- Number formatting via `Intl.NumberFormat` with locale support
- Date formatting via `Intl.DateTimeFormat`
- Create translation keys in `src/locales/en/dashboard.json`
- New keys for household view: `t("household.totalNetWorth")`, `t("household.yourShare")`, `t("household.acrossEntities", { count: 3 })`
- Entity type labels: `t("entity.type.Personal")`, `t("entity.type.Business")`, `t("entity.type.Trust")`
