# Legacy Builders Financial Platform - Execution Plan

## Executive Summary

**Product Name**: Legacy Financial (working title)
**Vision**: Comprehensive financial planning tools platform - the "Plaid for comprehensive financial planning data"
**Approach**: Tools-first (not advice) to minimize regulatory burden initially

### Phase Strategy
- **Phase 1 (MVP)**: Consumer web app with account aggregation, manual assets, beneficiary management, AI insights
- **Phase 2**: Professional API platform (FDX-compliant) for insurance agents, attorneys, advisors

### Core Differentiator
No existing platform combines: linked accounts + manual assets + beneficiary management + AI planning tools in one place.

---

## Architecture Overview

### Azure Services Stack

```
┌─────────────────────────────────────────────────────────────────┐
│                     Azure Static Web Apps                        │
│                   (React + TypeScript Frontend)                  │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Azure API Management                          │
│              (Gateway, Rate Limiting, Versioning)                │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Azure Functions (.NET 8)                     │
│                    (Isolated Worker Process)                     │
├─────────────────────────────────────────────────────────────────┤
│  AccountService │ AssetService │ BeneficiaryService │ AIService │
└─────────────────────────────────────────────────────────────────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        ▼                       ▼                       ▼
┌───────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Azure SQL   │     │  Azure Key Vault │     │ Azure Blob      │
│   Database    │     │  (Secrets)       │     │ Storage         │
└───────────────┘     └─────────────────┘     └─────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    External Integrations                         │
├─────────────────────────────────────────────────────────────────┤
│     Plaid (Account Aggregation)  │  Claude API (AI Insights)    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Part 1: Lovable Instructions (Frontend UI Generation)

### Initial Setup Prompt for Lovable

```
Create a modern financial planning web application called "Legacy Financial" with the following specifications:

TECH STACK:
- React 18+ with TypeScript (strict mode)
- Tailwind CSS with mobile-first approach
- Shadcn/ui component library
- React Query (TanStack Query) for server state
- React Hook Form + Zod for form validation
- Recharts for data visualization
- React Router v6 for navigation
- react-i18next for internationalization

DESIGN SYSTEM:
- Primary: Deep blue (#1E3A5F)
- Secondary: Slate (#475569)
- Accent: Teal (#0D9488)
- Success: Green (#22C55E)
- Warning: Amber (#F59E0B)
- Error: Red (#EF4444)
- Background: White (#FFFFFF) / Gray-50 (#F9FAFB)
- Font: Inter (body), system fonts fallback
- Border radius: 8px (cards), 6px (buttons), 4px (inputs)

CRITICAL REQUIREMENTS:
1. Mobile-first responsive design (base styles for mobile, then md: lg: xl: breakpoints)
2. All interactive elements minimum 44x44px touch targets
3. WCAG 2.1 AA accessibility (semantic HTML, ARIA labels, keyboard navigation)
4. All user-facing text must use i18n translation keys (t('key'))
5. Hide unauthorized features entirely (never show disabled states)
6. Professional, clean aesthetic similar to Plaid/Mercury/Wealthfront
```

---

### Screen-by-Screen Lovable Prompts

#### 1. Authentication Screens

```
SCREEN: Login Page
PATH: /login

Create a mobile-first login page with:

LAYOUT:
- Full-screen split layout: left side brand/hero (hidden on mobile), right side form
- Mobile: Form centered with logo above
- Desktop (md+): 50/50 split with gradient brand panel

COMPONENTS:
- Logo at top
- Email input with label (t('auth.email.label'))
- Password input with show/hide toggle (t('auth.password.label'))
- "Remember me" checkbox
- Primary button: (t('auth.login.submit'))
- Link: (t('auth.login.forgotPassword'))
- Link: (t('auth.login.signupPrompt')) → /register
- Divider with "or"
- Social login buttons (Google, Microsoft) - secondary style

ACCESSIBILITY:
- Form inputs have associated labels
- Error messages announced to screen readers (aria-live="polite")
- Focus trap within form
- Submit on Enter key

VALIDATION:
- Email: required, valid email format
- Password: required, min 8 characters
- Show inline errors below fields with error icon

ERROR HANDLING:
- Display API error codes as localized messages
- Error codes: AUTH_INVALID_CREDENTIALS, AUTH_ACCOUNT_LOCKED, AUTH_EMAIL_NOT_VERIFIED

LOADING STATE:
- Button shows spinner and (t('auth.login.loading'))
- Inputs disabled during submission
```

```
SCREEN: Registration Page
PATH: /register

Create a mobile-first registration page with:

LAYOUT:
- Same split layout as login
- Multi-step form wizard (3 steps) on mobile shows progress bar

STEP 1 - Account Info:
- First name input (t('auth.register.firstName'))
- Last name input (t('auth.register.lastName'))
- Email input (t('auth.register.email'))
- Password input with strength indicator
- Confirm password input
- Next button

STEP 2 - Profile Setup:
- Phone number (optional)
- Date of birth (for age-appropriate features)
- Country/Region dropdown
- Back/Next buttons

STEP 3 - Terms & Privacy:
- Checkbox: Accept Terms of Service (link opens modal)
- Checkbox: Accept Privacy Policy (link opens modal)
- Checkbox: Marketing emails (optional)
- Back/Create Account buttons

VALIDATION:
- Real-time password strength (weak/medium/strong)
- Password match validation
- Email uniqueness check (debounced API call)

ERROR CODES:
- AUTH_EMAIL_EXISTS, VALIDATION_FAILED, AUTH_WEAK_PASSWORD
```

---

#### 2. Main Dashboard

```
SCREEN: Dashboard
PATH: /dashboard
AUTH: Required

Create a mobile-first financial dashboard with:

LAYOUT - Mobile:
- Sticky header with hamburger menu, logo, notification bell
- Net worth card (full width, prominent)
- Horizontal scroll tabs: Overview | Assets | Liabilities | Goals
- Stacked cards below based on active tab
- Bottom navigation bar (Dashboard, Accounts, Assets, Beneficiaries, More)

LAYOUT - Desktop (lg+):
- Fixed left sidebar navigation (collapsible)
- Top header with search, notifications, profile dropdown
- Main content area with grid layout

NET WORTH CARD:
- Large number display with currency formatting (locale-aware)
- Trend indicator (up/down arrow with percentage)
- Sparkline chart (last 6 months)
- "As of [date]" timestamp
- t('dashboard.netWorth.title'), t('dashboard.netWorth.asOf')

QUICK STATS ROW (horizontal scroll on mobile):
- Total Assets card
- Total Liabilities card
- Monthly Cash Flow card
- Insurance Coverage card (shows gap if exists)

RECENT ACTIVITY SECTION:
- List of recent transactions (last 5)
- Each item: icon, description, amount, date
- "View All" link → /transactions
- t('dashboard.recentActivity.title')

AI INSIGHTS CARD:
- Icon + "AI Insights" header
- 2-3 insight cards with icons:
  - Insurance gap detected
  - Beneficiary review needed
  - Goal progress update
- Each insight has "Learn More" action
- t('dashboard.insights.title')

QUICK ACTIONS (floating action button on mobile):
- Add Account
- Add Asset
- Add Beneficiary
- Run Analysis

EMPTY STATES:
- If no accounts: Illustration + "Connect your first account" CTA
- If no assets: Illustration + "Add your first asset" CTA

ACCESSIBILITY:
- Skip to main content link
- Landmark regions (nav, main, aside)
- Chart has text alternative
- All cards keyboard navigable
```

---

#### 3. Accounts Management

```
SCREEN: Linked Accounts
PATH: /accounts
AUTH: Required

Create a mobile-first accounts management screen:

HEADER:
- Title: t('accounts.title')
- "Link Account" primary button (opens Plaid Link)

ACCOUNT LIST:
Mobile: Card-based list (full width cards)
Desktop: Table with sortable columns

EACH ACCOUNT CARD/ROW:
- Institution logo (40x40)
- Account name (e.g., "Chase Checking ****1234")
- Account type badge (Checking, Savings, Credit Card, Investment)
- Current balance (formatted for locale)
- Last synced timestamp
- Connection status indicator (green dot = healthy, yellow = needs attention, red = disconnected)
- Kebab menu: Refresh, View Transactions, Rename, Unlink

CONNECTION STATUS STATES:
- CONNECTED: Green indicator, "Last synced X minutes ago"
- NEEDS_REAUTH: Yellow indicator, "Reconnection required" + Reconnect button
- DISCONNECTED: Red indicator, "Connection lost" + Reconnect button
- SYNCING: Spinner, "Syncing..."

ACCOUNT TYPE SECTIONS:
Group accounts by type with collapsible headers:
- Depository (Checking, Savings)
- Credit (Credit Cards)
- Investment (Brokerage, Retirement)
- Loan (Mortgage, Auto, Student)

SUMMARY CARDS (top of page):
- Total in Depository accounts
- Total Credit Available / Used
- Total Investment Value
- Total Loan Balance

EMPTY STATE:
- Illustration of bank connection
- t('accounts.empty.title'): "No accounts linked yet"
- t('accounts.empty.description'): "Connect your bank accounts to see your complete financial picture"
- Primary CTA: "Link Your First Account"

PLAID LINK INTEGRATION:
- Button triggers Plaid Link modal
- On success: Show success toast, refresh account list
- On error: Show error with code (PLAID_LINK_ERROR, INSTITUTION_NOT_SUPPORTED)

ACCESSIBILITY:
- Account status announced to screen readers
- Table has proper headers and scope
- Focus management after Plaid modal closes
```

---

#### 4. Manual Assets Management

```
SCREEN: Assets
PATH: /assets
AUTH: Required

Create a mobile-first manual assets management screen:

HEADER:
- Title: t('assets.title')
- "Add Asset" primary button

ASSET CATEGORIES (Tab navigation):
- All Assets
- Real Estate
- Vehicles
- Business Interests
- Personal Property
- Other

ASSET LIST:
Mobile: Card grid (1 column)
Tablet: 2 columns
Desktop: 3-4 columns

EACH ASSET CARD:
- Asset image/icon (placeholder if none)
- Asset name (e.g., "123 Main St Property")
- Asset type badge
- Current value (formatted)
- Ownership percentage (if < 100%)
- Beneficiary indicator (icon if beneficiaries assigned)
- Last updated date
- Card is clickable → opens detail view

ASSET CARD ACTIONS (on hover/long press):
- Edit
- Update Value
- Manage Beneficiaries
- Delete (with confirmation)

SUMMARY SECTION (sticky on scroll):
- Total Asset Value (your share)
- Count by category
- Pie chart of asset allocation

ADD ASSET MODAL/DRAWER:
Responsive: Full-screen drawer on mobile, modal on desktop

Step 1 - Asset Type:
- Grid of asset type options with icons:
  - Real Estate (house icon)
  - Vehicle (car icon)
  - Business Interest (briefcase icon)
  - Personal Property (gem icon)
  - Collectibles (art icon)
  - Other (plus icon)

Step 2 - Asset Details (varies by type):

FOR REAL ESTATE:
- Property name/nickname
- Property type (Primary Residence, Investment, Vacation, Commercial)
- Address (with autocomplete)
- Purchase date
- Purchase price
- Current estimated value
- Ownership percentage (default 100%)
- Associated entity (LLC dropdown, optional)
- Mortgage linked? (toggle, links to liability)
- Notes

FOR VEHICLE:
- Year, Make, Model (cascading dropdowns)
- VIN (optional)
- Purchase date
- Purchase price
- Current value
- Ownership percentage
- Loan linked? (toggle)

FOR BUSINESS INTEREST:
- Business name
- Entity type (LLC, S-Corp, C-Corp, Partnership, Sole Prop)
- Your ownership percentage
- Estimated business value
- Your share value (calculated)
- Operating agreement on file? (toggle)
- Notes

Step 3 - Beneficiary Assignment (optional):
- "Add beneficiaries now?" toggle
- If yes: Beneficiary selection interface
- "Skip for now" option

VALIDATION:
- Required fields marked with asterisk
- Value must be positive number
- Ownership percentage 0-100
- Address validation for real estate

ERROR CODES:
- ASSET_CREATE_FAILED, VALIDATION_FAILED, ASSET_DUPLICATE_DETECTED
```

---

#### 5. Beneficiary Management

```
SCREEN: Beneficiaries
PATH: /beneficiaries
AUTH: Required

Create a mobile-first beneficiary management screen:

HEADER:
- Title: t('beneficiaries.title')
- "Add Beneficiary" primary button

TWO VIEWS (Toggle):
1. By Beneficiary - List of people, shows what they inherit
2. By Asset - List of assets, shows who inherits each

BY BENEFICIARY VIEW:
Each beneficiary card:
- Avatar (initials or photo)
- Name
- Relationship badge (Spouse, Child, Parent, Sibling, Trust, Charity, Other)
- Contact info (email/phone)
- Assets assigned count
- Expand to see: list of assets with percentage allocation

BY ASSET VIEW:
Each asset card:
- Asset name and type
- Current value
- Beneficiary allocation:
  - Primary beneficiaries list with percentages
  - Contingent beneficiaries list with percentages
  - Warning if percentages don't equal 100%
  - Warning if no beneficiaries assigned

ADD BENEFICIARY MODAL:
- Beneficiary type: Individual, Trust, Charity, Organization
- For Individual:
  - First name, Last name
  - Relationship to you
  - Date of birth (optional)
  - Email (optional)
  - Phone (optional)
  - Address (optional)
- For Trust:
  - Trust name
  - Trust type
  - Trustee name
  - Trust date
- For Charity:
  - Organization name
  - EIN (optional)
  - Contact info

ASSIGN BENEFICIARY TO ASSET MODAL:
- Select asset (searchable dropdown)
- Designation: Primary or Contingent
- Percentage allocation
- Per stirpes toggle (for individuals)
- Notes

BENEFICIARY COVERAGE SUMMARY:
Dashboard card showing:
- Assets with beneficiaries: X of Y
- Assets needing review (no beneficiaries)
- Last review date
- "Review All" CTA

WARNINGS/ALERTS:
- Asset has no beneficiaries (yellow warning)
- Percentages don't equal 100% (red error)
- Beneficiary info incomplete (yellow warning)

ERROR CODES:
- BENEFICIARY_CREATE_FAILED, ALLOCATION_INVALID, DUPLICATE_BENEFICIARY
```

---

#### 6. AI Insights & Analysis Tools

```
SCREEN: Planning Tools
PATH: /tools
AUTH: Required

Create a mobile-first financial planning tools hub:

HEADER:
- Title: t('tools.title')
- Subtitle: t('tools.subtitle'): "Analyze your finances and explore scenarios"

TOOL CATEGORIES (Tab navigation):
- All Tools
- Debt Analysis
- Insurance Analysis
- Estate Planning
- Goal Tracking

TOOL CARDS GRID:
Mobile: 1 column
Tablet: 2 columns
Desktop: 3 columns

DEBT PAYOFF ANALYZER:
Card preview:
- Icon (calculator)
- Title: t('tools.debtPayoff.title')
- Description: "Compare strategies to pay off debt faster"
- "Analyze" CTA

Full tool screen:
- List of debts (auto-populated from linked accounts + manual)
- Each debt: name, balance, interest rate, minimum payment
- Input: Extra monthly payment available
- Strategy selector:
  - Avalanche (highest interest first)
  - Snowball (lowest balance first)
  - Custom order
- Results comparison table:
  | Strategy | Payoff Time | Total Interest | Savings vs Minimum |
- Timeline chart showing balance over time
- "This analysis shows potential outcomes. Consult a financial professional for personalized advice."

INSURANCE COVERAGE ANALYZER:
Card preview:
- Icon (shield)
- Title: t('tools.insurance.title')
- Description: "Identify potential gaps in your coverage"

Full tool screen:
- Input section:
  - Annual income
  - Years of income to replace
  - Outstanding debts (auto-populated)
  - Dependents count
  - Existing coverage amounts (life, disability)
- Analysis output:
  - Calculated coverage need
  - Current coverage total
  - Gap (if any)
  - Coverage ratio visual (progress bar)
- Breakdown by category:
  - Income replacement need: $X
  - Debt payoff need: $X
  - Education funding need: $X (if dependents)
  - Final expenses: $X
- "These calculations are for educational purposes. Work with a licensed insurance agent for personalized recommendations."
- "Connect with an agent" CTA (Phase 2)

ESTATE PLANNING CHECKLIST:
Card preview:
- Icon (document)
- Title: t('tools.estatePlanning.title')
- Description: "Review your estate planning readiness"

Full tool screen:
- Checklist with completion status:
  [ ] Will or trust in place
  [ ] Beneficiaries designated on all accounts
  [ ] Healthcare directive
  [ ] Power of attorney (financial)
  [ ] Power of attorney (healthcare)
  [ ] Life insurance beneficiaries current
  [ ] Retirement account beneficiaries current
  [ ] Business succession plan (if applicable)
- Progress indicator: X of Y complete
- Each item expands to show:
  - Why it matters
  - Assets affected
  - "Learn more" link
- "This checklist is educational. Consult an estate planning attorney for legal advice."

NET WORTH TRACKER:
- Historical net worth chart (line chart)
- Breakdown: Assets vs Liabilities over time
- Milestones markers (bought house, paid off car, etc.)
- Filter by time range: 1M, 3M, 6M, 1Y, All

GOAL TRACKER:
- Add financial goals:
  - Goal name
  - Target amount
  - Target date
  - Current progress (linked account or manual)
- Visual progress bars
- Projected completion date based on current savings rate
- "On track" / "Behind" / "Ahead" status

DISCLAIMERS (Footer on all tool pages):
"Legacy Financial provides planning tools for educational purposes only. We do not provide personalized financial, investment, tax, or legal advice. All analyses are informational. Consult qualified professionals for personalized guidance."

ACCESSIBILITY:
- Charts have text alternatives
- Interactive elements keyboard accessible
- Results announced to screen readers
```

---

#### 7. Settings & Profile

```
SCREEN: Settings
PATH: /settings
AUTH: Required

Create a mobile-first settings screen:

NAVIGATION:
Mobile: Accordion sections
Desktop: Left sidebar with sections, main content area

SECTIONS:

PROFILE:
- Profile photo upload
- First name, Last name
- Email (read-only, link to change)
- Phone number
- Date of birth
- Address

SECURITY:
- Change password
- Two-factor authentication toggle
- Active sessions list with "Sign out all" option
- Login history (last 10 logins with IP/device)

NOTIFICATIONS:
- Email notifications toggles:
  - Weekly summary
  - Account alerts
  - Security alerts
  - Product updates
- Push notifications toggles (if mobile)

CONNECTED ACCOUNTS:
- List of linked financial institutions
- Quick link to Accounts page

DATA & PRIVACY:
- Export my data (GDPR compliance)
- Delete my account (with confirmation flow)
- Privacy preferences

DISPLAY:
- Theme: Light / Dark / System
- Language selector
- Currency display preference
- Date format preference

SUBSCRIPTION (if applicable):
- Current plan
- Upgrade/downgrade options
- Billing history
```

---

### Global Components Prompt

```
Create these reusable components for the Legacy Financial app:

NAVIGATION:

MobileBottomNav:
- Fixed bottom, 5 items max
- Icons + labels
- Active state indicator
- Hide on scroll down, show on scroll up (optional)
- Items: Dashboard, Accounts, Assets, Beneficiaries, More

DesktopSidebar:
- Fixed left, collapsible
- Logo at top
- Navigation items with icons
- Active state with background highlight
- User profile at bottom
- Collapse button

Header:
- Mobile: Hamburger, Logo, Notifications
- Desktop: Search bar, Notifications, Profile dropdown

FEEDBACK:

Toast/Notification:
- Success, Error, Warning, Info variants
- Auto-dismiss (configurable)
- Action button (optional)
- Accessible (role="alert")

LoadingSpinner:
- Sizes: sm, md, lg
- Can be inline or full-screen overlay

EmptyState:
- Illustration slot
- Title
- Description
- Primary action button
- Secondary action link

ErrorBoundary:
- Catches React errors
- Shows friendly error message
- "Try again" button
- Error code display for support

FORMS:

FormField:
- Label with required indicator
- Input/Select/Textarea
- Helper text
- Error message
- Accessible (label + input association)

CurrencyInput:
- Locale-aware formatting
- Currency symbol
- Decimal precision
- Min/max validation

PercentageInput:
- 0-100 range
- % symbol suffix
- Step increment buttons

DatePicker:
- Mobile-friendly
- Locale-aware formatting
- Min/max date constraints

SearchableSelect:
- Typeahead filtering
- Multi-select variant
- Create new option (optional)
- Accessible combobox pattern

DATA DISPLAY:

DataCard:
- Title, value, trend indicator
- Icon slot
- Click action (optional)
- Loading skeleton state

DataTable:
- Responsive (table on desktop, cards on mobile)
- Sortable columns
- Pagination
- Row selection (optional)
- Empty state
- Loading state

Chart:
- Wrapper for Recharts
- Responsive container
- Loading state
- Empty state
- Accessible (text alternative)

MODALS/DRAWERS:

Modal:
- Sizes: sm, md, lg, full
- Close button
- Title
- Content
- Footer with actions
- Focus trap
- Close on escape
- Close on overlay click (configurable)

Drawer:
- Mobile: Full screen from bottom
- Desktop: Side panel from right
- Same props as Modal

ConfirmationDialog:
- Title, message
- Confirm/Cancel buttons
- Destructive variant (red confirm button)
```

---

## Part 2: Claude Code Instructions (Backend API)

### Project Structure

```
src/
├── LegacyFinancial.Api/                 # Azure Functions project
│   ├── Functions/
│   │   ├── Accounts/
│   │   │   ├── GetAccounts.cs
│   │   │   ├── LinkAccount.cs
│   │   │   ├── RefreshAccount.cs
│   │   │   └── UnlinkAccount.cs
│   │   ├── Assets/
│   │   │   ├── GetAssets.cs
│   │   │   ├── CreateAsset.cs
│   │   │   ├── UpdateAsset.cs
│   │   │   └── DeleteAsset.cs
│   │   ├── Beneficiaries/
│   │   │   ├── GetBeneficiaries.cs
│   │   │   ├── CreateBeneficiary.cs
│   │   │   ├── UpdateBeneficiary.cs
│   │   │   ├── DeleteBeneficiary.cs
│   │   │   └── AssignBeneficiary.cs
│   │   ├── Analysis/
│   │   │   ├── GetNetWorth.cs
│   │   │   ├── AnalyzeDebtPayoff.cs
│   │   │   ├── AnalyzeInsuranceCoverage.cs
│   │   │   └── GetAIInsights.cs
│   │   └── Auth/
│   │       ├── Register.cs
│   │       ├── Login.cs
│   │       └── RefreshToken.cs
│   ├── Middleware/
│   │   ├── TenantMiddleware.cs
│   │   ├── AuthenticationMiddleware.cs
│   │   └── ExceptionMiddleware.cs
│   ├── Program.cs
│   └── host.json
│
├── LegacyFinancial.Core/                # Domain layer
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── LinkedAccount.cs
│   │   ├── Asset.cs
│   │   ├── Beneficiary.cs
│   │   ├── BeneficiaryAssignment.cs
│   │   └── AuditLog.cs
│   ├── Enums/
│   │   ├── AssetType.cs
│   │   ├── AccountType.cs
│   │   ├── BeneficiaryType.cs
│   │   └── RelationshipType.cs
│   ├── Interfaces/
│   │   ├── IAccountService.cs
│   │   ├── IAssetService.cs
│   │   ├── IBeneficiaryService.cs
│   │   ├── IAnalysisService.cs
│   │   └── IAuditService.cs
│   └── ValueObjects/
│       ├── Money.cs
│       ├── Percentage.cs
│       └── Address.cs
│
├── LegacyFinancial.Application/         # Application layer
│   ├── Services/
│   │   ├── AccountService.cs
│   │   ├── AssetService.cs
│   │   ├── BeneficiaryService.cs
│   │   ├── AnalysisService.cs
│   │   ├── PlaidService.cs
│   │   └── ClaudeAIService.cs
│   ├── DTOs/
│   │   ├── Requests/
│   │   └── Responses/
│   ├── Validators/
│   │   ├── CreateAssetValidator.cs
│   │   └── CreateBeneficiaryValidator.cs
│   └── Mappings/
│       └── MappingProfile.cs
│
├── LegacyFinancial.Infrastructure/      # Infrastructure layer
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/
│   │   └── Migrations/
│   ├── Repositories/
│   │   ├── AccountRepository.cs
│   │   ├── AssetRepository.cs
│   │   └── BeneficiaryRepository.cs
│   ├── External/
│   │   ├── PlaidClient.cs
│   │   └── ClaudeClient.cs
│   └── Logging/
│       └── AuditLogger.cs
│
└── LegacyFinancial.Tests/               # Test projects
    ├── Unit/
    ├── Integration/
    └── E2E/
```

---

### Claude Code Prompts - Backend Implementation

#### 1. Project Initialization

```
Create a new Azure Functions project (.NET 8 Isolated Worker) for Legacy Financial API with the following requirements:

PROJECT SETUP:
- Solution name: LegacyFinancial
- Projects: Api, Core, Application, Infrastructure, Tests
- .NET 8.0 with nullable reference types enabled
- Azure Functions v4 isolated worker process

NUGET PACKAGES:
Api:
- Microsoft.Azure.Functions.Worker
- Microsoft.Azure.Functions.Worker.Extensions.Http
- Microsoft.Azure.Functions.Worker.Extensions.OpenApi
- Microsoft.ApplicationInsights.WorkerService

Core:
- (No external dependencies - pure domain)

Application:
- FluentValidation
- AutoMapper
- MediatR

Infrastructure:
- Microsoft.EntityFrameworkCore.SqlServer
- Azure.Identity
- Azure.Security.KeyVault.Secrets
- Going.Plaid (Plaid SDK)

Tests:
- xUnit
- Moq
- FluentAssertions
- Microsoft.AspNetCore.Mvc.Testing

CONFIGURATION:
- local.settings.json template with all required settings
- Environment-based configuration (Development, Staging, Production)
- Key Vault integration for secrets

Create the solution structure with proper project references following clean architecture:
- Api → Application → Core
- Api → Infrastructure
- Infrastructure → Application → Core
```

---

#### 2. Core Domain Entities

```
Create the Core domain entities for Legacy Financial with full XML documentation:

USER ENTITY:
/// <summary>
/// Represents a user of the Legacy Financial platform.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties
    public ICollection<LinkedAccount> LinkedAccounts { get; set; }
    public ICollection<Asset> Assets { get; set; }
    public ICollection<Beneficiary> Beneficiaries { get; set; }
}

LINKED ACCOUNT ENTITY:
- Properties: Id, UserId, PlaidItemId, PlaidAccessToken, InstitutionId, InstitutionName, AccountMask, AccountName, AccountType, CurrentBalance, AvailableBalance, CurrencyCode, ConnectionStatus, LastSyncedAt, CreatedAt, UpdatedAt
- ConnectionStatus enum: Connected, NeedsReauth, Disconnected, Syncing

ASSET ENTITY:
- Properties: Id, UserId, Name, AssetType, Description, CurrentValue, CurrencyCode, PurchasePrice, PurchaseDate, OwnershipPercentage, AssociatedEntityId, AssociatedEntityName, Address (for real estate), Metadata (JSON for type-specific data), CreatedAt, UpdatedAt
- AssetType enum: RealEstate, Vehicle, BusinessInterest, PersonalProperty, Collectible, Other

BENEFICIARY ENTITY:
- Properties: Id, UserId, BeneficiaryType, FirstName, LastName, OrganizationName, TrustName, Relationship, Email, Phone, DateOfBirth, Address, TaxId, CreatedAt, UpdatedAt
- BeneficiaryType enum: Individual, Trust, Charity, Organization
- Relationship enum: Spouse, Child, Parent, Sibling, Grandchild, Other

BENEFICIARY ASSIGNMENT ENTITY:
- Properties: Id, BeneficiaryId, AssetId, LinkedAccountId (nullable - for either asset or account), DesignationType, AllocationPercentage, PerStirpes, Notes, CreatedAt, UpdatedAt
- DesignationType enum: Primary, Contingent

VALUE OBJECTS:
- Money: Amount, CurrencyCode with proper equality comparison
- Percentage: Value (0-100) with validation
- Address: Street1, Street2, City, State, PostalCode, Country

Include:
- Data annotations for validation
- Full XML documentation on all properties
- Proper encapsulation
- Audit properties (CreatedAt, UpdatedAt)
- Soft delete support where appropriate
```

---

#### 3. API Error Handling

```
Create a standardized error handling system for the Legacy Financial API:

ERROR CODES (create as constants):
public static class ErrorCodes
{
    // Authentication
    public const string AUTH_REQUIRED = "AUTH_REQUIRED";
    public const string AUTH_FORBIDDEN = "AUTH_FORBIDDEN";
    public const string AUTH_INVALID_CREDENTIALS = "AUTH_INVALID_CREDENTIALS";
    public const string AUTH_EMAIL_EXISTS = "AUTH_EMAIL_EXISTS";
    public const string AUTH_ACCOUNT_LOCKED = "AUTH_ACCOUNT_LOCKED";
    public const string AUTH_TOKEN_EXPIRED = "AUTH_TOKEN_EXPIRED";
    
    // Validation
    public const string VALIDATION_FAILED = "VALIDATION_FAILED";
    
    // Resources
    public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";
    public const string ACCOUNT_NOT_FOUND = "ACCOUNT_NOT_FOUND";
    public const string ASSET_NOT_FOUND = "ASSET_NOT_FOUND";
    public const string BENEFICIARY_NOT_FOUND = "BENEFICIARY_NOT_FOUND";
    
    // Business Logic
    public const string ALLOCATION_INVALID = "ALLOCATION_INVALID";
    public const string PLAID_LINK_ERROR = "PLAID_LINK_ERROR";
    public const string INSTITUTION_NOT_SUPPORTED = "INSTITUTION_NOT_SUPPORTED";
    
    // System
    public const string RATE_LIMITED = "RATE_LIMITED";
    public const string SERVER_ERROR = "SERVER_ERROR";
}

API ERROR RESPONSE:
public class ApiErrorResponse
{
    public string Code { get; set; }
    public string Message { get; set; }  // Default English for logging
    public Dictionary<string, object>? Details { get; set; }
    public string? TraceId { get; set; }
}

EXCEPTION MIDDLEWARE:
- Catch all unhandled exceptions
- Map known exceptions to appropriate error codes
- Log errors with correlation ID
- Return consistent ApiErrorResponse
- Never expose stack traces in production

CUSTOM EXCEPTIONS:
- NotFoundException : Exception
- ValidationException : Exception
- UnauthorizedException : Exception
- ForbiddenException : Exception
- BusinessRuleException : Exception

Include Application Insights integration for telemetry.
```

---

#### 4. Account Service Implementation

```
Create the Account Service for Plaid integration with full documentation:

INTERFACE:
/// <summary>
/// Service for managing linked financial accounts via Plaid.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Creates a Plaid Link token for account connection.
    /// </summary>
    Task<PlaidLinkTokenResponse> CreateLinkTokenAsync(Guid userId);
    
    /// <summary>
    /// Exchanges a public token for access token and links accounts.
    /// </summary>
    Task<IEnumerable<LinkedAccountDto>> ExchangePublicTokenAsync(Guid userId, string publicToken);
    
    /// <summary>
    /// Gets all linked accounts for a user.
    /// </summary>
    Task<IEnumerable<LinkedAccountDto>> GetAccountsAsync(Guid userId);
    
    /// <summary>
    /// Refreshes account balances from Plaid.
    /// </summary>
    Task<LinkedAccountDto> RefreshAccountAsync(Guid userId, Guid accountId);
    
    /// <summary>
    /// Unlinks an account (revokes Plaid access).
    /// </summary>
    Task UnlinkAccountAsync(Guid userId, Guid accountId);
}

IMPLEMENTATION REQUIREMENTS:
- Tenant isolation: All queries scoped to userId
- Encrypt PlaidAccessToken before storage (Azure Key Vault)
- Audit logging for all account operations
- Structured logging with correlation IDs
- Handle Plaid errors and map to error codes
- Background sync job for balance updates

PLAID ERROR MAPPING:
- ITEM_LOGIN_REQUIRED → ConnectionStatus.NeedsReauth
- INSTITUTION_NOT_SUPPORTED → ErrorCodes.INSTITUTION_NOT_SUPPORTED
- RATE_LIMIT_EXCEEDED → ErrorCodes.RATE_LIMITED

DTOs:
- LinkedAccountDto: Id, InstitutionName, InstitutionLogo, AccountName, AccountMask, AccountType, CurrentBalance, AvailableBalance, ConnectionStatus, LastSyncedAt
- PlaidLinkTokenResponse: LinkToken, Expiration
```

---

#### 5. Asset Service Implementation

```
Create the Asset Service with full CRUD operations:

INTERFACE:
public interface IAssetService
{
    Task<IEnumerable<AssetDto>> GetAssetsAsync(Guid userId, AssetType? filterType = null);
    Task<AssetDto> GetAssetByIdAsync(Guid userId, Guid assetId);
    Task<AssetDto> CreateAssetAsync(Guid userId, CreateAssetRequest request);
    Task<AssetDto> UpdateAssetAsync(Guid userId, Guid assetId, UpdateAssetRequest request);
    Task DeleteAssetAsync(Guid userId, Guid assetId);
    Task<AssetSummaryDto> GetAssetSummaryAsync(Guid userId);
}

CREATE ASSET REQUEST VALIDATION (FluentValidation):
- Name: Required, max 200 chars
- AssetType: Required, valid enum
- CurrentValue: Required, greater than 0
- OwnershipPercentage: 0-100 (default 100)
- PurchasePrice: Optional, greater than 0 if provided
- PurchaseDate: Optional, not in future
- For RealEstate: Address required
- For BusinessInterest: OwnershipPercentage required

ASSET SUMMARY DTO:
- TotalValue: Sum of (CurrentValue * OwnershipPercentage) for all assets
- CountByType: Dictionary<AssetType, int>
- ValueByType: Dictionary<AssetType, decimal>

IMPLEMENTATION REQUIREMENTS:
- Tenant isolation: All queries include UserId filter
- Optimistic concurrency with RowVersion
- Soft delete support
- Audit trail for value changes
- Calculate user's share based on OwnershipPercentage
```

---

#### 6. Beneficiary Service Implementation

```
Create the Beneficiary Service with allocation management:

INTERFACE:
public interface IBeneficiaryService
{
    Task<IEnumerable<BeneficiaryDto>> GetBeneficiariesAsync(Guid userId);
    Task<BeneficiaryDto> GetBeneficiaryByIdAsync(Guid userId, Guid beneficiaryId);
    Task<BeneficiaryDto> CreateBeneficiaryAsync(Guid userId, CreateBeneficiaryRequest request);
    Task<BeneficiaryDto> UpdateBeneficiaryAsync(Guid userId, Guid beneficiaryId, UpdateBeneficiaryRequest request);
    Task DeleteBeneficiaryAsync(Guid userId, Guid beneficiaryId);
    
    // Assignment operations
    Task<BeneficiaryAssignmentDto> AssignBeneficiaryAsync(Guid userId, AssignBeneficiaryRequest request);
    Task UpdateAssignmentAsync(Guid userId, Guid assignmentId, UpdateAssignmentRequest request);
    Task RemoveAssignmentAsync(Guid userId, Guid assignmentId);
    
    // Analysis
    Task<BeneficiaryCoverageDto> GetCoverageSummaryAsync(Guid userId);
}

ASSIGN BENEFICIARY REQUEST:
- BeneficiaryId: Required
- AssetId or LinkedAccountId: One required
- DesignationType: Primary or Contingent
- AllocationPercentage: 1-100
- PerStirpes: bool (default false)

VALIDATION RULES:
- Total Primary allocation for an asset must equal 100% (warn if not)
- Total Contingent allocation should equal 100% (warn if not)
- Cannot assign same beneficiary twice to same asset with same designation
- Cannot delete beneficiary with active assignments (must remove first)

COVERAGE SUMMARY DTO:
- TotalAssets: int
- AssetsWithBeneficiaries: int
- AssetsNeedingReview: List of asset IDs without beneficiaries
- LastReviewDate: DateTime?

IMPLEMENTATION:
- Tenant isolation on all queries
- Cascade handling for beneficiary deletion
- Warning system for incomplete allocations (not blocking)
- Audit logging for assignment changes
```

---

#### 7. Analysis Service (AI Integration)

```
Create the Analysis Service for financial planning tools:

INTERFACE:
public interface IAnalysisService
{
    Task<NetWorthDto> CalculateNetWorthAsync(Guid userId);
    Task<DebtPayoffAnalysisDto> AnalyzeDebtPayoffAsync(Guid userId, DebtPayoffRequest request);
    Task<InsuranceCoverageAnalysisDto> AnalyzeInsuranceCoverageAsync(Guid userId, InsuranceCoverageRequest request);
    Task<IEnumerable<AIInsightDto>> GetAIInsightsAsync(Guid userId);
}

NET WORTH CALCULATION:
- Sum all linked account balances
- Add all asset values (adjusted for ownership percentage)
- Subtract all liabilities (credit cards, loans from linked accounts)
- Return breakdown by category
- Include historical data points (if stored)

DEBT PAYOFF ANALYSIS:
Input:
- ExtraMonthlyPayment: decimal (optional, default 0)
- Strategy: Avalanche | Snowball | Custom

Output:
public class DebtPayoffAnalysisDto
{
    public List<DebtDto> Debts { get; set; }
    public List<StrategyResultDto> StrategyComparisons { get; set; }
    public string Disclaimer { get; set; }
}

public class StrategyResultDto
{
    public string StrategyName { get; set; }
    public int MonthsToPayoff { get; set; }
    public decimal TotalInterestPaid { get; set; }
    public decimal SavingsVsMinimum { get; set; }
    public List<PayoffMilestoneDto> Milestones { get; set; }
}

INSURANCE COVERAGE ANALYSIS:
Input:
- AnnualIncome: decimal
- YearsOfIncomeToReplace: int
- DependentsCount: int
- ExistingLifeInsurance: decimal
- ExistingDisabilityInsurance: decimal

Output:
public class InsuranceCoverageAnalysisDto
{
    public decimal CalculatedNeed { get; set; }
    public decimal CurrentCoverage { get; set; }
    public decimal Gap { get; set; }
    public decimal CoverageRatio { get; set; }
    public InsuranceBreakdownDto Breakdown { get; set; }
    public string Disclaimer { get; set; }
}

AI INSIGHTS (Claude Integration):
Create ClaudeAIService that:
- Gathers user's financial summary
- Sends to Claude Sonnet 4.5 with specific prompt
- Parses response into structured insights
- Categories: InsuranceGap, BeneficiaryReview, DebtOptimization, TaxPlanning, GoalProgress

PROMPT TEMPLATE:
"You are a financial analysis tool (not an advisor). Analyze this financial data and identify potential areas for review. Present findings as objective observations, not recommendations. Always note that professional consultation is advised for decisions.

Financial Profile:
{JSON of user's sanitized financial data}

Provide 3-5 insights in JSON format:
[{ \"category\": \"...\", \"title\": \"...\", \"description\": \"...\", \"severity\": \"info|warning|critical\" }]"

DISCLAIMERS (Required on all responses):
public const string ToolDisclaimer = 
    "This analysis is for educational purposes only and does not constitute financial, " +
    "investment, tax, or legal advice. Results are based on the information provided and " +
    "simplified calculations. Consult qualified professionals for personalized guidance.";
```

---

#### 8. Azure Functions Endpoints

```
Create Azure Functions HTTP endpoints following REST conventions:

ACCOUNTS:
[Function("GetAccounts")]
[OpenApiOperation("GetAccounts", "Accounts")]
[OpenApiSecurity("bearer", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer)]
[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(IEnumerable<LinkedAccountDto>))]
public async Task<HttpResponseData> GetAccounts(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts")] HttpRequestData req)

- GET    /api/accounts              → GetAccounts
- POST   /api/accounts/link-token   → CreateLinkToken
- POST   /api/accounts/exchange     → ExchangePublicToken
- POST   /api/accounts/{id}/refresh → RefreshAccount
- DELETE /api/accounts/{id}         → UnlinkAccount

ASSETS:
- GET    /api/assets                → GetAssets (with ?type= filter)
- GET    /api/assets/{id}           → GetAssetById
- POST   /api/assets                → CreateAsset
- PUT    /api/assets/{id}           → UpdateAsset
- DELETE /api/assets/{id}           → DeleteAsset
- GET    /api/assets/summary        → GetAssetSummary

BENEFICIARIES:
- GET    /api/beneficiaries              → GetBeneficiaries
- GET    /api/beneficiaries/{id}         → GetBeneficiaryById
- POST   /api/beneficiaries              → CreateBeneficiary
- PUT    /api/beneficiaries/{id}         → UpdateBeneficiary
- DELETE /api/beneficiaries/{id}         → DeleteBeneficiary
- POST   /api/beneficiaries/assign       → AssignBeneficiary
- PUT    /api/assignments/{id}           → UpdateAssignment
- DELETE /api/assignments/{id}           → RemoveAssignment
- GET    /api/beneficiaries/coverage     → GetCoverageSummary

ANALYSIS:
- GET    /api/analysis/net-worth         → GetNetWorth
- POST   /api/analysis/debt-payoff       → AnalyzeDebtPayoff
- POST   /api/analysis/insurance         → AnalyzeInsuranceCoverage
- GET    /api/analysis/insights          → GetAIInsights

AUTHENTICATION:
- POST   /api/auth/register              → Register
- POST   /api/auth/login                 → Login
- POST   /api/auth/refresh               → RefreshToken
- POST   /api/auth/logout                → Logout

ALL ENDPOINTS MUST:
- Extract user ID from JWT claims
- Validate tenant isolation
- Return appropriate error codes
- Log to Application Insights
- Include OpenAPI documentation
```

---

#### 9. Database Schema

```
Create Entity Framework Core configuration and migrations:

DbContext:
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<LinkedAccount> LinkedAccounts { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Beneficiary> Beneficiaries { get; set; }
    public DbSet<BeneficiaryAssignment> BeneficiaryAssignments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    // Enable tenant filtering
    public Guid? CurrentUserId { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply global query filter for tenant isolation
        modelBuilder.Entity<LinkedAccount>()
            .HasQueryFilter(a => CurrentUserId == null || a.UserId == CurrentUserId);
        // Repeat for Asset, Beneficiary, BeneficiaryAssignment
    }
}

INDEXES:
- Users: Email (unique)
- LinkedAccounts: UserId, PlaidItemId
- Assets: UserId, AssetType
- Beneficiaries: UserId
- BeneficiaryAssignments: BeneficiaryId, AssetId, LinkedAccountId

ENCRYPTION:
- PlaidAccessToken: Encrypted at rest using Azure Key Vault key
- User PII: Consider encryption for sensitive fields

AUDIT LOGGING:
- Override SaveChangesAsync to capture changes
- Store: EntityType, EntityId, Action, OldValues, NewValues, UserId, Timestamp
```

---

#### 10. Testing Requirements

```
Create comprehensive test coverage:

UNIT TESTS (Target: 90%+ coverage):

AccountServiceTests:
- CreateLinkToken_ReturnsValidToken
- ExchangePublicToken_CreatesLinkedAccounts
- GetAccounts_OnlyReturnsUserAccounts (tenant isolation)
- RefreshAccount_UpdatesBalance
- UnlinkAccount_RevokesPlaidAccess

AssetServiceTests:
- CreateAsset_ValidInput_CreatesAsset
- CreateAsset_InvalidOwnership_ThrowsValidation
- GetAssets_FiltersByType
- UpdateAsset_RecordsValueChange
- DeleteAsset_SoftDeletes

BeneficiaryServiceTests:
- CreateBeneficiary_ValidInput_CreatesBeneficiary
- AssignBeneficiary_ValidAllocation_CreatesAssignment
- AssignBeneficiary_DuplicateAssignment_ThrowsError
- GetCoverageSummary_CalculatesCorrectly

AnalysisServiceTests:
- CalculateNetWorth_IncludesAllSources
- AnalyzeDebtPayoff_Avalanche_CalculatesCorrectly
- AnalyzeDebtPayoff_Snowball_CalculatesCorrectly
- AnalyzeInsuranceCoverage_IdentifiesGap

INTEGRATION TESTS:
- API endpoints with in-memory database
- Plaid webhook handling
- Authentication flow

SECURITY TESTS:
- Tenant isolation (user A cannot access user B data)
- Authorization (unauthenticated requests rejected)
- Input validation (SQL injection, XSS prevention)

ACCESSIBILITY TESTS (Frontend):
- axe-core integration for each component
- Keyboard navigation testing
- Screen reader compatibility
```

---

## Part 3: Infrastructure as Code

### Azure Resources (Bicep/ARM)

```bicep
// main.bicep - Azure resources for Legacy Financial

// Resource Group assumed to exist

// Azure Static Web App
resource staticWebApp 'Microsoft.Web/staticSites@2022-09-01' = {
  name: 'swa-legacyfinancial-${environment}'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    repositoryUrl: repositoryUrl
    branch: 'main'
    buildProperties: {
      appLocation: '/frontend'
      apiLocation: ''
      outputLocation: 'dist'
    }
  }
}

// Azure Functions (API)
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: 'func-legacyfinancial-${environment}'
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'AzureWebJobsStorage', value: storageConnectionString }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'KeyVaultUri', value: keyVault.properties.vaultUri }
      ]
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Azure SQL Database
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: 'sql-legacyfinancial-${environment}'
  location: location
  properties: {
    administratorLogin: sqlAdminUsername
    administratorLoginPassword: sqlAdminPassword
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: 'sqldb-legacyfinancial'
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: 'kv-legacyfinancial-${environment}'
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    accessPolicies: []
    enableRbacAuthorization: true
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-legacyfinancial-${environment}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}
```

---

## Part 4: CI/CD Pipeline

### GitHub Actions Workflow

```yaml
# .github/workflows/deploy.yml
name: Build and Deploy

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

env:
  DOTNET_VERSION: '8.0.x'
  NODE_VERSION: '20.x'
  AZURE_FUNCTIONAPP_NAME: 'func-legacyfinancial'

jobs:
  build-api:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore dependencies
        run: dotnet restore
        working-directory: ./src
      
      - name: Build
        run: dotnet build --no-restore --configuration Release
        working-directory: ./src
      
      - name: Test
        run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage" --results-directory ./coverage
        working-directory: ./src
      
      - name: Check coverage threshold
        run: |
          # Fail if coverage < 90%
          coverage=$(cat ./coverage/**/coverage.cobertura.xml | grep -oP 'line-rate="\K[^"]+')
          if (( $(echo "$coverage < 0.90" | bc -l) )); then
            echo "Coverage $coverage is below 90% threshold"
            exit 1
          fi
      
      - name: Publish
        run: dotnet publish --configuration Release --output ./publish
        working-directory: ./src/LegacyFinancial.Api

  build-frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: ./frontend/package-lock.json
      
      - name: Install dependencies
        run: npm ci
        working-directory: ./frontend
      
      - name: Lint
        run: npm run lint
        working-directory: ./frontend
      
      - name: Test
        run: npm run test:coverage
        working-directory: ./frontend
      
      - name: Build
        run: npm run build
        working-directory: ./frontend

  deploy-staging:
    needs: [build-api, build-frontend]
    if: github.ref == 'refs/heads/develop'
    runs-on: ubuntu-latest
    environment: staging
    steps:
      - name: Deploy to Staging
        # Deploy steps here

  deploy-production:
    needs: [build-api, build-frontend]
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    environment: production
    steps:
      - name: Deploy to Production
        # Deploy steps here
```

---

## Part 5: Localization Keys Structure

```json
// en/common.json
{
  "app": {
    "name": "Legacy Financial",
    "tagline": "Your complete financial picture"
  },
  "nav": {
    "dashboard": "Dashboard",
    "accounts": "Accounts",
    "assets": "Assets",
    "beneficiaries": "Beneficiaries",
    "tools": "Planning Tools",
    "settings": "Settings"
  },
  "actions": {
    "save": "Save",
    "cancel": "Cancel",
    "delete": "Delete",
    "edit": "Edit",
    "add": "Add",
    "submit": "Submit",
    "continue": "Continue",
    "back": "Back"
  },
  "errors": {
    "AUTH_REQUIRED": "Please sign in to continue",
    "AUTH_FORBIDDEN": "You don't have permission to perform this action",
    "AUTH_INVALID_CREDENTIALS": "Invalid email or password",
    "AUTH_EMAIL_EXISTS": "An account with this email already exists",
    "VALIDATION_FAILED": "Please check your input and try again",
    "RESOURCE_NOT_FOUND": "The requested item was not found",
    "ACCOUNT_NOT_FOUND": "Account not found",
    "ASSET_NOT_FOUND": "Asset not found",
    "BENEFICIARY_NOT_FOUND": "Beneficiary not found",
    "ALLOCATION_INVALID": "Beneficiary allocations must total 100%",
    "PLAID_LINK_ERROR": "Unable to connect to your bank. Please try again.",
    "RATE_LIMITED": "Too many requests. Please wait a moment.",
    "SERVER_ERROR": "Something went wrong. Please try again later."
  }
}

// en/dashboard.json
{
  "dashboard": {
    "title": "Dashboard",
    "netWorth": {
      "title": "Net Worth",
      "asOf": "As of {{date}}"
    },
    "quickStats": {
      "totalAssets": "Total Assets",
      "totalLiabilities": "Total Liabilities",
      "cashFlow": "Monthly Cash Flow",
      "insuranceCoverage": "Insurance Coverage"
    },
    "recentActivity": {
      "title": "Recent Activity",
      "viewAll": "View All"
    },
    "insights": {
      "title": "AI Insights",
      "learnMore": "Learn More"
    }
  }
}

// en/accounts.json, en/assets.json, en/beneficiaries.json, en/tools.json...
```

---

## Implementation Checklist

### Phase 1: Foundation (Weeks 1-2)
- [ ] Set up Azure resources (Bicep deployment)
- [ ] Initialize .NET solution structure
- [ ] Initialize React frontend with Vite
- [ ] Configure CI/CD pipeline
- [ ] Set up development environment

### Phase 2: Authentication (Weeks 3-4)
- [ ] Implement user registration/login
- [ ] Set up Azure AD B2C or custom JWT auth
- [ ] Create auth screens in frontend
- [ ] Implement protected routes

### Phase 3: Account Aggregation (Weeks 5-6)
- [ ] Integrate Plaid SDK
- [ ] Build account linking flow
- [ ] Create accounts list/management UI
- [ ] Implement balance refresh

### Phase 4: Assets & Beneficiaries (Weeks 7-9)
- [ ] Build asset CRUD endpoints
- [ ] Create asset management UI
- [ ] Build beneficiary CRUD endpoints
- [ ] Create beneficiary management UI
- [ ] Implement assignment logic

### Phase 5: Analysis Tools (Weeks 10-11)
- [ ] Implement net worth calculation
- [ ] Build debt payoff analyzer
- [ ] Build insurance coverage analyzer
- [ ] Integrate Claude AI for insights
- [ ] Create planning tools UI

### Phase 6: Polish & Launch (Weeks 12-13)
- [ ] Accessibility audit and fixes
- [ ] Performance optimization
- [ ] Security penetration testing
- [ ] Documentation completion
- [ ] Beta testing
- [ ] Production deployment

---

## Recommendations vs Original Discussion

Based on our previous conversation, here are my enhanced recommendations:

### 1. **Start with Plaid, Design for FDX**
The original plan stands - use Plaid for immediate functionality but design data models aligned with FDX schemas for future compatibility.

### 2. **Modular Monolith → Microservices**
Start with Azure Functions grouped by domain (Accounts, Assets, Beneficiaries, Analysis) but keep them in one deployment. Extract to separate services only when scaling demands it.

### 3. **AI Cost Management**
Implement tiered AI features:
- Basic insights: Haiku 4.5 (cheaper, faster)
- Deep analysis: Sonnet 4.5 (on-demand)
- Batch processing for non-urgent insights

### 4. **Professional Handoff (Phase 2 Prep)**
Even in Phase 1, design the `ProfessionalHandoffPackage` DTO so you can easily expose it via API later.

### 5. **Tools Framing**
All AI responses must use "tools" language:
- "Analysis shows..." not "You should..."
- "Options include..." not "I recommend..."
- Always include disclaimers

### 6. **Mobile-First is Critical**
Financial planning happens on phones. Every screen must work beautifully on mobile first.

---

This execution plan provides detailed instructions for both Lovable (frontend generation) and Claude Code (backend implementation) while following all development guidelines for security, accessibility, localization, and documentation.
