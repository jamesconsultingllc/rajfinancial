# Lovable Prompts — RAJ Financial Asset Management UI

> **Usage**: Feed these 3 prompts to Lovable sequentially. Each builds on the previous.
> Each prompt is sized to stay within token limits.

---

## Prompt 1: Data Layer + Asset List Page

```
Build an Asset Management module for "RAJ Financial", a premium financial planning app.
Use React + TypeScript + Tailwind CSS + shadcn/ui + Lucide icons.

---

## BRAND & DESIGN SYSTEM

- Primary: #ebbb10 (Spanish Yellow gold)
- Gold scale: #fffbcc, #fff7b3, #f5e99a, #eed688, #e8c94d, #ebbb10, #d4a80e, #c3922e, #a67c26, #8a661f
- Neutrals (warm grays): #fafaf9, #f5f5f4, #e7e5e4, #d6d3d1, #a8a29e, #78716c, #57534e, #44403c, #292524, #1c1917
- Semantic: success #22c55e, warning #f59e0b, error #ef4444, info #3b82f6
- Font: Inter. Warm-tinted gold shadows: `0 4px 14px 0 rgb(235 187 16 / 0.25)`
- Glass card: `bg-white/70 backdrop-blur-md border border-gold-200/50`
- Gold focus rings: `focus:ring-2 focus:ring-[#ebbb10]/40 focus:border-[#ebbb10]`

---

## API CONTRACT (the backend already exists — match these types exactly)

### Enums

// Maps to C# AssetType enum — stored as string in API responses
type AssetType =
  | "RealEstate"       // 0 — properties, land, commercial
  | "Vehicle"          // 1 — cars, trucks, motorcycles, boats
  | "Investment"       // 2 — brokerage, stocks, bonds, mutual funds
  | "Retirement"       // 3 — 401k, IRA, Roth IRA, pension
  | "BankAccount"      // 4 — checking, savings, money market, CDs
  | "Insurance"        // 5 — whole life, universal life (cash value)
  | "Business"         // 6 — ownership stakes, partnerships, LLCs
  | "PersonalProperty" // 7 — jewelry, furniture, electronics, art
  | "Collectible"      // 8 — coins, stamps, wine, sports memorabilia
  | "Cryptocurrency"   // 9 — Bitcoin, Ethereum, etc.
  | "IntellectualProperty" // 10 — patents, trademarks, copyrights
  | "Other";           // 99 — catch-all

// Maps to C# DepreciationMethod enum
type DepreciationMethod =
  | "None"             // 0 — no depreciation, manual value
  | "StraightLine"     // 1 — (Cost - Salvage) / Useful Life
  | "DecliningBalance" // 2 — fixed rate to remaining book value
  | "Macrs";           // 3 — IRS Modified Accelerated Cost Recovery System

### GET /api/assets → AssetDto[]
Response shape for the list view:

interface AssetDto {
  id: string;             // GUID
  name: string;
  type: AssetType;
  currentValue: number;   // decimal
  purchasePrice?: number;
  purchaseDate?: string;  // ISO 8601 DateTimeOffset
  description?: string;
  location?: string;
  accountNumber?: string;
  institutionName?: string;
  isDepreciable: boolean;
  isDisposed: boolean;
  hasBeneficiaries: boolean;
  createdAt: string;
  updatedAt?: string;
}

Query params: `?type=RealEstate`, `?includeDisposed=true`, `?ownerUserId=<guid>`

### GET /api/assets/{id} → AssetDetailDto
Full detail for single asset view (used in Prompt 3):

interface AssetDetailDto extends Omit<AssetDto, never> {
  // Depreciation inputs (only when isDepreciable)
  depreciationMethod?: DepreciationMethod;
  salvageValue?: number;
  usefulLifeMonths?: number;
  inServiceDate?: string;
  // Depreciation computed at read time (not stored)
  accumulatedDepreciation?: number;
  bookValue?: number;
  monthlyDepreciation?: number;
  depreciationPercentComplete?: number; // 0.0–1.0
  // Disposal
  disposalDate?: string;
  disposalPrice?: number;
  disposalNotes?: string;
  // Valuation
  marketValue?: number;
  lastValuationDate?: string;
  // Beneficiaries
  beneficiaries: BeneficiaryAssignmentDto[];
}

interface BeneficiaryAssignmentDto {
  beneficiaryId: string;
  beneficiaryName: string;
  relationship: string;  // "Spouse", "Child", etc.
  allocationPercent: number;
  type: string;           // "Primary" or "Contingent"
}

### POST /api/assets (body: CreateAssetRequest) → 201 AssetDto

interface CreateAssetRequest {
  name: string;                      // required, max 200 chars
  type: AssetType;                   // required
  currentValue: number;              // required, >= 0
  purchasePrice?: number;            // >= 0
  purchaseDate?: string;
  description?: string;              // max 2000 chars
  location?: string;                 // max 500 chars
  accountNumber?: string;            // max 100 chars
  institutionName?: string;          // max 200 chars
  depreciationMethod?: DepreciationMethod;
  salvageValue?: number;             // >= 0
  usefulLifeMonths?: number;         // > 0
  inServiceDate?: string;
  marketValue?: number;              // >= 0
  lastValuationDate?: string;
}

### PUT /api/assets/{id} (body: UpdateAssetRequest) → 200 AssetDto
Same shape as CreateAssetRequest (all fields required for full replace).

### DELETE /api/assets/{id} → 204 No Content

### FUTURE ENDPOINTS (P1/P2 — not yet built, but design the UI to accommodate them)
- `GET /api/assets/summary` (P1) — will return total value, count by type, top category, assets without beneficiaries count. Leave space for summary cards at top of page.
- `POST /api/assets/{id}/dispose` (P1) — will accept `{ disposalDate, disposalPrice, disposalNotes }`. Plan for a "Mark as Disposed" action in the actions menu.
- `GET /api/assets/{id}/depreciation` (P2) — will return a year-by-year depreciation schedule. Plan for a "View Schedule" link in the detail view.

### Error response shape

interface ApiErrorResponse {
  code: string;    // e.g. "ASSET_NAME_REQUIRED", "ASSET_NOT_FOUND"
  message: string;
  details?: Record<string, unknown>;
}

Error codes: ASSET_NAME_REQUIRED, ASSET_USEFUL_LIFE_REQUIRED, ASSET_NAME_MAX_LENGTH, ASSET_DESCRIPTION_MAX_LENGTH, ASSET_LOCATION_MAX_LENGTH, ASSET_ACCOUNT_NUMBER_MAX_LENGTH, ASSET_INSTITUTION_NAME_MAX_LENGTH, ASSET_TYPE_INVALID, ASSET_DEPRECIATION_METHOD_INVALID, ASSET_USEFUL_LIFE_INVALID, ASSET_VALUE_NEGATIVE, ASSET_PURCHASE_PRICE_NEGATIVE, ASSET_SALVAGE_VALUE_NEGATIVE, ASSET_MARKET_VALUE_NEGATIVE, ASSET_NOT_FOUND

---

## ASSET TYPE → ICON MAPPING (Lucide)
RealEstate → Home, Vehicle → Car, Investment → TrendingUp, Retirement → Landmark, BankAccount → Wallet, Insurance → Shield, Business → Briefcase, PersonalProperty → Package, Collectible → Gem, Cryptocurrency → Bitcoin (or Coins), IntellectualProperty → Lightbulb, Other → CircleDot

---

## BUILD: Asset List Page (/assets)

1. **Summary Cards Row** — 4 cards across the top (use mock computed data for now since GET /api/assets/summary isn't built yet):
   - Total Assets Value (large gold number, currency formatted)
   - Number of Assets
   - Top Category (most valuable type)
   - Needs Attention (count where hasBeneficiaries === false, warning-colored)

2. **Header** — "Assets" title left, gold "Add Asset" button right. Mobile: button full-width below title.

3. **Filter Tabs** — pill/chip style: All, Real Estate, Vehicles, Business, Other. Gold active state. Filters the list by `type`.

4. **Desktop (lg+): Data Table** with columns:
   - Name (icon in gold-tinted rounded square + name + type subtitle)
   - Current Value (currency)
   - Purchase Price (currency, or "—")
   - Beneficiaries (green icon+count if `hasBeneficiaries`, yellow "None" chip if not)
   - Actions dropdown: Edit, Update Value, Mark as Disposed (disabled/greyed with "Coming soon" tooltip), Manage Beneficiaries, Delete (red)

5. **Mobile (<lg): Card Layout** replacing table:
   - Asset type icon + name + type label
   - Warning chip if no beneficiaries
   - Large bold currency value bottom-left, chevron-right bottom-right
   - Tappable cards, 44px min touch targets

6. **Empty State** — centered icon, "No assets yet" title, description, "Add Your First Asset" CTA button

7. **Loading State** — 3 skeleton cards with gold-tinted shimmer

Use realistic mock data: a primary residence ($425,000), a 2022 Tesla Model 3 ($35,000 depreciable), a Fidelity 401k ($180,000), a savings account ($25,000), some Bitcoin ($12,500), and a vintage watch collection ($8,000). Currency formatting: `Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })`.
```

---

## Prompt 2: Add/Edit Asset Form Dialog

```
Continue building the RAJ Financial Asset Management module. Add the Add/Edit Asset form as a modal dialog, driven by the API contract.

## API contract reminder

POST /api/assets body (CreateAssetRequest):
- name: string (required, max 200)
- type: AssetType (required — one of: RealEstate, Vehicle, Investment, Retirement, BankAccount, Insurance, Business, PersonalProperty, Collectible, Cryptocurrency, IntellectualProperty, Other)
- currentValue: number (required, >= 0)
- purchasePrice?: number (>= 0)
- purchaseDate?: string (ISO date)
- description?: string (max 2000)
- location?: string (max 500)
- accountNumber?: string (max 100)
- institutionName?: string (max 200)
- depreciationMethod?: DepreciationMethod (None, StraightLine, DecliningBalance, Macrs)
- salvageValue?: number (>= 0)
- usefulLifeMonths?: number (> 0)
- inServiceDate?: string (ISO date)
- marketValue?: number (>= 0)
- lastValuationDate?: string (ISO date)

PUT /api/assets/{id} body (UpdateAssetRequest): Same shape, all fields for full replace.

Error codes for validation: ASSET_NAME_REQUIRED, ASSET_NAME_MAX_LENGTH (200), ASSET_TYPE_INVALID, ASSET_VALUE_NEGATIVE, ASSET_PURCHASE_PRICE_NEGATIVE, ASSET_DESCRIPTION_MAX_LENGTH (2000), ASSET_LOCATION_MAX_LENGTH (500), ASSET_ACCOUNT_NUMBER_MAX_LENGTH (100), ASSET_INSTITUTION_NAME_MAX_LENGTH (200), ASSET_DEPRECIATION_METHOD_INVALID, ASSET_USEFUL_LIFE_REQUIRED (when method != None), ASSET_USEFUL_LIFE_INVALID (must be > 0), ASSET_SALVAGE_VALUE_NEGATIVE, ASSET_MARKET_VALUE_NEGATIVE

## Form Dialog Spec

### Dialog shell:
- Header: "Add New Asset" (create) or "Edit Asset" (edit mode, pre-populated)
- X close button, Escape to close, click-outside to close
- Scrollable content, max ~70vh
- Footer: Cancel (ghost) + Save (gold primary, spinner while submitting)

### Sections with conditional fields:

**Section 1 — Basic Information**
- Asset Name (text, required, max 200)
- Asset Type (select dropdown, all 12 types with friendly labels: "Real Estate", "Vehicle", "Investment Account", "Retirement Account", "Bank Account", "Insurance Policy", "Business Interest", "Personal Property", "Collectible", "Cryptocurrency", "Intellectual Property", "Other")
- Current Value (currency input with $ prefix, required, >= 0)
- Description (textarea, optional, max 2000, show char count)

**Section 2 — Purchase Details**
- Purchase Price (currency input, optional)
- Purchase Date (date picker, optional)
- Location (text, optional) — only show for: RealEstate, Vehicle, PersonalProperty, Business, Collectible
- Account Number (text, optional) — only show for: Investment, Retirement, BankAccount, Insurance, Cryptocurrency
- Institution Name (text, optional) — only show for: Investment, Retirement, BankAccount, Insurance

**Section 3 — Depreciation** (entire section only visible for depreciable types: RealEstate, Vehicle, Business, PersonalProperty, IntellectualProperty)
- Toggle: "This asset depreciates" (when off, skip depreciation fields)
- When on:
  - Depreciation Method (select: "Straight Line", "Declining Balance", "MACRS")
  - Salvage Value (currency input, >= 0)
  - Useful Life in Months (number input, > 0, show helper: "= X years, Y months")
  - In-Service Date (date picker)

**Section 4 — Valuation**
- Market Value (currency input, optional, >= 0)
- Last Valuation Date (date picker, optional)

### Validation:
- Client-side: match all error code constraints above
- Inline red error text below each invalid field
- If server returns an error code, map it to the corresponding field

### UX:
- When editing, pre-fill all fields from AssetDetailDto
- Toast on success: "Asset created successfully" / "Asset updated successfully"
- On save, optimistically close dialog and refresh the asset list

### Delete Confirmation (separate dialog):
- Title: "Delete Asset"
- Body: "Are you sure you want to delete **{name}**? This action cannot be undone."
- Cancel (ghost) + Delete (red destructive)
- Toast on success: "Asset deleted"

Keep gold theme consistent — gold focus rings on inputs, warm shadows on the dialog.
```

---

## Prompt 3: Asset Detail Page + Polish

```
Continue building the RAJ Financial Asset Management module. Add the Asset Detail page and polish the whole module.

## API contract reminder

GET /api/assets/{id} → AssetDetailDto:

{
  id, name, type, currentValue, purchasePrice?, purchaseDate?,
  description?, location?, accountNumber?, institutionName?,
  isDepreciable,
  depreciationMethod?, salvageValue?, usefulLifeMonths?, inServiceDate?,
  accumulatedDepreciation?, bookValue?, monthlyDepreciation?,
  depreciationPercentComplete?, // 0.0–1.0
  isDisposed,
  disposalDate?, disposalPrice?, disposalNotes?,
  marketValue?, lastValuationDate?,
  hasBeneficiaries,
  beneficiaries: [{ beneficiaryId, beneficiaryName, relationship, allocationPercent, type }],
  createdAt, updatedAt?
}

FUTURE: `POST /api/assets/{id}/dispose` (P1) — body: `{ disposalDate, disposalPrice, disposalNotes }` — show a "Mark as Disposed" button that opens a dispose form dialog (fields: disposal date, sale price, notes). Wire up the UI but show a "Coming Soon" state since the endpoint isn't live yet.

FUTURE: `GET /api/assets/{id}/depreciation` (P2) — will return year-by-year schedule. Show a "View Full Schedule" link that is disabled with a "Coming soon" tooltip.

## Asset Detail Page (/assets/:id)

### Layout:

1. **Back link**: "← Back to Assets" at top

2. **Header row**:
   - Left: large asset type icon (in gold-tinted rounded square) + asset name (h1) + type label (subtitle) + description if present
   - Right: Edit (gold outline button), Mark as Disposed (outline, coming soon tooltip), Delete (red outline)
   - Mobile: buttons stack full-width below the header

3. **Value Hero Card** (glass card):
   - "Current Value" label + large bold formatted dollar amount
   - If marketValue exists and differs from currentValue, show "Market Value: $X" line
   - If purchasePrice exists, compute and show gain/loss: "+$X,XXX (+XX.X%)" in green, or "-$X,XXX (-XX.X%)" in red
   - Last valuation date if present

4. **Info Cards Grid** (2 columns desktop, 1 column mobile):

   **Card: Purchase Details** (always show, hide individual fields if null)
   - Purchase Price (currency)
   - Purchase Date (formatted)
   - Location
   - Account Number (mask all but last 4 chars: "••••1234")
   - Institution Name

   **Card: Depreciation** (only if isDepreciable === true AND depreciationMethod !== "None")
   - Method (friendly label)
   - Salvage Value (currency)
   - Useful Life (format as "X years, Y months")
   - In-Service Date
   - Progress bar: depreciationPercentComplete × 100%, gold fill
   - Accumulated Depreciation (currency)
   - Current Book Value (currency)
   - Monthly Depreciation (currency)
   - "View Full Schedule" link (disabled, "Coming soon" tooltip — for future P2 endpoint)

   **Card: Disposal** (only if isDisposed === true)
   - Disposal Date
   - Disposal Price (currency)
   - Gain/Loss on disposal = disposalPrice - (bookValue ?? purchasePrice ?? currentValue), green/red
   - Disposal Notes

   **Card: Beneficiaries**
   - If beneficiaries array is non-empty: table/list showing name, relationship, allocation %, type (Primary/Contingent). Total allocation bar showing sum%.
   - If empty: warning card with yellow/amber border — "No beneficiaries assigned to this asset. Consider adding beneficiaries for estate planning purposes." + "Manage Beneficiaries" button (navigates to /beneficiaries?asset={id})

5. **Audit footer** (subtle, small text at bottom):
   - "Created: {formatted date}" | "Last updated: {formatted date}"

### Polish across the whole module:

- **Animations**: Cards fade-in with staggered upward slide on page load. Smooth tab transitions on the list page.
- **Currency**: Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }) everywhere.
- **Accessibility**: All form inputs have labels, visible focus indicators (gold rings), proper heading hierarchy, aria-label on icon-only buttons, keyboard nav (Escape closes dialogs, Tab through fields).
- **Mobile**: Everything mobile-first. 44px min touch targets. No horizontal scroll. Responsive grids collapse to single column.
- **Toasts**: Bottom-right success/error toasts for all operations.
- **Mock detail data**: Use the same primary residence from Prompt 1 as the detail mock, with: depreciationMethod=StraightLine, salvageValue=50000, usefulLifeMonths=360, inServiceDate=2020-01-15, and 2 beneficiaries (Sarah - Spouse - Primary - 60%, James Jr - Child - Contingent - 40%).
```
