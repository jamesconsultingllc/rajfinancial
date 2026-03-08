# RAJ Financial Software — Complete UI Documentation
## React 18 + Vite + TypeScript + Tailwind CSS + shadcn/ui

---

## Brand Identity

```
╔══════════════════════════════════════════════════════════════╗
║                                                              ║
║     ██████╗  █████╗      ██╗    ███████╗██╗███╗   ██╗       ║
║     ██╔══██╗██╔══██╗     ██║    ██╔════╝██║████╗  ██║       ║
║     ██████╔╝███████║     ██║    █████╗  ██║██╔██╗ ██║       ║
║     ██╔══██╗██╔══██║██   ██║    ██╔══╝  ██║██║╚██╗██║       ║
║     ██║  ██║██║  ██║╚█████╔╝    ██║     ██║██║ ╚████║       ║
║     ╚═╝  ╚═╝╚═╝  ╚═╝ ╚════╝     ╚═╝     ╚═╝╚═╝  ╚═══╝       ║
║                                                              ║
║                    Your Financial Future                     ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
```

**Name**: RAJ Financial Software
**Logo**: RF monogram with wing motif (gold gradient) — rendered by `Logo` component (`src/components/Logo.tsx`)
**Font**: Inter (body & UI) — loaded via Google Fonts in `index.css`
**Colors** (Gold palette — HSL variables defined in `index.css`):

| Swatch | Hex | Name | Usage |
|--------|-----|------|-------|
| `#fffbcc` | Lemon Chiffon | Lightest backgrounds |
| `#fff7b3` | Light cream | Subtle fills |
| `#f5e99a` | Soft gold | Hover states |
| `#eed688` | Flax | Secondary elements |
| `#e8c94d` | Bright gold | Active accents |
| `#ebbb10` | Spanish Yellow | **PRIMARY** — `hsl(43 92% 49%)` in light mode |
| `#d4a80e` | Rich gold | Dark-mode primary |
| `#c3922e` | UC Gold | Accent / depth |
| `#a67c26` | Deep gold | Borders |
| `#8a661f` | Darkest gold | Text on light backgrounds |

---

## Table of Contents

1. [Part 1: Solution Structure](#part-1-solution-structure)
2. [Part 2: Dependencies](#part-2-dependencies)
3. [Part 3: CSS Design System](#part-3-css-design-system)
4. [Part 4: Authentication & Authorization](#part-4-authentication--authorization)
5. [Part 5: App Configuration & Providers](#part-5-app-configuration--providers)
6. [Part 6: Dashboard Page](#part-6-dashboard-page)
7. [Part 7: Assets Page](#part-7-assets-page)
8. [Part 8: Profile Page](#part-8-profile-page)
9. [Part 9: Landing Page](#part-9-landing-page)
10. [Part 10: Shared UI Components](#part-10-shared-ui-components)
11. [Part 11: Custom Hooks](#part-11-custom-hooks)
12. [Part 12: Data Layer & Services](#part-12-data-layer--services)
13. [Part 13: Navigation & Layout](#part-13-navigation--layout)
14. [Part 14: Sensitive Field Handling](#part-14-sensitive-field-handling)
15. [Part 15: Settings & Privacy](#part-15-settings--privacy)
16. [Summary Table](#summary-table)

---

## Part 1: Solution Structure

The React client lives at `src/Client/` within the monorepo. It is a **Vite** project using the SWC compiler for fast HMR.

```
src/Client/
├── index.html                    # Vite HTML entry point
├── package.json                  # Dependencies & scripts
├── vite.config.ts                # Vite + Vitest configuration
├── tailwind.config.ts            # Tailwind CSS + shadcn/ui theme
├── tsconfig.json                 # TypeScript config (path aliases)
├── tsconfig.app.json             # App-specific TS config
├── tsconfig.node.json            # Node tooling TS config
├── postcss.config.js             # PostCSS (Tailwind + autoprefixer)
├── components.json               # shadcn/ui component registry config
├── public/
│   └── robots.txt
└── src/
    ├── main.tsx                  # React DOM entry — renders <App />
    ├── App.tsx                   # Provider tree + routes
    ├── App.css                   # (minimal, most styles in index.css)
    ├── index.css                 # Full design system — CSS variables, utilities
    ├── vite-env.d.ts             # Vite client types
    │
    ├── auth/                     # Authentication (MSAL / Entra External ID)
    │   ├── authConfig.ts         # MSAL configuration
    │   ├── AuthProvider.tsx       # MSAL provider wrapper
    │   ├── ProtectedRoute.tsx     # Role-based route guards
    │   ├── useAuth.ts            # Authentication hook
    │   └── __tests__/            # Auth unit tests
    │
    ├── components/               # Application components
    │   ├── dashboard/
    │   │   └── DashboardLayout.tsx  # Authenticated app shell (sidebar + top bar)
    │   ├── ui/                   # shadcn/ui primitives (49 components)
    │   │   ├── accordion.tsx
    │   │   ├── alert.tsx
    │   │   ├── avatar.tsx
    │   │   ├── badge.tsx
    │   │   ├── button.tsx
    │   │   ├── card.tsx
    │   │   ├── dialog.tsx
    │   │   ├── dropdown-menu.tsx
    │   │   ├── form.tsx
    │   │   ├── input.tsx
    │   │   ├── select.tsx
    │   │   ├── sheet.tsx
    │   │   ├── sidebar.tsx
    │   │   ├── skeleton.tsx
    │   │   ├── table.tsx
    │   │   ├── tabs.tsx
    │   │   ├── toast.tsx
    │   │   ├── tooltip.tsx
    │   │   └── ... (49 total)
    │   ├── CTASection.tsx        # Landing — call to action
    │   ├── FeatureCard.tsx       # Landing — feature card
    │   ├── FeaturesSection.tsx   # Landing — features grid
    │   ├── Footer.tsx            # Landing — footer
    │   ├── GlassCard.tsx         # Reusable glass morphism card
    │   ├── HeroSection.tsx       # Landing — hero banner
    │   ├── HowItWorksSection.tsx # Landing — how it works
    │   ├── Logo.tsx              # RF logo component
    │   ├── Navbar.tsx            # Landing — top navigation bar
    │   ├── NavLink.tsx           # Landing — nav link
    │   ├── SecuritySection.tsx   # Landing — security section
    │   ├── StatCard.tsx          # Landing — statistic card
    │   └── ThemeToggle.tsx       # Dark/light mode toggle
    │
    ├── data/
    │   └── mock-assets.ts        # Mock asset data + summary computation
    │
    ├── generated/
    │   └── memorypack/           # MemoryPack generated serializers
    │
    ├── hooks/
    │   ├── use-mobile.tsx        # Viewport breakpoint detection
    │   ├── use-theme.tsx         # Theme provider + toggle
    │   └── use-toast.ts          # Toast notification hook
    │
    ├── lib/
    │   └── utils.ts              # cn() helper (clsx + tailwind-merge)
    │
    ├── pages/
    │   ├── Index.tsx             # Landing / role-based redirect
    │   ├── Dashboard.tsx         # Main dashboard (net worth, accounts)
    │   ├── Assets.tsx            # Asset list with filters
    │   ├── Profile.tsx           # User profile management
    │   └── NotFound.tsx          # 404 page
    │
    ├── services/
    │   └── asset-service.ts      # Asset CRUD + TanStack Query hooks
    │
    ├── types/
    │   └── assets.ts             # Asset TypeScript interfaces & constants
    │
    └── assets/                   # Static images (logo, icons)
```

### Key Conventions

| Convention | Detail |
|------------|--------|
| **Path alias** | `@/*` maps to `./src/*` — configured in `tsconfig.json` and `vite.config.ts` |
| **Component style** | Function components with arrow syntax or `function` keyword |
| **Exports** | Named exports for components; `default` export for pages |
| **Naming** | PascalCase for components, kebab-case for files, camelCase for hooks |
| **CSS approach** | Tailwind utility classes; zero CSS modules; global design tokens in `index.css` |
| **Icon library** | Lucide React (`lucide-react`) — tree-shakeable SVG icons |
| **State management** | TanStack React Query for server state; React Context for UI state (theme, auth) |

---

## Part 2: Dependencies

### Runtime Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `react` | ^18.3.1 | UI framework |
| `react-dom` | ^18.3.1 | DOM renderer |
| `react-router-dom` | ^6.x | Client-side routing |
| `@tanstack/react-query` | ^5.x | Server state management, caching, mutations |
| `@azure/msal-browser` | ^3.x | MSAL authentication (Entra External ID) |
| `@azure/msal-react` | ^2.x | React bindings for MSAL |
| `react-helmet-async` | ^2.x | Document head management (SEO) |
| `recharts` | ^2.x | Chart/graph library for data visualization |
| `zod` | ^3.x | Runtime schema validation |
| `react-hook-form` | ^7.x | Form state management |
| `@hookform/resolvers` | ^3.x | Zod resolver for react-hook-form |
| `lucide-react` | ^0.462.x | SVG icon library (tree-shakeable) |
| `sonner` | ^1.x | Toast notifications |
| `date-fns` | ^3.x | Date manipulation |
| `clsx` | ^2.x | Conditional className composition |
| `tailwind-merge` | ^2.x | Tailwind class deduplication |
| `class-variance-authority` | ^0.7.x | Component variant API (used by shadcn/ui) |
| `cmdk` | ^1.x | Command palette (shadcn `<Command>`) |
| `input-otp` | ^1.x | OTP input component |
| `embla-carousel-react` | ^8.x | Carousel engine |
| `vaul` | ^1.x | Drawer component engine |
| `next-themes` | — | (not used; custom `use-theme.tsx` instead) |

### Radix UI Primitives (used by shadcn/ui)

All `@radix-ui/react-*` packages — these provide the accessible, unstyled primitives that shadcn/ui wraps:

`accordion`, `alert-dialog`, `aspect-ratio`, `avatar`, `checkbox`, `collapsible`, `context-menu`,
`dialog`, `dropdown-menu`, `hover-card`, `label`, `menubar`, `navigation-menu`, `popover`,
`progress`, `radio-group`, `scroll-area`, `select`, `separator`, `slider`, `slot`, `switch`,
`tabs`, `toast`, `toggle`, `toggle-group`, `tooltip`

### Dev Dependencies

| Package | Purpose |
|---------|---------|
| `vite` | Build tool + dev server |
| `@vitejs/plugin-react-swc` | SWC-based React transform (fast HMR) |
| `@vitejs/plugin-basic-ssl` | HTTPS in dev (required for MSAL) |
| `vitest` | Unit test runner (Vite-native) |
| `@testing-library/react` | Component testing utilities |
| `@testing-library/jest-dom` | Custom DOM matchers |
| `jsdom` | Browser environment for tests |
| `typescript` | Type checking |
| `tailwindcss` | Utility-first CSS framework |
| `postcss` + `autoprefixer` | CSS processing pipeline |
| `eslint` | Linting |
| `tailwindcss-animate` | Animation utilities for Tailwind |

### Scripts

```json
{
  "dev": "vite --host",
  "build": "tsc && vite build",
  "preview": "vite preview",
  "test": "vitest",
  "test:coverage": "vitest --coverage",
  "lint": "eslint ."
}
```


---

## Part 3: CSS Design System

> **File**: `src/Client/src/index.css` (307 lines)

The entire design system is built on **CSS custom properties** (HSL values) consumed by Tailwind via `tailwind.config.ts`. No CSS modules or Sass — just utility classes + a small set of global utility classes for glass morphism and animations.

### 3.1 CSS Variable Architecture

All colors use the HSL channel pattern: `--variable: H S% L%` (without `hsl()` wrapper). Tailwind applies `hsl(var(--variable))` at usage time, enabling opacity modifiers like `bg-primary/20`.

#### Light Mode (`:root`)

```css
:root {
  /* Core palette */
  --background: 40 20% 98%;          /* Near-white warm */
  --foreground: 43 20% 10%;          /* Dark warm */
  --primary: 43 92% 49%;             /* Spanish Yellow #ebbb10 */
  --primary-foreground: 43 20% 10%;  /* Dark text on gold */

  /* Surface hierarchy */
  --card: 40 25% 97%;
  --popover: 40 25% 97%;
  --muted: 40 15% 93%;
  --accent: 43 40% 90%;

  /* Semantic */
  --destructive: 0 84% 60%;          /* Red */
  --success: 142 76% 36%;            /* Green */
  --gold: 43 92% 49%;                /* Alias of primary */

  /* Borders & rings */
  --border: 40 15% 85%;
  --ring: 43 92% 49%;
  --radius: 0.625rem;                /* 10px — default border-radius */

  /* Sidebar (DashboardLayout) */
  --sidebar-background: 43 20% 10%;
  --sidebar-foreground: 40 20% 98%;
  --sidebar-primary: 43 92% 49%;
  --sidebar-accent: 43 15% 18%;
  --sidebar-border: 43 15% 18%;
  --sidebar-ring: 43 92% 49%;
}
```

#### Dark Mode (`.dark`)

```css
.dark {
  --background: 43 15% 7%;           /* Very dark warm */
  --foreground: 40 20% 98%;          /* Light text */
  --primary: 43 92% 49%;             /* Gold stays consistent */
  --primary-foreground: 43 20% 10%;

  --card: 43 12% 10%;
  --popover: 43 12% 10%;
  --muted: 43 10% 15%;
  --accent: 43 15% 15%;

  --border: 43 10% 18%;
  --ring: 43 92% 49%;

  --sidebar-background: 43 12% 6%;
  --sidebar-foreground: 40 20% 98%;
  --sidebar-accent: 43 15% 12%;
  --sidebar-border: 43 10% 15%;
}
```

### 3.2 Tailwind Configuration

> **File**: `src/Client/tailwind.config.ts` (121 lines)

The Tailwind config extends the default theme with shadcn/ui tokens:

```typescript
// tailwind.config.ts — key extensions
{
  theme: {
    extend: {
      colors: {
        // All mapped from CSS variables
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
        },
        gold: {
          DEFAULT: "hsl(var(--gold))",
          foreground: "hsl(var(--gold-foreground))",
        },
        success: {
          DEFAULT: "hsl(var(--success))",
          foreground: "hsl(var(--success-foreground))",
        },
        // card, popover, muted, accent, destructive, sidebar — same pattern
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      keyframes: {
        "accordion-down": { from: { height: "0" }, to: { height: "var(--radix-accordion-content-height)" } },
        "accordion-up":   { from: { height: "var(--radix-accordion-content-height)" }, to: { height: "0" } },
        "fade-in":        { from: { opacity: "0", transform: "translateY(8px)" }, to: { opacity: "1", transform: "translateY(0)" } },
        "fade-in-up":     { from: { opacity: "0", transform: "translateY(16px)" }, to: { opacity: "1", transform: "translateY(0)" } },
        "scale-in":       { from: { opacity: "0", transform: "scale(0.95)" }, to: { opacity: "1", transform: "scale(1)" } },
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up":   "accordion-up 0.2s ease-out",
        "fade-in":        "fade-in 0.6s ease-out forwards",
        "fade-in-up":     "fade-in-up 0.5s ease-out forwards",
        "scale-in":       "scale-in 0.3s ease-out forwards",
      },
    },
  },
}
```

### 3.3 Glass Morphism Utilities

Defined as global classes in `index.css`, these provide the premium translucent card aesthetic:

```css
/* Glass card — frosted glass effect */
.glass-card {
  background: hsl(var(--card) / 0.7);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border: 1px solid hsl(var(--border) / 0.5);
}

/* Gold glow — ambient glow behind gold-accented elements */
.gold-glow {
  box-shadow:
    0 0 20px hsl(var(--primary) / 0.15),
    0 0 60px hsl(var(--primary) / 0.05);
}

/* Animated gold border — shimmer effect on hover */
.gold-border-animated {
  position: relative;
  overflow: hidden;
}
.gold-border-animated::before {
  content: "";
  position: absolute;
  inset: 0;
  border-radius: inherit;
  padding: 1px;
  background: linear-gradient(
    135deg,
    hsl(var(--primary)),
    hsl(var(--primary) / 0.3),
    hsl(var(--primary))
  );
  mask: linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0);
  mask-composite: exclude;
}
```

### 3.4 Background Effects

```css
/* Subtle grid pattern for hero sections */
.grid-pattern {
  background-image:
    linear-gradient(hsl(var(--primary) / 0.03) 1px, transparent 1px),
    linear-gradient(90deg, hsl(var(--primary) / 0.03) 1px, transparent 1px);
  background-size: 60px 60px;
}

/* Radial gold glow for hero backgrounds */
.hero-glow {
  background: radial-gradient(
    ellipse at center,
    hsl(var(--primary) / 0.08) 0%,
    transparent 70%
  );
}

/* Section background helpers */
.section-dark  { @apply bg-background text-foreground; }
.section-light { @apply bg-muted/30 text-foreground; }
.section-muted { @apply text-muted-foreground; }
```

### 3.5 Custom Animations (in `index.css`)

```css
@keyframes float {
  0%, 100% { transform: translateY(0px) rotate(0deg); }
  50%      { transform: translateY(-20px) rotate(3deg); }
}
@keyframes pulse-gold {
  0%, 100% { opacity: 0.4; }
  50%      { opacity: 0.8; }
}
@keyframes shimmer {
  0%   { background-position: -200% 0; }
  100% { background-position: 200% 0; }
}

.animate-float      { animation: float 6s ease-in-out infinite; }
.animate-pulse-gold { animation: pulse-gold 3s ease-in-out infinite; }
.animate-shimmer    { animation: shimmer 2s linear infinite; }
```

### 3.6 Custom Button Variants

The shadcn `Button` component is extended with gold-themed variants:

```typescript
// In src/components/ui/button.tsx — buttonVariants
{
  variant: {
    gold: "bg-primary text-primary-foreground hover:bg-primary/90 shadow-md hover:shadow-gold-sm",
    goldOutline: "border-2 border-primary text-primary hover:bg-primary/10",
    // ... standard shadcn variants (default, destructive, outline, secondary, ghost, link)
  },
  size: {
    xl: "h-14 px-8 text-lg rounded-xl",
    // ... standard sizes (default, sm, lg, icon)
  },
}
```


---

## Part 4: Authentication & Authorization

> **Files**: `src/Client/src/auth/` — `authConfig.ts`, `AuthProvider.tsx`, `ProtectedRoute.tsx`, `useAuth.ts`

### 4.1 MSAL Configuration

The app authenticates via **Microsoft Entra External ID** (CIAM) using MSAL.js:

```typescript
// src/auth/authConfig.ts
export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_AZURE_CLIENT_ID ?? "default-client-id",
    authority: https://rajfinancialdev.ciamlogin.com/,
    knownAuthorities: [
      "rajfinancialdev.ciamlogin.com",    // Dev tenant
      "rajfinancial.ciamlogin.com",        // Prod tenant
    ],
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: "sessionStorage",  // Not localStorage — clears on tab close
    storeAuthStateInCookie: false,
  },
};

export const loginRequest = {
  scopes: ["openid", "profile", "email", "offline_access"],
};
```

**Environment variables** (set in `.env` or SWA config):
- `VITE_AZURE_CLIENT_ID` — Entra app registration client ID
- `VITE_AZURE_AUTHORITY` — (optional) override authority URL

### 4.2 Auth Provider

```typescript
// src/auth/AuthProvider.tsx
const msalInstance = new PublicClientApplication(msalConfig);

// Auto-set active account from cache on startup
const accounts = msalInstance.getAllAccounts();
if (accounts.length > 0) {
  msalInstance.setActiveAccount(accounts[0]);
}

// Listen for login success to set active account
msalInstance.addEventCallback((event) => {
  if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
    const { account } = event.payload as AuthenticationResult;
    msalInstance.setActiveAccount(account);
  }
});

export function AuthProvider({ children }: { children: React.ReactNode }) {
  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}
```

### 4.3 useAuth Hook

Provides a unified auth API consumed by all components:

```typescript
// src/auth/useAuth.ts
interface AuthUser {
  name: string;
  email: string;
  initials: string;    // Computed from name (e.g. "RP" for "Rajesh Patel")
  roles: string[];     // Parsed from idTokenClaims
}

interface UseAuthResult {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: AuthUser | null;
  login: () => Promise<void>;      // Triggers MSAL redirect
  logout: () => Promise<void>;     // MSAL logout redirect
  hasRole: (role: string) => boolean;
  isAdmin: boolean;                // Administrator | AdminAdvisor | AdminClient
  isClient: boolean;               // Explicit Client role OR no roles (implicit)
}
```

**Role parsing** — roles are extracted from `account.idTokenClaims.roles` (string array) or `account.idTokenClaims.extension_Role` (Entra custom attribute):

```typescript
function parseRoles(account: AccountInfo): string[] {
  const claims = account.idTokenClaims as Record<string, unknown>;
  if (Array.isArray(claims?.roles)) return claims.roles;
  if (typeof claims?.extension_Role === "string") return [claims.extension_Role];
  return [];
}
```

**Roles defined in the system**:

| Role | Description | Access Level |
|------|-------------|--------------|
| `Administrator` | Full system access | Admin dashboard, all tenants |
| `AdminAdvisor` | Advisor with admin privileges | Admin dashboard + advisor features |
| `AdminClient` | Client with admin privileges | Admin dashboard + client features |
| `Advisor` | Financial advisor | Advisor dashboard, client management |
| `Client` | Standard client | Client dashboard, own assets |
| `Viewer` | Read-only access | View-only dashboard |
| *(no roles)* | Implicit client | Same as Client (fallback) |

### 4.4 Protected Routes

```typescript
// src/auth/ProtectedRoute.tsx
type AuthPolicy = "RequireAuthenticated" | "RequireClient" | "RequireAdministrator";

function satisfiesPolicy(policy: AuthPolicy, user: AuthUser | null, isAuthenticated: boolean): boolean {
  if (!isAuthenticated || !user) return false;

  switch (policy) {
    case "RequireAuthenticated":
      return true;
    case "RequireClient":
      // Client role OR no roles at all (implicit client)
      return user.roles.includes("Client")
          || user.roles.includes("Administrator")
          || user.roles.includes("AdminClient")
          || user.roles.length === 0;
    case "RequireAdministrator":
      return user.roles.includes("Administrator")
          || user.roles.includes("AdminAdvisor")
          || user.roles.includes("AdminClient");
    default:
      return false;
  }
}
```

**Behavior on access failure**:
- **Not authenticated** → `RedirectToLogin` component triggers `instance.loginRedirect(loginRequest)`
- **Authenticated but wrong role** → `AccessDenied` component with Shield icon, "Go Back" and "Sign Out" buttons

### 4.5 Route Guard Usage

```tsx
// In App.tsx
<Route path="/dashboard" element={
  <ProtectedRoute policy="RequireClient">
    <Dashboard />
  </ProtectedRoute>
} />
<Route path="/profile" element={
  <ProtectedRoute policy="RequireAuthenticated">
    <Profile />
  </ProtectedRoute>
} />
```

---

## Part 5: App Configuration & Providers

> **File**: `src/Client/src/App.tsx` (82 lines)

### 5.1 Provider Hierarchy

The app wraps all components in a carefully ordered provider tree:

```
HelmetProvider              ← SEO / document head
└── AuthProvider            ← MSAL authentication
    └── ThemeProvider        ← Dark/light mode context
        └── QueryClientProvider ← TanStack React Query cache
            └── TooltipProvider  ← Radix tooltip context
                └── BrowserRouter ← React Router
                    └── Routes    ← Page components
```

```tsx
// src/App.tsx
function App() {
  return (
    <HelmetProvider>
      <AuthProvider>
        <ThemeProvider defaultTheme="dark" storageKey="raj-theme">
          <QueryClientProvider client={queryClient}>
            <TooltipProvider>
              <Toaster />
              <BrowserRouter>
                <Routes>
                  <Route path="/" element={<Index />} />
                  <Route path="/dashboard" element={
                    <ProtectedRoute policy="RequireClient"><Dashboard /></ProtectedRoute>
                  } />
                  <Route path="/assets" element={
                    <ProtectedRoute policy="RequireClient"><Assets /></ProtectedRoute>
                  } />
                  <Route path="/profile" element={
                    <ProtectedRoute policy="RequireAuthenticated"><Profile /></ProtectedRoute>
                  } />
                  <Route path="*" element={<NotFound />} />
                </Routes>
              </BrowserRouter>
            </TooltipProvider>
          </QueryClientProvider>
        </ThemeProvider>
      </AuthProvider>
    </HelmetProvider>
  );
}
```

### 5.2 Route Map

| Path | Page Component | Auth Policy | Layout |
|------|---------------|-------------|--------|
| `/` | `Index` | Public (auto-redirects if authenticated) | Landing (Navbar + sections) |
| `/dashboard` | `Dashboard` | `RequireClient` | `DashboardLayout` (sidebar) |
| `/assets` | `Assets` | `RequireClient` | `DashboardLayout` (sidebar) |
| `/profile` | `Profile` | `RequireAuthenticated` | `DashboardLayout` (sidebar) |
| `*` | `NotFound` | Public | Minimal (centered 404) |

> **Note**: `/admin/dashboard` and `/advisor/clients` routes are referenced in the `Index` page's redirect logic but are not yet implemented as routes. These are placeholders for future admin/advisor dashboards.

### 5.3 Vite Configuration

```typescript
// vite.config.ts
export default defineConfig({
  plugins: [
    react(),       // @vitejs/plugin-react-swc
    basicSsl(),    // HTTPS for local dev (required by MSAL)
  ],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 8080,
    https: true,
  },
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./src/setupTests.ts"],  // If present
  },
});
```


---

## Part 6: Dashboard Page

> **File**: `src/Client/src/pages/Dashboard.tsx`

### 6.1 Overview

The dashboard is a **static mock** providing a preview of the authenticated client experience. It renders inside `DashboardLayout` (see Part 13) and includes four sections:

1. **Net Worth Hero Card** — large gold-accented card showing total net worth
2. **Summary Cards Row** — 4 cards (Total Assets, Cash & Bank, Investments, Monthly Income)
3. **Accounts List** — top 5 accounts with icons, types, and balances
4. **Recent Activity** — last 5 transactions

### 6.2 Net Worth Hero

```tsx
<Card className="bg-gradient-to-br from-gold/10 via-background to-gold/5 border-gold/20">
  <CardHeader>
    <CardTitle className="text-lg text-muted-foreground">Total Net Worth</CardTitle>
    <div className="text-4xl font-bold text-gold">,250.00</div>
    <p className="text-sm text-emerald-500 flex items-center gap-1">
      <TrendingUp /> +2.4% from last month
    </p>
  </CardHeader>
</Card>
```

### 6.3 Summary Cards

| Card | Icon | Value | Change |
|------|------|-------|--------|
| Total Assets | `DollarSign` | `,250.00` | +2.4% |
| Cash & Bank | `Landmark` | `,420.00` | +1.2% |
| Investments | `TrendingUp` | `,830.00` | +3.1% |
| Monthly Income | `ArrowUpRight` | `,450.00` | +0.8% |

Each card uses the shadcn `<Card>` component with muted foreground labels and an icon in a rounded gold-tinted background circle.

### 6.4 Mock Data Structure

All data is **hardcoded** in the component (not from the API service layer):

```typescript
const accounts = [
  { name: "Chase Checking", type: "Checking", balance: 12500.0, icon: Landmark },
  { name: "Vanguard 401k", type: "Retirement", balance: 234830.0, icon: TrendingUp },
  { name: "Savings Account", type: "Savings", balance: 45420.0, icon: PiggyBank },
  { name: "Coinbase", type: "Crypto", balance: 28500.0, icon: Bitcoin },
  { name: "Primary Residence", type: "Real Estate", balance: 165000.0, icon: Home },
];
```

> **TODO**: Replace mock data with `useAssets` + `computeAssetsSummary` from Part 12.

---

## Part 7: Assets Page

> **File**: `src/Client/src/pages/Assets.tsx` (305 lines)

### 7.1 Layout

The Assets page is the primary CRUD interface for managing financial assets. It renders inside `DashboardLayout` and consists of:

1. **Header Row** — title, asset count badge, "Add Asset" gold button
2. **Summary Cards** — 4 metrics (Total Assets, Accounts, Asset Types, Growth)
3. **Filter Tabs** — 8 tabs for filtering by asset category
4. **Data Table / Mobile Cards** — responsive asset listing
5. **Loading State** — skeleton shimmer cards
6. **Empty State** — illustration with "Add Your First Asset" CTA

### 7.2 Filter Tabs

```typescript
const FILTER_TABS = [
  { value: "all", label: "All Assets" },
  { value: "bank", label: "Bank Accounts" },
  { value: "investment", label: "Investments" },
  { value: "retirement", label: "Retirement" },
  { value: "realestate", label: "Real Estate" },
  { value: "vehicle", label: "Vehicles" },
  { value: "crypto", label: "Cryptocurrency" },
  { value: "insurance", label: "Insurance" },
  { value: "other", label: "Other Assets" },
];
```

Tabs use shadcn `<Tabs>` + `<TabsList>` + `<TabsTrigger>` with horizontal scroll on mobile:

```tsx
<TabsList className="flex overflow-x-auto">
  {FILTER_TABS.map(tab => (
    <TabsTrigger key={tab.value} value={tab.value}>{tab.label}</TabsTrigger>
  ))}
</TabsList>
```

### 7.3 Summary Cards

| Card | Icon | Data Source |
|------|------|------------|
| Total Value | `DollarSign` | `computeAssetsSummary(assets).totalValue` |
| Total Accounts | `Landmark` | `assets.length` |
| Asset Types | `TrendingUp` | Distinct `assetType` count |
| Growth | `ArrowUpRight` | `computeAssetsSummary(assets).totalGrowth` |

### 7.4 Asset Icon Mapping

A helper maps `AssetType` → Lucide icon for display in table rows and cards:

```tsx
function AssetIcon({ type }: { type: string }) {
  const icons: Record<string, LucideIcon> = {
    BankAccount: Landmark,
    Investment: TrendingUp,
    RetirementAccount: PiggyBank,
    RealEstate: Home,
    Vehicle: Car,
    Cryptocurrency: Bitcoin,
    Insurance: Shield,
    PersonalProperty: Package,
    BusinessInterest: Briefcase,
    DigitalAsset: Laptop,
    Collectible: Gem,
    OtherAsset: Layers,
  };
  const Icon = icons[type] ?? Layers;
  return <Icon className="h-4 w-4 text-muted-foreground" />;
}
```

### 7.5 Actions Menu

Each asset row has a `<DropdownMenu>` with contextual actions:

```tsx
<DropdownMenu>
  <DropdownMenuTrigger asChild>
    <Button variant="ghost" size="icon">
      <MoreHorizontal className="h-4 w-4" />
    </Button>
  </DropdownMenuTrigger>
  <DropdownMenuContent align="end">
    <DropdownMenuItem><Edit className="mr-2 h-4 w-4" /> Edit</DropdownMenuItem>
    <DropdownMenuItem><RefreshCw className="mr-2 h-4 w-4" /> Update Value</DropdownMenuItem>
    <DropdownMenuItem><Archive className="mr-2 h-4 w-4" /> Mark as Disposed</DropdownMenuItem>
    <DropdownMenuSeparator />
    <DropdownMenuItem><Users className="mr-2 h-4 w-4" /> Manage Beneficiaries</DropdownMenuItem>
    <DropdownMenuSeparator />
    <DropdownMenuItem className="text-destructive">
      <Trash2 className="mr-2 h-4 w-4" /> Delete
    </DropdownMenuItem>
  </DropdownMenuContent>
</DropdownMenu>
```

### 7.6 Responsive Data Display

**Desktop** (`md:` and up) — Full `<Table>` with columns: Name, Type, Value, Beneficiaries, Last Updated, Actions.

**Mobile** (below `md:`) — Stacked `<Card>` layout:

```tsx
{/* Desktop table */}
<div className="hidden md:block">
  <Table>
    <TableHeader>
      <TableRow>
        <TableHead>Name</TableHead>
        <TableHead>Type</TableHead>
        <TableHead className="text-right">Value</TableHead>
        <TableHead>Beneficiaries</TableHead>
        <TableHead>Last Updated</TableHead>
        <TableHead className="text-right">Actions</TableHead>
      </TableRow>
    </TableHeader>
    <TableBody>
      {filteredAssets.map(asset => <DesktopRow key={asset.id} asset={asset} />)}
    </TableBody>
  </Table>
</div>

{/* Mobile cards */}
<div className="md:hidden space-y-3">
  {filteredAssets.map(asset => <MobileCard key={asset.id} asset={asset} />)}
</div>
```

### 7.7 Beneficiary Status Badge

Displays beneficiary assignment status per asset:

```tsx
function BeneficiaryStatus({ count }: { count: number }) {
  if (count === 0) {
    return <Badge variant="outline" className="text-amber-500 border-amber-500/30">
      <AlertCircle className="mr-1 h-3 w-3" /> None
    </Badge>;
  }
  return <Badge variant="outline" className="text-emerald-500 border-emerald-500/30">
    <CheckCircle className="mr-1 h-3 w-3" /> {count} assigned
  </Badge>;
}
```

### 7.8 Loading & Empty States

**Loading**: 3 skeleton `<Card>` components with pulsing animation (`animate-pulse`).

**Empty**: Centered Layers icon, heading "No assets yet", subheading "Add your first asset to start tracking your portfolio", and gold `<Button>` CTA.


---

## Part 8: Profile Page

> **File**: `src/Client/src/pages/Profile.tsx` (159 lines)

### 8.1 Overview

The Profile page renders inside `DashboardLayout` and provides a user settings interface with three sections:

1. **Avatar Section** — profile photo with camera upload button
2. **Personal Information Form** — editable user details
3. **Security Section** — password, 2FA, and Entra ID connection status

### 8.2 Avatar Section

```tsx
<div className="flex items-center gap-6">
  <div className="relative">
    <Avatar className="h-24 w-24">
      <AvatarFallback className="bg-gold/10 text-gold text-2xl">
        {user.initials}
      </AvatarFallback>
    </Avatar>
    <Button
      size="icon"
      variant="outline"
      className="absolute -bottom-2 -right-2 h-8 w-8 rounded-full"
      aria-label="Upload profile photo"
    >
      <Camera className="h-4 w-4" />
    </Button>
  </div>
  <div>
    <h2 className="text-xl font-semibold">{user.name}</h2>
    <p className="text-muted-foreground">{user.email}</p>
  </div>
</div>
```

### 8.3 Personal Information Form

Uses shadcn `<Input>` + `<Label>` in a responsive grid:

```tsx
<div className="grid grid-cols-1 md:grid-cols-2 gap-4">
  <div className="space-y-2">
    <Label htmlFor="firstName">First Name</Label>
    <Input id="firstName" defaultValue="Rajesh" />
  </div>
  <div className="space-y-2">
    <Label htmlFor="lastName">Last Name</Label>
    <Input id="lastName" defaultValue="Patel" />
  </div>
  <div className="space-y-2">
    <Label htmlFor="email">Email</Label>
    <Input id="email" type="email" defaultValue="rajesh@rajfinancial.com" />
  </div>
  <div className="space-y-2">
    <Label htmlFor="phone">Phone</Label>
    <Input id="phone" type="tel" defaultValue="+1 (555) 123-4567" />
  </div>
  <div className="space-y-2 md:col-span-2">
    <Label htmlFor="address">Address</Label>
    <Input id="address" defaultValue="123 Financial District, New York, NY" />
  </div>
  <div className="space-y-2 md:col-span-2">
    <Label htmlFor="company">Company</Label>
    <Input id="company" defaultValue="Raj Financial Services" />
  </div>
</div>
```

> **TODO**: Wire up to API endpoint for user profile CRUD. Replace mock `defaultValue` with `useAuth().user` data + form state management (React Hook Form + Zod).

### 8.4 Security Section

Three security items displayed as bordered rows:

| Feature | Current State | Action |
|---------|--------------|--------|
| Password | `****` (masked) | Change Password button |
| Two-Factor Authentication | Not enabled | Enable 2FA button |
| Entra ID | Connected | Badge (`text-emerald-500`) |

### 8.5 Save Action

```tsx
<Button variant="gold" className="w-full md:w-auto">
  Save Changes
</Button>
```

---

## Part 9: Landing Page (Index)

> **File**: `src/Client/src/pages/Index.tsx` (87 lines)

### 9.1 Role-Based Redirect

When an **authenticated** user navigates to `/`, the page auto-redirects based on their highest role:

```typescript
useEffect(() => {
  if (isAuthenticated && user) {
    if (isAdmin) {
      navigate("/admin/dashboard", { replace: true });
    } else if (user.roles.includes("Advisor")) {
      navigate("/advisor/clients", { replace: true });
    } else {
      navigate("/dashboard", { replace: true });
    }
  }
}, [isAuthenticated, user, isAdmin, navigate]);
```

**Redirect priority**: Administrator → Advisor → Client (default)

A loading spinner is shown while the redirect is in progress.

### 9.2 Landing Page Sections

For **unauthenticated** visitors, the landing page renders a full marketing site:

```tsx
<>
  <Helmet>
    <title>Raj Financial — Premium Financial Planning</title>
    <meta name="description" content="Take control of your financial future..." />
  </Helmet>
  <Navbar />
  <HeroSection />
  <FeaturesSection />
  <HowItWorksSection />
  <SecuritySection />
  <CTASection />
  <Footer />
</>
```

### 9.3 HeroSection

> **File**: `src/Client/src/components/HeroSection.tsx`

Full-viewport hero with layered background effects:

| Layer | CSS Class | Description |
|-------|-----------|-------------|
| Background | `bg-background` | Theme-aware base |
| Grid Pattern | `grid-pattern` | Subtle grid overlay (see Part 3.4) |
| Glow | `hero-glow` | Radial gold gradient (see Part 3.4) |
| Content | `relative z-10` | Above all effects |

**Content hierarchy**:
1. **Badge**: "Premium Financial Planning" pill with gold star icon
2. **Heading**: "Take Control of Your Financial Future" (`text-4xl md:text-5xl lg:text-6xl`)
3. **Subheading**: Feature description paragraph
4. **CTAs**: Two buttons — "Start Building Wealth" (`variant="gold"`) + "See How It Works" (`variant="goldOutline"`)
5. **Trust Indicators**: Three items — Bank-Level Security (Shield), Free to Start (CheckCircle), Setup in Minutes (Clock)

**Floating Logo Animation**:

```tsx
<div className="animate-float">
  <Logo className="h-16 w-16 text-gold" />
</div>
```

Uses `animate-float` keyframes from `tailwind.config.ts` (see Part 3.5).

**Staggered Fade-In**: Each content section uses `animate-fade-in` with increasing `animationDelay`:

```tsx
<div className="animate-fade-in" style={{ animationDelay: "0.2s" }}>...</div>
<div className="animate-fade-in" style={{ animationDelay: "0.4s" }}>...</div>
<div className="animate-fade-in" style={{ animationDelay: "0.6s" }}>...</div>
```

### 9.4 FeaturesSection

> **File**: `src/Client/src/components/FeaturesSection.tsx` (121 lines)

6 features displayed in a responsive grid (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3`):

| Feature | Icon | Description |
|---------|------|-------------|
| Net Worth Tracking | `DollarSign` | Real-time portfolio valuation |
| Bank Account Linking | `Landmark` | Connect checking, savings, credit cards |
| Smart Analytics | `TrendingUp` | AI-powered insights and projections |
| Debt Payoff Planner | `Calculator` | Snowball/avalanche debt strategies |
| Insurance Calculator | `Shield` | Coverage analysis and gap identification |
| Beneficiary Management | `Users` | Estate planning and designations |

Each feature uses the `<FeatureCard>` component (`src/components/FeatureCard.tsx`):
- Glass morphism card with hover elevation
- Icon in gold-tinted circle
- Title, description, and bullet-point feature list
- Staggered `animate-fade-in` (delay = `index * 0.1s`)

### 9.5 Additional Sections

| Component | File | Description |
|-----------|------|-------------|
| `HowItWorksSection` | `HowItWorksSection.tsx` | 3-step onboarding flow (Sign Up → Link Accounts → Track & Grow) |
| `SecuritySection` | `SecuritySection.tsx` | Security features highlight (encryption, 2FA, compliance) |
| `CTASection` | `CTASection.tsx` | Final call-to-action with sign-up button |
| `Footer` | `Footer.tsx` | Logo + copyright text |


---

## Part 10: Shared UI Components

### 10.1 shadcn/ui Component Library

The project uses **shadcn/ui** — a collection of accessible, customizable components built on **Radix UI** primitives. Components live in `src/Client/src/components/ui/` and are **copied into the project** (not imported from `node_modules`), allowing full customization.

**Installed components** (49 total):

| Component | Radix Primitive | Description |
|-----------|----------------|-------------|
| `accordion.tsx` | `@radix-ui/react-accordion` | Collapsible content sections |
| `alert-dialog.tsx` | `@radix-ui/react-alert-dialog` | Confirmation dialogs |
| `alert.tsx` | — | Status alert banners |
| `aspect-ratio.tsx` | `@radix-ui/react-aspect-ratio` | Fixed aspect ratio containers |
| `avatar.tsx` | `@radix-ui/react-avatar` | User avatars with fallback |
| `badge.tsx` | — | Status labels and tags |
| `breadcrumb.tsx` | — | Navigation breadcrumbs |
| `button.tsx` | `@radix-ui/react-slot` | Buttons (+ gold variants) |
| `calendar.tsx` | `react-day-picker` | Date picker calendar |
| `card.tsx` | — | Content cards |
| `carousel.tsx` | `embla-carousel-react` | Image/content carousel |
| `chart.tsx` | `recharts` | Chart wrapper with theming |
| `checkbox.tsx` | `@radix-ui/react-checkbox` | Checkboxes |
| `collapsible.tsx` | `@radix-ui/react-collapsible` | Expandable sections |
| `command.tsx` | `cmdk` | Command palette |
| `context-menu.tsx` | `@radix-ui/react-context-menu` | Right-click menus |
| `dialog.tsx` | `@radix-ui/react-dialog` | Modal dialogs |
| `drawer.tsx` | `vaul` | Bottom sheet drawer |
| `dropdown-menu.tsx` | `@radix-ui/react-dropdown-menu` | Dropdown menus |
| `form.tsx` | `react-hook-form` | Form field wrappers |
| `hover-card.tsx` | `@radix-ui/react-hover-card` | Hover-triggered cards |
| `input.tsx` | — | Text inputs |
| `input-otp.tsx` | `input-otp` | OTP code input |
| `label.tsx` | `@radix-ui/react-label` | Form labels |
| `menubar.tsx` | `@radix-ui/react-menubar` | Application menu bar |
| `navigation-menu.tsx` | `@radix-ui/react-navigation-menu` | Multi-level navigation |
| `pagination.tsx` | — | Page navigation |
| `popover.tsx` | `@radix-ui/react-popover` | Popovers |
| `progress.tsx` | `@radix-ui/react-progress` | Progress bars |
| `radio-group.tsx` | `@radix-ui/react-radio-group` | Radio buttons |
| `resizable.tsx` | `react-resizable-panels` | Resizable panel layouts |
| `scroll-area.tsx` | `@radix-ui/react-scroll-area` | Custom scrollbars |
| `select.tsx` | `@radix-ui/react-select` | Select dropdowns |
| `separator.tsx` | `@radix-ui/react-separator` | Visual dividers |
| `sheet.tsx` | `@radix-ui/react-dialog` | Side panel overlays |
| `sidebar.tsx` | — | Application sidebar |
| `skeleton.tsx` | — | Loading placeholders |
| `slider.tsx` | `@radix-ui/react-slider` | Range sliders |
| `sonner.tsx` | `sonner` | Toast notifications |
| `switch.tsx` | `@radix-ui/react-switch` | Toggle switches |
| `table.tsx` | — | Data tables |
| `tabs.tsx` | `@radix-ui/react-tabs` | Tab navigation |
| `textarea.tsx` | — | Multi-line text inputs |
| `toast.tsx` | `@radix-ui/react-toast` | Toast notifications (Radix) |
| `toaster.tsx` | — | Toast container |
| `toggle.tsx` | `@radix-ui/react-toggle` | Toggle buttons |
| `toggle-group.tsx` | `@radix-ui/react-toggle-group` | Grouped toggles |
| `tooltip.tsx` | `@radix-ui/react-tooltip` | Hover tooltips |

### 10.2 GlassCard Component

> **File**: `src/Client/src/components/GlassCard.tsx`

A reusable card with glass morphism styling and optional interactive effects:

```tsx
interface GlassCardProps extends React.HTMLAttributes<HTMLDivElement> {
  hover?: boolean;   // Enable hover lift effect
  glow?: boolean;    // Enable gold glow on hover
  children: React.ReactNode;
}

export function GlassCard({ hover, glow, className, children, ...props }: GlassCardProps) {
  return (
    <div
      className={cn(
        "glass-card rounded-xl p-6",
        hover && "transition-transform hover:-translate-y-1",
        glow && "gold-glow",
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
}
```

Uses `glass-card` and `gold-glow` CSS utilities defined in Part 3.3.

### 10.3 ThemeToggle Component

> **File**: `src/Client/src/components/ThemeToggle.tsx`

Toggles between light and dark mode using the `useTheme` hook:

```tsx
export function ThemeToggle() {
  const { theme, setTheme } = useTheme();
  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
      aria-label={Switch to  mode}
    >
      <Sun className="h-5 w-5 rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0" />
      <Moon className="absolute h-5 w-5 rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100" />
    </Button>
  );
}
```

Uses CSS transitions for smooth sun→moon icon animation.

### 10.4 Logo Component

> **File**: `src/Client/src/components/Logo.tsx`

SVG monogram "RF" rendered inline with dynamic sizing. Accepts standard SVG props (`className`, etc.).

### 10.5 StatCard Component

> **File**: `src/Client/src/components/StatCard.tsx`

Reusable metric display card used in landing page stats sections. Accepts `value`, `label`, and optional `icon` props.

---

## Part 11: Custom Hooks

### 11.1 useTheme

> **File**: `src/Client/src/hooks/use-theme.tsx`

Provides dark/light mode management via React context:

```typescript
type Theme = "dark" | "light" | "system";

interface ThemeProviderState {
  theme: Theme;
  setTheme: (theme: Theme) => void;
}

// ThemeProvider wraps the app (see Part 5.1)
export function ThemeProvider({
  children,
  defaultTheme = "dark",
  storageKey = "raj-theme",
}: ThemeProviderProps) {
  const [theme, setTheme] = useState<Theme>(
    () => (localStorage.getItem(storageKey) as Theme) || defaultTheme
  );

  useEffect(() => {
    const root = window.document.documentElement;
    root.classList.remove("light", "dark");

    if (theme === "system") {
      const systemTheme = window.matchMedia("(prefers-color-scheme: dark)").matches
        ? "dark" : "light";
      root.classList.add(systemTheme);
    } else {
      root.classList.add(theme);
    }
  }, [theme]);

  // ...context provider
}

export const useTheme = () => useContext(ThemeProviderContext);
```

**Key behaviors**:
- Persists choice to `localStorage` under key `raj-theme`
- Applies `dark` or `light` class to `<html>` element
- Supports `system` preference detection via `matchMedia`
- Default theme is `dark`

### 11.2 useIsMobile

> **File**: `src/Client/src/hooks/use-mobile.tsx`

Reactive mobile breakpoint detection:

```typescript
const MOBILE_BREAKPOINT = 768;

export function useIsMobile() {
  const [isMobile, setIsMobile] = useState<boolean | undefined>(undefined);

  useEffect(() => {
    const mql = window.matchMedia((max-width: px));
    const onChange = () => setIsMobile(window.innerWidth < MOBILE_BREAKPOINT);
    mql.addEventListener("change", onChange);
    setIsMobile(window.innerWidth < MOBILE_BREAKPOINT);
    return () => mql.removeEventListener("change", onChange);
  }, []);

  return !!isMobile;
}
```

**Breakpoint**: `768px` — aligns with Tailwind's `md:` breakpoint.

### 11.3 useToast

> **File**: `src/Client/src/hooks/use-toast.ts`

Toast notification state management hook, consumed by the `<Toaster />` component. Provides `toast()`, `dismiss()`, and `toasts` array.

This wraps the Radix-based toast system. In practice, the project also uses **Sonner** (`<Toaster />` from `sonner`) for simpler toast calls:

```typescript
import { toast } from "sonner";
toast.success("Asset created successfully");
toast.error("Failed to delete asset");
```


---

## Part 12: Data Layer & State Management

### 12.1 Asset Types (TypeScript)

> **File**: `src/Client/src/types/assets.ts` (127 lines)

```typescript
type AssetType =
  | "BankAccount"
  | "Investment"
  | "RetirementAccount"
  | "RealEstate"
  | "Vehicle"
  | "Cryptocurrency"
  | "Insurance"
  | "PersonalProperty"
  | "BusinessInterest"
  | "DigitalAsset"
  | "Collectible"
  | "OtherAsset";
```

**DTOs**:

```typescript
interface AssetDto {
  id: string;
  name: string;
  assetType: AssetType;
  currentValue: number;
  purchasePrice?: number;
  purchaseDate?: string;
  institution?: string;
  accountNumber?: string;        // Sensitive — masked by default
  notes?: string;
  beneficiaryCount: number;
  isDisposed: boolean;
  lastUpdated: string;
}

interface AssetDetailDto extends AssetDto {
  metadata: Record<string, unknown>;  // Type-specific fields
}

interface CreateAssetRequest {
  name: string;
  assetType: AssetType;
  currentValue: number;
  purchasePrice?: number;
  purchaseDate?: string;
  institution?: string;
  accountNumber?: string;
  notes?: string;
  metadata?: Record<string, unknown>;
}
```

**Helper maps**:

```typescript
const ASSET_TYPE_LABELS: Record<AssetType, string> = {
  BankAccount: "Bank Account",
  Investment: "Investment",
  RetirementAccount: "Retirement Account",
  RealEstate: "Real Estate",
  Vehicle: "Vehicle",
  Cryptocurrency: "Cryptocurrency",
  Insurance: "Insurance",
  PersonalProperty: "Personal Property",
  BusinessInterest: "Business Interest",
  DigitalAsset: "Digital Asset",
  Collectible: "Collectible",
  OtherAsset: "Other Asset",
};

const ASSET_TYPE_ICONS: Record<AssetType, LucideIcon> = {
  BankAccount: Landmark,
  Investment: TrendingUp,
  RetirementAccount: PiggyBank,
  RealEstate: Home,
  Vehicle: Car,
  Cryptocurrency: Bitcoin,
  Insurance: Shield,
  PersonalProperty: Package,
  BusinessInterest: Briefcase,
  DigitalAsset: Laptop,
  Collectible: Gem,
  OtherAsset: Layers,
};
```

**Utility function**:

```typescript
function formatCurrency(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(value);
}
```

### 12.2 Mock Asset Data

> **File**: `src/Client/src/data/mock-assets.ts` (102 lines)

Provides 6 sample assets for development:

| Name | Type | Value |
|------|------|-------|
| Chase Premier Checking | BankAccount | `,430.00` |
| Vanguard S&P 500 ETF | Investment | `,250.00` |
| Fidelity 401(k) | RetirementAccount | `,500.00` |
| Downtown Condo | RealEstate | `,000.00` |
| Tesla Model 3 | Vehicle | `,900.00` |
| Bitcoin Holdings | Cryptocurrency | `,800.00` |

**Summary computation**:

```typescript
function computeAssetsSummary(assets: AssetDto[]) {
  return {
    totalValue: assets.reduce((sum, a) => sum + a.currentValue, 0),
    totalGrowth: assets.reduce((sum, a) => {
      if (a.purchasePrice) return sum + (a.currentValue - a.purchasePrice);
      return sum;
    }, 0),
    assetCount: assets.length,
    typeCount: new Set(assets.map(a => a.assetType)).size,
  };
}
```

### 12.3 Asset Service (TanStack Query)

> **File**: `src/Client/src/services/asset-service.ts`

A **mock service layer** that simulates API calls with `Promise` + `setTimeout` delays. Built on **TanStack React Query v5** for caching, refetching, and mutation management.

**Query hooks**:

```typescript
// Fetch all assets
function useAssets() {
  return useQuery({
    queryKey: ["assets"],
    queryFn: async () => {
      await delay(800);
      return [...mockAssets];  // Returns mock data
    },
  });
}

// Fetch single asset
function useAsset(id: string) {
  return useQuery({
    queryKey: ["assets", id],
    queryFn: async () => {
      await delay(500);
      return mockAssets.find(a => a.id === id) ?? null;
    },
    enabled: !!id,
  });
}
```

**Mutation hooks**:

```typescript
// Create asset
function useCreateAsset() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateAssetRequest) => {
      await delay(1000);
      const newAsset: AssetDto = { id: crypto.randomUUID(), ...request, /* defaults */ };
      mockAssets.push(newAsset);
      return newAsset;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["assets"] }),
  });
}

// Update asset
function useUpdateAsset() { /* similar pattern */ }

// Delete asset
function useDeleteAsset() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await delay(500);
      const index = mockAssets.findIndex(a => a.id === id);
      if (index !== -1) mockAssets.splice(index, 1);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["assets"] }),
  });
}
```

**Query key convention**: `["assets"]` for list, `["assets", id]` for single item. Mutations invalidate the list key to trigger automatic refetch.

> **TODO**: Replace mock implementations with real `fetch()` calls to the .NET API (see API Tracking doc).

---

## Part 13: Navigation & Layout

### 13.1 DashboardLayout

> **File**: `src/Client/src/components/dashboard/DashboardLayout.tsx` (226 lines)

The authenticated app shell with a **collapsible sidebar** and **top bar**, used by Dashboard, Assets, and Profile pages.

#### Sidebar Navigation Items

```typescript
const navItems = [
  { label: "Dashboard",    icon: LayoutDashboard, href: "/dashboard" },
  { label: "Assets",       icon: Briefcase,       href: "/assets" },
  { label: "Accounts",     icon: Landmark,        href: "/accounts" },
  { label: "Net Worth",    icon: TrendingUp,      href: "/net-worth" },
  { label: "Insurance",    icon: Shield,          href: "/insurance" },
  { label: "Documents",    icon: FileText,        href: "/documents" },
  { label: "Beneficiaries",icon: Users,           href: "/beneficiaries" },
];

const bottomNavItems = [
  { label: "Settings",     icon: Settings,        href: "/settings" },
];
```

#### Desktop Sidebar (`md:` and up)

```tsx
<aside className={cn(
  "hidden md:flex flex-col border-r bg-card transition-all duration-300",
  isCollapsed ? "w-16" : "w-64"
)}>
  {/* Logo + collapse toggle */}
  <div className="flex items-center justify-between p-4">
    {!isCollapsed && <Logo className="h-8" />}
    <Button variant="ghost" size="icon" onClick={toggleCollapse}>
      {isCollapsed ? <ChevronRight /> : <ChevronLeft />}
    </Button>
  </div>

  {/* Nav links */}
  <nav className="flex-1 space-y-1 px-2">
    {navItems.map(item => (
      <NavLink key={item.href} to={item.href} className={({ isActive }) =>
        cn("flex items-center gap-3 rounded-lg px-3 py-2 transition-colors",
           isActive ? "bg-gold/10 text-gold" : "text-muted-foreground hover:bg-accent")
      }>
        <item.icon className="h-5 w-5" />
        {!isCollapsed && <span>{item.label}</span>}
      </NavLink>
    ))}
  </nav>

  {/* Bottom nav (Settings) */}
  <div className="border-t px-2 py-2">
    {bottomNavItems.map(/* same pattern */)}
  </div>
</aside>
```

**Active state**: Current route gets `bg-gold/10 text-gold` highlight.

**Collapsed state**: Only icons visible, sidebar width shrinks from `w-64` to `w-16`.

#### Mobile Sidebar

On screens below `md:`, the sidebar becomes an overlay triggered by a hamburger button:

```tsx
{/* Mobile overlay */}
{isMobileOpen && (
  <>
    <div className="fixed inset-0 z-40 bg-black/50 md:hidden" onClick={closeMobile} />
    <aside className="fixed inset-y-0 left-0 z-50 w-64 bg-card md:hidden">
      {/* Same nav items */}
    </aside>
  </>
)}
```

#### Top Bar

```tsx
<header className="sticky top-0 z-30 flex items-center justify-between border-b bg-card/80 backdrop-blur px-4 h-14">
  {/* Mobile hamburger */}
  <Button variant="ghost" size="icon" className="md:hidden" onClick={toggleMobile}>
    <Menu className="h-5 w-5" />
  </Button>

  {/* Search (desktop only) */}
  <div className="hidden md:flex items-center gap-2 flex-1 max-w-md">
    <Search className="h-4 w-4 text-muted-foreground" />
    <Input placeholder="Search..." className="border-0 bg-transparent" />
  </div>

  {/* Actions */}
  <div className="flex items-center gap-2">
    <ThemeToggle />
    <Button variant="ghost" size="icon" className="relative" aria-label="Notifications">
      <Bell className="h-5 w-5" />
      <span className="absolute top-1 right-1 h-2 w-2 rounded-full bg-red-500" />
    </Button>
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" className="gap-2">
          <Avatar className="h-8 w-8">
            <AvatarFallback className="bg-gold/10 text-gold text-sm">RP</AvatarFallback>
          </Avatar>
          <span className="hidden md:inline text-sm">{user?.name}</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem><User className="mr-2 h-4 w-4" /> Profile</DropdownMenuItem>
        <DropdownMenuItem><Settings className="mr-2 h-4 w-4" /> Settings</DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={logout}>
          <LogOut className="mr-2 h-4 w-4" /> Sign Out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  </div>
</header>
```

### 13.2 Landing Navbar

> **File**: `src/Client/src/components/Navbar.tsx`

Fixed navigation bar for the unauthenticated landing page:

```tsx
<nav className="fixed top-0 left-0 right-0 z-50 bg-background/80 backdrop-blur border-b">
  <div className="container flex items-center justify-between h-16">
    <Logo className="h-8" />

    {/* Desktop links */}
    <div className="hidden md:flex items-center gap-6">
      <NavLink to="#features">Features</NavLink>
      <NavLink to="#how-it-works">How It Works</NavLink>
      <NavLink to="#security">Security</NavLink>
      <Button variant="ghost" onClick={login}>Sign In</Button>
      <Button variant="gold" onClick={login}>Get Started</Button>
    </div>

    {/* Mobile hamburger */}
    <Button variant="ghost" size="icon" className="md:hidden" onClick={toggleMenu}>
      <Menu className="h-5 w-5" />
    </Button>
  </div>

  {/* Mobile dropdown */}
  {isOpen && (
    <div className="md:hidden border-t bg-background p-4 space-y-2">
      {/* Same links stacked vertically */}
    </div>
  )}
</nav>
```


---

## Part 14: Sensitive Field Handling

> **Cross-reference**: `docs/ASSET_TYPE_SPECIFICATIONS.md` — Sensitive Field Handling section

### 14.1 Principle

Sensitive financial data (account numbers, routing numbers, VINs) is **masked by default** in all API responses and UI displays. Users must explicitly request to reveal values, and revealed values auto-hide after a timeout.

### 14.2 Sensitive Fields Registry

| Asset Type | Field | Mask Format |
|-----------|-------|-------------|
| BankAccount | `accountNumber` | `****1234` (last 4 digits) |
| BankAccount | `routingNumber` | `****5678` (last 4 digits) |
| Vehicle | `vin` | `*************7890` (last 4 chars) |
| Investment | `accountNumber` | `****1234` (last 4 digits) |
| RetirementAccount | `accountNumber` | `****1234` (last 4 digits) |
| Cryptocurrency | `walletAddress` | `0x1234...abcd` (first 6 + last 4) |

### 14.3 API Behavior

- **Default response**: All sensitive fields return masked values
- **Reveal endpoint**: `GET /api/assets/{id}/sensitive/{fieldName}` returns the unmasked value (requires re-authentication or step-up auth)
- **Storage**: All sensitive values encrypted at rest using AES-256

### 14.4 UI Pattern

**Toggle component** using Eye/EyeOff icons with auto-hide:

```tsx
function SensitiveField({ label, maskedValue, assetId, fieldName }: Props) {
  const [revealed, setRevealed] = useState(false);
  const [realValue, setRealValue] = useState<string | null>(null);

  const handleReveal = async () => {
    if (revealed) {
      setRevealed(false);
      return;
    }
    // Fetch real value from API
    const value = await revealSensitiveField(assetId, fieldName);
    setRealValue(value);
    setRevealed(true);

    // Auto-hide after 30 seconds
    setTimeout(() => setRevealed(false), 30_000);
  };

  return (
    <div className="flex items-center gap-2">
      <Label>{label}</Label>
      <span className="font-mono text-sm">
        {revealed ? realValue : maskedValue}
      </span>
      <Button variant="ghost" size="icon" onClick={handleReveal} aria-label={
        revealed ? Hide  : Reveal 
      }>
        {revealed ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
      </Button>
    </div>
  );
}
```

**Planned hook** (`useSensitiveField`):

```typescript
function useSensitiveField(assetId: string, fieldName: string) {
  const [isRevealed, setIsRevealed] = useState(false);
  const [value, setValue] = useState<string | null>(null);
  const timerRef = useRef<ReturnType<typeof setTimeout>>();

  const reveal = useCallback(async () => {
    const data = await fetchSensitiveField(assetId, fieldName);
    setValue(data);
    setIsRevealed(true);
    timerRef.current = setTimeout(() => {
      setIsRevealed(false);
      setValue(null);
    }, 30_000);
  }, [assetId, fieldName]);

  const hide = useCallback(() => {
    setIsRevealed(false);
    setValue(null);
    if (timerRef.current) clearTimeout(timerRef.current);
  }, []);

  useEffect(() => () => {
    if (timerRef.current) clearTimeout(timerRef.current);
  }, []);

  return { isRevealed, value, reveal, hide };
}
```

### 14.5 Backend Attribute

```csharp
/// <summary>
/// Marks a property as containing sensitive data that must be masked in API responses.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveDataAttribute : Attribute
{
    public int UnmaskedSuffixLength { get; }
    public char MaskCharacter { get; }

    public SensitiveDataAttribute(int unmaskedSuffixLength = 4, char maskCharacter = '*')
    {
        UnmaskedSuffixLength = unmaskedSuffixLength;
        MaskCharacter = maskCharacter;
    }
}

// Usage
public class BankAccountMetadata
{
    [SensitiveData(unmaskedSuffixLength: 4)]
    public string AccountNumber { get; set; } = string.Empty;

    [SensitiveData(unmaskedSuffixLength: 4)]
    public string RoutingNumber { get; set; } = string.Empty;
}
```

---

## Part 15: Future Pages & Planned Features

The following pages are referenced in DashboardLayout navigation or Index.tsx redirect logic but are **not yet implemented**:

| Route | Page | Status | Description |
|-------|------|--------|-------------|
| `/accounts` | Accounts | Planned | Linked financial account management |
| `/net-worth` | Net Worth | Planned | Net worth tracker with historical charts |
| `/insurance` | Insurance | Planned | Insurance policy calculator and coverage analysis |
| `/documents` | Documents | Planned | Document storage and management |
| `/beneficiaries` | Beneficiaries | Planned | Beneficiary designation management |
| `/settings` | Settings | Planned | App settings and preferences |
| `/admin/dashboard` | Admin Dashboard | Planned | Administrator overview panel |
| `/advisor/clients` | Advisor Clients | Planned | Advisor's client list management |

### 15.1 Planned Integrations

| Integration | Library/Service | Purpose |
|-------------|----------------|---------|
| Plaid | `react-plaid-link` | Bank account linking |
| Azure Application Insights | `@microsoft/applicationinsights-web` | Telemetry |
| React Hook Form + Zod | Already installed | Form validation (partially wired) |
| i18next | `react-i18next` | Localization (not yet configured) |

### 15.2 Accessibility Roadmap

- [ ] Add skip link to `DashboardLayout` (`<a href="#main-content" className="sr-only focus:not-sr-only">`)
- [ ] Ensure all interactive elements have visible focus indicators
- [ ] Add `aria-live` regions for toast and async loading states
- [ ] Test with screen reader (NVDA/VoiceOver)
- [ ] Run `axe-core` automated audit on all pages
- [ ] Ensure 4.5:1 color contrast for all text on gold backgrounds

---

## Summary Table

| Part | Topic | Key Files | Status |
|------|-------|-----------|--------|
| 1 | Solution Structure | `src/Client/` | Documented |
| 2 | Dependencies | `package.json` | Documented |
| 3 | CSS Design System | `index.css`, `tailwind.config.ts` | Documented |
| 4 | Authentication | `src/auth/*` | Documented |
| 5 | App Configuration | `App.tsx`, `vite.config.ts` | Documented |
| 6 | Dashboard | `pages/Dashboard.tsx` | Documented (mock data) |
| 7 | Assets | `pages/Assets.tsx` | Documented |
| 8 | Profile | `pages/Profile.tsx` | Documented (mock data) |
| 9 | Landing Page | `pages/Index.tsx`, `HeroSection.tsx` | Documented |
| 10 | Shared UI Components | `components/ui/*`, `GlassCard.tsx` | Documented |
| 11 | Custom Hooks | `hooks/*` | Documented |
| 12 | Data Layer | `types/assets.ts`, `services/*` | Documented |
| 13 | Navigation & Layout | `DashboardLayout.tsx`, `Navbar.tsx` | Documented |
| 14 | Sensitive Field Handling | Cross-ref: `ASSET_TYPE_SPECIFICATIONS.md` | Design spec |
| 15 | Future Pages | — | Planned |

---

*Last updated: 2026-02-26*
*Stack: React 18 · Vite 6 · TypeScript · Tailwind CSS 4 · shadcn/ui · MSAL · TanStack Query v5*

