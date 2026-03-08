# 07 — Dashboard & Reporting

> Dashboard page aggregating net worth, portfolio allocation, spending summary, beneficiary coverage, recent transactions, and account balances. Includes report export and tier gating.

**ADO Tracking:** [Epic #373 — 07 - Dashboard & Reporting](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/373)

| # | Feature | State |
|---|---------|-------|
| [374](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/374) | Dashboard Widgets | New |
| [375](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/375) | Dashboard Service & API | New |
| [376](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/376) | Dashboard Page UI | New |
| [523](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/523) | Report Generation & Export | New |

---

## Overview

The Dashboard is the **landing page after login** — a single-screen financial snapshot that aggregates data from assets (doc 05), accounts & transactions (doc 06), and contacts/beneficiaries (doc 04). It is read-only; all mutations happen on their respective feature pages.

### Design Goals

1. **At-a-Glance Health** — Net worth, cash flow, and coverage status visible without scrolling on desktop
2. **Actionable Alerts** — Surface assets without beneficiaries, pending transactions, allocation warnings
3. **Tier Differentiation** — Free users see basic widgets; Premium unlocks spending trends, export, and full history charts
4. **Advisor View** — Advisors with `DataAccessGrant` see the same dashboard for their client's data (read-only)
5. **Mobile-First** — Widgets stack vertically on mobile; horizontal grid on desktop

---

## Dashboard Widgets

### Widget Inventory

| # | Widget | Data Source | Free | Premium | Priority |
|---|--------|-------------|------|---------|----------|
| 1 | Net Worth | `DashboardSummaryDto` | ✅ | ✅ | P0 |
| 2 | Asset Allocation | `PortfolioSummaryDto.ByType` | ✅ | ✅ | P0 |
| 3 | Account Balances | `LinkedAccountDto[]` | ✅ | ✅ | P0 |
| 4 | Spending Summary | `TransactionSummaryDto` | ✅ (current month) | ✅ (any range) | P0 |
| 5 | Recent Transactions | `TransactionDto[]` (last 5) | ✅ | ✅ | P1 |
| 6 | Beneficiary Coverage | `BeneficiaryCoverageDto` | ✅ | ✅ | P1 |
| 7 | Spending Trends | `MonthlyTrendDto[]` (6–12 months) | ❌ | ✅ | P2 |
| 8 | Alerts & Actions | Computed from coverage + pending | ✅ | ✅ | P1 |

---

## Net Worth Calculation

Net worth is computed server-side from two sources:

```
Net Worth = Total Asset Value + Total Account Balances - Total Liabilities (future)
```

| Component | Source | Field |
|-----------|--------|-------|
| Total Asset Value | `PortfolioSummaryDto.TotalValue` | Sum of `CurrentValue` across non-disposed assets |
| Total Account Balances | Sum of `LinkedAccountDto.CurrentBalance` | Across all linked accounts |
| Total Liabilities | *Not in MVP* | Future: sum of liability balances |

### Currency

All values are in USD (`IsoCurrencyCode = "USD"`). Multi-currency support is not in MVP scope.

---

## Data Aggregation

### Server-Side Aggregation Strategy

The dashboard calls a **single dedicated endpoint** (`GET /api/dashboard`) that internally fans out to existing services, avoiding N+1 API calls from the client.

```csharp
/// <summary>
/// Orchestrates data collection from multiple services to build
/// the dashboard summary. Runs queries in parallel where possible.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Returns the complete dashboard summary for a user.
    /// </summary>
    /// <param name="requestingUserId">The authenticated user (or advisor) making the request.</param>
    /// <param name="ownerUserId">The data owner (same as requestingUserId, or client's ID for advisor view).</param>
    /// <returns>Aggregated dashboard data from all feature services.</returns>
    Task<DashboardSummaryDto> GetDashboardAsync(string requestingUserId, string ownerUserId);
}
```

### Internal Fan-Out

```csharp
public class DashboardService : IDashboardService
{
    private readonly IAssetService _assetService;
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly IContactService _contactService;

    public async Task<DashboardSummaryDto> GetDashboardAsync(
        string requestingUserId, string ownerUserId)
    {
        // Run independent queries in parallel
        var portfolioTask = _assetService.GetPortfolioSummaryAsync(ownerUserId);
        var accountsTask = _accountService.GetAccountsAsync(requestingUserId, ownerUserId);
        var summaryTask = _transactionService.GetSummaryAsync(
            requestingUserId, ownerUserId,
            new TransactionSummaryParameters
            {
                From = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                To = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        var coverageTask = _contactService.GetCoverageSummaryAsync(
            Guid.Parse(ownerUserId));

        await Task.WhenAll(portfolioTask, accountsTask, summaryTask, coverageTask);

        var portfolio = await portfolioTask;
        var accounts = await accountsTask;
        var spending = await summaryTask;
        var coverage = await coverageTask;

        return new DashboardSummaryDto
        {
            NetWorth = portfolio.TotalValue + accounts.Sum(a => a.CurrentBalance ?? 0),
            Portfolio = portfolio,
            Accounts = accounts.ToList(),
            Spending = spending,
            BeneficiaryCoverage = coverage,
            Alerts = BuildAlerts(portfolio, accounts, spending, coverage)
        };
    }
}
```

---

## DTOs

### DashboardSummaryDto

```csharp
/// <summary>
/// Top-level response for the dashboard endpoint.
/// Aggregates data from assets, accounts, transactions, and contacts.
/// </summary>
public sealed record DashboardSummaryDto
{
    /// <summary>
    /// Calculated net worth: TotalAssetValue + TotalAccountBalances.
    /// </summary>
    public decimal NetWorth { get; init; }

    /// <summary>
    /// Portfolio summary from IAssetService.
    /// </summary>
    public required PortfolioSummaryDto Portfolio { get; init; }

    /// <summary>
    /// All linked accounts with current balances.
    /// </summary>
    public List<LinkedAccountDto> Accounts { get; init; } = [];

    /// <summary>
    /// Spending summary for the default period (current month for free, configurable for premium).
    /// </summary>
    public required TransactionSummaryDto Spending { get; init; }

    /// <summary>
    /// Beneficiary coverage statistics across all assets.
    /// </summary>
    public required BeneficiaryCoverageDto BeneficiaryCoverage { get; init; }

    /// <summary>
    /// Actionable alerts computed from the aggregated data.
    /// </summary>
    public List<DashboardAlertDto> Alerts { get; init; } = [];

    /// <summary>
    /// Monthly spending trends (Premium only — null for free tier).
    /// </summary>
    public List<MonthlyTrendDto>? SpendingTrends { get; init; }
}
```

### DashboardAlertDto

```csharp
/// <summary>
/// An actionable alert surfaced on the dashboard.
/// </summary>
public sealed record DashboardAlertDto
{
    /// <summary>
    /// Alert severity for visual treatment.
    /// </summary>
    public required AlertSeverity Severity { get; init; }

    /// <summary>
    /// Machine-readable alert type for i18n translation keys.
    /// </summary>
    public required string AlertCode { get; init; }

    /// <summary>
    /// Default English message (client should prefer translating AlertCode).
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional navigation target (e.g., "/assets/123" or "/contacts").
    /// </summary>
    public string? ActionUrl { get; init; }

    /// <summary>
    /// Additional context for the alert.
    /// </summary>
    public Dictionary<string, object>? Details { get; init; }
}

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
```

### Alert Codes

| Code | Severity | Condition | Message |
|------|----------|-----------|---------|
| `BENEFICIARY_COVERAGE_LOW` | Warning | CoveragePercent < 50% | "X of Y assets have no beneficiary assigned" |
| `BENEFICIARY_ALLOCATION_MISMATCH` | Warning | Any AssetCoverageItem.AllocationWarning | "Asset '...' beneficiary allocations don't total 100%" |
| `PENDING_TRANSACTIONS` | Info | Pending transaction count > 0 | "X pending transactions" |
| `ACCOUNT_SYNC_STALE` | Warning | Plaid account LastSyncAt > 24h ago | "Account '...' hasn't synced in X days" |
| `NO_ACCOUNTS_LINKED` | Info | Account count == 0 | "Link a bank account to track spending" |
| `NO_ASSETS_TRACKED` | Info | TotalAssets == 0 | "Add your first asset to start tracking" |
| `TIER_LIMIT_APPROACHING` | Warning | Free tier at 80%+ of any limit | "You've used X of Y free assets" |

### MonthlyTrendDto

```csharp
/// <summary>
/// Monthly aggregation for spending trend charts. Premium only.
/// </summary>
public sealed record MonthlyTrendDto
{
    /// <summary>
    /// First day of the month.
    /// </summary>
    public DateOnly Month { get; init; }

    /// <summary>
    /// Total income for the month (credits, positive).
    /// </summary>
    public decimal Income { get; init; }

    /// <summary>
    /// Total expenses for the month (debits, positive).
    /// </summary>
    public decimal Expenses { get; init; }

    /// <summary>
    /// Income - Expenses.
    /// </summary>
    public decimal NetCashFlow { get; init; }

    /// <summary>
    /// Snapshot of net worth at end of month (sum of asset values + account balances).
    /// </summary>
    public decimal? NetWorthSnapshot { get; init; }
}
```

> **Note**: `MonthlyTrendDto[]` is populated only for Premium users. Free tier receives `null` for the `SpendingTrends` property. Net worth snapshots require a scheduled job to capture monthly snapshots — see [Snapshot Strategy](#net-worth-snapshot-strategy).

---

## API Endpoints

### Dashboard

```
GET    /api/dashboard                      → GetDashboard
GET    /api/dashboard/trends               → GetSpendingTrends (Premium only)
GET    /api/dashboard/export               → ExportReport (Premium only)
```

### Query Parameters for `GET /api/dashboard`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | authenticated user | Override for advisor viewing client data |

> **Note**: `userId` is only accepted when the requesting user has a `DataAccessGrant` from the target user or has the `Administrator` role. Otherwise returns `403 Forbidden`.

### Query Parameters for `GET /api/dashboard/trends`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `months` | `int` | 6 | Number of months to include (max 24) |
| `userId` | `string?` | authenticated user | Override for advisor view |

### Query Parameters for `GET /api/dashboard/export`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `format` | `string` | `csv` | Export format: `csv` or `pdf` |
| `sections` | `string` | `all` | Comma-separated: `assets,accounts,transactions,beneficiaries` |
| `from` | `DateOnly?` | 1 year ago | Transaction date range start |
| `to` | `DateOnly?` | today | Transaction date range end |
| `userId` | `string?` | authenticated user | Override for advisor view |

---

## Service Layer

### IDashboardService

```csharp
/// <summary>
/// Aggregates data from feature services to build dashboard views
/// and supports report export.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Returns the complete dashboard summary including net worth,
    /// portfolio, accounts, spending, beneficiary coverage, and alerts.
    /// </summary>
    Task<DashboardSummaryDto> GetDashboardAsync(
        string requestingUserId, string ownerUserId);

    /// <summary>
    /// Returns monthly spending and net worth trends.
    /// Premium only — throws TIER_PREMIUM_REQUIRED for free-tier users.
    /// </summary>
    Task<IReadOnlyList<MonthlyTrendDto>> GetSpendingTrendsAsync(
        string requestingUserId, string ownerUserId, int months = 6);

    /// <summary>
    /// Generates a report export (CSV or PDF) of the user's financial data.
    /// Premium only — throws TIER_PREMIUM_REQUIRED for free-tier users.
    /// </summary>
    Task<ReportExportResult> ExportReportAsync(
        string requestingUserId, string ownerUserId, ReportExportRequest request);
}
```

### ReportExportRequest / ReportExportResult

```csharp
/// <summary>
/// Parameters for report export.
/// </summary>
public sealed record ReportExportRequest
{
    public ReportFormat Format { get; init; } = ReportFormat.Csv;
    public List<ReportSection> Sections { get; init; } = [ReportSection.All];
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}

public enum ReportFormat
{
    Csv = 0,
    Pdf = 1
}

public enum ReportSection
{
    All = 0,
    Assets = 1,
    Accounts = 2,
    Transactions = 3,
    Beneficiaries = 4
}

/// <summary>
/// Result of a report generation. Contains a SAS URL for download.
/// </summary>
public sealed record ReportExportResult
{
    /// <summary>
    /// Azure Blob Storage SAS URL for downloading the report.
    /// Valid for 15 minutes.
    /// </summary>
    public required string DownloadUrl { get; init; }

    /// <summary>
    /// Filename for the Content-Disposition header.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// MIME type of the generated file.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }
}
```

---

## Azure Functions

### GetDashboard

```csharp
/// <summary>
/// Returns the aggregated dashboard summary for the authenticated user.
/// Supports advisor view via userId query parameter with DataAccessGrant validation.
/// </summary>
/// <response code="200">Dashboard summary returned successfully.</response>
/// <response code="401">Not authenticated.</response>
/// <response code="403">DataAccessGrant required for viewing another user's data.</response>
[Function("GetDashboard")]
[Authorize]
public async Task<HttpResponseData> GetDashboard(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard")]
    HttpRequestData req,
    FunctionContext context)
{
    var authenticatedUserId = context.GetUserId();
    var targetUserId = req.Query["userId"] ?? authenticatedUserId;

    // Authorization: if viewing another user's data, validate DataAccessGrant
    if (targetUserId != authenticatedUserId)
    {
        await _authorizationService.ValidateAccessAsync(
            authenticatedUserId, targetUserId, DataCategories.All);
    }

    var dashboard = await _dashboardService.GetDashboardAsync(
        authenticatedUserId, targetUserId);

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(dashboard);
    return response;
}
```

### GetSpendingTrends

```csharp
/// <summary>
/// Returns monthly spending and net worth trends. Premium only.
/// </summary>
/// <response code="200">Trend data returned.</response>
/// <response code="402">Free-tier user — upgrade required.</response>
[Function("GetSpendingTrends")]
[Authorize]
public async Task<HttpResponseData> GetSpendingTrends(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard/trends")]
    HttpRequestData req,
    FunctionContext context)
{
    var authenticatedUserId = context.GetUserId();
    var targetUserId = req.Query["userId"] ?? authenticatedUserId;
    var months = int.TryParse(req.Query["months"], out var m) ? m : 6;

    if (targetUserId != authenticatedUserId)
    {
        await _authorizationService.ValidateAccessAsync(
            authenticatedUserId, targetUserId, DataCategories.Accounts);
    }

    var trends = await _dashboardService.GetSpendingTrendsAsync(
        authenticatedUserId, targetUserId, months);

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(trends);
    return response;
}
```

### ExportReport

```csharp
/// <summary>
/// Generates a financial report export (CSV or PDF). Premium only.
/// Returns a SAS URL for downloading the generated file.
/// </summary>
/// <response code="200">Report generated, download URL returned.</response>
/// <response code="402">Free-tier user — upgrade required.</response>
[Function("ExportReport")]
[Authorize]
public async Task<HttpResponseData> ExportReport(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard/export")]
    HttpRequestData req,
    FunctionContext context)
{
    var authenticatedUserId = context.GetUserId();
    var targetUserId = req.Query["userId"] ?? authenticatedUserId;

    if (targetUserId != authenticatedUserId)
    {
        await _authorizationService.ValidateAccessAsync(
            authenticatedUserId, targetUserId, DataCategories.All);
    }

    var request = new ReportExportRequest
    {
        Format = Enum.TryParse<ReportFormat>(req.Query["format"], true, out var f)
            ? f : ReportFormat.Csv,
        From = DateOnly.TryParse(req.Query["from"], out var from)
            ? from : DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
        To = DateOnly.TryParse(req.Query["to"], out var to)
            ? to : DateOnly.FromDateTime(DateTime.UtcNow)
    };

    // Parse sections
    var sectionParam = req.Query["sections"] ?? "all";
    request = request with
    {
        Sections = sectionParam.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? [ReportSection.All]
            : sectionParam.Split(',')
                .Select(s => Enum.TryParse<ReportSection>(s.Trim(), true, out var sec)
                    ? sec : ReportSection.All)
                .Distinct()
                .ToList()
    };

    var result = await _dashboardService.ExportReportAsync(
        authenticatedUserId, targetUserId, request);

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(result);
    return response;
}
```

---

## Net Worth Snapshot Strategy

Monthly net worth snapshots enable trend charts to show historical net worth even when asset values change.

### NetWorthSnapshot Entity

```csharp
/// <summary>
/// Point-in-time snapshot of a user's total net worth.
/// Captured monthly by a scheduled Azure Function.
/// </summary>
public class NetWorthSnapshot
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly SnapshotDate { get; set; }
    public decimal TotalAssetValue { get; set; }
    public decimal TotalAccountBalances { get; set; }
    public decimal TotalLiabilities { get; set; }       // Future — 0 for MVP
    public decimal NetWorth { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

### Database Schema

```sql
CREATE TABLE NetWorthSnapshots (
    Id                      uniqueidentifier    NOT NULL DEFAULT NEWSEQUENTIALID(),
    UserId                  nvarchar(128)       NOT NULL,
    SnapshotDate            date                NOT NULL,
    TotalAssetValue         decimal(18,2)       NOT NULL,
    TotalAccountBalances    decimal(18,2)       NOT NULL,
    TotalLiabilities        decimal(18,2)       NOT NULL DEFAULT 0,
    NetWorth                decimal(18,2)       NOT NULL,
    CreatedAt               datetimeoffset      NOT NULL,

    CONSTRAINT PK_NetWorthSnapshots PRIMARY KEY (Id),
    CONSTRAINT UQ_NetWorthSnapshots_User_Date UNIQUE (UserId, SnapshotDate)
);

CREATE NONCLUSTERED INDEX IX_NetWorthSnapshots_User_Date
    ON NetWorthSnapshots (UserId, SnapshotDate DESC)
    INCLUDE (NetWorth, TotalAssetValue, TotalAccountBalances);
```

### EF Core Configuration

```csharp
public class NetWorthSnapshotConfiguration : IEntityTypeConfiguration<NetWorthSnapshot>
{
    public void Configure(EntityTypeBuilder<NetWorthSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.UserId).HasMaxLength(128).IsRequired();
        builder.Property(s => s.TotalAssetValue).HasPrecision(18, 2);
        builder.Property(s => s.TotalAccountBalances).HasPrecision(18, 2);
        builder.Property(s => s.TotalLiabilities).HasPrecision(18, 2);
        builder.Property(s => s.NetWorth).HasPrecision(18, 2);

        builder.HasIndex(s => new { s.UserId, s.SnapshotDate })
            .IsUnique()
            .IsDescending(false, true)
            .HasDatabaseName("IX_NetWorthSnapshots_User_Date");
    }
}
```

### Scheduled Snapshot Function

```csharp
/// <summary>
/// Timer-triggered function that captures net worth snapshots monthly.
/// Runs on the 1st of each month at 02:00 UTC.
/// </summary>
[Function("CaptureNetWorthSnapshots")]
public async Task CaptureNetWorthSnapshots(
    [TimerTrigger("0 0 2 1 * *")] TimerInfo timer,
    FunctionContext context)
{
    var logger = context.GetLogger<DashboardFunctions>();
    logger.LogInformation(
        "Net worth snapshot job started at {Timestamp}", DateTimeOffset.UtcNow);

    var snapshotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)); // Last day of prev month
    var processedCount = await _dashboardService.CaptureSnapshotsAsync(snapshotDate);

    logger.LogInformation(
        "Net worth snapshot job completed. {Count} snapshots captured for {Date}",
        processedCount, snapshotDate);
}
```

> **Note**: The snapshot function iterates over all active users and captures their net worth. For MVP with low user counts, sequential processing is acceptable. At scale, batch via Azure Queue.

---

## Report Export

### CSV Export Format

Reports are generated as UTF-8 CSV files with BOM (for Excel compatibility).

#### Assets Section

```csv
Asset Name,Type,Current Value,Purchase Price,Purchase Date,Status,Beneficiary Count
Primary Home,RealEstate,450000.00,380000.00,2018-06-15,Active,2
Tesla Model Y,Vehicle,42000.00,55000.00,2022-01-10,Active,1
```

#### Accounts Section

```csv
Account Name,Institution,Type,Current Balance,Source,Last Synced
Joint Checking,Chase,Checking,12500.00,Plaid,2026-02-28T14:30:00Z
Savings,Ally Bank,Savings,45000.00,Manual,
```

#### Transactions Section (Date-Filtered)

```csv
Date,Account,Merchant,Category,Amount,Type,Notes
2026-02-28,Joint Checking,Whole Foods,Food and Drink,127.50,Debit,Groceries
2026-02-27,Joint Checking,Amazon,Shopping,45.99,Debit,
```

#### Beneficiaries Section

```csv
Contact Name,Type,Linked Assets,Primary Allocation Total,Contingent Allocation Total
Jane Doe,Individual,3,100%,0%
Family Trust,Trust,2,50%,100%
```

### PDF Export

PDF reports use a branded template with RAJ Financial headers and the same data sections as CSV. Generated server-side using a headless rendering approach.

```csharp
public interface IReportGenerator
{
    /// <summary>
    /// Generates a CSV report and returns the byte content.
    /// </summary>
    Task<byte[]> GenerateCsvAsync(DashboardSummaryDto data, ReportExportRequest request);

    /// <summary>
    /// Generates a branded PDF report and returns the byte content.
    /// </summary>
    Task<byte[]> GeneratePdfAsync(DashboardSummaryDto data, ReportExportRequest request);
}
```

### Blob Storage

Reports are uploaded to Azure Blob Storage with a short-lived SAS URL for download.

| Setting | Value |
|---------|-------|
| Container | `reports` |
| Path | `{userId}/{timestamp}_{format}.{ext}` |
| SAS TTL | 15 minutes |
| Retention | 7 days (auto-deleted via lifecycle policy) |

---

## TypeScript Types

```typescript
/** Aggregated dashboard response. */
interface DashboardSummaryDto {
  netWorth: number;
  portfolio: PortfolioSummaryDto;
  accounts: LinkedAccountDto[];
  spending: TransactionSummaryDto;
  beneficiaryCoverage: BeneficiaryCoverageDto;
  alerts: DashboardAlertDto[];
  spendingTrends: MonthlyTrendDto[] | null;
}

interface DashboardAlertDto {
  severity: 'Info' | 'Warning' | 'Critical';
  alertCode: string;
  message: string;
  actionUrl?: string;
  details?: Record<string, unknown>;
}

interface MonthlyTrendDto {
  month: string;           // ISO date string (first of month)
  income: number;
  expenses: number;
  netCashFlow: number;
  netWorthSnapshot?: number;
}

/** Report export result with download URL. */
interface ReportExportResult {
  downloadUrl: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
}

/** Referenced types from other docs — not redefined here. */
// PortfolioSummaryDto    → see 05-assets-portfolio.md
// LinkedAccountDto       → see 06-accounts-transactions.md
// TransactionSummaryDto  → see 06-accounts-transactions.md
// BeneficiaryCoverageDto → see 04-contacts-beneficiaries.md
```

---

## React Query Hooks

```typescript
import { useQuery } from '@tanstack/react-query';

/**
 * Fetches the aggregated dashboard summary.
 *
 * @param userId - Optional override for advisor viewing client data.
 * @returns Dashboard data including net worth, portfolio, accounts, spending, and alerts.
 *
 * @example
 * const { data: dashboard, isLoading } = useDashboard();
 */
export function useDashboard(userId?: string) {
  return useQuery({
    queryKey: ['dashboard', userId ?? 'me'],
    queryFn: () => fetchDashboard(userId),
    staleTime: 5 * 60 * 1000,       // 5 minutes
    refetchOnWindowFocus: true,
  });
}

/**
 * Fetches monthly spending and net worth trends. Premium only.
 *
 * @param months - Number of months to fetch (default 6).
 * @param userId - Optional override for advisor view.
 * @returns Array of monthly trend data points.
 */
export function useSpendingTrends(months = 6, userId?: string) {
  return useQuery({
    queryKey: ['dashboard', 'trends', months, userId ?? 'me'],
    queryFn: () => fetchSpendingTrends(months, userId),
    staleTime: 30 * 60 * 1000,      // 30 minutes — trends change infrequently
    enabled: true,                    // Controlled by tier check in component
  });
}
```

---

## UI Layout

### Desktop Layout (≥ md breakpoint)

```
┌──────────────────────────────────────────────────────────┐
│  Dashboard                                    [Export ▾]  │
├────────────────────────┬─────────────────────────────────┤
│  Net Worth             │  Asset Allocation (donut chart)  │
│  $547,500              │  [RealEstate | Vehicle | ...]    │
├────────────────────────┼─────────────────────────────────┤
│  Account Balances      │  Spending Summary (this month)   │
│  ┌──────────────────┐  │  Income:    $8,200              │
│  │ Chase Checking   │  │  Expenses:  $5,430              │
│  │ $12,500          │  │  Net:       +$2,770             │
│  │ Ally Savings     │  │                                 │
│  │ $45,000          │  │  [Top Categories bar chart]     │
│  └──────────────────┘  │                                 │
├────────────────────────┴─────────────────────────────────┤
│  Beneficiary Coverage                                     │
│  ████████████░░░░░ 67% covered  (4/6 assets)             │
│  [View uncovered assets →]                                │
├──────────────────────────────────────────────────────────┤
│  Alerts (2)                                               │
│  ⚠ 2 assets have no beneficiary assigned                  │
│  ℹ 3 pending transactions                                 │
├──────────────────────────────────────────────────────────┤
│  Recent Transactions                                      │
│  Feb 28  Whole Foods       Food & Drink     -$127.50      │
│  Feb 27  Amazon            Shopping         -$45.99       │
│  Feb 26  Employer          Payroll         +$4,100.00     │
│  [View all transactions →]                                │
├──────────────────────────────────────────────────────────┤
│  Spending Trends (Premium)                    [6m|12m]    │
│  ┌──────────────────────────────────────────────────┐    │
│  │  📊 Bar chart: Income vs Expenses by month       │    │
│  │  📈 Line overlay: Net Worth                      │    │
│  └──────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────┘
```

### Mobile Layout (< md breakpoint)

Widgets stack vertically in order of priority:

1. Net Worth card (hero)
2. Alerts (if any)
3. Account Balances (horizontal scroll cards)
4. Spending Summary (compact)
5. Recent Transactions (last 3, not 5)
6. Asset Allocation (simplified list, not chart)
7. Beneficiary Coverage (progress bar)
8. Spending Trends (Premium — swipeable chart)

### Responsive Implementation

```tsx
/**
 * Dashboard page with responsive widget grid.
 * Mobile: single-column stack. Desktop: 2-column grid.
 */
export function DashboardPage() {
  const { t } = useTranslation();
  const { data: dashboard, isLoading } = useDashboard();

  if (isLoading) return <DashboardSkeleton />;

  return (
    <div className="space-y-4 md:space-y-6">
      {/* Hero: Net Worth */}
      <NetWorthCard netWorth={dashboard.netWorth} />

      {/* Alerts */}
      {dashboard.alerts.length > 0 && (
        <AlertsList alerts={dashboard.alerts} />
      )}

      {/* Two-column grid on desktop */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 md:gap-6">
        <AccountBalancesCard accounts={dashboard.accounts} />
        <SpendingSummaryCard spending={dashboard.spending} />
      </div>

      {/* Beneficiary Coverage — full width */}
      <BeneficiaryCoverageCard coverage={dashboard.beneficiaryCoverage} />

      {/* Recent Transactions */}
      <RecentTransactionsCard />

      {/* Asset Allocation */}
      <AssetAllocationCard portfolio={dashboard.portfolio} />

      {/* Premium: Spending Trends */}
      <SpendingTrendsCard />
    </div>
  );
}
```

### Chart Library

Use **Recharts** (already in React/Vite ecosystem) for:
- **Donut chart**: Asset allocation by type
- **Bar chart**: Spending by category, monthly income vs expenses
- **Line chart**: Net worth trend overlay
- **Progress bar**: Beneficiary coverage (can use Tailwind/shadcn Progress component)

```json
// package.json addition
{
  "recharts": "^2.15.0"
}
```

---

## Validation Rules

| Rule | Level |
|------|-------|
| `months` for trends must be 1–24 | Error |
| `format` for export must be `csv` or `pdf` | Error |
| `sections` must contain valid section names | Error |
| `from` must be ≤ `to` for export date range | Error |
| `userId` override requires valid DataAccessGrant or Administrator role | Error (403) |
| Export not available for free tier | Error (402) |
| Trends not available for free tier | Error (402) |

---

## Tier Gating

| Feature | Free | Premium |
|---------|------|---------|
| Dashboard summary | ✅ | ✅ |
| Net worth calculation | ✅ | ✅ |
| Current month spending | ✅ | ✅ |
| Custom date range spending | ❌ | ✅ |
| Spending trends (6–24 months) | ❌ | ✅ |
| Report export (CSV) | ❌ | ✅ |
| Report export (PDF) | ❌ | ✅ |
| Advisor data sharing view | ❌ | ✅ |

When a free-tier user accesses a Premium feature, the API returns `402 Payment Required` with error code `TIER_PREMIUM_REQUIRED`. The UI shows an upgrade prompt instead of the feature.

```tsx
/**
 * Wrapper that shows upgrade prompt for premium-only features.
 * Renders children only if user has premium subscription.
 */
export function PremiumGate({ 
  feature, 
  children 
}: { 
  feature: string; 
  children: React.ReactNode; 
}) {
  const { isPremium } = useSubscription();
  const { t } = useTranslation();

  if (!isPremium) {
    return (
      <Card className="p-6 text-center">
        <Lock className="mx-auto h-8 w-8 text-muted-foreground mb-2" aria-hidden="true" />
        <p className="text-sm text-muted-foreground">
          {t('tier.premiumRequired', { feature })}
        </p>
        <Button variant="outline" className="mt-4" asChild>
          <a href="/settings/subscription">{t('tier.upgrade')}</a>
        </Button>
      </Card>
    );
  }

  return <>{children}</>;
}
```

---

## Caching Strategy

| Data | Cache TTL | Invalidation |
|------|-----------|-------------|
| Dashboard summary | 5 minutes (Redis) | Asset/Account/Transaction mutation |
| Spending trends | 30 minutes (Redis) | Transaction sync |
| Net worth snapshots | 24 hours (Redis) | Monthly snapshot job |
| Report files | 7 days (Blob) | Lifecycle policy auto-delete |

### Cache Key Pattern

```
dashboard:{userId}:summary          → DashboardSummaryDto
dashboard:{userId}:trends:{months}  → MonthlyTrendDto[]
dashboard:{userId}:snapshot:{date}  → NetWorthSnapshot
```

### Cache Invalidation

```csharp
/// <summary>
/// Invalidates dashboard cache when underlying data changes.
/// Called by asset, account, transaction, and contact services after mutations.
/// </summary>
public async Task InvalidateDashboardCacheAsync(string userId)
{
    var cache = _cacheProvider.GetDatabase();
    var pattern = $"dashboard:{userId}:*";

    // Delete all matching keys
    var keys = _cacheProvider.GetServer().Keys(pattern: pattern);
    foreach (var key in keys)
    {
        await cache.KeyDeleteAsync(key);
    }

    _logger.LogInformation(
        "Dashboard cache invalidated for user {UserId}", userId);
}
```

---

## Security & Access Control

### Authorization Model

Dashboard operations follow the **three-tier authorization pattern** defined in [03-authorization-data-access.md](03-authorization-data-access.md):

| Tier | Access Level | Description |
|------|-------------|-------------|
| **Owner** | Full dashboard | Authenticated user viewing their own data |
| **Granted** | Read-only dashboard | Advisor with `DataAccessGrant` (requires `Accounts` + `Assets` + `Contacts` categories — Dashboard uses `DataCategories.All`) |
| **Administrator** | Full read, audit | Platform admins |

### Operation-Level Access Control

| Operation | Owner | Granted (Advisor) | Administrator | Unauthenticated |
|-----------|-------|--------------------|---------------|-----------------|
| View dashboard | ✅ | ✅ (read-only) | ✅ | ❌ 401 |
| View spending trends | ✅ (Premium) | ✅ (read-only, Premium) | ✅ | ❌ 401 |
| Export report | ✅ (Premium) | ✅ (read-only, Premium) | ✅ | ❌ 401 |

### Data Privacy

- The dashboard endpoint **never returns** `UserId`, `TenantId`, or `PlaidAccessToken` — these are internal scoping fields excluded from all DTOs.
- Sensitive fields (SSN, EIN) are **never included** in dashboard data or report exports — only masked formats if referenced in beneficiary coverage.
- Report exports are stored in Azure Blob Storage with per-user path isolation (`{userId}/`) and time-limited SAS URLs.
- Audit logging captures all report export requests (who exported, what scope, timestamp).

### Audit Logging

| Event | Logged Data | Severity |
|-------|------------|----------|
| Dashboard viewed (advisor/admin) | RequestingUserId, OwnerUserId | Information |
| Report exported | UserId, Format, Sections, DateRange | Information |
| Unauthorized dashboard access attempt | RequestingUserId, TargetUserId | **Warning** |
| Premium feature access by free user | UserId, Feature | Information |

---

## Error Codes

| Code | HTTP | Condition |
|------|------|-----------|
| `AUTH_REQUIRED` | 401 | Not authenticated |
| `AUTH_FORBIDDEN` | 403 | No DataAccessGrant for target user |
| `TIER_PREMIUM_REQUIRED` | 402 | Free user accessing trends/export |
| `VALIDATION_FAILED` | 400 | Invalid query parameters |
| `RESOURCE_NOT_FOUND` | 404 | Target user does not exist |
| `SERVER_ERROR` | 500 | Internal aggregation failure |

---

## Cross-References

- Portfolio summary & asset types: [05-assets-portfolio.md](05-assets-portfolio.md)
- Account balances & transaction DTOs: [06-accounts-transactions.md](06-accounts-transactions.md)
- Beneficiary coverage analysis: [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md)
- Authorization & DataAccessGrant: [03-authorization-data-access.md](03-authorization-data-access.md)
- Tier model & error codes: [01-platform-infrastructure.md](01-platform-infrastructure.md)
- Transaction storage specification: [../TRANSACTION_STORAGE_SPECIFICATION.md](../TRANSACTION_STORAGE_SPECIFICATION.md)

---

*Last Updated: February 2026*
