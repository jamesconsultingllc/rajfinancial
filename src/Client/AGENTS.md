# Client-Side Agent Instructions (TypeScript)

> **Frontend-specific directives.** The shared rules at the [repo root AGENTS.md](../AGENTS.md) also apply.

---

## Required Packages

Install these when scaffolding a new client-side project. Versions will drift — use latest stable.

### Core Runtime

| Package | Purpose |
|---------|---------|
| `react` / `next` / `vite` | Framework (pick one per project) |
| `typescript` | Type safety — **strict mode required** |
| `tailwindcss` | Utility-first CSS (mobile-first) |
| `@tanstack/react-query` | Server state management, caching, mutations |
| `react-hook-form` | Form state + validation |
| `zod` | Schema validation (shared with API contracts) |
| `react-router-dom` | Client-side routing |
| `react-helmet-async` | Per-page SEO `<title>` and meta tags |

### Auth (MSAL / Entra)

| Package | Purpose |
|---------|--------|
| `@azure/msal-browser` | MSAL.js browser auth (Entra External ID / B2C) |
| `@azure/msal-react` | React hooks + components for MSAL |

### BDD / TDD / Unit Testing

| Package | Purpose |
|---------|---------|
| `vitest` | Unit test runner (fast, Vite-native) |
| `@testing-library/react` | Component testing — test behavior, not implementation |
| `@testing-library/jest-dom` | DOM assertion matchers |
| `@testing-library/user-event` | Realistic user interaction simulation |
| `msw` | Mock Service Worker — API mocking at the network level |
| `@faker-js/faker` | Realistic test data generation |

### BDD + E2E Testing

| Package | Purpose |
|---------|---------|
| `@playwright/test` | Cross-browser E2E tests |
| `playwright-bdd` | Gherkin `.feature` files running as Playwright E2E tests |

### Accessibility Testing

| Package | Purpose |
|---------|---------|
| `@axe-core/playwright` | axe accessibility engine in Playwright E2E tests |
| `jest-axe` | axe assertions in Vitest/Jest unit tests |
| `eslint-plugin-jsx-a11y` | Lint-time a11y rule enforcement |

### Localization (i18n)

| Package | Purpose |
|---------|---------|
| `react-i18next` + `i18next` | Runtime translation framework |
| `i18next-browser-languagedetector` | Auto-detect user locale |
| `eslint-plugin-i18next` | Lint rule: flag hardcoded user-facing strings |
| `i18next-parser` | Extract translation keys from code, detect missing translations |

### Logging & Telemetry

| Package | Purpose |
|---------|---------|
| `@opentelemetry/api` | OTel trace/metric API (vendor-neutral) |
| `@opentelemetry/sdk-trace-web` | Browser tracing SDK |
| `@opentelemetry/sdk-metrics` | Browser metrics SDK |
| `@opentelemetry/exporter-trace-otlp-http` | Export traces to collector |
| `@opentelemetry/instrumentation-fetch` | Auto-instrument `fetch` calls |
| `@opentelemetry/instrumentation-document-load` | Page load performance spans |

### Code Quality

| Package | Purpose |
|---------|---------|
| `eslint` | Linting |
| `prettier` | Formatting — **required, not optional** |
| `eslint-plugin-react-hooks` | React hooks rules |
| `@typescript-eslint/eslint-plugin` | TypeScript-specific lint rules |
| `eslint-plugin-jsx-a11y` | Accessibility lint rules |
| `eslint-plugin-i18next` | Flag hardcoded user-facing strings |
| `eslint-plugin-import` | Import ordering and grouping |

---

## TypeScript Strict Mode — Non-Negotiable

`tsconfig.json` **must** have `"strict": true`. Never disable these individually:

```jsonc
{
  "compilerOptions": {
    "strict": true,               // Enables ALL strict checks
    // NEVER set these to false:
    // "strictNullChecks": false   ← defeats TypeScript's core value
    // "noImplicitAny": false      ← allows untyped code to slip in
  }
}
```

---

## Project Structure (Vertical Slices)

Organize by **feature**, not by layer:

```
src/
├── features/
│   ├── orders/                  # One vertical slice
│   │   ├── components/
│   │   │   ├── OrderList.tsx
│   │   │   └── OrderList.test.tsx
│   │   ├── hooks/
│   │   │   ├── use-orders.ts
│   │   │   └── use-orders.test.ts
│   │   ├── api/
│   │   │   └── orders-api.ts
│   │   ├── types/
│   │   │   └── order.ts
│   │   ├── locales/
│   │   │   ├── en.json
│   │   │   └── es.json
│   │   └── orders.feature        # BDD spec for this slice
│   └── auth/                     # Another vertical slice
│       └── ...
├── shared/                       # Truly shared utilities only
│   ├── components/
│   ├── hooks/
│   └── lib/
├── generated/
│   └── memorypack/              # Auto-generated TS types from C# DTOs
├── app/                          # Routes / pages (thin wrappers)
└── test/
    ├── setup.ts                  # Vitest global setup
    ├── msal-mocks.tsx            # Reusable MSAL auth test utilities
    ├── msw-handlers.ts           # MSW request handlers
    └── e2e/                      # Playwright E2E tests
        └── features/             # .feature files + step defs
```

---

## MemoryPack DTO Generation

When the backend uses MemoryPack, **auto-generate TypeScript types from C# DTOs** so API contracts stay in sync:

### package.json Scripts

```json
{
  "scripts": {
    "gen:dtos": "dotnet build ../Shared/MyApp.Shared.csproj",
    "predev": "npm run gen:dtos",
    "prebuild": "npm run gen:dtos",
    "pretest": "npm run gen:dtos"
  }
}
```

C# DTOs use `[MemoryPackable]` + `[GenerateTypeScript]` → generated TS classes with `serialize()` / `deserialize()` appear in `src/generated/memorypack/`.

---

## API Client Pattern (MemoryPack + Auth)

Wrap `fetch` with token injection and binary serialization:

```typescript
/**
 * Authenticated API client with MemoryPack binary support.
 * Acquires MSAL token silently, falls back to interactive redirect.
 * Sends Accept: application/x-memorypack for binary responses.
 */
export async function apiClient<T>(
  path: string,
  options: {
    method?: string;
    body?: Uint8Array | object;
    deserialize: (buf: Uint8Array) => T;
  }
): Promise<T> {
  const token = await acquireTokenSilentOrRedirect();

  const response = await fetch(`/api${path}`, {
    method: options.method ?? "GET",
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/x-memorypack",
      ...(options.body instanceof Uint8Array
        ? { "Content-Type": "application/x-memorypack" }
        : options.body
          ? { "Content-Type": "application/json" }
          : {}),
    },
    body: options.body instanceof Uint8Array
      ? options.body
      : options.body
        ? JSON.stringify(options.body)
        : undefined,
  });

  if (!response.ok) {
    const error = await response.json();
    throw new ApiError(error.code, error.message, response.status, error.details);
  }

  const buf = new Uint8Array(await response.arrayBuffer());
  return options.deserialize(buf);
}

// Usage with auto-generated DTO:
const profile = await apiClient("/auth/me", {
  deserialize: (buf) => UserProfileResponse.deserialize(buf),
});
```

---

## Auth Pattern (MSAL + Route Guards)

### Provider

```tsx
/**
 * Singleton MSAL instance. Initialize once, gate the app on completion.
 */
const msalInstance = new PublicClientApplication(msalConfig);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [initialized, setInitialized] = useState(false);

  useEffect(() => {
    msalInstance.initialize().then(() => {
      msalInstance.handleRedirectPromise();
      setInitialized(true);
    });
  }, []);

  if (!initialized) return <LoadingSpinner />;

  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}
```

### Policy-Based Route Guards

```tsx
/**
 * Route guard that checks role-based policies.
 * Unauthenticated users are redirected to MSAL login.
 * Unauthorized users see a 403 page.
 */
export function ProtectedRoute({
  policy,
  children,
}: {
  policy: "RequireAuthenticated" | "RequireClient" | "RequireAdministrator";
  children: React.ReactNode;
}) {
  const { isAuthenticated, roles } = useAuth();

  if (!isAuthenticated) {
    triggerLoginRedirect();
    return null;
  }

  if (!satisfiesPolicy(policy, roles)) return <ForbiddenPage />;

  return <>{children}</>;
}
```

---

## React Query Error Boundary

Route critical API errors (401, 403) to a global error boundary while letting validation errors surface inline:

```tsx
/**
 * Determines if an error should propagate to ApiErrorBoundary.
 * 401/403 → boundary handles (re-auth or forbidden page).
 * 400/422 → inline handling (form errors, toasts).
 */
function shouldThrowApiError(error: Error): boolean {
  if (error instanceof ApiError) {
    return [401, 403, 0].includes(error.status);
  }
  return true; // Unknown errors always throw
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,
      gcTime: 10 * 60 * 1000,
      retry: 1,
      throwOnError: shouldThrowApiError,
    },
  },
});
```

---

## Auth Test Mocks

Create reusable MSAL test utilities to avoid boilerplate in every test file:

```tsx
// test/msal-mocks.tsx
import { vi } from "vitest";

/** Set mock auth state for tests. */
export function setMockAuthState(overrides: {
  isAuthenticated?: boolean;
  roles?: string[];
  userId?: string;
}) {
  vi.mocked(useAuth).mockReturnValue({
    isAuthenticated: overrides.isAuthenticated ?? true,
    roles: overrides.roles ?? ["Client"],
    user: { id: overrides.userId ?? "test-user-id", name: "Test User" },
    login: vi.fn(),
    logout: vi.fn(),
  });
}

/** Test wrapper with all required providers. */
export function TestProviders({ children }: { children: React.ReactNode }) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return (
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{children}</MemoryRouter>
    </QueryClientProvider>
  );
}
```

---

## Lazy Loading (Route-Level Code Splitting)

**Lazy-load page components** to reduce initial bundle size:

```tsx
import { lazy, Suspense } from "react";

const Dashboard = lazy(() => import("./pages/Dashboard"));
const Assets = lazy(() => import("./pages/Assets"));
const Settings = lazy(() => import("./pages/Settings"));

function App() {
  return (
    <Suspense fallback={<LoadingSpinner />}>
      <Routes>
        <Route path="/dashboard" element={
          <ProtectedRoute policy="RequireClient">
            <Dashboard />
          </ProtectedRoute>
        } />
        {/* ... */}
      </Routes>
    </Suspense>
  );
}
```

---

## Accessibility Requirements

### Rules

- Semantic HTML elements (`<button>`, `<nav>`, `<main>`, `<article>`)
- ARIA attributes where semantic HTML is insufficient
- Keyboard navigation for all interactive elements
- Visible focus indicators (never `outline: none` without replacement)
- Color contrast: 4.5:1 for text, 3:1 for large text
- Alt text for all images and meaningful icons
- Screen reader support with labels and live regions
- **Skip-to-content link** as the first focusable element in every layout
- Form labels associated with inputs
- Minimum 44x44px touch targets

### Skip Link Pattern

```tsx
{/* First element inside <body> / root layout */}
<a
  href="#main-content"
  className="sr-only focus:not-sr-only focus:absolute focus:z-50 focus:p-4 focus:bg-background focus:text-foreground"
>
  Skip to main content
</a>

{/* Target */}
<main id="main-content">{children}</main>
```

### Testing Pattern

```tsx
// Unit test with jest-axe
import { axe, toHaveNoViolations } from "jest-axe";
expect.extend(toHaveNoViolations);

it("should have no a11y violations", async () => {
  const { container } = render(<OrderList orders={mockOrders} />);
  expect(await axe(container)).toHaveNoViolations();
});

// E2E test with @axe-core/playwright
import AxeBuilder from "@axe-core/playwright";

test("order page passes a11y", async ({ page }) => {
  await page.goto("/orders");
  const results = await new AxeBuilder({ page }).analyze();
  expect(results.violations).toEqual([]);
});
```

---

## Localization Requirements

### Rules

- **Never hardcode user-facing strings** — always use `t()` from `react-i18next`
- API returns error codes; client localizes messages
- Support RTL layouts (CSS logical properties: `ms-`, `me-`, `ps-`, `pe-` in Tailwind)
- Format dates/numbers/currencies per locale (use `Intl` APIs)
- Account for text expansion (30-50% longer than English)
- Use ICU message format for pluralization

### Translation File Structure

```json
// src/features/orders/locales/en.json
{
  "orders": {
    "title": "My Orders",
    "empty": "No orders found",
    "status": {
      "pending": "Pending",
      "shipped": "Shipped",
      "delivered": "Delivered"
    },
    "count": "{{count}} order",
    "count_other": "{{count}} orders"
  }
}
```

### CI Check

Run `i18next-parser` in CI to fail on missing keys:

```bash
npx i18next-parser --fail-on-update
```

---

## Mobile Responsiveness

**Mobile-first CSS approach.** Design for mobile viewport first, enhance for desktop.

### Rules

- Base styles target mobile; use `sm:`, `md:`, `lg:`, `xl:` to add desktop complexity
- Touch-friendly: minimum 44x44px interactive targets
- No horizontal scrolling on any viewport
- Responsive tables: card layout on mobile or horizontal scroll
- Minimum 16px body text
- Collapsible sidebar → hamburger on mobile
- Test on real devices, not just browser dev tools

### Pattern

```tsx
{/* Mobile-first responsive grid */}
<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
  {orders.map(order => <OrderCard key={order.id} order={order} />)}
</div>

{/* Responsive table → cards on mobile */}
<div className="hidden md:block">
  <OrderTable orders={orders} />
</div>
<div className="md:hidden space-y-4">
  {orders.map(order => <OrderCard key={order.id} order={order} />)}
</div>
```

---

## Authorization (Frontend)

1. **Hide, Don't Disable** — Unauthorized features must be **hidden entirely**
2. **Conditional Rendering** — Check permissions before rendering
3. **Route Guards** — Redirect unauthorized access
4. **No Client-Side Trust** — UI hiding is UX only; always enforce server-side

```tsx
// ✅ Correct: Hide unauthorized features
{hasPermission("users:manage") && <MenuItem>Manage Users</MenuItem>}

// ❌ Incorrect: Disabled but visible
<MenuItem disabled={!hasPermission("users:manage")}>Manage Users</MenuItem>
```

---

## Error Handling (Frontend)

- API returns error codes — client maps to localized messages via `t(`errors.${code}`)`
- Use React Error Boundaries for unexpected crashes
- Show user-friendly messages, never stack traces
- Log errors to telemetry backend with correlation IDs

```tsx
function handleApiError(error: ApiError) {
  const message = t(`errors.${error.code}`, {
    defaultValue: t("errors.UNKNOWN"),
    ...error.details,
  });
  toast.error(message);
}
```

---

## Logging & Telemetry (Frontend)

### Structured Browser Logging

```typescript
// ✅ Correct: Structured context object
logger.info("Order placed", {
  orderId: order.id,
  userId,
  amount: order.total,
  itemCount: order.items.length,
  correlationId: ctx.correlationId,
});

// ❌ Incorrect: Template literal
logger.info(`Order ${order.id} placed by ${userId}`);
```

### Tracing (Browser)

```typescript
import { trace } from "@opentelemetry/api";

const tracer = trace.getTracer("my-app-client");

async function fetchOrders(): Promise<Order[]> {
  return tracer.startActiveSpan("fetchOrders", async (span) => {
    try {
      const orders = await api.getOrders();
      span.setAttribute("order.count", orders.length);
      return orders;
    } catch (err) {
      span.recordException(err as Error);
      span.setStatus({ code: SpanStatusCode.ERROR });
      throw err;
    } finally {
      span.end();
    }
  });
}
```

### What to Track (Frontend-Specific)

- Page views and navigation timing
- Core Web Vitals (LCP, FID/INP, CLS)
- API call duration and failure rates
- User interactions (clicks, form submissions) — anonymized
- Client-side errors (unhandled rejections, error boundaries)

---

## Documentation (TypeScript)

Use JSDoc for all public exports:

```typescript
/**
 * Fetches paginated orders for the current user.
 *
 * @param page - Page number (1-indexed)
 * @param pageSize - Number of items per page (default 20)
 * @returns Paginated order list with total count
 * @throws {ApiError} When the API request fails
 *
 * @example
 * const { data, isLoading } = useOrders({ page: 1, pageSize: 20 });
 */
export function useOrders(params: OrderQueryParams) { ... }
```

---

## Client-Side Checklist (Pre-Merge)

- [ ] `tsconfig.json` has `"strict": true` — no exceptions
- [ ] All user-facing strings use `t()` — no hardcoded text
- [ ] `eslint-plugin-i18next` enabled — flags missed hardcoded strings
- [ ] `eslint-plugin-jsx-a11y` enabled — flags a11y violations at lint time
- [ ] Components pass `jest-axe` and `@axe-core/playwright` checks
- [ ] Skip-to-content link present in every layout
- [ ] Mobile layout tested at 320px, 375px, 768px, 1024px, 1440px
- [ ] Unauthorized features hidden (not disabled) via policy-based route guards
- [ ] API errors mapped to localized messages via error codes
- [ ] React Query error boundary routes 401/403 to global handler
- [ ] OTel tracing on API calls and critical user flows
- [ ] Core Web Vitals tracked
- [ ] BDD `.feature` files written before implementation
- [ ] 90%+ test coverage on new code (enforced in CI)
- [ ] `i18next-parser` reports no missing translation keys
- [ ] Route-level lazy loading with `React.lazy` + `Suspense`
- [ ] Generated MemoryPack types up-to-date (`gen:dtos` runs in pre-hooks)
- [ ] Reusable auth test mocks used (no per-test MSAL boilerplate)
- [ ] Prettier configured and enforced
