# Transaction Storage Specification

> **Last Updated**: February 27, 2026
> **Status**: Draft
> **Depends On**: LinkedAccount entity (not yet implemented), Plaid integration (not yet implemented), UserProfile entity (`src/Shared/Entities/UserProfile.cs` — exists)

---

## Design Principle

Transactions are **high-volume, append-mostly, read-heavy** data synced from Plaid. The storage strategy uses a **hybrid approach**: typed relational columns for fields that are queried, filtered, or aggregated — and a JSON column for the full Plaid payload to avoid schema changes when Plaid evolves their API.

Transactions are **not user-editable**. They are synced from Plaid and treated as immutable source-of-truth records. Users can categorize and annotate transactions but cannot modify the core financial data.

> **Tier gating**: Plaid-synced transactions are a **Premium feature**. Free-tier users can enter manual transactions (future) and view document-uploaded data. See [Tier Gating & Access Control](#tier-gating--access-control) for enforcement details.

---

## Table of Contents

1. [Entity Design](#entity-design)
2. [Database Schema](#database-schema)
3. [DTOs & Contracts](#dtos--contracts)
4. [API Endpoints](#api-endpoints)
5. [Service Layer](#service-layer)
6. [Plaid Sync Strategy](#plaid-sync-strategy)
7. [Tier Gating & Access Control](#tier-gating--access-control)
8. [Security & Privacy](#security--privacy)
9. [UI Design](#ui-design)
10. [Performance & Scaling](#performance--scaling)
11. [Error Codes](#error-codes)
12. [Validation Rules](#validation-rules)
13. [Future Considerations](#future-considerations)

---

## Entity Design

### Transaction Entity

```csharp
/// <summary>
/// A financial transaction synced from Plaid. Immutable core fields — only
/// user annotations (category overrides, notes, tags) are editable.
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
    /// Tenant ID for advisor-client data sharing. When an advisor views a client's
    /// transactions, TenantId matches the client's UserId. Required for the
    /// DataAccessGrant authorization model.
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

/// <summary>
/// Plaid's transaction_type values. Maps 1:1 from Plaid's API — do not add
/// custom values here. See: https://plaid.com/docs/api/products/transactions/#transactions-sync-response-added-transaction-type
/// </summary>
public enum TransactionType
{
    Place,
    Digital,
    Special,
    Unresolved
}

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
| `Amount` | Aggregation: spending totals, monthly summaries, net worth calculations |
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

### Table: `Transactions`

```sql
CREATE TABLE Transactions (
    Id                      uniqueidentifier    NOT NULL DEFAULT NEWSEQUENTIALID(),
    UserId                  nvarchar(128)       NOT NULL,   -- Entra Object ID (string)
    TenantId                nvarchar(128)       NOT NULL,   -- Tenant scope for advisor-client sharing
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
    Source                  nvarchar(10)        NOT NULL DEFAULT 'Plaid',  -- Plaid | Manual
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
-- Primary query pattern: user's transactions for an account in a date range
-- Uses TenantId (not UserId) as the leading key for advisor-client sharing support
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
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        // String identity columns
        builder.Property(t => t.UserId).HasMaxLength(128).IsRequired();
        builder.Property(t => t.TenantId).HasMaxLength(128).IsRequired();

        // Indexes — use TenantId as leading key for advisor-client sharing
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
            .WithMany()
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
| `UserProfile` | `Transaction` | **Restrict** | Users are soft-deleted; transactions are retained for audit |

---

## DTOs & Contracts

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

Users can only annotate — not modify core financial data.

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

### Paginated Response

```csharp
/// <summary>
/// Cursor-based paginated response for transaction lists.
/// Uses cursor (last transaction date + id) instead of offset for stable pagination
/// as new transactions are continuously synced.
/// </summary>
/// <remarks>
/// Cursor format: Base64(Date|Id) — e.g. Base64("2026-02-15|a1b2c3d4-...")
/// The cursor encodes the composite sort key (Date DESC, Id) so the next page
/// starts exactly after the last item returned. Opaque to the client — clients
/// must not parse or construct cursors; they only pass them back to the API.
/// </remarks>
public sealed record PaginatedTransactionsDto
{
    public List<TransactionDto> Transactions { get; init; } = [];
    public int TotalCount { get; init; }
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
}
```

### TypeScript Types

```typescript
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
  userTags?: string[];       // JSON array of tag strings
  isHidden: boolean;
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
}
```

---

## API Endpoints

```
# Transaction queries
GET    /api/transactions                    → GetTransactions
GET    /api/transactions/{id}              → GetTransactionById
GET    /api/transactions/summary           → GetTransactionSummary

# User annotations (only editable fields)
PATCH  /api/transactions/{id}              → UpdateTransaction

# Account-scoped queries
GET    /api/accounts/{accountId}/transactions → GetAccountTransactions

# Sync (triggered by system, not directly by user — see Plaid Sync Strategy)
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
| `cursor` | `string?` | null | Pagination cursor |
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

### ITransactionService

```csharp
/// <summary>
/// Provides query and annotation operations for Plaid-synced transactions.
/// All read operations enforce three-tier authorization
/// (Owner → DataAccessGrant → Administrator) via IAuthorizationService.
/// Data Category: DataCategories.Accounts
/// </summary>
public interface ITransactionService
{
    // === Queries ===

    /// <summary>
    /// Gets paginated transactions with filtering and search.
    /// </summary>
    Task<PaginatedTransactionsDto> GetTransactionsAsync(
        string requestingUserId,
        string ownerUserId,
        TransactionQueryParameters parameters);

    /// <summary>
    /// Gets full transaction detail including parsed location and payment metadata.
    /// </summary>
    Task<TransactionDetailDto> GetTransactionByIdAsync(
        string requestingUserId,
        Guid transactionId);

    /// <summary>
    /// Gets aggregated spending summary for a date range.
    /// </summary>
    Task<TransactionSummaryDto> GetSummaryAsync(
        string requestingUserId,
        string ownerUserId,
        TransactionSummaryParameters parameters);

    // === User annotations ===

    /// <summary>
    /// Updates user-editable fields (category override, notes, tags, hidden).
    /// Core financial data is immutable.
    /// </summary>
    Task<TransactionDto> UpdateTransactionAsync(
        string requestingUserId,
        Guid transactionId,
        UpdateTransactionRequest request);

    // === Sync (called by PlaidService, not by API functions directly) ===

    /// <summary>
    /// Upserts transactions from a Plaid sync response.
    /// Handles inserts, updates (pending → posted), and removes.
    /// Returns count of changes applied.
    /// </summary>
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
User links account via Plaid Link
  → ExchangePublicToken()
    → PlaidService stores LinkedAccount
    → PlaidService calls Plaid Transactions Sync API (initial)
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

Add to `LinkedAccount` entity:

```csharp
// On LinkedAccount entity
public string? PlaidSyncCursor { get; set; }    // Plaid's cursor for incremental sync
public DateTimeOffset? LastTransactionSyncAt { get; set; }
```

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
            // Update core fields but preserve user annotations
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

Plaid-synced transactions are a **Premium feature**. Transaction access varies by tier:

| Capability | Free Tier | Premium Tier |
|------------|-----------|-------------|
| Plaid account linking | Not available | Up to 10 linked accounts |
| Transaction history (Plaid) | Not available | Full history (Plaid provides ~2 years) |
| Manual transaction entry | Up to 3 months of history | Unlimited |
| Transaction annotations | Available on manual entries | Available on all |
| Spending summary/charts | Manual entries only | All transactions |
| CSV export | Not available | Available |

### Enforcement Points

**API layer** — The `ITransactionService` checks the user's subscription tier before executing Plaid-dependent operations:

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
        // Filter to manual-only transactions and enforce 3-month history limit
        parameters = parameters with
        {
            SourceFilter = TransactionSource.Manual,
            From = parameters.From ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
        };
    }

    // 3. Execute query...
}
```

**Plaid sync** — Sync operations reject free-tier users:

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

**UI layer** — Free-tier users see:
- Transactions page with empty state: "Upgrade to Premium to link your bank accounts and see transactions automatically"
- Manual transaction entry form (future feature) as the free-tier alternative
- Upgrade CTA button linking to subscription management

### Free-Tier History Limit

Free-tier manual transactions are **not deleted** after 3 months — the 3-month limit applies to **query results**, not storage. If a user upgrades to Premium later, their full manual history becomes visible.

```csharp
// Free tier: enforce 3-month window on reads, not on storage
if (tier == SubscriptionTier.Free && parameters.From < threeMonthsAgo)
{
    parameters = parameters with { From = threeMonthsAgo };
}
```

---

## Security & Privacy

### Access Control

| Operation | Authorization | Notes |
|-----------|--------------|-------|
| Read transactions | Owner + DataAccessGrant(Accounts, Read) + Administrator | Standard three-tier |
| Update annotations | Owner + DataAccessGrant(Accounts, Full) + Administrator | Only user-editable fields |
| Sync transactions | System-initiated only | Triggered by Plaid webhook or account refresh |
| Delete transactions | Not exposed | Only via Plaid sync (removed[]) or account unlink cascade |

### Data Classification

| Field | Classification | Treatment |
|-------|---------------|-----------|
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
| Premium: Plaid-synced transactions | Indefinite (while account is linked) | User expects full history |
| Premium: Unlinked account transactions | **Cascade-deleted** with LinkedAccount | No orphaned financial data |
| Free: Manual transactions | Stored indefinitely; **queried** with 3-month window | Upgrade path preserves data |
| `PlaidRawJson` | Consider TTL of 24 months | Reduce storage; core fields are in typed columns |
| Pending transactions | Auto-removed by Plaid sync | Plaid replaces pending with posted |

---

## UI Design

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
<Table aria-label={t('transactions.table.label')}>
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

> **Note**: Plaid amounts are positive for debits (money out) and negative for credits (money in). The UI inverts this for display: negative Plaid amount → green (income), positive → red/neutral (expense).

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

Uses `recharts` PieChart or BarChart on the summary endpoint data:

```tsx
<Card>
  <CardHeader>
    <CardTitle>{t('transactions.charts.spendingByCategory')}</CardTitle>
  </CardHeader>
  <CardContent>
    {/* Screen reader alternative for the chart */}
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
/** Fetches paginated transactions with filters. */
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
{ label: "Transactions", icon: Receipt, href: "/transactions" },
```

Add route to `App.tsx`:

```tsx
<Route path="/transactions" element={
  <ProtectedRoute policy="RequireClient"><Transactions /></ProtectedRoute>
} />
```

---

## Performance & Scaling

### Expected Volumes

| Metric | Estimate | Notes |
|--------|----------|-------|
| Transactions per user/month | ~200-500 | Typical consumer |
| Transactions per user/year | ~3,000-6,000 | |
| Transactions per user (5-year) | ~15,000-30,000 | Still manageable in SQL |
| Total transactions at 10K users | ~30-60M | May need partitioning at this scale |

### SQL is Sufficient Because

- Personal finance volumes are modest — not e-commerce or banking-scale
- Azure SQL supports up to 4TB per database, billions of rows
- Composite indexes on `(TenantId, LinkedAccountId, Date DESC)` make the primary query pattern fast
- Cursor-based pagination avoids `OFFSET` performance degradation
- `PlaidRawJson` is `nvarchar(max)` — stored out-of-row automatically by SQL Server for large values

### When to Consider Moving to Cosmos DB

Move the `Transactions` table to Cosmos DB **only if**:
- Query latency on the primary access pattern exceeds 100ms at p95
- Storage costs for `PlaidRawJson` become significant
- User count exceeds ~50K active users with multi-year history

**Migration path**: The typed columns already serve as the partition-friendly schema. Partition key would be `UserId`, sort key would be `Date`. The rest of the app (assets, contacts, etc.) stays in Azure SQL.

### Caching Strategy

| Data | Cache | TTL | Invalidation |
|------|-------|-----|-------------|
| Transaction list (paginated) | None | — | Data changes too frequently for list caching |
| Transaction summary | Redis | 15 min | Invalidate on sync or annotation update |
| Category list (distinct categories) | Redis | 1 hour | Invalidate on sync |
| Account transaction counts | Redis | 15 min | Invalidate on sync |

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
```

---

## Validation Rules

| Rule | Level | Notes |
|------|-------|-------|
| `from` must be before `to` in date range queries | Error | |
| `limit` must be 1-200 | Error | Default 50 |
| `UserNotes` max 1000 chars | Error | |
| `UserTagsJson` max 500 chars total, max 20 tags | Error | JSON array of strings |
| `UserCategoryOverride` max 100 chars | Error | |
| Cannot modify core financial fields (amount, date, name) | Error | 400 if attempted |
| Date range max span: 2 years per query | Error | Prevents full-table scans |

---

## Future Considerations

| Feature | Priority | Notes |
|---------|----------|-------|
| Recurring transaction detection | P2 | Group by merchant + similar amount + regular interval |
| Budget tracking | P2 | Set spending limits per category, alert when exceeded |
| Transaction rules | P2 | Auto-categorize based on merchant patterns |
| Receipt image attachment | P3 | Link photos to transactions via Azure Blob Storage |
| CSV export | P2 | Export filtered transactions as CSV |
| Investment transactions | P2 | Plaid `/investments/transactions` — different schema, separate table |
| Transaction search (full-text) | P2 | Azure SQL full-text index or Azure Cognitive Search |
| Cosmos DB migration | P3 | Only if SQL performance degrades at scale |

---

## Cross-References

- **Asset Types**: See [`ASSET_TYPE_SPECIFICATIONS.md`](ASSET_TYPE_SPECIFICATIONS.md) — transactions complement asset values with cash flow data
- **Contact Model**: See [04-contacts-beneficiaries.md](features/04-contacts-beneficiaries.md) — no direct relationship (transactions don't link to contacts)
- **API Tracking**: See [`RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md`](RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md) — add Transaction endpoints to tracking
- **Plaid Integration**: See [`RAJ_FINANCIAL_INTEGRATIONS_API.md`](RAJ_FINANCIAL_INTEGRATIONS_API.md) — PlaidService handles sync orchestration
- **Sensitive Fields**: See `ASSET_TYPE_SPECIFICATIONS.md` — Sensitive Field Handling section for encryption/masking patterns
