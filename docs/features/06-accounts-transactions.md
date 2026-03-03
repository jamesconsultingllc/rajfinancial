# 06 — Accounts & Transactions

> Linked bank accounts via Plaid, manual accounts, transaction sync, cursor-based pagination, spending summaries, user annotations, and tier gating.

**ADO Tracking:** [Epic #309 — 06 - Accounts & Transactions](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/309)

| # | Feature | State |
|---|---------|-------|
| [310](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/310) | Plaid Service Integration | New |
| [311](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/311) | Account Management UI | New |
| [312](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/312) | Plaid Webhook Handling | New |
| [522](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/522) | Transaction Service & API | New |
| [524](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/524) | Spending & Transaction UI | New |

---

## Overview

RAJ Financial connects users' bank accounts via **Plaid** (Premium) or **manual entry** (Free) and syncs transactions into a relational store optimized for date-range queries, category aggregation, and merchant search. The transaction pipeline follows a **hybrid storage** strategy — typed relational columns for queried fields, a JSON column for the full Plaid payload to avoid schema changes as Plaid evolves.

Key design decisions:

- **Immutable core fields** — transactions synced from Plaid cannot be modified by users. Only annotations (category override, notes, tags, hidden flag) are editable.
- **Cursor-based pagination** — stable under continuous Plaid sync; no `OFFSET` degradation.
- **TenantId-leading indexes** — advisor-client sharing via `DataAccessGrant` works without index changes.
- **Free-tier query window** — manual transactions stored indefinitely but queried with a 3-month window on Free; upgrading instantly reveals full history.

---

## LinkedAccount Entity

```csharp
/// <summary>
/// A bank, investment, or credit account linked via Plaid or entered manually.
/// Serves as the parent entity for transactions — deleting a LinkedAccount
/// cascades to all its transactions.
/// </summary>
public class LinkedAccount
{
    // === Identity & scoping ===
    public Guid Id { get; set; }

    /// <summary>
    /// Entra Object ID of the account owner.
    /// String type — consistent with UserProfile.Id, Asset.UserId, Contact.UserId.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID for advisor-client data sharing.
    /// Matches UserId for direct owners; used by DataAccessGrant authorization.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    // === Account details ===
    public string Name { get; set; } = string.Empty;
    public string? OfficialName { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string? InstitutionId { get; set; }
    public AccountType Type { get; set; }
    public AccountSubtype? Subtype { get; set; }
    public string? Mask { get; set; } // Last 4 digits

    // === Balance (updated on sync) ===
    public decimal? CurrentBalance { get; set; }
    public decimal? AvailableBalance { get; set; }
    public string IsoCurrencyCode { get; set; } = "USD";

    // === Plaid integration (Premium only) ===
    /// <summary>
    /// Encrypted Plaid access token. AES-256, keys in Azure Key Vault.
    /// Null for manual accounts.
    /// </summary>
    public string? PlaidAccessToken { get; set; }

    public string? PlaidAccountId { get; set; }
    public string? PlaidItemId { get; set; }

    /// <summary>
    /// Plaid's cursor for incremental transaction sync.
    /// Each sync call returns a new cursor; stored here for the next call.
    /// </summary>
    public string? PlaidSyncCursor { get; set; }

    public DateTimeOffset? LastTransactionSyncAt { get; set; }

    // === Source tracking ===
    public AccountSource Source { get; set; }

    // === Audit ===
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // === Navigation ===
    public ICollection<Transaction> Transactions { get; set; } = [];
}
```

### AccountType Enum

```csharp
/// <summary>
/// Bank account types. Maps to Plaid account types for linked accounts;
/// also used for manual account entry.
/// </summary>
public enum AccountType
{
    Checking,
    Savings,
    CreditCard,
    Investment,
    Loan,
    Mortgage,
    Other
}
```

### AccountSubtype Enum

```csharp
/// <summary>
/// Optional refinement of AccountType. Maps to Plaid account subtypes.
/// </summary>
public enum AccountSubtype
{
    // Checking
    Regular,

    // Savings
    MoneyMarket,
    CertificateOfDeposit,
    HighYield,

    // Investment
    Brokerage,
    Ira,
    Roth,
    FiveZeroOneK,

    // Loan
    Auto,
    Personal,
    Student,

    // Credit
    Rewards,
    Business,

    // Other
    Other
}
```

### AccountSource Enum

```csharp
/// <summary>
/// How the account was created. Determines whether Plaid sync is available.
/// </summary>
public enum AccountSource
{
    Plaid,      // Linked via Plaid Link — Premium only
    Manual      // Entered by user — Free + Premium
}
```

---

## Transaction Entity

```csharp
/// <summary>
/// A financial transaction synced from Plaid or entered manually.
/// Immutable core fields — only user annotations (category overrides,
/// notes, tags) are editable.
/// </summary>
public class Transaction
{
    // === Identity & tenant scoping ===
    public Guid Id { get; set; }

    /// <summary>
    /// Entra Object ID of the asset owner (e.g. "a1b2c3d4-...").
    /// String type — consistent with UserProfile.Id, Asset.UserId, Contact.UserId.
    /// Used for tenant-scoped queries and three-tier authorization.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID for advisor-client data sharing. When an advisor views
    /// a client's transactions, TenantId matches the client's UserId.
    /// Required for the DataAccessGrant authorization model.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    public Guid LinkedAccountId { get; set; }
    public string PlaidTransactionId { get; set; } = string.Empty;

    // === Core financial data (immutable after sync) ===
    public decimal Amount { get; set; }
    public string IsoCurrencyCode { get; set; } = "USD";
    public DateOnly Date { get; set; }
    public DateOnly? AuthorizedDate { get; set; }

    // === Merchant / description ===
    public string Name { get; set; } = string.Empty;
    public string? MerchantName { get; set; }
    public string? MerchantEntityId { get; set; }

    // === Categorization (Plaid-assigned defaults, user-overridable) ===
    public string? PlaidPrimaryCategory { get; set; }
    public string? PlaidDetailedCategory { get; set; }
    public string? PlaidCategoryIconUrl { get; set; }
    public string? UserCategoryOverride { get; set; }

    // === Transaction metadata ===
    public string PaymentChannel { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public TransactionSource Source { get; set; }
    public bool IsPending { get; set; }

    // === User annotations (the only editable fields) ===
    public string? UserNotes { get; set; }

    /// <summary>
    /// User-defined tags stored as a JSON array (e.g. ["tax-deductible", "business"]).
    /// JSON array enables tag-level querying via JSON_VALUE/OPENJSON without
    /// LIKE pattern matching issues that comma-separated strings have.
    /// </summary>
    public string? UserTagsJson { get; set; }

    public bool IsHidden { get; set; }

    // === Promotional financing (user-annotated for debt payoff integration) ===
    /// <summary>
    /// Indicates this purchase has promotional/introductory financing.
    /// Common with PayPal Credit, store cards, "Buy Now Pay Later" services.
    /// When true, PromoRate and PromoExpirationDate should be set.
    /// </summary>
    public bool HasPromoFinancing { get; set; }

    /// <summary>
    /// Promotional interest rate as decimal (e.g., 0 for "0% APR").
    /// Only applies if HasPromoFinancing is true.
    /// </summary>
    public decimal? PromoRate { get; set; }

    /// <summary>
    /// When the promotional rate expires and reverts to standard APR.
    /// Only applies if HasPromoFinancing is true.
    /// </summary>
    public DateOnly? PromoExpirationDate { get; set; }

    /// <summary>
    /// The standard APR that applies after promo expires.
    /// If null, uses the LinkedAccount's default rate.
    /// </summary>
    public decimal? PostPromoRate { get; set; }

    // === Raw Plaid payload ===
    /// <summary>
    /// Complete Plaid transaction JSON. Contains location, payment_meta,
    /// counterparties, personal_finance_category confidence, and any fields
    /// Plaid adds in the future. Never returned to the client by default —
    /// only used for detail views and debugging.
    /// </summary>
    public string? PlaidRawJson { get; set; }

    // === Audit ===
    public DateTimeOffset SyncedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // === Navigation ===
    public LinkedAccount LinkedAccount { get; set; } = null!;
}
```

### TransactionType Enum

```csharp
/// <summary>
/// Plaid's transaction_type values. Maps 1:1 from Plaid's API — do not add
/// custom values here.
/// See: https://plaid.com/docs/api/products/transactions/#transactions-sync-response-added-transaction-type
/// </summary>
public enum TransactionType
{
    Place,
    Digital,
    Special,
    Unresolved
}
```

### TransactionSource Enum

```csharp
/// <summary>
/// How the transaction was created. Used for tier gating —
/// free-tier users can only see Manual transactions.
/// </summary>
public enum TransactionSource
{
    Plaid,      // Synced from Plaid
    Manual      // Entered by user (future feature)
}
```

### Why These Columns Are Typed (Not in JSON)

| Column | Reason |
|--------|--------|
| `Amount` | Aggregation: spending totals, monthly summaries, net worth |
| `Date` | Primary filter: date range queries on every screen |
| `Name` / `MerchantName` | Search: "show me all Amazon transactions" |
| `PlaidPrimaryCategory` / `PlaidDetailedCategory` | Grouping: spending by category charts |
| `PaymentChannel` | Filter: in-store vs. online |
| `IsPending` | Filter: exclude pending from totals |
| `UserCategoryOverride` | Filter: user's own categorization |
| `IsHidden` | Filter: exclude hidden from views |

### What Stays in `PlaidRawJson`

- `location` (city, region, lat/lng, store_number) — display-only on detail view
- `payment_meta` (reference_number, ppd_id, payee, etc.) — rarely accessed
- `counterparties` (name, type, logo, website, confidence) — display-only
- `personal_finance_category.confidence_level` — informational
- `check_number`, `transaction_code` — edge cases
- Any future Plaid fields — no schema migration needed

---

## Database Schema

### Table: `LinkedAccounts`

```sql
CREATE TABLE LinkedAccounts (
    Id                      uniqueidentifier    NOT NULL DEFAULT NEWSEQUENTIALID(),
    UserId                  nvarchar(128)       NOT NULL,
    TenantId                nvarchar(128)       NOT NULL,
    Name                    nvarchar(200)       NOT NULL,
    OfficialName            nvarchar(200)       NULL,
    InstitutionName         nvarchar(200)       NOT NULL,
    InstitutionId           nvarchar(100)       NULL,
    Type                    nvarchar(20)        NOT NULL,
    Subtype                 nvarchar(30)        NULL,
    Mask                    nvarchar(4)         NULL,
    CurrentBalance          decimal(18,2)       NULL,
    AvailableBalance        decimal(18,2)       NULL,
    IsoCurrencyCode         nvarchar(3)         NOT NULL DEFAULT 'USD',
    PlaidAccessToken        nvarchar(500)       NULL,   -- AES-256 encrypted
    PlaidAccountId          nvarchar(100)       NULL,
    PlaidItemId             nvarchar(100)       NULL,
    PlaidSyncCursor         nvarchar(500)       NULL,
    LastTransactionSyncAt   datetimeoffset      NULL,
    Source                  nvarchar(10)        NOT NULL DEFAULT 'Manual',
    CreatedAt               datetimeoffset      NOT NULL,
    UpdatedAt               datetimeoffset      NULL,

    CONSTRAINT PK_LinkedAccounts PRIMARY KEY (Id),
    CONSTRAINT UQ_LinkedAccounts_PlaidAccountId UNIQUE (PlaidAccountId)
);

-- Primary query pattern: user's accounts
CREATE NONCLUSTERED INDEX IX_LinkedAccounts_User
    ON LinkedAccounts (UserId)
    INCLUDE (Name, InstitutionName, Type, CurrentBalance, Source);

-- Advisor-client sharing: accounts by tenant
CREATE NONCLUSTERED INDEX IX_LinkedAccounts_Tenant
    ON LinkedAccounts (TenantId)
    INCLUDE (UserId, Name, InstitutionName, Type, CurrentBalance);
```

### Table: `Transactions`

```sql
CREATE TABLE Transactions (
    Id                      uniqueidentifier    NOT NULL DEFAULT NEWSEQUENTIALID(),
    UserId                  nvarchar(128)       NOT NULL,
    TenantId                nvarchar(128)       NOT NULL,
    LinkedAccountId         uniqueidentifier    NOT NULL,
    PlaidTransactionId      nvarchar(100)       NOT NULL,
    Amount                  decimal(18,2)       NOT NULL,
    IsoCurrencyCode         nvarchar(3)         NOT NULL DEFAULT 'USD',
    Date                    date                NOT NULL,
    AuthorizedDate          date                NULL,
    Name                    nvarchar(500)       NOT NULL,
    MerchantName            nvarchar(200)       NULL,
    MerchantEntityId        nvarchar(100)       NULL,
    PlaidPrimaryCategory    nvarchar(100)       NULL,
    PlaidDetailedCategory   nvarchar(100)       NULL,
    PlaidCategoryIconUrl    nvarchar(500)       NULL,
    UserCategoryOverride    nvarchar(100)       NULL,
    PaymentChannel          nvarchar(20)        NOT NULL,
    TransactionType         nvarchar(20)        NOT NULL,
    Source                  nvarchar(10)        NOT NULL DEFAULT 'Plaid',
    IsPending               bit                 NOT NULL DEFAULT 0,
    UserNotes               nvarchar(1000)      NULL,
    UserTagsJson            nvarchar(500)       NULL,   -- JSON array: ["tax-deductible","business"]
    IsHidden                bit                 NOT NULL DEFAULT 0,
    PlaidRawJson            nvarchar(max)       NULL,
    SyncedAt                datetimeoffset      NOT NULL,
    UpdatedAt               datetimeoffset      NULL,

    CONSTRAINT PK_Transactions PRIMARY KEY (Id),
    CONSTRAINT FK_Transactions_LinkedAccounts FOREIGN KEY (LinkedAccountId)
        REFERENCES LinkedAccounts(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Transactions_PlaidId UNIQUE (PlaidTransactionId)
);
```

### Indexes

```sql
-- Primary query pattern: user's transactions for an account in a date range.
-- TenantId leads (not UserId) for advisor-client sharing support.
CREATE NONCLUSTERED INDEX IX_Transactions_Tenant_Account_Date
    ON Transactions (TenantId, LinkedAccountId, Date DESC)
    INCLUDE (Amount, Name, MerchantName, PlaidPrimaryCategory, IsPending, IsHidden);

-- Cross-account date queries (e.g., "all transactions this month")
CREATE NONCLUSTERED INDEX IX_Transactions_Tenant_Date
    ON Transactions (TenantId, Date DESC)
    INCLUDE (LinkedAccountId, Amount, Name, MerchantName, PlaidPrimaryCategory);

-- Category aggregation (spending by category)
CREATE NONCLUSTERED INDEX IX_Transactions_Tenant_Category
    ON Transactions (TenantId, PlaidPrimaryCategory)
    INCLUDE (Amount, Date, PlaidDetailedCategory);

-- Merchant search
CREATE NONCLUSTERED INDEX IX_Transactions_Tenant_Merchant
    ON Transactions (TenantId, MerchantName)
    INCLUDE (Amount, Date);

-- Plaid dedup on sync
CREATE UNIQUE NONCLUSTERED INDEX IX_Transactions_PlaidId
    ON Transactions (PlaidTransactionId);

-- Pending transaction cleanup
CREATE NONCLUSTERED INDEX IX_Transactions_Pending
    ON Transactions (LinkedAccountId, IsPending)
    WHERE IsPending = 1;
```

### EF Core Configuration

```csharp
public class LinkedAccountConfiguration : IEntityTypeConfiguration<LinkedAccount>
{
    public void Configure(EntityTypeBuilder<LinkedAccount> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).HasMaxLength(128).IsRequired();
        builder.Property(a => a.TenantId).HasMaxLength(128).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.OfficialName).HasMaxLength(200);
        builder.Property(a => a.InstitutionName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.InstitutionId).HasMaxLength(100);
        builder.Property(a => a.Mask).HasMaxLength(4);
        builder.Property(a => a.CurrentBalance).HasPrecision(18, 2);
        builder.Property(a => a.AvailableBalance).HasPrecision(18, 2);
        builder.Property(a => a.IsoCurrencyCode).HasMaxLength(3);
        builder.Property(a => a.PlaidAccessToken).HasMaxLength(500);
        builder.Property(a => a.PlaidAccountId).HasMaxLength(100);
        builder.Property(a => a.PlaidItemId).HasMaxLength(100);
        builder.Property(a => a.PlaidSyncCursor).HasMaxLength(500);

        builder.Property(a => a.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Subtype)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.Source)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.HasIndex(a => a.UserId).HasDatabaseName("IX_LinkedAccounts_User");
        builder.HasIndex(a => a.TenantId).HasDatabaseName("IX_LinkedAccounts_Tenant");
        builder.HasIndex(a => a.PlaidAccountId)
            .IsUnique()
            .HasFilter("[PlaidAccountId] IS NOT NULL")
            .HasDatabaseName("UQ_LinkedAccounts_PlaidAccountId");

        builder.HasMany(a => a.Transactions)
            .WithOne(t => t.LinkedAccount)
            .HasForeignKey(t => t.LinkedAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

```csharp
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        // String identity columns
        builder.Property(t => t.UserId).HasMaxLength(128).IsRequired();
        builder.Property(t => t.TenantId).HasMaxLength(128).IsRequired();

        // Indexes — TenantId as leading key for advisor-client sharing
        builder.HasIndex(t => new { t.TenantId, t.LinkedAccountId, t.Date })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_Transactions_Tenant_Account_Date");

        builder.HasIndex(t => new { t.TenantId, t.Date })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Transactions_Tenant_Date");

        builder.HasIndex(t => new { t.TenantId, t.PlaidPrimaryCategory })
            .HasDatabaseName("IX_Transactions_Tenant_Category");

        builder.HasIndex(t => new { t.TenantId, t.MerchantName })
            .HasDatabaseName("IX_Transactions_Tenant_Merchant");

        builder.HasIndex(t => t.PlaidTransactionId)
            .IsUnique()
            .HasDatabaseName("IX_Transactions_PlaidId");

        // Column types
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.IsoCurrencyCode).HasMaxLength(3);
        builder.Property(t => t.PlaidTransactionId).HasMaxLength(100);
        builder.Property(t => t.Name).HasMaxLength(500);
        builder.Property(t => t.MerchantName).HasMaxLength(200);
        builder.Property(t => t.MerchantEntityId).HasMaxLength(100);
        builder.Property(t => t.PlaidPrimaryCategory).HasMaxLength(100);
        builder.Property(t => t.PlaidDetailedCategory).HasMaxLength(100);
        builder.Property(t => t.PlaidCategoryIconUrl).HasMaxLength(500);
        builder.Property(t => t.UserCategoryOverride).HasMaxLength(100);
        builder.Property(t => t.PaymentChannel).HasMaxLength(20);
        builder.Property(t => t.UserNotes).HasMaxLength(1000);
        builder.Property(t => t.UserTagsJson).HasMaxLength(500);
        builder.Property(t => t.PlaidRawJson).HasColumnType("nvarchar(max)");

        // Enums as strings
        builder.Property(t => t.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Source)
            .HasConversion<string>()
            .HasMaxLength(10);

        // Relationships
        builder.HasOne(t => t.LinkedAccount)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.LinkedAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // No FK constraint to UserProfile — UserId is a string (Entra Object ID)
        // scoped by middleware, not by DB-level FK. Consistent with Asset entity pattern.
    }
}
```

### Cascade Behavior

| Parent Deleted | Child | Behavior | Rationale |
|----------------|-------|----------|-----------|
| `LinkedAccount` | `Transaction` | **Cascade** | Unlinking an account deletes its transaction history |
| `UserProfile` | `LinkedAccount` | **Restrict** | Users are soft-deleted; accounts retained for audit |
| `UserProfile` | `Transaction` | **Restrict** | Same — retained for audit |

---

## DTOs & Contracts

### LinkedAccountDto

```csharp
/// <summary>
/// Linked account data returned to the client.
/// Excludes PlaidAccessToken, PlaidSyncCursor, and other internal fields.
/// </summary>
public sealed record LinkedAccountDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? OfficialName { get; init; }
    public required string InstitutionName { get; init; }
    public required AccountType Type { get; init; }
    public AccountSubtype? Subtype { get; init; }
    public string? Mask { get; init; }
    public decimal? CurrentBalance { get; init; }
    public decimal? AvailableBalance { get; init; }
    public required string IsoCurrencyCode { get; init; }
    public required AccountSource Source { get; init; }
    public DateTimeOffset? LastSyncAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

### CreateManualAccountRequest

```csharp
/// <summary>
/// Create a manually-entered bank account. Available on Free + Premium tiers.
/// </summary>
public sealed record CreateManualAccountRequest
{
    public required string Name { get; init; }
    public required string InstitutionName { get; init; }
    public required AccountType Type { get; init; }
    public AccountSubtype? Subtype { get; init; }
    public string? Mask { get; init; }
    public decimal? CurrentBalance { get; init; }
    public string IsoCurrencyCode { get; init; } = "USD";
}
```

### TransactionDto (List View)

```csharp
/// <summary>
/// Transaction data returned in list/search views. Excludes PlaidRawJson
/// for payload efficiency.
/// </summary>
public sealed record TransactionDto
{
    public required Guid Id { get; init; }
    public required Guid LinkedAccountId { get; init; }
    public required string AccountName { get; init; }
    public required decimal Amount { get; init; }
    public required string IsoCurrencyCode { get; init; }
    public required DateOnly Date { get; init; }
    public DateOnly? AuthorizedDate { get; init; }
    public required string Name { get; init; }
    public string? MerchantName { get; init; }
    public string? PrimaryCategory { get; init; }
    public string? DetailedCategory { get; init; }
    public string? CategoryIconUrl { get; init; }
    public string? UserCategoryOverride { get; init; }
    public required string PaymentChannel { get; init; }
    public required TransactionType TransactionType { get; init; }
    public required bool IsPending { get; init; }
    public string? UserNotes { get; init; }
    public List<string>? UserTags { get; init; }
    public bool IsHidden { get; init; }
}
```

> **Note**: `Id` remains `Guid` on the DTO (the Transaction's own PK). `UserId`/`TenantId` are **never returned** in DTOs — they are internal scoping fields. `LinkedAccountId` is `Guid` because it's the LinkedAccount's PK.

### TransactionDetailDto (Detail View)

```csharp
/// <summary>
/// Full transaction detail including parsed location and payment metadata
/// extracted from PlaidRawJson. Used for the transaction detail drawer/modal.
/// </summary>
public sealed record TransactionDetailDto : TransactionDto
{
    public TransactionLocationDto? Location { get; init; }
    public TransactionPaymentMetaDto? PaymentMeta { get; init; }
    public List<TransactionCounterpartyDto>? Counterparties { get; init; }
    public string? CheckNumber { get; init; }
    public string? CategoryConfidence { get; init; }
    public DateTimeOffset SyncedAt { get; init; }
}

public sealed record TransactionLocationDto
{
    public string? City { get; init; }
    public string? Region { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? StoreNumber { get; init; }
    public double? Lat { get; init; }
    public double? Lon { get; init; }
}

public sealed record TransactionPaymentMetaDto
{
    public string? ReferenceNumber { get; init; }
    public string? PpdId { get; init; }
    public string? Payee { get; init; }
    public string? ByOrderOf { get; init; }
    public string? Payer { get; init; }
    public string? PaymentMethod { get; init; }
    public string? PaymentProcessor { get; init; }
    public string? Reason { get; init; }
}

public sealed record TransactionCounterpartyDto
{
    public string? Name { get; init; }
    public string? Type { get; init; }
    public string? LogoUrl { get; init; }
    public string? Website { get; init; }
}
```

### UpdateTransactionRequest

```csharp
/// <summary>
/// Update user-editable fields on a transaction.
/// Core financial data (amount, date, merchant) is immutable.
/// </summary>
public sealed record UpdateTransactionRequest
{
    public string? UserCategoryOverride { get; init; }
    public string? UserNotes { get; init; }
    public List<string>? UserTags { get; init; }
    public bool? IsHidden { get; init; }
}
```

### TransactionSummaryDto

```csharp
/// <summary>
/// Aggregated transaction statistics for a time period.
/// </summary>
/// <remarks>
/// Income vs. Expense classification:
/// - Plaid amounts are POSITIVE for debits (money out) and NEGATIVE for credits (money in).
/// - TotalIncome = SUM of transactions where Amount &lt; 0 (negated to show as positive).
/// - TotalExpenses = SUM of transactions where Amount &gt; 0.
/// - NetCashFlow = TotalIncome - TotalExpenses.
/// - Pending transactions (IsPending = true) are EXCLUDED from summaries.
/// - Hidden transactions (IsHidden = true) are EXCLUDED from summaries.
/// </remarks>
public sealed record TransactionSummaryDto
{
    public decimal TotalIncome { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal NetCashFlow { get; init; }
    public int TransactionCount { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public List<CategoryBreakdownDto> SpendingByCategory { get; init; } = [];
    public List<MerchantBreakdownDto> TopMerchants { get; init; } = [];
}

public sealed record CategoryBreakdownDto
{
    public string Category { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public int Count { get; init; }
    public decimal Percentage { get; init; }
}

public sealed record MerchantBreakdownDto
{
    public string MerchantName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public int Count { get; init; }
}
```

### PaginatedTransactionsDto

```csharp
/// <summary>
/// Cursor-based paginated response for transaction lists.
/// Cursor format: Base64(Date|Id) — opaque to the client.
/// </summary>
public sealed record PaginatedTransactionsDto
{
    public List<TransactionDto> Transactions { get; init; } = [];
    public int TotalCount { get; init; }
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
}
```

### TypeScript Interfaces

```typescript
interface LinkedAccountDto {
  id: string;
  name: string;
  officialName?: string;
  institutionName: string;
  type: AccountType;
  subtype?: AccountSubtype;
  mask?: string;
  currentBalance?: number;
  availableBalance?: number;
  isoCurrencyCode: string;
  source: AccountSource;
  lastSyncAt?: string;
  createdAt: string;
}

type AccountType = "Checking" | "Savings" | "CreditCard" | "Investment" | "Loan" | "Mortgage" | "Other";
type AccountSource = "Plaid" | "Manual";

interface TransactionDto {
  id: string;
  linkedAccountId: string;
  accountName: string;
  amount: number;
  isoCurrencyCode: string;
  date: string;              // ISO date (YYYY-MM-DD)
  authorizedDate?: string;
  name: string;
  merchantName?: string;
  primaryCategory?: string;
  detailedCategory?: string;
  categoryIconUrl?: string;
  userCategoryOverride?: string;
  paymentChannel: string;
  transactionType: TransactionType;
  isPending: boolean;
  userNotes?: string;
  userTags?: string[];
  isHidden: boolean;
  // Promotional financing (for debt payoff integration)
  hasPromoFinancing: boolean;
  promoRate?: number;              // 0 for "0% APR"
  promoExpirationDate?: string;    // ISO date
  postPromoRate?: number;          // Rate after promo expires
}

/** Maps 1:1 from Plaid's transaction_type field. Do not add custom values. */
type TransactionType = "Place" | "Digital" | "Special" | "Unresolved";

interface TransactionSummaryDto {
  totalIncome: number;       // SUM where Plaid amount < 0 (credits), shown as positive
  totalExpenses: number;     // SUM where Plaid amount > 0 (debits)
  netCashFlow: number;       // totalIncome - totalExpenses
  transactionCount: number;
  periodStart: string;
  periodEnd: string;
  spendingByCategory: CategoryBreakdownDto[];
  topMerchants: MerchantBreakdownDto[];
}

interface UpdateTransactionRequest {
  userCategoryOverride?: string;
  userNotes?: string;
  userTags?: string[];
  isHidden?: boolean;
  // Promotional financing fields
  hasPromoFinancing?: boolean;
  promoRate?: number;
  promoExpirationDate?: string;
  postPromoRate?: number;
}

interface PaginatedTransactionsDto {
  transactions: TransactionDto[];
  totalCount: number;
  nextCursor?: string;
  hasMore: boolean;
}
```

---

## API Endpoints

### Account Endpoints

```
GET    /api/accounts                       → GetAccounts
GET    /api/accounts/{id}                  → GetAccountById
POST   /api/accounts                       → CreateManualAccount
PUT    /api/accounts/{id}                  → UpdateManualAccount
DELETE /api/accounts/{id}                  → DeleteAccount (cascade deletes transactions)
POST   /api/accounts/link                  → LinkPlaidAccount (Premium only)
POST   /api/accounts/{id}/refresh          → RefreshAccountTransactions (Premium only)
```

### Transaction Endpoints

```
GET    /api/transactions                   → GetTransactions
GET    /api/transactions/{id}              → GetTransactionById
GET    /api/transactions/summary           → GetTransactionSummary
PATCH  /api/transactions/{id}              → UpdateTransaction (annotations only)
GET    /api/accounts/{accountId}/transactions → GetAccountTransactions
POST   /api/transactions/sync              → SyncTransactions (internal/admin)
```

### Query Parameters for `GET /api/transactions`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `accountId` | `Guid?` | null | Filter by linked account |
| `from` | `DateOnly?` | 30 days ago | Start date (inclusive) |
| `to` | `DateOnly?` | today | End date (inclusive) |
| `category` | `string?` | null | Filter by primary category |
| `merchantName` | `string?` | null | Search by merchant (contains) |
| `minAmount` | `decimal?` | null | Minimum amount (absolute value) |
| `maxAmount` | `decimal?` | null | Maximum amount (absolute value) |
| `isPending` | `bool?` | null | Filter pending/posted |
| `includeHidden` | `bool` | false | Include hidden transactions |
| `search` | `string?` | null | Full-text search on name + merchant |
| `cursor` | `string?` | null | Pagination cursor (opaque Base64) |
| `limit` | `int` | 50 | Page size (max 200) |

### Query Parameters for `GET /api/transactions/summary`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `accountId` | `Guid?` | null | Filter by linked account |
| `from` | `DateOnly` | **required** | Period start |
| `to` | `DateOnly` | **required** | Period end |
| `topMerchants` | `int` | 10 | Number of top merchants to include |

---

## Service Layer

### IAccountService

```csharp
/// <summary>
/// Provides CRUD operations for linked bank accounts and Plaid integration.
/// </summary>
public interface IAccountService
{
    Task<IReadOnlyList<LinkedAccountDto>> GetAccountsAsync(
        string requestingUserId, string ownerUserId);

    Task<LinkedAccountDto> GetAccountByIdAsync(
        string requestingUserId, Guid accountId);

    Task<LinkedAccountDto> CreateManualAccountAsync(
        string userId, CreateManualAccountRequest request);

    Task<LinkedAccountDto> UpdateManualAccountAsync(
        string userId, Guid accountId, CreateManualAccountRequest request);

    Task DeleteAccountAsync(string userId, Guid accountId);

    /// <summary>
    /// Links a Plaid account using the public token from Plaid Link.
    /// Premium only — throws TIER_PREMIUM_REQUIRED for free-tier users.
    /// </summary>
    Task<LinkedAccountDto> LinkPlaidAccountAsync(
        string userId, string publicToken, string plaidAccountId);

    /// <summary>
    /// Triggers a manual transaction refresh for a Plaid-linked account.
    /// </summary>
    Task RefreshAccountAsync(string userId, Guid accountId);
}
```

### ITransactionService

```csharp
/// <summary>
/// Provides query and annotation operations for transactions.
/// All read operations enforce three-tier authorization
/// (Owner → DataAccessGrant → Administrator) via IAuthorizationService.
/// Data Category: DataCategories.Accounts
/// </summary>
public interface ITransactionService
{
    // === Queries ===

    Task<PaginatedTransactionsDto> GetTransactionsAsync(
        string requestingUserId,
        string ownerUserId,
        TransactionQueryParameters parameters);

    Task<TransactionDetailDto> GetTransactionByIdAsync(
        string requestingUserId,
        Guid transactionId);

    Task<TransactionSummaryDto> GetSummaryAsync(
        string requestingUserId,
        string ownerUserId,
        TransactionSummaryParameters parameters);

    // === User annotations ===

    Task<TransactionDto> UpdateTransactionAsync(
        string requestingUserId,
        Guid transactionId,
        UpdateTransactionRequest request);

    // === Sync (called by PlaidService, not by API functions directly) ===

    Task<TransactionSyncResult> UpsertFromPlaidAsync(
        string userId,
        Guid linkedAccountId,
        IReadOnlyList<PlaidTransactionData> added,
        IReadOnlyList<PlaidTransactionData> modified,
        IReadOnlyList<string> removed);
}

public sealed record TransactionSyncResult
{
    public int Added { get; init; }
    public int Modified { get; init; }
    public int Removed { get; init; }
}
```

### TransactionQueryParameters

```csharp
public sealed record TransactionQueryParameters
{
    public Guid? LinkedAccountId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public string? Category { get; init; }
    public string? MerchantName { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public bool? IsPending { get; init; }
    public TransactionSource? SourceFilter { get; init; }
    public bool IncludeHidden { get; init; }
    public string? Search { get; init; }
    public string? Cursor { get; init; }
    public int Limit { get; init; } = 50;
}
```

---

## Plaid Sync Strategy

### Initial Sync (After Account Link)

```
User links account via Plaid Link (Premium)
  → ExchangePublicToken()
    → PlaidService stores LinkedAccount (encrypted access token)
    → PlaidService calls Plaid Transactions Sync API (initial — no cursor)
    → TransactionService.UpsertFromPlaidAsync() — bulk insert
    → Store Plaid sync cursor on LinkedAccount for incremental syncs
```

### Incremental Sync (Ongoing)

```
Plaid webhook: SYNC_UPDATES_AVAILABLE
  → PlaidWebhook function receives webhook
    → PlaidService calls Plaid Transactions Sync API (with stored cursor)
    → Response contains: added[], modified[], removed[]
    → TransactionService.UpsertFromPlaidAsync()
      - added: INSERT new rows
      - modified: UPDATE existing rows (pending → posted, amount corrections)
      - removed: DELETE rows (Plaid removed/corrected the transaction)
    → Update sync cursor on LinkedAccount
```

### Manual Refresh

```
User clicks "Refresh" on account
  → POST /api/accounts/{id}/refresh
    → PlaidService calls Plaid Transactions Sync API
    → Same UpsertFromPlaidAsync() flow
```

### Sync Cursor Storage

The cursor is stored on `LinkedAccount.PlaidSyncCursor`. Each Plaid sync call returns a new cursor; the stored cursor is passed to the next call for incremental updates.

### Plaid Transactions Sync API Call

```csharp
/// <summary>
/// Calls Plaid's /transactions/sync endpoint for incremental updates.
/// Uses cursor-based pagination — each call returns a cursor for the next call.
/// </summary>
public async Task SyncTransactionsForAccountAsync(string userId, Guid linkedAccountId)
{
    var account = await GetLinkedAccountAsync(userId, linkedAccountId);
    var accessToken = _encryptionService.Decrypt(account.PlaidAccessToken);
    var cursor = account.PlaidSyncCursor;
    var hasMore = true;

    var allAdded = new List<PlaidTransactionData>();
    var allModified = new List<PlaidTransactionData>();
    var allRemoved = new List<string>();

    while (hasMore)
    {
        var response = await _plaidClient.TransactionsSyncAsync(new TransactionsSyncRequest
        {
            AccessToken = accessToken,
            Cursor = cursor,
            Count = 500  // Max per page
        });

        allAdded.AddRange(MapTransactions(response.Added));
        allModified.AddRange(MapTransactions(response.Modified));
        allRemoved.AddRange(response.Removed.Select(r => r.TransactionId));

        cursor = response.NextCursor;
        hasMore = response.HasMore;
    }

    // Upsert into our database
    var result = await _transactionService.UpsertFromPlaidAsync(
        userId, linkedAccountId, allAdded, allModified, allRemoved);

    // Store cursor for next incremental sync
    account.PlaidSyncCursor = cursor;
    account.LastTransactionSyncAt = DateTimeOffset.UtcNow;
    await _accountRepository.UpdateAsync(account);

    _logger.LogInformation(
        "Transaction sync complete for account {AccountId}: +{Added} ~{Modified} -{Removed}",
        linkedAccountId, result.Added, result.Modified, result.Removed);
}
```

### Upsert Logic (Handling Pending → Posted)

```csharp
public async Task<TransactionSyncResult> UpsertFromPlaidAsync(
    string userId, Guid linkedAccountId,
    IReadOnlyList<PlaidTransactionData> added,
    IReadOnlyList<PlaidTransactionData> modified,
    IReadOnlyList<string> removed)
{
    var addedCount = 0;
    var modifiedCount = 0;
    var removedCount = 0;

    // 1. Remove deleted transactions
    if (removed.Count > 0)
    {
        removedCount = await _dbContext.Transactions
            .Where(t => removed.Contains(t.PlaidTransactionId))
            .ExecuteDeleteAsync();
    }

    // 2. Add new transactions (bulk insert)
    if (added.Count > 0)
    {
        var newTransactions = added.Select(p => MapToEntity(userId, linkedAccountId, p)).ToList();
        await _dbContext.Transactions.AddRangeAsync(newTransactions);
        addedCount = newTransactions.Count;
    }

    // 3. Update modified transactions (pending → posted, amount corrections)
    foreach (var mod in modified)
    {
        var existing = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.PlaidTransactionId == mod.PlaidTransactionId);

        if (existing != null)
        {
            // Update core fields but PRESERVE user annotations
            existing.Amount = mod.Amount;
            existing.Date = mod.Date;
            existing.AuthorizedDate = mod.AuthorizedDate;
            existing.Name = mod.Name;
            existing.MerchantName = mod.MerchantName;
            existing.PlaidPrimaryCategory = mod.PrimaryCategory;
            existing.PlaidDetailedCategory = mod.DetailedCategory;
            existing.IsPending = mod.IsPending;
            existing.PlaidRawJson = mod.RawJson;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            modifiedCount++;
        }
    }

    await _dbContext.SaveChangesAsync();

    return new TransactionSyncResult
    {
        Added = addedCount,
        Modified = modifiedCount,
        Removed = removedCount
    };
}
```

---

## Tier Gating & Access Control

### Subscription Tiers

| Capability | Free Tier | Premium Tier |
|------------|-----------|-------------|
| Manual accounts | Up to 3 | Unlimited |
| Plaid account linking | Not available | Up to 10 linked accounts |
| Transaction history (Plaid) | Not available | Full history (~2 years from Plaid) |
| Manual transaction entry | Up to 3 months of history | Unlimited |
| Transaction annotations | Available on manual entries | Available on all |
| Spending summary/charts | Manual entries only | All transactions |
| CSV export | Not available | Available |

### Enforcement Points

**API layer** — `ITransactionService` checks subscription tier:

```csharp
// In TransactionService
public async Task<PaginatedTransactionsDto> GetTransactionsAsync(
    string requestingUserId, string ownerUserId, TransactionQueryParameters parameters)
{
    // 1. Authorization check (three-tier)
    await _authorizationService.CheckAccessAsync(
        requestingUserId, ownerUserId, DataCategories.Accounts, AccessType.Read);

    // 2. Tier check — free users can only see manual transactions
    var tier = await _subscriptionService.GetTierAsync(ownerUserId);
    if (tier == SubscriptionTier.Free)
    {
        parameters = parameters with
        {
            SourceFilter = TransactionSource.Manual,
            From = parameters.From ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
        };
    }

    // 3. Execute query...
}
```

**Plaid sync** — Reject free-tier users:

```csharp
public async Task SyncTransactionsForAccountAsync(string userId, Guid linkedAccountId)
{
    var tier = await _subscriptionService.GetTierAsync(userId);
    if (tier == SubscriptionTier.Free)
        throw new BusinessRuleException(TransactionErrorCodes.TIER_PREMIUM_REQUIRED,
            "Plaid transaction sync requires a Premium subscription");

    // ... proceed with sync
}
```

**Account creation** — Enforce manual account limit:

```csharp
public async Task<LinkedAccountDto> CreateManualAccountAsync(
    string userId, CreateManualAccountRequest request)
{
    var tier = await _subscriptionService.GetTierAsync(userId);
    if (tier == SubscriptionTier.Free)
    {
        var existingCount = await _dbContext.LinkedAccounts
            .CountAsync(a => a.UserId == userId && a.Source == AccountSource.Manual);
        if (existingCount >= 3)
            throw new BusinessRuleException(TransactionErrorCodes.TIER_LIMIT_REACHED,
                "Free tier allows up to 3 manual accounts");
    }

    // ... create account
}
```

**UI layer** — Free-tier users see:
- Transactions page with empty state: "Upgrade to Premium to link your bank accounts and see transactions automatically"
- Manual transaction entry form as the free-tier alternative
- Upgrade CTA button linking to subscription management

### Free-Tier History Limit

Free-tier manual transactions are **not deleted** after 3 months — the 3-month limit applies to **query results**, not storage. If a user upgrades to Premium, their full manual history becomes visible immediately.

---

## Security & Privacy

### Access Control

| Operation | Authorization | Notes |
|-----------|--------------|-------|
| Read accounts | Owner + DataAccessGrant(Accounts, Read) + Administrator | Standard three-tier |
| Create/update/delete account | Owner + Administrator | No grant-based write |
| Read transactions | Owner + DataAccessGrant(Accounts, Read) + Administrator | Standard three-tier |
| Update annotations | Owner + DataAccessGrant(Accounts, Full) + Administrator | Only user-editable fields |
| Sync transactions | System-initiated only | Triggered by Plaid webhook or account refresh |
| Delete transactions | Not exposed | Only via Plaid sync (removed[]) or account unlink cascade |

### Data Classification

| Field | Classification | Treatment |
|-------|---------------|-----------|
| `PlaidAccessToken` | Secret | AES-256 encrypted, Key Vault, never logged, never returned |
| `PlaidTransactionId` | Internal identifier | Never returned to client |
| `PlaidRawJson` | Sensitive PII | Never returned in list views; detail view only |
| `MerchantEntityId` | Internal identifier | Never returned to client |
| `Amount`, `Name`, `MerchantName` | Financial data | Returned to authorized users |
| `UserNotes`, `UserTags` | User-generated | Returned to authorized users |

### Plaid Access Token Security

- Plaid access tokens are **encrypted at rest** via `IEncryptionService` (AES-256, keys in Azure Key Vault)
- Access tokens are stored on `LinkedAccount`, **never on Transaction**
- Access tokens are **never logged** — even in debug mode

### Audit Logging

```csharp
// Log account operations
_auditLogger.LogDataModification(
    action: "AccountLinked",
    resourceType: "LinkedAccount",
    resourceId: accountId,
    userId: userId,
    details: new { Source = "Plaid", InstitutionName = institution }
);

// Log sync operations
_auditLogger.LogDataModification(
    action: "TransactionSync",
    resourceType: "LinkedAccount",
    resourceId: linkedAccountId,
    userId: userId,
    details: new { Added = result.Added, Modified = result.Modified, Removed = result.Removed }
);

// Log user annotation updates
_auditLogger.LogDataModification(
    action: "TransactionAnnotated",
    resourceType: "Transaction",
    resourceId: transactionId,
    userId: requestingUserId,
    details: new { Fields = changedFields }
);
```

### Data Retention

| Policy | Duration | Rationale |
|--------|----------|-----------|
| Premium: Plaid-synced transactions | Indefinite (while account linked) | User expects full history |
| Premium: Unlinked account transactions | **Cascade-deleted** with LinkedAccount | No orphaned financial data |
| Free: Manual transactions | Stored indefinitely; **queried** with 3-month window | Upgrade path preserves data |
| `PlaidRawJson` | Consider TTL of 24 months | Reduce storage; core fields are typed columns |
| Pending transactions | Auto-removed by Plaid sync | Plaid replaces pending with posted |

---

## Validation Rules

| Rule | Level | Notes |
|------|-------|-------|
| `from` must be before `to` in date range queries | Error | |
| `limit` must be 1–200 | Error | Default 50 |
| `UserNotes` max 1000 chars | Error | |
| `UserTagsJson` max 500 chars total, max 20 tags | Error | JSON array of strings |
| `UserCategoryOverride` max 100 chars | Error | |
| Cannot modify core financial fields (amount, date, name) | Error | 400 with `IMMUTABLE_FIELD` |
| Date range max span: 2 years per query | Error | Prevents full-table scans |
| Account `Name` required, max 200 chars | Error | |
| Account `InstitutionName` required, max 200 chars | Error | |
| Account `Mask` max 4 chars | Error | Last 4 digits only |
| Free tier: max 3 manual accounts | Error | `TIER_LIMIT_REACHED` |
| Plaid linking requires Premium | Error | `TIER_PREMIUM_REQUIRED` |

---

## Error Codes

```csharp
public static class TransactionErrorCodes
{
    public const string NOT_FOUND = "TRANSACTION_NOT_FOUND";
    public const string SYNC_FAILED = "TRANSACTION_SYNC_FAILED";
    public const string SYNC_IN_PROGRESS = "TRANSACTION_SYNC_IN_PROGRESS";
    public const string ACCOUNT_NOT_LINKED = "ACCOUNT_NOT_LINKED";
    public const string INVALID_DATE_RANGE = "TRANSACTION_INVALID_DATE_RANGE";
    public const string CATEGORY_NOT_FOUND = "TRANSACTION_CATEGORY_NOT_FOUND";
    public const string TIER_PREMIUM_REQUIRED = "TIER_PREMIUM_REQUIRED";
    public const string TIER_LIMIT_REACHED = "TIER_LIMIT_REACHED";
    public const string IMMUTABLE_FIELD = "TRANSACTION_IMMUTABLE_FIELD";
}

public static class AccountErrorCodes
{
    public const string NOT_FOUND = "ACCOUNT_NOT_FOUND";
    public const string ALREADY_LINKED = "ACCOUNT_ALREADY_LINKED";
    public const string LINK_FAILED = "ACCOUNT_LINK_FAILED";
    public const string NOT_PLAID = "ACCOUNT_NOT_PLAID";
    public const string TIER_PREMIUM_REQUIRED = "TIER_PREMIUM_REQUIRED";
    public const string TIER_LIMIT_REACHED = "TIER_LIMIT_REACHED";
}
```

---

## UI Design

### Account List Page

> **Route**: `/accounts`
> **Auth Policy**: `RequireClient`
> **Layout**: `DashboardLayout` (sidebar)

```tsx
<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
  {accounts.map(account => (
    <Card key={account.id}>
      <CardHeader className="flex flex-row items-center gap-3">
        <InstitutionLogo name={account.institutionName} className="h-10 w-10" />
        <div className="flex-1 min-w-0">
          <CardTitle className="text-base truncate">{account.name}</CardTitle>
          <p className="text-sm text-muted-foreground">{account.institutionName}</p>
        </div>
        {account.source === "Plaid" && (
          <Badge variant="outline" className="text-emerald-500 shrink-0">
            {t('accounts.status.linked')}
          </Badge>
        )}
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-mono font-semibold">
          {formatCurrency(account.currentBalance)}
        </div>
        <p className="text-xs text-muted-foreground mt-1">
          {account.lastSyncAt
            ? t('accounts.lastSync', { date: formatRelative(account.lastSyncAt) })
            : t('accounts.manual')}
        </p>
      </CardContent>
    </Card>
  ))}
</div>
```

### Transaction List Page

> **Route**: `/transactions`
> **Auth Policy**: `RequireClient`
> **Layout**: `DashboardLayout` (sidebar)

#### Summary Cards Row

| Card | Data | Icon |
|------|------|------|
| Income (This Month) | `summary.totalIncome` | `ArrowDownLeft` (green) |
| Spending (This Month) | `summary.totalExpenses` | `ArrowUpRight` (red) |
| Net Cash Flow | `summary.netCashFlow` | `TrendingUp` / `TrendingDown` |
| Transactions | `summary.transactionCount` | `Receipt` |

#### Filter Bar

```tsx
<div className="flex flex-col md:flex-row gap-3" role="search" aria-label={t('transactions.filters.label')}>
  {/* Account selector */}
  <Select value={accountId} onValueChange={setAccountId}>
    <SelectTrigger aria-label={t('transactions.filters.account')}>
      <SelectValue placeholder={t('transactions.filters.allAccounts')} />
    </SelectTrigger>
    <SelectContent>
      <SelectItem value="all">{t('transactions.filters.allAccounts')}</SelectItem>
      {accounts.map(a => <SelectItem key={a.id} value={a.id}>{a.name}</SelectItem>)}
    </SelectContent>
  </Select>

  {/* Date range */}
  <DateRangePicker from={dateFrom} to={dateTo} onChange={setDateRange}
    aria-label={t('transactions.filters.dateRange')} />

  {/* Category filter */}
  <Select value={category} onValueChange={setCategory}>
    <SelectTrigger aria-label={t('transactions.filters.category')}>
      <SelectValue placeholder={t('transactions.filters.allCategories')} />
    </SelectTrigger>
    <SelectContent>
      {categories.map(c => <SelectItem key={c} value={c}>{c}</SelectItem>)}
    </SelectContent>
  </Select>

  {/* Search */}
  <div className="relative flex-1">
    <label htmlFor="transaction-search" className="sr-only">{t('transactions.filters.search')}</label>
    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" aria-hidden="true" />
    <Input id="transaction-search" placeholder={t('transactions.filters.searchPlaceholder')}
           className="pl-9" value={search} onChange={e => setSearch(e.target.value)} />
  </div>
</div>
```

#### Transaction List (Desktop)

```tsx
<Table className="hidden md:table" aria-label={t('transactions.table.label')}>
  <TableHeader>
    <TableRow>
      <TableHead>{t('transactions.table.date')}</TableHead>
      <TableHead>{t('transactions.table.description')}</TableHead>
      <TableHead>{t('transactions.table.category')}</TableHead>
      <TableHead>{t('transactions.table.account')}</TableHead>
      <TableHead className="text-right">{t('transactions.table.amount')}</TableHead>
      <TableHead className="w-10"><span className="sr-only">{t('transactions.table.actions')}</span></TableHead>
    </TableRow>
  </TableHeader>
  <TableBody>
    {transactions.map(tx => (
      <TableRow key={tx.id} className={tx.isPending ? "opacity-60" : ""}>
        <TableCell className="text-muted-foreground text-sm">
          {formatDate(tx.date)}
        </TableCell>
        <TableCell>
          <div className="flex items-center gap-3">
            <div className="h-8 w-8 rounded-full bg-muted flex items-center justify-center">
              <CategoryIcon category={tx.primaryCategory} />
            </div>
            <div>
              <div className="font-medium">{tx.merchantName ?? tx.name}</div>
              {tx.isPending && (
                <Badge variant="outline" className="text-amber-500 text-xs">
                  {t('transactions.status.pending')}
                </Badge>
              )}
            </div>
          </div>
        </TableCell>
        <TableCell>
          <Badge variant="secondary">
            {tx.userCategoryOverride ?? tx.primaryCategory ?? t('transactions.category.uncategorized')}
          </Badge>
        </TableCell>
        <TableCell className="text-muted-foreground text-sm">{tx.accountName}</TableCell>
        <TableCell className={cn("text-right font-mono font-medium",
          tx.amount < 0 ? "text-emerald-500" : "text-foreground"
        )}>
          {formatCurrency(tx.amount)}
        </TableCell>
        <TableCell>
          <TransactionActions transaction={tx}
            aria-label={t('transactions.actions.menuLabel', { name: tx.merchantName ?? tx.name })} />
        </TableCell>
      </TableRow>
    ))}
  </TableBody>
</Table>
```

> **Note**: Plaid amounts are positive for debits (money out) and negative for credits (money in). The UI inverts: negative → green (income), positive → neutral (expense).

#### Transaction List (Mobile)

```tsx
<div className="md:hidden space-y-2">
  {transactions.map(tx => (
    <Card key={tx.id} className={tx.isPending ? "opacity-60" : ""}>
      <CardContent className="flex items-center gap-3 p-3">
        <div className="h-10 w-10 rounded-full bg-muted flex items-center justify-center shrink-0">
          <CategoryIcon category={tx.primaryCategory} />
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-medium truncate">{tx.merchantName ?? tx.name}</div>
          <div className="text-xs text-muted-foreground">
            {formatDate(tx.date)} · {tx.accountName}
          </div>
        </div>
        <div className={cn("font-mono font-medium text-sm",
          tx.amount < 0 ? "text-emerald-500" : "text-foreground"
        )}>
          {formatCurrency(tx.amount)}
        </div>
      </CardContent>
    </Card>
  ))}
</div>
```

#### Transaction Detail (Sheet/Drawer)

Clicking a transaction opens a side sheet with full details:

```
┌──────────────────────────────────────┐
│  ← Back                       ⋯     │
│                                      │
│  ┌────────────────────────────────┐  │
│  │  🛒  Amazon.com               │  │
│  │  -$47.99                      │  │
│  │  February 15, 2026            │  │
│  └────────────────────────────────┘  │
│                                      │
│  Details                             │
│  ─────────────────────────────────   │
│  Account:    Chase Checking          │
│  Category:   Shopping > Online       │
│  Channel:    Online                  │
│  Status:     Posted                  │
│  Auth Date:  February 14, 2026       │
│                                      │
│  Location                            │
│  ─────────────────────────────────   │
│  Seattle, WA                         │
│                                      │
│  Your Notes                          │
│  ─────────────────────────────────   │
│  ┌────────────────────────────────┐  │
│  │ Office supplies for Q1        │  │
│  └────────────────────────────────┘  │
│                                      │
│  Category:  [Shopping ▾] (override)  │
│  Tags:      [tax-deductible] [+]     │
│  Hidden:    [ ] Hide from reports    │
│                                      │
│  [Save Changes]                      │
└──────────────────────────────────────┘
```

#### Spending by Category Chart

```tsx
<Card>
  <CardHeader>
    <CardTitle>{t('transactions.charts.spendingByCategory')}</CardTitle>
  </CardHeader>
  <CardContent>
    {/* Screen reader alternative */}
    <div className="sr-only" role="table" aria-label={t('transactions.charts.spendingByCategory')}>
      <div role="rowgroup">
        {summary.spendingByCategory.map(entry => (
          <div key={entry.category} role="row">
            <span role="cell">{entry.category}</span>
            <span role="cell">{formatCurrency(entry.amount)} ({entry.percentage}%)</span>
          </div>
        ))}
      </div>
    </div>
    <ResponsiveContainer width="100%" height={300} aria-hidden="true">
      <PieChart>
        <Pie data={summary.spendingByCategory} dataKey="amount" nameKey="category"
             cx="50%" cy="50%" innerRadius={60} outerRadius={100}>
          {summary.spendingByCategory.map((entry, i) => (
            <Cell key={i} fill={CATEGORY_COLORS[entry.category] ?? CATEGORY_COLORS.default} />
          ))}
        </Pie>
        <Tooltip formatter={formatCurrency} />
        <Legend />
      </PieChart>
    </ResponsiveContainer>
  </CardContent>
</Card>
```

### TanStack Query Hooks

```typescript
/** Fetches all linked accounts for the current user. */
function useAccounts() {
  return useQuery({
    queryKey: ["accounts"],
    queryFn: () => api.get<LinkedAccountDto[]>("/accounts"),
  });
}

/** Fetches paginated transactions with filters (infinite scroll). */
function useTransactions(params: TransactionQueryParams) {
  return useInfiniteQuery({
    queryKey: ["transactions", params],
    queryFn: ({ pageParam }) =>
      api.get<PaginatedTransactionsDto>("/transactions", {
        params: { ...params, cursor: pageParam },
      }),
    getNextPageParam: (lastPage) =>
      lastPage.hasMore ? lastPage.nextCursor : undefined,
    initialPageParam: undefined as string | undefined,
  });
}

/** Fetches a single transaction with full detail. */
function useTransaction(id: string) {
  return useQuery({
    queryKey: ["transactions", id],
    queryFn: () => api.get<TransactionDetailDto>(`/transactions/${id}`),
    enabled: !!id,
  });
}

/** Fetches spending summary for a date range. */
function useTransactionSummary(params: { from: string; to: string; accountId?: string }) {
  return useQuery({
    queryKey: ["transactions", "summary", params],
    queryFn: () => api.get<TransactionSummaryDto>("/transactions/summary", { params }),
  });
}

/** Updates user annotations on a transaction. */
function useUpdateTransaction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: UpdateTransactionRequest & { id: string }) =>
      api.patch<TransactionDto>(`/transactions/${id}`, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["transactions"] });
      queryClient.invalidateQueries({ queryKey: ["transactions", variables.id] });
    },
  });
}
```

### Navigation Integration

Add to `DashboardLayout` sidebar:

```typescript
{ label: "Accounts", icon: Landmark, href: "/accounts" },
{ label: "Transactions", icon: Receipt, href: "/transactions" },
```

Add routes to `App.tsx`:

```tsx
<Route path="/accounts" element={
  <ProtectedRoute policy="RequireClient"><Accounts /></ProtectedRoute>
} />
<Route path="/transactions" element={
  <ProtectedRoute policy="RequireClient"><Transactions /></ProtectedRoute>
} />
```

---

## Performance & Scaling

### Expected Volumes

| Metric | Estimate | Notes |
|--------|----------|-------|
| Transactions per user/month | ~200–500 | Typical consumer |
| Transactions per user/year | ~3,000–6,000 | |
| Transactions per user (5-year) | ~15,000–30,000 | Still manageable in SQL |
| Total transactions at 10K users | ~30–60M | May need partitioning at this scale |

### SQL is Sufficient Because

- Personal finance volumes are modest — not e-commerce or banking-scale
- Azure SQL supports up to 4TB per database, billions of rows
- Composite indexes on `(TenantId, LinkedAccountId, Date DESC)` make the primary query pattern fast
- Cursor-based pagination avoids `OFFSET` performance degradation
- `PlaidRawJson` is `nvarchar(max)` — stored out-of-row automatically by SQL Server for large values

### When to Consider Cosmos DB

Move the `Transactions` table to Cosmos DB **only if**:
- Query latency on the primary access pattern exceeds 100ms at p95
- Storage costs for `PlaidRawJson` become significant
- User count exceeds ~50K active users with multi-year history

**Migration path**: Typed columns map naturally to a Cosmos DB schema. Partition key = `UserId`, sort key = `Date`. The rest of the app (assets, contacts, etc.) stays in Azure SQL.

### Caching Strategy

| Data | Cache | TTL | Invalidation |
|------|-------|-----|-------------|
| Transaction list (paginated) | None | — | Data changes too frequently |
| Transaction summary | Redis | 15 min | Invalidate on sync or annotation |
| Category list (distinct) | Redis | 1 hour | Invalidate on sync |
| Account transaction counts | Redis | 15 min | Invalidate on sync |
| Account list | Redis | 5 min | Invalidate on create/update/delete |

---

## Future Considerations

| Feature | Priority | Notes |
|---------|----------|-------|
| Recurring transaction detection | P2 | Group by merchant + similar amount + regular interval |
| Budget tracking | P2 | Set spending limits per category, alert when exceeded |
| Transaction rules | P2 | Auto-categorize based on merchant patterns |
| Receipt image attachment | P3 | Link photos to transactions via Azure Blob Storage |
| CSV export | P2 | Export filtered transactions as CSV (Premium) |
| Investment transactions | P2 | Plaid `/investments/transactions` — different schema, separate table |
| Transaction search (full-text) | P2 | Azure SQL full-text index or Azure Cognitive Search |
| Cosmos DB migration | P3 | Only if SQL performance degrades at scale |
| Manual transaction entry UI | P1 | Free-tier alternative to Plaid — basic transaction form |

---

## Cross-References

- Authorization model (3-tier, DataAccessGrant): [03-authorization-data-access.md](03-authorization-data-access.md)
- Asset types & portfolio (FinancialAccount → BankAccount linking): [05-assets-portfolio.md](05-assets-portfolio.md)
- Contact model (no direct relationship — transactions don't link to contacts): [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md)
- Full transaction storage specification (master reference): [../TRANSACTION_STORAGE_SPECIFICATION.md](../TRANSACTION_STORAGE_SPECIFICATION.md)
- Platform infrastructure & tier model: [01-platform-infrastructure.md](01-platform-infrastructure.md)
- Dashboard & reporting (consumes TransactionSummaryDto): [07-dashboard-reporting.md](07-dashboard-reporting.md)
- User profile & settings (subscription tier management): [10-user-profile-settings.md](10-user-profile-settings.md)

---

*Last Updated: February 2026*
