# 12 — Entity Structure & Multi-Entity Architecture

> Core Entity model (Personal, Business, Trust), entity-scoped sub-features, unified role system, navigation restructure, cloud storage integration, alert system, financial statements, inter-entity transfers, and household aggregation.

---

## Overview

RAJ Financial restructures from a flat "My Account" layout to an **Entity-First Architecture** where every financial concept (assets, liabilities, income, expenses, accounts, transactions, documents) is scoped to an **Entity**. Three entity types exist:

- **Personal** — Individual/family finances (one per user, auto-created)
- **Business** — LLCs, corporations, partnerships, sole proprietorships
- **Trust** — Revocable, irrevocable, special needs, charitable, etc.

Each entity functions as a self-contained financial unit with its own:
- Income sources & expenses/bills
- Assets & liabilities
- Accounts & transactions
- Documents (stored in user's own cloud storage)
- Insurance coverage
- Debt payoff plans
- AI-powered overview analysis
- Financial statements (P&L, Balance Sheet, Cash Flow)

---

## 1. Core Entity Data Model

### Strategy: TPH + JSON Metadata

Uses Table-Per-Hierarchy (single `Entities` table) with JSON columns for type-specific fields — matching the existing asset metadata pattern with EF Core `ToJson()`. Base fields are real columns; Business and Trust metadata live in JSON columns.

### Entity Table

```csharp
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class Entity
{
    // ── Base fields (real columns) ──
    [MemoryPackOrder(0)]  public Guid Id { get; set; }
    [MemoryPackOrder(1)]  public Guid UserId { get; set; }
    [MemoryPackOrder(2)]  public EntityType Type { get; set; }
    [MemoryPackOrder(3)]  public string Name { get; set; } = string.Empty;
    [MemoryPackOrder(4)]  public string Slug { get; set; } = string.Empty;
    [MemoryPackOrder(5)]  public Guid? ParentEntityId { get; set; }
    [MemoryPackOrder(6)]  public Guid? StorageConnectionId { get; set; }
    [MemoryPackOrder(7)]  public bool IsActive { get; set; } = true;
    [MemoryPackOrder(8)]  public DateTimeOffset CreatedAt { get; set; }
    [MemoryPackOrder(9)]  public DateTimeOffset UpdatedAt { get; set; }

    // ── Type-specific (JSON columns via ToJson()) ──
    [MemoryPackOrder(10)] public BusinessEntityMetadata? Business { get; set; }
    [MemoryPackOrder(11)] public TrustEntityMetadata? Trust { get; set; }

    // ── Navigation ──
    public Entity? ParentEntity { get; set; }
    public ICollection<Entity> ChildEntities { get; set; } = [];
    public ICollection<EntityRole> Roles { get; set; } = [];
    public StorageConnection? StorageConnection { get; set; }
}
```

### EntityType Enum

```csharp
public enum EntityType
{
    Personal = 0,
    Business = 1,
    Trust    = 2,
}
```

### Business Metadata

```csharp
public sealed record BusinessEntityMetadata
{
    public BusinessFormationType EntityFormationType { get; init; }
    public string? Ein { get; init; }
    public string? DunsNumber { get; init; }
    public string? NaicsCode { get; init; }
    public string? Industry { get; init; }
    public string? StateOfFormation { get; init; }
    public DateTimeOffset? FormationDate { get; init; }
    public int? FiscalYearEnd { get; init; }        // Month number (1–12)
    public string? RegisteredAgentName { get; init; }
    public ContactAddress? RegisteredAgentAddress { get; init; }
    public decimal? AnnualRevenue { get; init; }
    public int? NumberOfEmployees { get; init; }
    public TaxClassification? TaxClassification { get; init; }
    public StateRegistration[]? Registrations { get; init; }
}

public enum BusinessFormationType
{
    SoleProprietorship = 0,
    SingleMemberLLC    = 1,
    MultiMemberLLC     = 2,
    SCorporation       = 3,
    CCorporation       = 4,
    Partnership        = 5,
    LimitedPartnership = 6,
    NonProfit          = 7,
}

public enum TaxClassification
{
    SoleProprietor       = 0,
    Partnership          = 1,
    SCorporation         = 2,
    CCorporation         = 3,
    NonProfit501c3       = 4,
    DisregardedEntity    = 5,
}

public sealed record StateRegistration
{
    public string State { get; init; } = string.Empty;
    public string? RegistrationNumber { get; init; }
    public string? SosFilingNumber { get; init; }
    public DateTimeOffset? RegisteredDate { get; init; }
    public DateTimeOffset? AnnualReportDueDate { get; init; }
    public bool IsInGoodStanding { get; init; }
}
```

### Trust Metadata

```csharp
public sealed record TrustEntityMetadata
{
    public TrustCategory Category { get; init; }
    public TrustPurpose Purpose { get; init; }
    public string? SpecificType { get; init; }    // e.g., "GRAT", "QPRT", "ILIT"
    public string? Ein { get; init; }
    public DateTimeOffset? TrustDate { get; init; }
    public string? Jurisdiction { get; init; }
    public bool IsGrantorTrust { get; init; }
    public bool HasCrummeyProvisions { get; init; }
    public bool IsGstExempt { get; init; }
    public decimal? FundingAmount { get; init; }
    public string? SuccessorTrusteePlan { get; init; }
}

public enum TrustCategory
{
    Revocable   = 0,
    Irrevocable = 1,
}

public enum TrustPurpose
{
    AssetProtection   = 0,
    EstatePlanning    = 1,
    Charitable        = 2,
    SpecialNeeds      = 3,
    Education         = 4,
    BusinessSuccession = 5,
    TaxPlanning       = 6,
    Other             = 7,
}
```

### Personal Entity

The Personal entity has no type-specific metadata — the user profile itself serves that purpose. One Personal entity is **auto-created per user** on registration. It cannot be deleted.

### Entity Nesting

`ParentEntityId` enables grouping (e.g., a holding company owns subsidiaries). The data model allows unlimited depth, but the **UI caps visual nesting at 2 levels** (parent → child). Deeper nesting is allowed in the DB but renders flat in the sidebar beyond level 2.

---

## 2. Entity Roles (Unified Org Chart & Trust Roles)

### EntityRole Table

A single `EntityRole` table handles both business org chart positions AND trust roles (trustee, beneficiary, grantor, etc.):

```csharp
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class EntityRole
{
    [MemoryPackOrder(0)]  public Guid Id { get; set; }
    [MemoryPackOrder(1)]  public Guid EntityId { get; set; }
    [MemoryPackOrder(2)]  public Guid ContactId { get; set; }
    [MemoryPackOrder(3)]  public EntityRoleType RoleType { get; set; }
    [MemoryPackOrder(4)]  public string? Title { get; set; }           // e.g., "CEO", "Managing Member"
    [MemoryPackOrder(5)]  public decimal? OwnershipPercent { get; set; }
    [MemoryPackOrder(6)]  public decimal? BeneficialInterestPercent { get; set; }
    [MemoryPackOrder(7)]  public bool IsSignatory { get; set; }
    [MemoryPackOrder(8)]  public bool IsPrimary { get; set; }          // Primary trustee, etc.
    [MemoryPackOrder(9)]  public int SortOrder { get; set; }           // For succession ordering
    [MemoryPackOrder(10)] public DateTimeOffset? EffectiveDate { get; set; }
    [MemoryPackOrder(11)] public DateTimeOffset? EndDate { get; set; }
    [MemoryPackOrder(12)] public string? Notes { get; set; }

    // Navigation
    public Entity Entity { get; set; } = null!;
    public Contact Contact { get; set; } = null!;
}
```

### EntityRoleType Enum

```csharp
public enum EntityRoleType
{
    // Business roles
    Owner           = 0,
    Officer         = 1,
    Director        = 2,
    RegisteredAgent = 3,
    Employee        = 4,
    Accountant      = 5,
    Attorney        = 6,

    // Trust roles
    Grantor         = 10,
    Trustee         = 11,
    SuccessorTrustee = 12,
    Beneficiary     = 13,
    Protector       = 14,
    TrustAdvisor    = 15,

    // Shared
    Other           = 99,
}
```

### Household Inclusion (Derived, Not Stored)

An entity is included in the user's **household aggregate** if any person in the household holds an ownership or beneficiary role on that entity. Specifically:

- **Business**: Included if a household member has `EntityRoleType.Owner` with `OwnershipPercent > 0`
- **Trust**: Included if a household member has `EntityRoleType.Grantor` or `EntityRoleType.Beneficiary`
- **Personal**: Always included (it IS the household)

This is computed at query time, not stored as a boolean.

### "Your Share" Calculation

For household dashboard aggregation:
- **Personal**: 100% of all values
- **Business**: Values multiplied by the user's `OwnershipPercent` (e.g., 40% of a $1M business = $400K)
- **Trust**: Values multiplied by `BeneficialInterestPercent` (or 100% if grantor of revocable trust)

---

## 3. Entity-Scoped Sub-Features

Every existing feature gains an `EntityId` foreign key. When a user navigates to `/business/acme-llc/assets`, they see only assets belonging to that entity.

### Affected Tables

| Table | New Column | Notes |
|-------|-----------|-------|
| Asset | `EntityId` (required) | All 12 asset types scoped to entity |
| Account | `EntityId` (required) | Bank, investment, Plaid-linked accounts |
| Transaction | via Account | Inherited through account's entity |
| IncomeSource | `EntityId` (required) | New table (see Section 3a) |
| Bill | `EntityId` (required) | New table (see Section 3b) |
| InsuranceCoverage | `EntityId` (required) | New table (see Section 3c) |
| DebtPayoffPlan | `EntityId` (required) | New table (see Section 3d) |
| Document | `EntityId` (required) | New table (see Section 5) |
| Contact | `EntityId` (nullable) | Contacts can be shared or entity-specific |
| Alert | `EntityId` (required) | New table (see Section 6) |

### Migration Strategy

1. Create Personal entity for every existing user
2. Backfill `EntityId` on all existing rows pointing to the user's Personal entity
3. Make `EntityId` non-nullable after backfill

### 3a. Income Sources

New `IncomeSource` table — employment (salary/paystub with deduction breakdown), rental property income, investment dividends, business income, retirement benefits, freelance, etc. See `LOVABLE_INCOME_EARNINGS_PROMPT.md` for full type definitions.

Key hooks consumed by other features:
- `useIncomeSummary(entityId)` → Insurance calculator (annual income), Dashboard (cash flow), Debt payoff (disposable income)

### 3b. Bills & Recurring Expenses

New `Bill` table — recurring obligations with auto-import from Plaid (credit cards, loans get balance/min payment/due date synced) and from uploaded statements. See `LOVABLE_BILLS_EXPENSES_PROMPT.md` for full type definitions.

Key hooks consumed by other features:
- `useBillsSummary(entityId)` → Insurance calculator (debt totals), Debt payoff (auto-populate debts), Dashboard (spending/alerts)

### 3c. Insurance Coverage Manager

Entity-scoped insurance tracking with three views:

- **Recommended Coverage** — Industry-based recommendations for businesses, life/disability/property for personal, liability for trusts
- **Current Policies** — Existing policies with coverage amounts, premiums, expiration tracking
- **Gap Analysis** — Comparison of recommended vs. current, highlighting coverage gaps

For businesses, recommendations are driven by `Industry` and `BusinessFormationType` from entity metadata (e.g., a restaurant needs general liability, workers' comp, liquor liability; a tech company needs E&O, cyber liability).

### 3d. Debt Payoff Plans

Entity-scoped, persisted debt payoff plans (not stateless). Auto-populated from the entity's liabilities (bills with debt categories). Takes into account entity income and expenses to recommend feasible payment amounts. Supports 4 strategies: Avalanche, Snowball, Cash Flow Index, Custom.

### 3e. Entity Overview Page

Each entity has an Overview page (`/:entityType/:slug/overview`) that displays:
- Summary cards (net worth, income, expenses, assets, liabilities)
- AI-powered analysis widget (spending patterns, recommendations, risk alerts)
- Recent activity feed
- Quick action buttons

AI analysis runs on the entity's own data — no separate AI Insights page needed.

---

## 4. Navigation Structure

### Sidebar Layout

```
Personal
  ├── Overview
  ├── Income
  ├── Bills & Expenses
  ├── Assets
  ├── Accounts & Transactions
  ├── Insurance
  ├── Debt Payoff
  ├── Estate Planning
  └── Documents

Business
  ├── [Entity Selector — Acme LLC ▾]
  ├── Overview
  ├── Income
  ├── Bills & Expenses
  ├── Assets
  ├── Accounts & Transactions
  ├── Insurance
  ├── Debt Payoff
  ├── Compliance & Docs
  └── Documents

Trusts
  ├── [Entity Selector — Family Trust ▾]
  ├── Overview
  ├── Income
  ├── Bills & Expenses
  ├── Assets
  ├── Accounts & Transactions
  ├── Insurance
  ├── Documents
  └── Trust Administration

Household (top-level)
  └── Dashboard (aggregate view)

Settings
```

### URL Routing

Entity type and slug in the URL path:

```
/personal/overview
/personal/assets
/personal/income
/business/acme-llc/overview
/business/acme-llc/assets
/trust/family-trust/overview
/trust/family-trust/assets
/household/dashboard
```

### Reusable Page Components

The same React components render in different entity contexts. An `EntityProvider` context provides `entityId`, `entityType`, and `entitySlug`. Pages call `useEntityContext()` to scope their data fetching:

```tsx
// All these render the same <AssetsPage /> component
/personal/assets         → entityId = personal-entity-guid
/business/acme-llc/assets → entityId = acme-llc-entity-guid
/trust/family-trust/assets → entityId = family-trust-entity-guid
```

---

## 5. Storage Connections & Documents

### Zero-Storage Document Model

Documents are stored in the user's own cloud storage (OneDrive, Google Drive, Dropbox). The app stores only references (metadata + cloud file path). No files live on our servers.

### StorageConnection Table

```csharp
public class StorageConnection
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? EntityId { get; set; }           // Which entity this connection serves
    public StorageProvider Provider { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string EncryptedAccessToken { get; set; } = string.Empty;
    public string EncryptedRefreshToken { get; set; } = string.Empty;
    public DateTimeOffset TokenExpiresAt { get; set; }
    public string? RootFolderPath { get; set; }   // e.g., "/RajFinancial/Personal"
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum StorageProvider
{
    OneDrive     = 0,
    GoogleDrive  = 1,
    Dropbox      = 2,
}
```

### Per-Entity Storage

Each entity can have its own storage connection. A user might use:
- Personal OneDrive for personal documents
- Business Google Drive for business documents
- Same or different provider for each trust

### Document Table

```csharp
public class Document
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid StorageConnectionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string CloudFilePath { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long? FileSizeBytes { get; set; }
    public DocumentCategory Category { get; set; }
    public string? Description { get; set; }
    public Guid? LinkedEntityRoleId { get; set; }  // Link to a specific role/contact
    public Guid? LinkedAssetId { get; set; }        // Link to an asset
    public DateTimeOffset? ExpirationDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum DocumentCategory
{
    // Business compliance
    OperatingAgreement    = 0,
    ArticlesOfIncorporation = 1,
    SosRegistration       = 2,
    EinLetter             = 3,
    BusinessLicense       = 4,
    AnnualReport          = 5,

    // Trust documents
    TrustAgreement        = 10,
    TrustAmendment        = 11,
    CertificateOfTrust    = 12,
    LetterOfIntent        = 13,

    // Personal / shared
    TaxReturn             = 20,
    InsurancePolicy       = 21,
    BankStatement         = 22,
    InvestmentStatement   = 23,
    Deed                  = 24,
    Title                 = 25,
    Will                  = 26,
    PowerOfAttorney       = 27,
    BeneficiaryDesignation = 28,
    Receipt               = 29,
    Contract              = 30,
    Other                 = 99,
}
```

### Upload Flow

1. User must connect a storage provider for the entity before uploading
2. On upload, file is written to cloud storage via OAuth (Graph API for OneDrive, etc.)
3. A `Document` reference is created in our DB with the cloud path
4. On download/view, app fetches from cloud storage using stored tokens

### Tier Limits

No tier gating on storage providers — naturally bounded by entity count. Free users get fewer entities, so fewer storage connections.

---

## 6. Unified Alert System

### Alert Table

```csharp
public class Alert
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EntityId { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionLabel { get; set; }
    public string? ActionPath { get; set; }        // In-app route
    public bool IsRead { get; set; }
    public bool IsDismissed { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public enum AlertType
{
    // Bills & payments
    BillDue              = 0,
    BillOverdue          = 1,
    PaymentConfirmed     = 2,

    // Documents & compliance
    DocumentExpiring     = 10,
    AnnualReportDue      = 11,
    SosFilingDue         = 12,
    LicenseExpiring      = 13,

    // Insurance
    InsuranceCoverageGap = 20,
    PolicyExpiring       = 21,
    PremiumDue           = 22,

    // Tax
    EstimatedTaxDue      = 30,
    TaxFilingDeadline    = 31,

    // Accounts
    AccountSyncFailed    = 40,
    LowBalance           = 41,
    LargeTransaction     = 42,

    // Debt
    PromoRateExpiring    = 50,
    DebtMilestone        = 51,

    // Estate
    BeneficiaryMissing   = 60,
    EstatePlanReview     = 61,

    // General
    Custom               = 99,
}

public enum AlertSeverity
{
    Info     = 0,
    Warning  = 1,
    Critical = 2,
}
```

### Alert Generation

Alerts are **computed** by evaluating rules against data — not manually created. A background process (or on-demand computation) checks:

- Bills due within 7 days → `BillDue` alert
- Bills past due → `BillOverdue` alert (Critical)
- Documents with `ExpirationDate` within 30 days → `DocumentExpiring`
- Insurance coverage gaps detected → `InsuranceCoverageGap`
- Promo rates expiring within 3 months → `PromoRateExpiring`
- State registrations with upcoming annual report dates → `AnnualReportDue`
- Quarterly estimated tax dates → `EstimatedTaxDue`
- Plaid account sync failures → `AccountSyncFailed`

### Alert Display

- **Household Dashboard**: Aggregated alerts across all entities, grouped by severity
- **Entity Overview**: Entity-specific alerts
- **Bell icon in header**: Unread alert count badge, dropdown with recent alerts

---

## 7. Financial Statements

Auto-generated from existing data (income, bills, assets, liabilities, transactions). No manual journal entries — computed views.

### Supported Statements

| Statement | Source Data | Period |
|-----------|-----------|--------|
| **Profit & Loss (P&L)** | Income sources (revenue) - Bills/expenses (COGS, operating expenses) | Monthly, Quarterly, Annual |
| **Balance Sheet** | Assets (current + long-term) - Liabilities (current + long-term) = Equity | Point-in-time snapshot |
| **Cash Flow Statement** | Transaction inflows - outflows, categorized by operating/investing/financing | Monthly, Quarterly, Annual |

### Computation

```
P&L:
  Revenue = SUM(IncomeSource.grossAmount) for period
  Expenses = SUM(Bill.amount) for period, grouped by category
  Net Income = Revenue - Expenses

Balance Sheet:
  Assets = SUM(Asset.currentValue) grouped by type
  Liabilities = SUM(Bill.currentBalance) where category is debt
  Equity = Assets - Liabilities

Cash Flow:
  Operating = Revenue cash in - Operating expense cash out
  Investing = Asset purchases/sales from transactions
  Financing = Loan proceeds - Loan payments from transactions
```

### Entity Scoping

Each entity gets its own financial statements. The Household Dashboard shows a consolidated view across included entities (with "Your Share" adjustments for businesses and trusts).

### Tier Limits

- **Free**: Current month statement only
- **Premium**: Full history + export to PDF/CSV

---

## 8. Inter-Entity Transfers

Support financial transfers between entities (e.g., owner contribution to LLC, trust distribution to personal).

### Transfer Types

| Transfer | From | To | Example |
|----------|------|----|---------|
| `OwnerContribution` | Personal | Business | Owner puts $10K into LLC |
| `OwnerDistribution` | Business | Personal | LLC distributes profits |
| `TrustFunding` | Personal | Trust | Grantor funds irrevocable trust |
| `TrustDistribution` | Trust | Personal | Trust distributes to beneficiary |
| `TrustToBusinessTransfer` | Trust | Business | Trust invests in family LLC |
| `BusinessToTrustTransfer` | Business | Trust | Business funds charitable trust |
| `InterBusinessTransfer` | Business | Business | Parent co to subsidiary |
| `InterTrustTransfer` | Trust | Trust | Trust-to-trust transfer |
| `AssetTransfer` | Any | Any | Move asset between entities |
| `LoanRepayment` | Any | Any | Entity repays inter-entity loan |

### Transfer Table

```csharp
public class EntityTransfer
{
    public Guid Id { get; set; }
    public Guid FromEntityId { get; set; }
    public Guid ToEntityId { get; set; }
    public EntityTransferType TransferType { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? LinkedAssetId { get; set; }       // For asset transfers
    public Guid? FromTransactionId { get; set; }   // Debit side
    public Guid? ToTransactionId { get; set; }     // Credit side
    public bool IsRecurring { get; set; }
    public string? RecurrenceCron { get; set; }    // For recurring transfers
    public DateTimeOffset TransferDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

### Behavior

- Creates matching transactions on **both** sides (debit on source entity, credit on destination entity)
- Asset transfers move the `EntityId` on the asset record
- Recurring transfers (e.g., monthly owner distribution) supported via cron schedule
- Transfer history visible on both entity Overview pages

### Tier Limits

- **Free**: No inter-entity transfers
- **Premium**: Unlimited transfers

---

## 9. AR & Invoicing (Future Phase)

Planned for later implementation. Design notes for forward compatibility:

### Accounts Receivable

- AR is essentially "expected incoming money" — rent from tenants, invoices sent to clients
- `AccountsReceivable` table with `EntityId`, `ContactId` (who owes), amount, due date, status
- Integrates with Income Sources (rental income) and Alerts (past-due AR)

### Invoicing

- Generate invoices from the app, sent via email or shared link
- Invoice → AR entry when sent, AR entry → Transaction when paid
- Invoice line items, tax calculation, payment terms

### Accounts Payable

- AP overlaps heavily with Bills — a bill IS an accounts payable record
- Future enhancement: formal AP aging reports, vendor management, purchase orders

### Payroll (Future)

- Employee/contractor payment tracking
- W-2/1099 preparation data
- Integration with business entity roles (employees listed in org chart)

These features use the Entity structure designed above — `EntityId` on all records, scoped to business entities primarily.

---

## 10. Household Dashboard

Top-level aggregate view across all entities included in the household.

### Route

`/household/dashboard`

### Display Modes

Two columns for every aggregate number:
- **Total**: Raw sum across all included entities
- **Your Share**: Adjusted by ownership/beneficial interest percentages

Example:
| | Total | Your Share |
|---|---|---|
| Net Worth | $2,450,000 | $1,820,000 |
| Monthly Income | $28,000 | $21,200 |
| Monthly Expenses | $12,500 | $9,800 |

### Entity Quick Cards

Grid of cards, one per entity, showing:
- Entity name + type badge
- Net worth (or "Your Share" amount)
- Key metric (income for personal, revenue for business, corpus value for trust)
- Link to entity Overview

### Cross-Entity AI Analysis

The Household Dashboard includes an AI widget that analyzes across all entities:
- Cross-entity optimization opportunities (e.g., "Move $X from personal to LLC for tax benefit")
- Household-level risk assessment
- Consolidated insurance coverage review

### Household Alerts

Aggregated alerts from all entities, sorted by severity and due date.

---

## Tier Limits Summary

| Feature | Free | Premium |
|---------|------|---------|
| Business entities | 1 | Unlimited |
| Trust entities | 1 | Unlimited |
| Income sources per entity | 3 | Unlimited |
| Bills per entity | 5 | Unlimited |
| Assets per entity | 10 | Unlimited |
| Documents per entity | 5 | Unlimited |
| Plaid connections | 0 | Unlimited |
| Financial statements | Current month | Full history + export |
| Inter-entity transfers | None | Unlimited |
| AR / Invoicing | None | Full access |
| AI insights per month | 3 (BYOK) | Unlimited (platform key) |
| Storage providers | Any supported | Any supported |

---

## EF Core Configuration Notes

### Entity Configuration

```csharp
builder.Entity<Entity>(e =>
{
    e.ToTable("Entities");
    e.HasKey(x => x.Id);
    e.HasIndex(x => new { x.UserId, x.Slug }).IsUnique();
    e.Property(x => x.Type).HasConversion<string>();
    e.Property(x => x.Name).HasMaxLength(200);
    e.Property(x => x.Slug).HasMaxLength(200);

    // JSON metadata columns
    e.OwnsOne(x => x.Business, b => b.ToJson());
    e.OwnsOne(x => x.Trust, t => t.ToJson());

    // Self-referencing parent-child
    e.HasOne(x => x.ParentEntity)
     .WithMany(x => x.ChildEntities)
     .HasForeignKey(x => x.ParentEntityId)
     .OnDelete(DeleteBehavior.Restrict);

    // Storage connection
    e.HasOne(x => x.StorageConnection)
     .WithMany()
     .HasForeignKey(x => x.StorageConnectionId)
     .OnDelete(DeleteBehavior.SetNull);
});
```

### EntityRole Configuration

```csharp
builder.Entity<EntityRole>(e =>
{
    e.ToTable("EntityRoles");
    e.HasKey(x => x.Id);
    e.Property(x => x.RoleType).HasConversion<string>();
    e.Property(x => x.OwnershipPercent).HasPrecision(5, 2);
    e.Property(x => x.BeneficialInterestPercent).HasPrecision(5, 2);

    e.HasOne(x => x.Entity)
     .WithMany(x => x.Roles)
     .HasForeignKey(x => x.EntityId)
     .OnDelete(DeleteBehavior.Cascade);

    e.HasOne(x => x.Contact)
     .WithMany()
     .HasForeignKey(x => x.ContactId)
     .OnDelete(DeleteBehavior.Restrict);
});
```

---

## Contact Role Update

Add `Tenant` to the existing `AssetContactRole` enum for rental property management:

```csharp
public enum AssetContactRole
{
    // ... existing values ...
    Tenant = N,  // New — for rental property contacts
}
```

This allows tracking tenants on rental property assets without a separate property management feature.
