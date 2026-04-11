# Lovable Prompt — Bills & Recurring Expenses Page

## Context

Build the **Bills & Recurring Expenses** page scoped per entity. Bills are entity-specific — **Personal** entities have utilities, subscriptions, personal debts (mortgage, student loans, credit cards); **Business** entities have vendor invoices, business debt, business insurance, SaaS subscriptions; **Trust** entities have trust administrative expenses, trust accounting fees. For credit cards and loans linked via Plaid, the balance, minimum payment, and due date are **auto-imported** and stay synced. Users can also import bill data from uploaded statements (parsed via document processing). Manual bills can be added for utilities, subscriptions, rent, etc. Bills data feeds into the debt payoff calculator, dashboard spending, and insurance coverage calculations. Follow the same layout patterns as the existing **Assets** page (summary cards, filter tabs, view toggle, form sheet).

## Route & Nav

- **Route**: `/:entityType/:slug/bills` (e.g., `/personal/bills`, `/business/acme-llc/bills`)
- This page renders inside an existing `EntityProvider` layout — do NOT add routes to `App.tsx` or modify `DashboardLayout.tsx`
- **Nav**: Sub-item under each entity section in the sidebar. Icon: `Receipt` from lucide-react. Label: "Bills"
- **Page file**: `src/pages/Bills.tsx`

## Entity Context

This page renders inside an `EntityProvider`. All data fetching and mutations are scoped to the current entity:

```tsx
const { entityId, entityType, entitySlug } = useEntityContext();
```

All API calls use `entityId`. The same `Bills.tsx` component renders for all entity types — the data and mock content vary by entity type, but the UI structure is identical.

## Data Types

Create `src/types/bills.ts`:

```typescript
type BillCategory =
  | "CreditCard"
  | "Loan"
  | "Mortgage"
  | "AutoLoan"
  | "StudentLoan"
  | "PersonalLoan"
  | "Utilities"
  | "Insurance"
  | "Subscription"
  | "Rent"
  | "ChildCare"
  | "Medical"
  | "Other";

type BillFrequency =
  | "Weekly"
  | "BiWeekly"
  | "Monthly"
  | "Quarterly"
  | "SemiAnnually"
  | "Annually";

type BillSource = "Manual" | "Plaid" | "StatementImport";

type BillStatus = "Current" | "Due" | "Overdue" | "PaidThisMonth";

interface BillDto {
  id: string;
  entityId: string;
  name: string;                     // e.g., "Chase Sapphire Visa", "Electric Bill"
  category: BillCategory;
  amount: number;                   // Regular payment amount
  frequency: BillFrequency;
  annualizedAmount: number;         // Computed: amount * periods per year
  dueDay?: number;                  // Day of month (1-31) when bill is due
  nextDueDate?: string;             // Computed next due date
  status: BillStatus;
  isAutoPay: boolean;
  source: BillSource;
  isActive: boolean;

  // Debt-specific fields (CreditCard, Loan, Mortgage, etc.)
  currentBalance?: number;          // Outstanding balance
  minimumPayment?: number;          // Minimum due
  interestRate?: number;            // APR %
  creditLimit?: number;             // For credit cards
  originalLoanAmount?: number;      // For loans
  remainingPayments?: number;       // Months until payoff
  payoffDate?: string;              // Estimated payoff date

  // Linking
  linkedAccountId?: string;         // Link to Plaid account
  linkedAssetId?: string;           // Link to asset (mortgage → real estate)

  // Plaid sync
  lastSyncAt?: string;              // When Plaid data was last refreshed
  plaidAccountMask?: string;        // ••••1234

  // Statement import
  importedFromDocument?: string;    // Document ID that sourced this bill

  notes?: string;
  createdAt: string;
  updatedAt: string;
}

interface BillSummaryDto {
  entityId: string;
  totalMonthlyBills: number;        // Sum of all monthly-equivalent amounts
  totalAnnualBills: number;
  billCount: number;
  activeBillCount: number;
  totalDebtBalance: number;         // Sum of all currentBalance (debt categories)
  totalMinimumPayments: number;     // Sum of all minimumPayment
  upcomingBills: UpcomingBillItem[];
  byCategory: BillByCategoryItem[];
  overdueBills: BillDto[];
}

interface UpcomingBillItem {
  billId: string;
  name: string;
  category: BillCategory;
  amount: number;
  dueDate: string;
  daysUntilDue: number;
  isAutoPay: boolean;
  status: BillStatus;
}

interface BillByCategoryItem {
  category: BillCategory;
  label: string;
  monthlyTotal: number;
  percentage: number;
  billCount: number;
}

interface CreateBillRequest {
  entityId: string;
  name: string;
  category: BillCategory;
  amount: number;
  frequency: BillFrequency;
  dueDay?: number;
  isAutoPay?: boolean;
  currentBalance?: number;
  minimumPayment?: number;
  interestRate?: number;
  creditLimit?: number;
  originalLoanAmount?: number;
  linkedAccountId?: string;
  linkedAssetId?: string;
  notes?: string;
}

// For Plaid auto-import
interface PlaidBillImport {
  accountId: string;
  accountName: string;
  institutionName: string;
  accountType: string;             // "credit" | "loan" | "mortgage"
  mask: string;
  currentBalance: number;
  minimumPayment?: number;
  nextPaymentDueDate?: string;
  interestRate?: number;
  creditLimit?: number;
  lastPaymentAmount?: number;
  lastPaymentDate?: string;
  isAlreadyLinked: boolean;        // Already exists as a bill
}

// For statement import
interface StatementBillExtraction {
  documentId: string;
  fileName: string;
  extractedBills: ExtractedBillItem[];
}

interface ExtractedBillItem {
  name: string;
  category: BillCategory;
  amount: number;
  dueDate?: string;
  balance?: number;
  minimumPayment?: number;
  interestRate?: number;
  confidence: number;              // 0-1, AI extraction confidence
  isSelected: boolean;             // User toggle to import or skip
}
```

## API Service

Create `src/services/bills-service.ts` following the same TanStack Query pattern. All endpoints are entity-scoped:

- `GET /api/entities/{entityId}/bills` → `useBills(entityId)` — list all bills for entity
- `GET /api/entities/{entityId}/bills/summary` → `useBillsSummary(entityId)` — aggregated summary for entity
- `POST /api/entities/{entityId}/bills` → `useCreateBill(entityId, request)` — add manual bill
- `PUT /api/entities/{entityId}/bills/{id}` → `useUpdateBill(entityId, id, request)` — update
- `DELETE /api/entities/{entityId}/bills/{id}` → `useDeleteBill(entityId, id)` — delete
- `GET /api/entities/{entityId}/bills/plaid-import` → `usePlaidBillImport(entityId)` — fetch importable Plaid accounts linked to this entity
- `POST /api/entities/{entityId}/bills/plaid-import` → `useImportPlaidBills(entityId, selectedAccountIds)` — import selected Plaid accounts as bills
- `POST /api/entities/{entityId}/bills/statement-import` → `useStatementBillImport(entityId, documentId)` — extract bills from uploaded statement
- Use mock data initially.

## Mock Data

Mock data varies by entity type. Use `entityType` from `useEntityContext()` to select the appropriate mock set.

### Personal Entity Bills (default, 7 bills)

| Bill | Category | Amount | Frequency | Balance | Min Payment | APR | Due Day | Source |
|------|----------|--------|-----------|---------|-------------|-----|---------|--------|
| Chase Sapphire Visa | CreditCard | $340 | Monthly | $8,500 | $170 | 24.99% | 15 | Plaid |
| Student Loan (Navient) | StudentLoan | $250 | Monthly | $22,000 | $250 | 5.50% | 1 | Plaid |
| Auto Loan (Toyota Financial) | AutoLoan | $380 | Monthly | $15,200 | $380 | 6.99% | 20 | Plaid |
| Chase Mortgage | Mortgage | $2,100 | Monthly | $287,000 | $2,100 | 3.25% | 1 | Plaid |
| Electric (Duke Energy) | Utilities | $145 | Monthly | — | — | — | 22 | Manual |
| Netflix + Spotify | Subscription | $28 | Monthly | — | — | — | 5 | Manual |
| State Farm Auto Insurance | Insurance | $165 | Monthly | — | — | — | 10 | Manual |

### Business Entity Bills (6 bills)

| Bill | Category | Amount | Frequency | Balance | Min Payment | APR | Due Day | Source |
|------|----------|--------|-----------|---------|-------------|-----|---------|--------|
| Business Credit Line (Chase) | CreditCard | $1,200 | Monthly | $35,000 | $700 | 18.50% | 10 | Plaid |
| Office Lease | Rent | $4,500 | Monthly | — | — | — | 1 | Manual |
| AWS Cloud Services | Subscription | $850 | Monthly | — | — | — | 15 | Manual |
| Business Insurance (Hartford) | Insurance | $320 | Monthly | — | — | — | 5 | Manual |
| Vendor Invoice (Supplies Co.) | Other | $1,100 | Monthly | — | — | — | 20 | Manual |
| Equipment Loan | Loan | $680 | Monthly | $24,000 | $680 | 7.25% | 25 | Plaid |

### Trust Entity Bills (4 bills)

| Bill | Category | Amount | Frequency | Balance | Min Payment | APR | Due Day | Source |
|------|----------|--------|-----------|---------|-------------|-----|---------|--------|
| Trust Administration Fee | Other | $500 | Quarterly | — | — | — | 1 | Manual |
| Trust Accounting Fees | Other | $250 | Monthly | — | — | — | 15 | Manual |
| Trust Tax Preparation | Other | $1,200 | Annually | — | — | — | 1 | Manual |
| Trust Property Insurance | Insurance | $180 | Monthly | — | — | — | 10 | Manual |

**Plaid importable accounts** (for import dialog — scoped to entity's linked accounts only):
- Amex Gold (Credit, ••••3001, $1,200 balance, $35 min, not yet linked)
- Home Equity LOC (Loan, ••••7788, $45,000 balance, $280 min, not yet linked)

**Summary totals** (Personal entity example):
- Monthly Bills: $3,408
- Annual Bills: $40,896
- Total Debt Balance: $332,700
- Total Min Payments: $2,900
- Upcoming (next 7 days): 2 bills
- Overdue: 0

## Summary Cards Row

Top of page, 4 summary cards (`grid grid-cols-2 lg:grid-cols-4 gap-4`):

| Card | Content |
|------|---------|
| Monthly Bills | "$3,408/mo" with "Annual: $40,896" subtitle |
| Total Debt | "$332,700" across N debt accounts (red text) |
| Minimum Payments | "$2,900/mo" total minimums due |
| Upcoming | "2 bills due this week" (amber if any, green if none) |

## Upcoming Bills Banner

Below summary cards, show a horizontal scrollable row of upcoming bills (next 14 days):

```
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│ Mar 10      │ │ Mar 15      │ │ Mar 20      │
│ State Farm  │ │ Chase Visa  │ │ Auto Loan   │
│ $165        │ │ $340        │ │ $380        │
│ ✓ AutoPay   │ │ ⚠ Manual    │ │ ✓ AutoPay   │
└─────────────┘ └─────────────┘ └─────────────┘
```

Color coding:
- **Overdue**: Red border + "Overdue" badge
- **Due within 3 days**: Amber border + "Due Soon" badge
- **Upcoming**: Default border
- **Paid this month**: Green border + check icon + "Paid" badge

Each card shows: due date, name, amount, autopay status.

## Filter Tabs

Horizontal filter tabs:
`All | Credit Cards | Loans | Utilities | Subscriptions | Insurance | Other`

Where:
- "Credit Cards" = CreditCard category
- "Loans" = Loan, Mortgage, AutoLoan, StudentLoan, PersonalLoan
- "Other" = Rent, ChildCare, Medical, Other

## View Toggle (Table + Card Grid)

### Table View (default on desktop)

| Column | Content |
|--------|---------|
| Bill | Name + category icon + source badge (Plaid/Manual/Import) |
| Category | Badge |
| Amount | Payment amount + frequency (e.g., "$340/mo") |
| Due Date | Day of month + next due date + status badge |
| Balance | Current balance (mono, red if debt) or "—" |
| Min Payment | Minimum due or "—" |
| APR | Interest rate or "—" |
| AutoPay | Check icon (green) or X (muted) |
| Last Synced | Relative timestamp for Plaid, "—" for Manual |
| Actions | 3-dot menu: Edit, Mark Paid, Sync (Plaid), View Account, Delete |

### Card Grid (default on mobile)

Each card (`grid grid-cols-1 sm:grid-cols-2 gap-4`):
- **Category icon** (colored circle)
- **Bill name** + source badge
- **Payment amount** (large, mono font) + frequency
- **Due date** with status badge (Current/Due/Overdue/Paid)
- **Balance** (if debt type, red text)
- **AutoPay** indicator
- **3-dot menu**

## Add/Edit Bill Sheet

`Sheet` with fields:

### Common Fields (all categories)
- **Bill Name** (required, text)
- **Category** (required, select dropdown — changes which metadata fields appear)
- **Payment Amount** (required, currency)
- **Frequency** (required, select: Monthly, Quarterly, etc.)
- **Due Day of Month** (optional, 1-31)
- **AutoPay Enabled** (switch)
- **Currently Active** (switch, default on)
- **Notes** (optional, textarea)

### Debt Fields (shown when category is CreditCard, Loan, Mortgage, AutoLoan, StudentLoan, PersonalLoan)
- **Current Balance** (currency)
- **Minimum Payment** (currency)
- **Interest Rate (APR)** (%)
- **Credit Limit** (currency, shown only for CreditCard)
- **Original Loan Amount** (currency, shown only for loan types)
- **Link to Account** (optional, select from user's linked Plaid accounts)
- **Link to Asset** (optional, select: Mortgage → RealEstate assets, AutoLoan → Vehicle assets)

### Utility / Subscription Fields (shown for Utilities, Subscription, Insurance, Rent, etc.)
- **Provider/Company** (text, optional)
- **Account Number** (text, optional, masked in display)
- **Average Amount** (currency, if amount varies — e.g., electric bill)

## Plaid Auto-Import Dialog

Button at top of page: **"Import from Linked Accounts"** (only visible if the current entity has linked Plaid accounts)

Opens a `Dialog` showing importable Plaid credit card and loan accounts:

```
┌─────────────────────────────────────────────────────────┐
│  Import Bills from Linked Accounts                       │
│                                                          │
│  These accounts can be tracked as recurring bills.       │
│  Balance, minimum payment, and due date will sync        │
│  automatically from your bank.                           │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │ [✓] Amex Gold Card          ••••3001               │  │
│  │     Balance: $1,200  |  Min: $35  |  Due: Mar 18   │  │
│  │     APR: 21.24%  |  Limit: $15,000                 │  │
│  ├────────────────────────────────────────────────────┤  │
│  │ [✓] Home Equity LOC         ••••7788               │  │
│  │     Balance: $45,000  |  Min: $280  |  Due: Mar 1  │  │
│  │     APR: 8.50%                                     │  │
│  ├────────────────────────────────────────────────────┤  │
│  │ [—] Chase Sapphire Visa     ••••4829               │  │
│  │     Already imported ✓                              │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  ℹ Imported bills sync automatically when accounts       │
│    refresh via Plaid.                                    │
│                                                          │
│              [Cancel]  [Import Selected (2)]             │
└─────────────────────────────────────────────────────────┘
```

- Show all credit card and loan-type Plaid accounts linked to this entity
- Already-imported accounts shown as disabled with "Already imported" label
- Checkbox selection for new accounts
- On import, create Bill entries with `source: "Plaid"` and `linkedAccountId`
- Plaid-sourced bills show a sync badge and "Last synced" timestamp
- Plaid-sourced bill fields (balance, min payment, due date) are read-only with "Synced from bank" label

## Statement Import Flow

Button: **"Import from Statement"** (triggers document upload or links to `/insights` Document Import tab)

When a statement has been parsed and contains bill-like data:

1. Show extraction preview dialog with confidence scores
2. Each extracted bill row: Name, category (editable dropdown), amount, due date, balance, confidence bar
3. Checkbox to select which to import
4. Low confidence items (< 70%) highlighted amber with "Review" label
5. "Import Selected" creates bill entries with `source: "StatementImport"`

## Integration Points (How Other Features Consume This Data)

The `useBillsSummary(entityId)` hook is consumed by other entity-scoped features:

1. **Debt Payoff Calculator** — `useBillsSummary(entityId)` auto-populates debts from this entity's bills where `category` is CreditCard, Loan, Mortgage, AutoLoan, StudentLoan, PersonalLoan. Show info banner: "Debts auto-filled from your [Bills](../bills). Edit here to override." (Use entity-relative paths.)

2. **Insurance Calculator** — `useBillsSummary(entityId)` — `totalDebtBalance` auto-fills "Mortgage Balance" (from Mortgage bills) and "Other Debts" (from other debt-category bills). Banner: "Debts auto-filled from your [Bills](../bills)." (Use entity-relative paths.)

3. **Dashboard** — `useBillsSummary(entityId)` — entity overview: `totalMonthlyBills` shown in spending summary. `upcomingBills` shown in alerts/actions widget. `overdueBills` trigger dashboard alerts.

4. **AI Insights** — Full bill profile for the entity available for spending analysis and optimization recommendations.

## Mark as Paid

For manual bills, 3-dot menu includes "Mark Paid":
- Sets status to "PaidThisMonth" for the current billing cycle
- Shows green check badge
- Auto-resets to "Current" when the next billing cycle starts (client-side based on dueDay)
- Plaid-sourced bills auto-detect payment status from transactions (future enhancement — mock as manual for now)

## Delete Confirmation

- **Manual bills**: Standard "Are you sure?" dialog
- **Plaid-sourced bills**: "This will stop tracking this bill. The linked account will not be affected." with option to "Stop tracking" vs "Cancel"

## Empty State

- Receipt icon
- "No bills tracked yet"
- "Add your recurring bills to track due dates, monitor spending, and feed your financial tools"
- "Add Bill" primary button + "Import from Accounts" secondary button (if Plaid accounts exist)

## Skeleton Loading

Card-shaped skeletons while loading (same pattern as Assets page).

## Premium Tier Gating

- **Free tier**: Up to 5 manual bills, no Plaid import, no statement import
- **Premium**: Unlimited bills, Plaid auto-import + auto-sync, statement import, overdue notifications

Show Plaid import button with Premium lock for Free tier users.

## Accessibility

- All form inputs have proper `<label>` associations
- Currency inputs use `inputMode="decimal"` for mobile keyboards
- Filter tabs use `role="tablist"` / `role="tab"`
- Cards are focusable with keyboard navigation
- Dialog uses `role="dialog"` with `aria-labelledby` and focus trap
- Checkbox list in import dialog uses `role="group"` with `aria-label`
- Status badges include `aria-label` (e.g., `aria-label="Status: Overdue"`)
- Upcoming bills scroll container has `aria-label="Upcoming bills"` and keyboard scrollable
- All interactive elements have minimum 44x44px touch targets
- Overdue alerts use `role="alert"` for screen reader announcement

## i18n

- All text uses `useTranslation()` with `t("bills.summary.monthlyTotal")` pattern
- Create translation keys in `src/locales/en/bills.json`
- Currency/number formatting via `Intl.NumberFormat`
- Date formatting via `Intl.DateTimeFormat`
- Category labels translatable: `t("bills.category.creditCard")`
- Due date relative text: `t("bills.dueIn", { days: 3 })` → "Due in 3 days"
