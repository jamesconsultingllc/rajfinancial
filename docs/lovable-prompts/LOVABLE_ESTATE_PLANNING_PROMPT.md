# Lovable Prompt — Estate Planning Page

## Context

Build the **Estate Planning** page. This page is **entity-scoped**: when accessed from a Personal or Business entity, it shows trusts that entity **owns or is a beneficiary of** (via `EntityRole`). When accessed from a Trust entity, it shows that trust's own estate data (roles, assets, beneficiaries). Provides beneficiary coverage analysis, trust management, allocation validation, gap detection, and estate document tracking. Follow the same layout patterns as the existing **Assets** page (filter tabs, summary cards, responsive grid). Use Recharts for charts.

## Route & Nav

- **Route**: `/:entityType/:slug/estate` (e.g., `/personal/estate`, `/business/acme-corp/estate`, `/trust/family-trust/estate`)
- Renders inside the `EntityProvider` layout — no changes needed to `App.tsx` routes or `DashboardLayout.tsx` nav
- **Page file**: `src/pages/EstatePlanning.tsx`

## Entity Context

This page renders inside an `EntityProvider`:
```tsx
const { entityId, entityType, entitySlug } = useEntityContext();
```

**Behavior by entity type:**
- **Personal/Business**: Shows trusts where this entity's owner has a role (grantor, beneficiary, trustee). Shows beneficiary coverage for this entity's assets.
- **Trust**: Shows this trust's own roles, linked assets, beneficiary designations, and trust administration details.

## Data Types

Create `src/types/estate.ts`:

```typescript
type CoverageLevel = "Critical" | "NeedsAttention" | "Good" | "Complete";

interface EstateSummaryDto {
  entityId: string;
  coverage: EstateCoverageDto;
  trusts: TrustOverviewDto[];
  gaps: EstateGapItem[];
  allocationValidation: AllocationValidationResult[];
  statistics: EstateStatistics;
}

interface EstateCoverageDto {
  totalAssets: number;
  coveredAssets: number;
  coveragePercentage: number;
  level: CoverageLevel;
  totalValue: number;
  coveredValue: number;
  uncoveredValue: number;
}

interface TrustOverviewDto {
  id: string;
  trustName: string;
  trustCategory: string;
  trustPurpose?: string;
  roles: TrustRoleSummary[];
  linkedAssets: TrustLinkedAsset[];
  issues: string[]; // e.g., "Missing successor trustee"
}

interface TrustRoleSummary {
  contactName: string;
  role: string; // Grantor, Trustee, SuccessorTrustee, Beneficiary, etc.
  successionOrder?: number;
  ownershipPercent?: number;        // From EntityRole — ownership interest in trust
  beneficialInterestPercent?: number; // From EntityRole — beneficial interest in trust
}

interface TrustLinkedAsset {
  assetId: string;
  assetName: string;
  assetType: string;
  currentValue: number;
  allocationPercentage: number;
}

interface EstateGapItem {
  assetId: string;
  assetName: string;
  assetType: string;
  currentValue: number;
  issue: "NoBeneficiary" | "IncompletePrimary" | "NoContingent" | "AllocationMismatch";
  issueDescription: string;
}

interface AllocationValidationResult {
  assetId: string;
  assetName: string;
  assetType: string;
  primaryTotal: number;   // Should be 100
  contingentTotal: number; // Should be 100
  hasPerStirpes: boolean;
  isValid: boolean;
  warnings: string[];
}

interface EstateStatistics {
  totalAssets: number;
  totalAssetsValue: number;
  assetsWithBeneficiaries: number;
  assetsWithoutBeneficiaries: number;
  totalTrusts: number;
  totalBeneficiaries: number;
  averageCoverage: number;
}
```

## API Service

Create `src/services/estate-service.ts` following the same TanStack Query pattern:

- `GET /api/entities/{entityId}/estate/summary` -> `useEstateSummary(entityId)` — fetch entity-scoped estate summary
- Use mock data initially.

## Mock Data

Generate realistic estate planning mock data for two entity contexts:

### Personal Entity View (entityId: "personal-john-doe")

Shows trusts where John's personal entity has roles, plus coverage for personal assets.

**Coverage**: 7 of 10 assets covered = 70% "Good", total value $1.53M, covered $1.1M, uncovered $430K

**Trusts** (2 trusts where this entity has an EntityRole):
- "James Family Living Trust" — Revocable, roles: John (Grantor+Trustee, ownershipPercent: 100), Mary (SuccessorTrustee), Alice+Bob (Beneficiaries, beneficialInterestPercent: 50 each), linked to 3 assets ($750K). Issue: none.
- "Education Trust" — Irrevocable, roles: John (Grantor, ownershipPercent: 100), Bank of America (Trustee), Emma (Beneficiary, beneficialInterestPercent: 100), linked to 1 asset ($50K). Issue: "No successor trustee designated"

**Gaps** (3 items):
- Primary Residence ($450K) — "NoBeneficiary", "No beneficiary designated for this asset"
- Collectible Art ($30K) — "NoBeneficiary", "No beneficiary designated"
- Fidelity Brokerage ($312K) — "NoContingent", "No contingent beneficiary assigned"

**Allocation Validation** (2 issues):
- Vehicle ($45K) — primary 80%, contingent 0%, invalid, warning: "Primary allocation only 80%, should total 100%"
- Investment Account ($198K) — primary 100%, contingent 50%, warning: "Contingent allocation only 50%"

**Statistics**: 10 total assets, $1.53M total value, 7 with beneficiaries, 3 without, 2 trusts, 8 beneficiaries

### Trust Entity View (entityId: "trust-james-family")

Shows the trust's own roles, assets, and beneficiary designations.

**Coverage**: 3 of 3 assets covered = 100% "Complete", total value $750K, covered $750K, uncovered $0

**Trusts** (1 — self):
- "James Family Living Trust" — Revocable, roles: John (Grantor+Trustee, ownershipPercent: 100), Mary (SuccessorTrustee), Alice (Beneficiary, beneficialInterestPercent: 50), Bob (Beneficiary, beneficialInterestPercent: 50), linked to 3 assets ($750K). Issue: none.

**Gaps**: Empty — all trust assets covered

**Allocation Validation**: All valid — all assets at 100% primary and contingent

**Statistics**: 3 total assets, $750K total value, 3 with beneficiaries, 0 without, 1 trust (self), 4 roles (grantor, trustee, 2 beneficiaries)

## Page Layout

### Summary Cards Row

Top of page, 4 summary cards in a responsive row (`grid grid-cols-2 lg:grid-cols-4 gap-4`):

| Card | Content |
|------|---------|
| Coverage | Circular progress: "70%" with level badge "Good" |
| Covered Value | "$1.1M of $1.53M" with progress bar |
| Assets at Risk | "3 assets" needing attention (red if > 0) |
| Trusts | "2 trusts" with link to trust section |

### Tab Navigation

Horizontal tabs below summary cards:
`Overview | Trusts | Gaps & Issues | Allocation`

### Overview Tab (default)

- **Coverage Donut Chart** (Recharts `<PieChart>`): Covered vs Uncovered segments, center label showing percentage
- **Estate Checklist**: Vertical checklist with check/x icons:
  - "All assets have primary beneficiaries" (check/x based on coverage)
  - "All assets have contingent beneficiaries"
  - "All allocations total 100%"
  - "All trusts have successor trustees"
  - "Estate documents are up to date"
- **Quick Stats Grid**: 2x3 grid of stat cards with key numbers

### Trusts Tab

- Card for each trust in `grid grid-cols-1 lg:grid-cols-2 gap-4`
- Each trust card:
  - **Header**: Trust name + category badge (Revocable/Irrevocable) + purpose
  - **Roles Section**: List of `EntityRole` relationships with contact names, grouped by role type, ordered by succession. Show `ownershipPercent` and `beneficialInterestPercent` where present (e.g., "Alice — Beneficiary (50% beneficial interest)")
  - **Linked Assets Section**: Table of linked assets with name, type, value, allocation %
  - **Issues Section**: Warning banner if any structural issues exist (amber border, AlertTriangle icon)
  - **Total Value**: Sum of linked asset values
- **For Personal/Business entities**: Cards show the relationship context (e.g., "You are Grantor & Trustee of this trust")
- **For Trust entities**: Single card showing the trust's own full detail view
- Empty state: "No trusts created yet" with description and "Add Trust" button linking to entity-relative contacts path

### Gaps & Issues Tab

- List of `EstateGapItem` entries as alert-style cards
- Each card:
  - Left: Warning icon (color by severity)
  - Center: Asset name + type badge, issue description
  - Right: Asset value, "Fix" button linking to the entity-relative asset beneficiary page (e.g., `/${entityType}/${entitySlug}/assets/${assetId}/beneficiaries`)
- Color coding:
  - `NoBeneficiary`: Red/destructive border
  - `IncompletePrimary` / `AllocationMismatch`: Amber/warning border
  - `NoContingent`: Yellow/info border
- Empty state (all clear): Green success card "All assets are properly covered!" with Shield check icon

### Allocation Tab

- Table view of all assets with allocation validation:

| Column | Content |
|--------|---------|
| Asset Name | Name + type badge |
| Value | Current value (mono font) |
| Primary % | Percentage with progress bar (green at 100%, red below) |
| Contingent % | Percentage with progress bar |
| Per Stirpes | Check icon if any beneficiary is per stirpes |
| Status | "Valid" (green check) or "Issues" (amber warning) |
| Actions | "Review" button linking to entity-relative asset beneficiary section |

- Mobile: Card layout showing asset name, value, primary/contingent percentages, and status badge

## Premium Tier Gating

- **Free tier**: Shows coverage overview + basic gap list only
- **Premium**: Full trust management, allocation validation, detailed analysis
- Use lock overlay (same pattern as Dashboard) on Trusts tab and Allocation tab for Free users

## Skeleton Loading

- Summary cards: 4 skeleton cards with `h-24` height
- Tab content: skeleton cards matching tab layout
- Table: skeleton rows

## Accessibility

- Tab navigation uses `role="tablist"` / `role="tab"` / `role="tabpanel"` with `aria-selected`
- Progress bars use `role="progressbar"` with `aria-valuenow`
- Alert cards use appropriate `role="alert"` for critical issues
- Trust role hierarchy is navigable by keyboard
- All icons have `aria-hidden="true"` (decorative) or `aria-label` (functional)
- Minimum 44x44px touch targets on all buttons

## i18n

- All text uses `useTranslation()` with `t("estate.coverage.title")` pattern
- Create translation keys in `src/locales/en/estate.json`
- Number/currency formatting via `Intl.NumberFormat`
