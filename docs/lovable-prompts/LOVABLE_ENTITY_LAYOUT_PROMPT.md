# Lovable Prompt — Entity Navigation & Layout (Foundation)

## Context

**This is a foundational restructure.** The app currently uses flat routes (`/assets`, `/income`, `/estate`) with a single "My Account" sidebar. We're migrating to an **Entity-First Architecture** where every financial feature (assets, income, bills, accounts, insurance, etc.) is scoped to an **Entity**. Three entity types exist:

- **Personal** — Individual/family finances (one per user, auto-created)
- **Business** — LLCs, corporations, partnerships (user can have multiple)
- **Trust** — Revocable, irrevocable, special needs, etc. (user can have multiple)

This prompt creates the entity shell: context provider, routing layout, sidebar navigation with entity sections, and entity switcher. **All existing pages (Income, Estate Planning, Assets, Accounts, Transactions, Contacts) must be refactored to render inside the entity layout.**

## What to Build

1. **`EntityProvider`** context with `useEntityContext()` hook
2. **`EntityLayout`** wrapper component with entity-scoped sidebar
3. **Entity-scoped routing** using `/:entityType/:slug/*` pattern
4. **Refactor all existing feature pages** to use entity context
5. **Household Dashboard** route at `/household/dashboard`

---

## Data Types

Create `src/types/entity.ts`:

```typescript
type EntityType = "Personal" | "Business" | "Trust";

interface EntityDto {
  id: string;
  userId: string;
  type: EntityType;
  name: string;
  slug: string;
  parentEntityId?: string;
  isActive: boolean;
  business?: BusinessEntityMetadata;
  trust?: TrustEntityMetadata;
  createdAt: string;
  updatedAt: string;
}

interface BusinessEntityMetadata {
  formationType: string;
  ein?: string;
  industry?: string;
  stateOfFormation?: string;
  annualRevenue?: number;
  numberOfEmployees?: number;
}

interface TrustEntityMetadata {
  category: "Revocable" | "Irrevocable";
  purpose?: string;
  specificType?: string;
  jurisdiction?: string;
  isGrantorTrust: boolean;
  trustDate?: string;
}
```

## Mock Entities

Create `src/data/mock-entities.ts`:

```typescript
export const MOCK_ENTITIES: EntityDto[] = [
  {
    id: "entity-personal-001",
    userId: "user-001",
    type: "Personal",
    name: "Personal",
    slug: "personal",
    isActive: true,
    createdAt: "2025-01-15T00:00:00Z",
    updatedAt: "2026-03-01T00:00:00Z",
  },
  {
    id: "entity-business-001",
    userId: "user-001",
    type: "Business",
    name: "Acme LLC",
    slug: "acme-llc",
    isActive: true,
    business: {
      formationType: "SingleMemberLLC",
      ein: "12-3456789",
      industry: "Technology",
      stateOfFormation: "Delaware",
      annualRevenue: 500000,
      numberOfEmployees: 5,
    },
    createdAt: "2025-06-01T00:00:00Z",
    updatedAt: "2026-03-01T00:00:00Z",
  },
  {
    id: "entity-business-002",
    userId: "user-001",
    type: "Business",
    name: "Side Hustle Inc",
    slug: "side-hustle-inc",
    isActive: true,
    business: {
      formationType: "SCorporation",
      industry: "Consulting",
      stateOfFormation: "Texas",
      annualRevenue: 85000,
      numberOfEmployees: 1,
    },
    createdAt: "2026-01-10T00:00:00Z",
    updatedAt: "2026-03-01T00:00:00Z",
  },
  {
    id: "entity-trust-001",
    userId: "user-001",
    type: "Trust",
    name: "James Family Trust",
    slug: "james-family-trust",
    isActive: true,
    trust: {
      category: "Revocable",
      purpose: "EstatePlanning",
      isGrantorTrust: true,
      jurisdiction: "California",
      trustDate: "2020-03-15T00:00:00Z",
    },
    createdAt: "2025-03-15T00:00:00Z",
    updatedAt: "2026-03-01T00:00:00Z",
  },
];
```

---

## EntityProvider & Hook

Create `src/contexts/EntityContext.tsx`:

```tsx
import { createContext, useContext, ReactNode } from "react";

interface EntityContextValue {
  entityId: string;
  entityType: EntityType;
  entitySlug: string;
  entityName: string;
  entity: EntityDto;
}

const EntityContext = createContext<EntityContextValue | undefined>(undefined);

export function EntityProvider({
  entity,
  children,
}: {
  entity: EntityDto;
  children: ReactNode;
}) {
  const value: EntityContextValue = {
    entityId: entity.id,
    entityType: entity.type,
    entitySlug: entity.slug,
    entityName: entity.name,
    entity,
  };

  return (
    <EntityContext.Provider value={value}>{children}</EntityContext.Provider>
  );
}

export function useEntityContext(): EntityContextValue {
  const context = useContext(EntityContext);
  if (!context) {
    throw new Error("useEntityContext must be used within an EntityProvider");
  }
  return context;
}

/**
 * Builds entity-relative path for navigation links.
 * @example entityPath("/income") → "/personal/income" or "/business/acme-llc/income"
 */
export function useEntityPath() {
  const { entityType, entitySlug } = useEntityContext();
  return (subPath: string) => {
    const base = entityType === "Personal"
      ? `/personal`
      : `/${entityType.toLowerCase()}/${entitySlug}`;
    return `${base}${subPath}`;
  };
}
```

---

## Entity Service

Create `src/services/entity-service.ts`:

```typescript
import { useQuery } from "@tanstack/react-query";
import { MOCK_ENTITIES } from "@/data/mock-entities";
import type { EntityDto } from "@/types/entity";

/**
 * Fetches all entities for the current user.
 * Returns mock data until the API is ready.
 */
export function useEntities() {
  return useQuery<EntityDto[]>({
    queryKey: ["entities"],
    queryFn: async () => {
      // TODO: Replace with real API call: GET /api/entities
      return MOCK_ENTITIES;
    },
  });
}

/**
 * Finds a specific entity by type and slug from the user's entities.
 */
export function useEntityBySlug(entityType: string, slug: string) {
  const { data: entities, ...rest } = useEntities();
  const entity = entities?.find(
    (e) =>
      e.type.toLowerCase() === entityType.toLowerCase() &&
      e.slug === slug
  );
  return { data: entity, ...rest };
}

/**
 * Returns entities grouped by type for sidebar rendering.
 */
export function useEntitiesByType() {
  const { data: entities, ...rest } = useEntities();
  const grouped = {
    personal: entities?.find((e) => e.type === "Personal"),
    businesses: entities?.filter((e) => e.type === "Business") ?? [],
    trusts: entities?.filter((e) => e.type === "Trust") ?? [],
  };
  return { data: grouped, ...rest };
}
```

---

## Sidebar Navigation Restructure

**Replace** the current flat `clientNavSections` in `DashboardLayout.tsx` with a dynamic, entity-aware sidebar. The new sidebar structure:

```
Home
Household Dashboard

─── Personal ───────────────
  Overview
  Income
  Bills & Expenses
  Assets
  Accounts
  Transactions
  Insurance
  Debt Payoff
  Documents

─── Business ───────────────
  [Acme LLC ▾]          ← entity switcher (if multiple)
  Overview
  Income
  Bills & Expenses
  Assets
  Accounts
  Transactions
  Insurance
  Debt Payoff
  Documents

─── Estate Planning ────────
  [James Family Trust ▾] ← entity/trust switcher (if multiple)
  Overview               ← estate planning IS the overview (roles, beneficiaries, coverage)
  Income
  Bills & Expenses
  Assets
  Accounts
  Insurance
  Documents

─── Settings ───────────────
  Settings
  Log out
```

### Entity Section Component

Create `src/components/sidebar/EntityNavSection.tsx`:

Each entity section renders:
1. **Section header** with entity type icon and label
   - Personal: `User` icon
   - Business: `Building2` icon
   - Estate Planning: `Landmark` icon (contains Trust entities)

2. **Entity switcher** (for Business and Estate Planning sections):
   - **Multiple entities**: Show a dropdown/select at the top of the section displaying the current entity name. Selecting a different entity updates the URL slug and all nav links in that section.
   - **Single entity**: Hide the dropdown, just show the entity name as a label above the nav items. Still show the "+ Add" button.
   - **No entities**: Show only the "+ Add" button with helper text (e.g., "Create your first business entity to track its finances separately").
   - **"+ Add" button**: Always visible at the bottom of the section (or inside the dropdown as the last option):
     - Business section: "+ Add Business"
     - Estate Planning section: "+ Add Trust"
     - Opens a `Sheet`/`Dialog` to create a new entity:
       - **Business fields**: Name (required), Formation Type (select: LLC, S-Corp, C-Corp, Partnership, Sole Proprietorship, Non-Profit), EIN (optional), State of Formation (select), Industry (text)
       - **Trust fields**: Name (required), Category (select: Revocable, Irrevocable), Purpose (select: Asset Protection, Estate Planning, Charitable, Special Needs, Education, Tax Planning), Jurisdiction (text), Trust Date (date picker)
     - On submit, the new entity appears in the sidebar and the user navigates to its Overview page.

3. **Nav items** — same sub-items for all entity types, using entity-relative paths:

```typescript
// Shared nav items for Personal and Business entities
const baseEntityNavItems = [
  { icon: LayoutDashboard, label: "Overview", subPath: "/overview" },
  { icon: DollarSign, label: "Income", subPath: "/income" },
  { icon: Receipt, label: "Bills & Expenses", subPath: "/bills" },
  { icon: Package, label: "Assets", subPath: "/assets" },
  { icon: Wallet, label: "Accounts", subPath: "/accounts" },
  { icon: Receipt, label: "Transactions", subPath: "/transactions" },
  { icon: Shield, label: "Insurance", subPath: "/insurance" },
  { icon: Calculator, label: "Debt Payoff", subPath: "/debt-payoff" },
  { icon: FileText, label: "Documents", subPath: "/documents" },
];

// Personal and Business both use baseEntityNavItems — no Estate Planning
// link needed since all estate planning lives in the Estate Planning section

// Trust entities under Estate Planning section — Overview IS the estate
// planning view (roles, beneficiaries, coverage, trust admin), so no
// separate "Estate Planning" nav item needed
const trustNavItems = [
  { icon: LayoutDashboard, label: "Overview", subPath: "/overview" },  // combines estate + overview
  { icon: DollarSign, label: "Income", subPath: "/income" },
  { icon: Receipt, label: "Bills & Expenses", subPath: "/bills" },
  { icon: Package, label: "Assets", subPath: "/assets" },
  { icon: Wallet, label: "Accounts", subPath: "/accounts" },
  { icon: Shield, label: "Insurance", subPath: "/insurance" },
  { icon: FileText, label: "Documents", subPath: "/documents" },
];
```

Use the appropriate nav items array based on entity type:
- `entityType === "Personal"` → `personalNavItems`
- `entityType === "Business"` → `baseEntityNavItems`
- `entityType === "Trust"` → `trustNavItems`

Each item's `path` is computed:
- Personal: `/personal${subPath}` (e.g., `/personal/income`)
- Business: `/business/${slug}${subPath}` (e.g., `/business/acme-llc/income`)
- Trust: `/trust/${slug}${subPath}` (e.g., `/trust/james-family-trust/income`)

### Active State

Highlight the current nav item by matching `useLocation().pathname` against computed paths.

### Collapsible Sections

Each entity section is collapsible (click header to expand/collapse). Default: Personal expanded, Business collapsed, Estate Planning collapsed. Remember collapse state in localStorage.

---

## Routing Setup

Update `App.tsx` to use entity-scoped routing. **Remove** the current flat routes for `/income`, `/estate`, `/assets`, `/accounts`, `/transactions`, `/contacts` and replace with:

```tsx
import { EntityLayout } from "@/components/layout/EntityLayout";

{/* Household Dashboard */}
<Route path="/household/dashboard" element={
  <ProtectedRoute policy="RequireClient">
    <DashboardLayout><Dashboard /></DashboardLayout>
  </ProtectedRoute>
} />

{/* Personal entity routes (no slug needed — one per user) */}
<Route path="/personal/*" element={
  <ProtectedRoute policy="RequireClient">
    <EntityLayout entityType="Personal" />
  </ProtectedRoute>
} />

{/* Business entity routes (slug required) */}
<Route path="/business/:slug/*" element={
  <ProtectedRoute policy="RequireClient">
    <EntityLayout entityType="Business" />
  </ProtectedRoute>
} />

{/* Trust entity routes (slug required) */}
<Route path="/trust/:slug/*" element={
  <ProtectedRoute policy="RequireClient">
    <EntityLayout entityType="Trust" />
  </ProtectedRoute>
} />

{/* Redirect old flat routes to personal entity */}
<Route path="/assets" element={<Navigate to="/personal/assets" replace />} />
<Route path="/income" element={<Navigate to="/personal/income" replace />} />
<Route path="/estate" element={<Navigate to="/personal/estate" replace />} />
<Route path="/accounts" element={<Navigate to="/personal/accounts" replace />} />
<Route path="/transactions" element={<Navigate to="/personal/transactions" replace />} />
<Route path="/contacts" element={<Navigate to="/personal/contacts" replace />} />
<Route path="/dashboard" element={<Navigate to="/household/dashboard" replace />} />
```

---

## EntityLayout Component

Create `src/components/layout/EntityLayout.tsx`:

```tsx
import { Routes, Route, useParams } from "react-router-dom";
import { EntityProvider } from "@/contexts/EntityContext";
import { useEntityBySlug, useEntities } from "@/services/entity-service";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";

// Import all feature pages
import Income from "@/pages/Income";
import EstatePlanning from "@/pages/EstatePlanning";
import Assets from "@/pages/Assets";
import Accounts from "@/pages/Accounts";
import Transactions from "@/pages/Transactions";
import Contacts from "@/pages/Contacts";
// import EntityOverview from "@/pages/EntityOverview"; // TODO

interface EntityLayoutProps {
  entityType: "Personal" | "Business" | "Trust";
}

export function EntityLayout({ entityType }: EntityLayoutProps) {
  const { slug } = useParams();

  // Personal entity has no slug — use the user's personal entity
  const resolvedSlug = entityType === "Personal" ? "personal" : slug!;

  const { data: entity, isLoading } = useEntityBySlug(entityType, resolvedSlug);

  if (isLoading) return <DashboardLayout><LoadingSkeleton /></DashboardLayout>;
  if (!entity) return <DashboardLayout><EntityNotFound /></DashboardLayout>;

  return (
    <EntityProvider entity={entity}>
      <DashboardLayout>
        <Routes>
          <Route path="overview" element={<EntityOverviewPlaceholder />} />
          <Route path="income" element={<Income />} />
          <Route path="bills" element={<BillsPlaceholder />} />
          <Route path="assets" element={<Assets />} />
          <Route path="accounts" element={<Accounts />} />
          <Route path="transactions" element={<Transactions />} />
          <Route path="contacts" element={<Contacts />} />
          <Route path="insurance" element={<InsurancePlaceholder />} />
          <Route path="debt-payoff" element={<DebtPayoffPlaceholder />} />
          <Route path="estate" element={<EstatePlanning />} />
          <Route path="documents" element={<DocumentsPlaceholder />} />
          <Route index element={<Navigate to="overview" replace />} />
        </Routes>
      </DashboardLayout>
    </EntityProvider>
  );
}
```

For pages that don't exist yet, create minimal placeholder components:

```tsx
function EntityOverviewPlaceholder() {
  const { entityName } = useEntityContext();
  return (
    <div className="flex flex-col items-center justify-center h-64 text-muted-foreground">
      <LayoutDashboard className="h-12 w-12 mb-4" />
      <h2 className="text-lg font-semibold">{entityName} Overview</h2>
      <p className="text-sm">Coming soon</p>
    </div>
  );
}
```

---

## Refactor Existing Pages

### Income Page (`src/pages/Income.tsx`)

The Income page currently:
- Uses hardcoded `"personal-default"` entity ID
- Has its own page wrapper

**Changes needed:**
1. Import and call `useEntityContext()` at the top:
   ```tsx
   const { entityId, entityType, entitySlug } = useEntityContext();
   ```
2. Pass `entityId` to all service hooks: `useIncomeSources(entityId)`, `useIncomeSummary(entityId)`
3. Remove `<DashboardLayout>` wrapper (EntityLayout already provides it)
4. Update any hardcoded links (e.g., `/income`) to use entity-relative paths

### Estate Planning Page (`src/pages/EstatePlanning.tsx`)

Same pattern:
1. Add `useEntityContext()`
2. Pass `entityId` to `useEstateSummary(entityId)`
3. Remove `<DashboardLayout>` wrapper
4. Update hardcoded links to entity-relative paths

### Assets Page (`src/pages/Assets.tsx`)

1. Add `useEntityContext()`
2. Pass `entityId` to asset hooks
3. Remove `<DashboardLayout>` wrapper
4. Update links

### Accounts Page (`src/pages/Accounts.tsx`)

1. Add `useEntityContext()`
2. Pass `entityId` to account hooks
3. Remove `<DashboardLayout>` wrapper

### Transactions Page (`src/pages/Transactions.tsx`)

1. Add `useEntityContext()`
2. Pass `entityId` to transaction hooks
3. Remove `<DashboardLayout>` wrapper

### Contacts Page (`src/pages/Contacts.tsx`)

1. Add `useEntityContext()`
2. Pass `entityId` to contact hooks (contacts can be entity-scoped or shared)
3. Remove `<DashboardLayout>` wrapper

---

## Entity Type Badges & Icons

Create `src/components/entity/EntityTypeBadge.tsx`:

```tsx
import { User, Building2, Landmark } from "lucide-react";
import { Badge } from "@/components/ui/badge";

const entityConfig = {
  Personal: { icon: User, color: "bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300", label: "Personal" },
  Business: { icon: Building2, color: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300", label: "Business" },
  Trust: { icon: Landmark, color: "bg-purple-100 text-purple-700 dark:bg-purple-950 dark:text-purple-300", label: "Trust" },
};

export function EntityTypeBadge({ type }: { type: EntityType }) {
  const config = entityConfig[type];
  const Icon = config.icon;
  return (
    <Badge variant="outline" className={cn("gap-1", config.color)}>
      <Icon className="h-3 w-3" />
      {config.label}
    </Badge>
  );
}
```

---

## Page Header with Entity Context

Each feature page should show the entity context in its header. Create `src/components/entity/EntityPageHeader.tsx`:

```tsx
export function EntityPageHeader({
  title,
  description,
  actions,
}: {
  title: string;
  description?: string;
  actions?: ReactNode;
}) {
  const { entityName, entityType } = useEntityContext();

  return (
    <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between mb-6">
      <div>
        <div className="flex items-center gap-2">
          <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
          <EntityTypeBadge type={entityType} />
        </div>
        {description && (
          <p className="text-sm text-muted-foreground mt-1">{description}</p>
        )}
        <p className="text-xs text-muted-foreground">{entityName}</p>
      </div>
      {actions && <div className="flex gap-2">{actions}</div>}
    </div>
  );
}
```

---

## Default Redirect

When user logs in, redirect to `/household/dashboard` (was `/dashboard`). Update `Index.tsx`:
- Authenticated client users → `/household/dashboard`
- Admin users → `/admin/dashboard`

---

## Accessibility

- Entity switcher dropdown: `role="listbox"` with `aria-label="Select business entity"`
- Collapsible sections: `aria-expanded` on headers, `aria-controls` on content
- Entity type badges: `aria-label` with full type name
- Nav items: `aria-current="page"` on active item
- Sidebar: `role="navigation"` with `aria-label="Entity navigation"`
- Entity section headers use `<h2>` for proper heading hierarchy

## i18n

- All entity type labels use translation keys: `t("entity.type.personal")`, `t("entity.type.business")`, `t("entity.type.trust")`
- Section headers: `t("nav.section.personal")`, `t("nav.section.business")`, `t("nav.section.estatePlanning")`
- Entity switcher labels: `t("entity.switcher.select")`, `t("entity.switcher.addBusiness")`, `t("entity.switcher.addTrust")`
- Create translation keys in `src/locales/en/entity.json`

## Mobile

- Entity sections stack vertically in mobile sidebar (same hamburger menu pattern)
- Entity switcher is full-width on mobile
- Collapsed sidebar shows only entity type icons (Personal=User, Business=Building2, Estate Planning=Landmark)
- Touch targets minimum 44x44px on all nav items and entity switcher
