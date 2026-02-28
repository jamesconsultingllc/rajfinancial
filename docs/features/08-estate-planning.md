# 08 — Estate Planning & Beneficiary Analysis

> Beneficiary coverage analysis, trust management views, allocation validation, coverage gap detection, estate document tracking, and per-stirpes distribution modeling.

**ADO Tracking:** [Epic #414 — 08 - Estate Planning](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/414)

| # | Feature | State |
|---|---------|-------|
| [415](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/415) | Insurance Needs Calculator | New |
| [525](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/525) | Trust Management Views | New |
| [526](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/526) | Beneficiary Coverage Analysis | New |
| [527](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/527) | Estate Planning UI | New |

---

## Overview

Estate planning in RAJ Financial aggregates beneficiary, trust, and asset data from docs [04](04-contacts-beneficiaries.md) and [05](05-assets-portfolio.md) into **actionable views** that help users:

1. **Identify gaps** — assets without beneficiaries, trusts without trustees
2. **Validate allocations** — primary and contingent allocations should total 100% per asset
3. **Track trust structures** — who holds what role in each trust entity
4. **Monitor coverage** — a single percentage showing how well-protected the estate is
5. **Generate estate summaries** — exportable overview of the full estate plan

> Estate planning features use `DataCategory.Planning` for data sharing grants.

---

## Design Goals

| Goal | Description |
|------|-------------|
| **Read-only aggregation** | No new entities — views over existing Contact/Asset/AssetContactLink data |
| **Gap-first UX** | Surface uncovered assets and allocation warnings prominently |
| **Trust-aware** | Deep rendering of trust hierarchies (Grantor → Trustee → Beneficiaries) |
| **Tier-gated analysis** | Basic coverage available to all; detailed analysis is Premium |
| **Advisor-compatible** | All views support the `(requestingUserId, ownerUserId)` pattern |

---

## Coverage Analysis

### How Coverage Is Calculated

An asset is **covered** when it has at least one `AssetContactLink` where `Role == Beneficiary` and `Designation == Primary`. Coverage percentage is:

```
CoveragePercent = (AssetsWithBeneficiaries / TotalActiveAssets) × 100
```

Disposed assets are excluded from coverage calculations.

### Coverage Levels

| Coverage % | Status | Color | Description |
|-----------|--------|-------|-------------|
| 100% | Complete | Green | All assets have primary beneficiaries |
| 75–99% | Good | Blue | Most assets covered, minor gaps |
| 50–74% | Needs Attention | Yellow/Amber | Significant gaps exist |
| 0–49% | Critical | Red | Most assets unprotected |

### BeneficiaryCoverageDto

Defined in [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md) — referenced here, not redefined:

```csharp
// From doc 04 — provides per-asset breakdown
BeneficiaryCoverageDto
├── TotalAssets: int
├── AssetsWithBeneficiaries: int
├── AssetsWithoutBeneficiaries: int
├── CoveragePercent: decimal (0-100)
└── Assets: List<AssetCoverageItem>
    ├── AssetId, AssetName, AssetType, CurrentValue
    ├── PrimaryAllocationTotal, ContingentAllocationTotal
    ├── HasPrimaryBeneficiary, HasContingentBeneficiary
    └── AllocationWarning (true if totals ≠ 100%)
```

---

## Allocation Validation

### Rules

| Rule | Severity | Description |
|------|----------|-------------|
| Primary allocations must total 100% | Warning | Partial primary allocations leave estate in partial intestacy |
| Contingent allocations must total 100% | Warning | Contingent beneficiaries cover when primary cannot inherit |
| No primary beneficiary assigned | Warning | Asset passes through probate |
| Duplicate beneficiary on same asset | Error | Same contact linked twice as beneficiary |
| Per-stirpes with no descendants | Info | Per-stirpes setting is valid but has no practical effect if beneficiary has no children |
| Zero allocation percentage | Error | Beneficiary link exists but has 0% allocation |

### AllocationValidationResult

```csharp
/// <summary>
/// Validation result for beneficiary allocations on a single asset.
/// </summary>
public sealed record AllocationValidationResult
{
    public Guid AssetId { get; init; }
    public string AssetName { get; init; } = string.Empty;
    public decimal PrimaryTotal { get; init; }
    public decimal ContingentTotal { get; init; }
    public bool PrimaryValid { get; init; }     // true if == 100 or no primaries
    public bool ContingentValid { get; init; }  // true if == 100 or no contingents
    public List<AllocationIssue> Issues { get; init; } = [];
}

/// <summary>
/// An individual allocation issue found during validation.
/// </summary>
public sealed record AllocationIssue
{
    public string IssueCode { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;  // Error, Warning, Info
    public string Message { get; init; } = string.Empty;
    public Guid? ContactId { get; init; }
    public string? ContactDisplayName { get; init; }
}
```

### Issue Codes

| Code | Severity | Message |
|------|----------|---------|
| `ALLOCATION_PRIMARY_INCOMPLETE` | Warning | Primary beneficiary allocations total {total}% — should be 100% |
| `ALLOCATION_CONTINGENT_INCOMPLETE` | Warning | Contingent beneficiary allocations total {total}% — should be 100% |
| `ALLOCATION_PRIMARY_OVER` | Error | Primary allocations exceed 100% ({total}%) |
| `ALLOCATION_CONTINGENT_OVER` | Error | Contingent allocations exceed 100% ({total}%) |
| `NO_PRIMARY_BENEFICIARY` | Warning | No primary beneficiary assigned |
| `NO_CONTINGENT_BENEFICIARY` | Info | No contingent beneficiary assigned |
| `ZERO_ALLOCATION` | Error | {ContactName} has 0% allocation |
| `DUPLICATE_BENEFICIARY` | Error | {ContactName} is linked as beneficiary twice |

---

## Trust Overview

### Trust Hierarchy View

For each `TrustContact`, the estate planning view renders the full role hierarchy:

```
Family Trust (Revocable Living / General)
├── Grantor: John Doe
├── Trustee: John Doe
├── Successor Trustee 1: Jane Doe
├── Successor Trustee 2: First National Bank
├── Beneficiary (Primary): Jane Doe — 60%
├── Beneficiary (Primary): Mike Doe — 40%
├── Remainder Beneficiary: Doe Family Foundation
└── Trust Protector: Robert Smith, Esq.
```

### TrustOverviewDto

```csharp
/// <summary>
/// Comprehensive view of a trust for estate planning purposes.
/// Aggregates TrustContact, TrustRoles, and linked assets.
/// </summary>
public sealed record TrustOverviewDto
{
    public Guid TrustContactId { get; init; }
    public string TrustName { get; init; } = string.Empty;
    public TrustCategory Category { get; init; }
    public TrustPurpose Purpose { get; init; }
    public string? SpecificType { get; init; }
    public DateOnly? TrustDate { get; init; }
    public string? StateOfFormation { get; init; }
    public bool IsGrantorTrust { get; init; }
    public bool HasCrummeyProvisions { get; init; }
    public bool IsGstExempt { get; init; }

    /// <summary>All roles in this trust, grouped by RoleType.</summary>
    public List<TrustRoleDto> Roles { get; init; } = [];

    /// <summary>Assets linked to this trust contact (as beneficiary, trustee, etc.).</summary>
    public List<AssetContactLinkDto> LinkedAssets { get; init; } = [];

    /// <summary>Validation issues specific to this trust structure.</summary>
    public List<TrustIssue> Issues { get; init; } = [];

    /// <summary>Total value of assets where this trust is a primary beneficiary.</summary>
    public decimal TotalPrimaryBeneficiaryValue { get; init; }
}

/// <summary>
/// A structural issue with the trust configuration.
/// </summary>
public sealed record TrustIssue
{
    public string IssueCode { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
```

### Trust Issue Codes

| Code | Severity | Message |
|------|----------|---------|
| `TRUST_NO_TRUSTEE` | Warning | Trust has no trustee assigned |
| `TRUST_NO_SUCCESSOR` | Info | Trust has no successor trustee |
| `TRUST_NO_BENEFICIARY` | Warning | Trust has no beneficiary roles assigned |
| `TRUST_GRANTOR_IS_SOLE_TRUSTEE` | Info | Grantor is the only trustee — estate continuity concern |
| `TRUST_NO_ASSETS` | Info | Trust is not linked to any assets |
| `TRUST_IRREVOCABLE_NO_PROTECTOR` | Info | Irrevocable trust has no trust protector |

---

## Estate Summary

### EstateSummaryDto

```csharp
/// <summary>
/// Top-level estate planning summary aggregating coverage, trusts, and gaps.
/// </summary>
public sealed record EstateSummaryDto
{
    /// <summary>Overall beneficiary coverage analysis.</summary>
    public BeneficiaryCoverageDto Coverage { get; init; } = null!;

    /// <summary>Trust hierarchy overviews.</summary>
    public List<TrustOverviewDto> Trusts { get; init; } = [];

    /// <summary>Assets that need attention (no beneficiary or allocation issues).</summary>
    public List<EstateGapItem> Gaps { get; init; } = [];

    /// <summary>Allocation validation results across all assets.</summary>
    public List<AllocationValidationResult> AllocationValidation { get; init; } = [];

    /// <summary>Roll-up statistics.</summary>
    public EstateStatistics Statistics { get; init; } = null!;
}

/// <summary>
/// Roll-up statistics for the estate planning overview.
/// </summary>
public sealed record EstateStatistics
{
    public int TotalActiveAssets { get; init; }
    public decimal TotalEstateValue { get; init; }
    public int TotalContacts { get; init; }
    public int TotalTrusts { get; init; }
    public int AssetsFullyCovered { get; init; }
    public int AssetsPartiallyCovered { get; init; }
    public int AssetsUncovered { get; init; }
    public int AllocationWarningCount { get; init; }
    public int TrustIssueCount { get; init; }
}

/// <summary>
/// An asset that needs estate planning attention.
/// </summary>
public sealed record EstateGapItem
{
    public Guid AssetId { get; init; }
    public string AssetName { get; init; } = string.Empty;
    public AssetType AssetType { get; init; }
    public decimal? CurrentValue { get; init; }
    public string GapType { get; init; } = string.Empty;   // "NO_BENEFICIARY", "ALLOCATION_WARNING"
    public string Description { get; init; } = string.Empty;
    public string ActionUrl { get; init; } = string.Empty;  // Deep link to fix
}
```

---

## Service Interface

```csharp
/// <summary>
/// Estate planning analysis service.
/// All methods support advisor-view via ownerUserId parameter.
/// </summary>
public interface IEstatePlanningService
{
    /// <summary>
    /// Returns the full estate planning summary including coverage,
    /// trusts, gaps, and allocation validation.
    /// </summary>
    Task<EstateSummaryDto> GetEstateSummaryAsync(
        string requestingUserId, string ownerUserId);

    /// <summary>
    /// Returns detailed trust overview with role hierarchy and linked assets.
    /// </summary>
    Task<TrustOverviewDto> GetTrustOverviewAsync(
        string requestingUserId, string ownerUserId, Guid trustContactId);

    /// <summary>
    /// Validates beneficiary allocations across all assets.
    /// Returns per-asset validation results with issue details.
    /// </summary>
    Task<List<AllocationValidationResult>> ValidateAllocationsAsync(
        string requestingUserId, string ownerUserId);

    /// <summary>
    /// Returns only assets with coverage gaps (no beneficiaries or allocation issues).
    /// </summary>
    Task<List<EstateGapItem>> GetCoverageGapsAsync(
        string requestingUserId, string ownerUserId);
}
```

### Implementation — Fan-Out Pattern

```csharp
public class EstatePlanningService : IEstatePlanningService
{
    private readonly IContactService _contactService;
    private readonly IAssetService _assetService;
    private readonly IDataAccessService _authorizationService;
    private readonly ILogger<EstatePlanningService> _logger;

    public async Task<EstateSummaryDto> GetEstateSummaryAsync(
        string requestingUserId, string ownerUserId)
    {
        // Parallel fan-out to gather all needed data
        var coverageTask = _contactService.GetCoverageSummaryAsync(
            Guid.Parse(ownerUserId));
        var trustsTask = GetAllTrustOverviewsAsync(ownerUserId);
        var validationTask = ValidateAllocationsInternalAsync(ownerUserId);

        await Task.WhenAll(coverageTask, trustsTask, validationTask);

        var coverage = coverageTask.Result;
        var trusts = trustsTask.Result;
        var validation = validationTask.Result;

        // Derive gaps from coverage + validation
        var gaps = DeriveGaps(coverage, validation);

        // Build statistics
        var statistics = new EstateStatistics
        {
            TotalActiveAssets = coverage.TotalAssets,
            TotalEstateValue = coverage.Assets.Sum(a => a.CurrentValue ?? 0),
            TotalContacts = await _contactService.GetContactCountAsync(ownerUserId),
            TotalTrusts = trusts.Count,
            AssetsFullyCovered = coverage.Assets.Count(a =>
                a.HasPrimaryBeneficiary && !a.AllocationWarning),
            AssetsPartiallyCovered = coverage.Assets.Count(a =>
                a.HasPrimaryBeneficiary && a.AllocationWarning),
            AssetsUncovered = coverage.AssetsWithoutBeneficiaries,
            AllocationWarningCount = validation.Count(v => v.Issues.Count > 0),
            TrustIssueCount = trusts.Sum(t => t.Issues.Count)
        };

        return new EstateSummaryDto
        {
            Coverage = coverage,
            Trusts = trusts,
            Gaps = gaps,
            AllocationValidation = validation,
            Statistics = statistics
        };
    }
}
```

---

## API Endpoints

| Method | Route | Description | Tier |
|--------|-------|-------------|------|
| `GET` | `/api/estate` | Full estate planning summary | Premium |
| `GET` | `/api/estate/coverage` | Beneficiary coverage only | All |
| `GET` | `/api/estate/trusts` | All trust overviews | Premium |
| `GET` | `/api/estate/trusts/{id}` | Single trust detail | Premium |
| `GET` | `/api/estate/gaps` | Coverage gaps and issues | Premium |
| `GET` | `/api/estate/validation` | Allocation validation results | Premium |

### Query Parameters

All estate endpoints support:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | string | Authenticated user | Override for advisor view |

### Coverage Endpoint (Free Tier)

Free-tier users can access the basic `/api/estate/coverage` endpoint, which returns the same `BeneficiaryCoverageDto` used by the dashboard widget. The detailed estate summary, trust analysis, and allocation validation require Premium.

---

## Azure Functions

### GetEstateSummary

```csharp
/// <summary>
/// Returns the full estate planning summary. Premium only.
/// </summary>
/// <response code="200">Estate summary returned.</response>
/// <response code="402">Free-tier user — upgrade required.</response>
[Function("GetEstateSummary")]
[Authorize]
public async Task<HttpResponseData> GetEstateSummary(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "estate")]
    HttpRequestData req,
    FunctionContext context)
{
    var authenticatedUserId = context.GetUserId();
    var targetUserId = req.Query["userId"] ?? authenticatedUserId;

    if (targetUserId != authenticatedUserId)
    {
        await _authorizationService.ValidateAccessAsync(
            authenticatedUserId, targetUserId, DataCategories.Planning);
    }

    var summary = await _estatePlanningService.GetEstateSummaryAsync(
        authenticatedUserId, targetUserId);

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(summary);
    return response;
}
```

### GetCoverageGaps

```csharp
/// <summary>
/// Returns assets with estate planning gaps. Premium only.
/// </summary>
[Function("GetCoverageGaps")]
[Authorize]
public async Task<HttpResponseData> GetCoverageGaps(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "estate/gaps")]
    HttpRequestData req,
    FunctionContext context)
{
    var authenticatedUserId = context.GetUserId();
    var targetUserId = req.Query["userId"] ?? authenticatedUserId;

    if (targetUserId != authenticatedUserId)
    {
        await _authorizationService.ValidateAccessAsync(
            authenticatedUserId, targetUserId, DataCategories.Planning);
    }

    var gaps = await _estatePlanningService.GetCoverageGapsAsync(
        authenticatedUserId, targetUserId);

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(gaps);
    return response;
}
```

### GetTrustOverview

```csharp
/// <summary>
/// Returns detailed trust overview with role hierarchy. Premium only.
/// </summary>
[Function("GetTrustOverview")]
[Authorize]
public async Task<HttpResponseData> GetTrustOverview(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "estate/trusts/{trustContactId}")]
    HttpRequestData req,
    string trustContactId,
    FunctionContext context)
{
    var authenticatedUserId = context.GetUserId();
    var targetUserId = req.Query["userId"] ?? authenticatedUserId;

    if (targetUserId != authenticatedUserId)
    {
        await _authorizationService.ValidateAccessAsync(
            authenticatedUserId, targetUserId, DataCategories.Planning);
    }

    var trustId = Guid.Parse(trustContactId);
    var overview = await _estatePlanningService.GetTrustOverviewAsync(
        authenticatedUserId, targetUserId, trustId);

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(overview);
    return response;
}
```

---

## TypeScript Types

```typescript
/** Full estate planning summary. */
interface EstateSummaryDto {
  coverage: BeneficiaryCoverageDto;
  trusts: TrustOverviewDto[];
  gaps: EstateGapItem[];
  allocationValidation: AllocationValidationResult[];
  statistics: EstateStatistics;
}

interface EstateStatistics {
  totalActiveAssets: number;
  totalEstateValue: number;
  totalContacts: number;
  totalTrusts: number;
  assetsFullyCovered: number;
  assetsPartiallyCovered: number;
  assetsUncovered: number;
  allocationWarningCount: number;
  trustIssueCount: number;
}

interface EstateGapItem {
  assetId: string;
  assetName: string;
  assetType: AssetType;
  currentValue: number | null;
  gapType: string;
  description: string;
  actionUrl: string;
}

interface TrustOverviewDto {
  trustContactId: string;
  trustName: string;
  category: TrustCategory;
  purpose: TrustPurpose;
  specificType?: string;
  trustDate?: string;
  stateOfFormation?: string;
  isGrantorTrust: boolean;
  hasCrummeyProvisions: boolean;
  isGstExempt: boolean;
  roles: TrustRoleDto[];
  linkedAssets: AssetContactLinkDto[];
  issues: TrustIssue[];
  totalPrimaryBeneficiaryValue: number;
}

interface TrustIssue {
  issueCode: string;
  severity: string;
  message: string;
}

interface AllocationValidationResult {
  assetId: string;
  assetName: string;
  primaryTotal: number;
  contingentTotal: number;
  primaryValid: boolean;
  contingentValid: boolean;
  issues: AllocationIssue[];
}

interface AllocationIssue {
  issueCode: string;
  severity: string;
  message: string;
  contactId?: string;
  contactDisplayName?: string;
}

/** Referenced types — not redefined here. */
// BeneficiaryCoverageDto → see 04-contacts-beneficiaries.md
// TrustRoleDto           → see 04-contacts-beneficiaries.md
// AssetContactLinkDto    → see 04-contacts-beneficiaries.md
```

---

## React Query Hooks

```typescript
import { useQuery } from '@tanstack/react-query';

/**
 * Fetches the full estate planning summary. Premium only.
 *
 * @param userId - Optional override for advisor viewing client estate.
 * @returns Estate summary with coverage, trusts, gaps, and validation.
 */
export function useEstateSummary(userId?: string) {
  return useQuery({
    queryKey: ['estate', 'summary', userId ?? 'me'],
    queryFn: () => fetchEstateSummary(userId),
    staleTime: 10 * 60 * 1000,    // 10 minutes — estate data changes infrequently
  });
}

/**
 * Fetches beneficiary coverage analysis. Available to all tiers.
 *
 * @param userId - Optional override for advisor view.
 * @returns Coverage data with per-asset breakdown.
 */
export function useBeneficiaryCoverage(userId?: string) {
  return useQuery({
    queryKey: ['estate', 'coverage', userId ?? 'me'],
    queryFn: () => fetchBeneficiaryCoverage(userId),
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Fetches detailed trust overview.
 *
 * @param trustContactId - The trust contact ID.
 * @param userId - Optional override for advisor view.
 */
export function useTrustOverview(trustContactId: string, userId?: string) {
  return useQuery({
    queryKey: ['estate', 'trust', trustContactId, userId ?? 'me'],
    queryFn: () => fetchTrustOverview(trustContactId, userId),
    staleTime: 10 * 60 * 1000,
    enabled: !!trustContactId,
  });
}

/**
 * Fetches coverage gaps. Premium only.
 */
export function useCoverageGaps(userId?: string) {
  return useQuery({
    queryKey: ['estate', 'gaps', userId ?? 'me'],
    queryFn: () => fetchCoverageGaps(userId),
    staleTime: 10 * 60 * 1000,
  });
}
```

---

## UI Layout

### Estate Planning Page — Desktop (≥ md)

```
┌──────────────────────────────────────────────────────────┐
│  Estate Planning                                          │
├──────────────────────────────────────────────────────────┤
│  Coverage Overview                                        │
│  ████████████████░░░░ 80% covered  (8/10 assets)         │
│                                                           │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│  │ 10       │ │ $547K    │ │ 2        │ │ 3        │    │
│  │ Assets   │ │ Estate   │ │ Trusts   │ │ Warnings │    │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘    │
├──────────────────────────────────────────────────────────┤
│  ⚠ Gaps & Issues (3)                                     │
│  ┌────────────────────────────────────────────────────┐  │
│  │ 🔴 Tesla Model Y — No beneficiary assigned         │  │
│  │    [Assign beneficiary →]                           │  │
│  │ 🟡 Primary Home — Primary allocation total: 70%    │  │
│  │    [Fix allocation →]                               │  │
│  │ 🟡 Investment Account — No contingent beneficiary   │  │
│  │    [Add contingent →]                               │  │
│  └────────────────────────────────────────────────────┘  │
├────────────────────────┬─────────────────────────────────┤
│  Trusts (2)            │  All Assets Coverage Detail      │
│  ┌──────────────────┐  │  ┌───────────────────────────┐  │
│  │ Family Trust      │  │  │ Primary Home        ✅   │  │
│  │ Revocable Living  │  │  │ Tesla Model Y       ❌   │  │
│  │ 3 roles, 2 assets │  │  │ Investment Acct     ⚠️   │  │
│  │ [View details →]  │  │  │ Retirement 401k     ✅   │  │
│  ├──────────────────┤  │  │ Bank Account        ✅   │  │
│  │ Insurance Trust   │  │  │ Life Insurance      ✅   │  │
│  │ Irrevocable/ILIT  │  │  │ Business LLC        ❌   │  │
│  │ 4 roles, 1 asset  │  │  │ Crypto Wallet       ✅   │  │
│  │ [View details →]  │  │  │ Art Collection      ✅   │  │
│  └──────────────────┘  │  │ Rental Property     ✅   │  │
│                        │  └───────────────────────────┘  │
└────────────────────────┴─────────────────────────────────┘
```

### Mobile Layout (< md)

Sections stack vertically:

1. Coverage progress bar (hero)
2. Statistics row (horizontal scroll)
3. Gaps & Issues (expandable accordion)
4. All Assets coverage list
5. Trusts (expandable cards)

### Responsive Implementation

```tsx
/**
 * Estate planning page with coverage analysis and trust management.
 * Available to all users (basic coverage) and Premium (full analysis).
 */
export function EstatePlanningPage() {
  const { t } = useTranslation();
  const { isPremium } = useSubscription();

  // Basic coverage for all tiers
  const { data: coverage, isLoading: coverageLoading } = useBeneficiaryCoverage();

  // Full summary for Premium
  const { data: estate, isLoading: estateLoading } = useEstateSummary();

  if (coverageLoading) return <EstateSkeleton />;

  return (
    <div className="space-y-4 md:space-y-6">
      {/* Coverage Overview — available to all tiers */}
      <CoverageOverviewCard coverage={coverage} />

      {/* Statistics */}
      {estate && <EstateStatisticsRow statistics={estate.statistics} />}

      {/* Premium: Gaps & Issues */}
      <PremiumGate feature={t('estate.gaps')}>
        {estate && estate.gaps.length > 0 && (
          <GapsAndIssuesCard gaps={estate.gaps} />
        )}
      </PremiumGate>

      {/* Premium: Two-column layout on desktop */}
      <PremiumGate feature={t('estate.details')}>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 md:gap-6">
          <TrustListCard trusts={estate?.trusts ?? []} />
          <AssetCoverageListCard assets={coverage?.assets ?? []} />
        </div>
      </PremiumGate>
    </div>
  );
}
```

### Trust Detail Page

When a user clicks "View details" on a trust, a detail view renders:

```tsx
/**
 * Trust detail page showing full role hierarchy and linked assets.
 */
export function TrustDetailPage({ trustContactId }: { trustContactId: string }) {
  const { t } = useTranslation();
  const { data: trust, isLoading } = useTrustOverview(trustContactId);

  if (isLoading) return <TrustDetailSkeleton />;
  if (!trust) return <NotFoundMessage />;

  return (
    <div className="space-y-4 md:space-y-6">
      {/* Trust Header */}
      <Card className="p-6">
        <h1 className="text-2xl font-bold">{trust.trustName}</h1>
        <div className="flex flex-wrap gap-2 mt-2">
          <Badge>{trust.category}</Badge>
          <Badge variant="outline">{trust.purpose}</Badge>
          {trust.specificType && <Badge variant="secondary">{trust.specificType}</Badge>}
        </div>
        <dl className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-4 text-sm">
          <div>
            <dt className="text-muted-foreground">{t('estate.trust.date')}</dt>
            <dd>{trust.trustDate ?? '—'}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t('estate.trust.state')}</dt>
            <dd>{trust.stateOfFormation ?? '—'}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t('estate.trust.grantor')}</dt>
            <dd>{trust.isGrantorTrust ? t('common.yes') : t('common.no')}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t('estate.trust.value')}</dt>
            <dd>{formatCurrency(trust.totalPrimaryBeneficiaryValue)}</dd>
          </div>
        </dl>
      </Card>

      {/* Issues */}
      {trust.issues.length > 0 && <TrustIssuesList issues={trust.issues} />}

      {/* Role Hierarchy */}
      <RoleHierarchyCard roles={trust.roles} />

      {/* Linked Assets */}
      <LinkedAssetsCard assets={trust.linkedAssets} />
    </div>
  );
}
```

---

## Per-Stirpes Distribution Modeling

Per-stirpes distribution means that if a beneficiary predeceases the asset owner, the beneficiary's share passes to their descendants equally. The UI models this visually:

```
Asset: Primary Home ($450,000)
├── Primary Beneficiary: Jane Doe — 60% ($270,000)
│   └── (per stirpes: if Jane predeceases, share splits to her children)
├── Primary Beneficiary: Mike Doe — 40% ($180,000)
│   └── (per stirpes: disabled — Mike has no designated descendants)
└── Contingent: Family Trust — 100% ($450,000)
```

> Per-stirpes modeling in MVP is **informational only** — the platform does not track descendant contacts automatically. Users set the `PerStirpes` flag on `AssetContactLink` and the UI explains the legal implication.

---

## Tier Gating

| Feature | Free | Premium |
|---------|------|---------|
| Beneficiary coverage (basic %) | ✅ | ✅ |
| Per-asset coverage list | ✅ | ✅ |
| Full estate summary | ❌ | ✅ |
| Allocation validation | ❌ | ✅ |
| Trust analysis | ❌ | ✅ |
| Coverage gap detection | ❌ | ✅ |

---

## Caching Strategy

| Data | Cache TTL | Invalidation |
|------|-----------|-------------|
| Estate summary | 10 minutes (Redis) | Contact/Asset/AssetContactLink mutation |
| Beneficiary coverage | 5 minutes (Redis) | AssetContactLink mutation |
| Trust overview | 10 minutes (Redis) | TrustRole mutation |

### Cache Key Pattern

```
estate:{userId}:summary        → EstateSummaryDto
estate:{userId}:coverage       → BeneficiaryCoverageDto
estate:{userId}:trust:{id}     → TrustOverviewDto
estate:{userId}:gaps           → List<EstateGapItem>
estate:{userId}:validation     → List<AllocationValidationResult>
```

---

## Security & Access Control

### Authorization Model

Estate planning operations follow the **three-tier authorization pattern** from [03-authorization-data-access.md](03-authorization-data-access.md):

| Tier | Access Level | Description |
|------|-------------|-------------|
| **Owner** | Full analysis | Authenticated user viewing their own estate plan |
| **Granted** | Read-only analysis | Advisor with `DataAccessGrant` including `Planning` category |
| **Administrator** | Full read, audit | Platform admins |

### Operation-Level Access Control

| Operation | Owner | Granted (Advisor) | Administrator | Unauthenticated |
|-----------|-------|--------------------|---------------|-----------------|
| View coverage (basic) | ✅ | ✅ (read-only) | ✅ | ❌ 401 |
| View estate summary | ✅ (Premium) | ✅ (read-only, Premium) | ✅ | ❌ 401 |
| View trust details | ✅ (Premium) | ✅ (read-only, Premium) | ✅ | ❌ 401 |
| View coverage gaps | ✅ (Premium) | ✅ (read-only, Premium) | ✅ | ❌ 401 |
| View allocation validation | ✅ (Premium) | ✅ (read-only, Premium) | ✅ | ❌ 401 |

### Data Privacy

- SSN and EIN values are **never included** in estate planning DTOs — only masked formats from [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md). 
- Estate planning is read-only aggregation. No mutations happen through estate endpoints — all changes go through Contact and Asset APIs.

### Audit Logging

| Event | Logged Data | Severity |
|-------|------------|----------|
| Estate summary viewed (advisor/admin) | RequestingUserId, OwnerUserId | Information |
| Trust detail viewed (advisor/admin) | RequestingUserId, OwnerUserId, TrustId | Information |
| Unauthorized estate access attempt | RequestingUserId, TargetUserId | **Warning** |

---

## Error Codes

| Code | HTTP | Condition |
|------|------|-----------|
| `AUTH_REQUIRED` | 401 | Not authenticated |
| `AUTH_FORBIDDEN` | 403 | No DataAccessGrant for target user's Planning category |
| `TIER_PREMIUM_REQUIRED` | 402 | Free user accessing detailed estate analysis |
| `RESOURCE_NOT_FOUND` | 404 | Trust contact ID does not exist or doesn't belong to user |
| `SERVER_ERROR` | 500 | Internal aggregation failure |

---

## Navigation

Estate planning is accessible from:

| Entry Point | Location | Roles |
|-------------|----------|-------|
| Sidebar nav | `/estate` | Client, Advisor, Admin |
| Dashboard widget | Beneficiary coverage card → "View estate plan →" | All |
| Asset detail | "Beneficiaries" tab → "Estate planning →" | All |
| Contact detail | Trust contact → "View in estate plan →" | All |

---

## Cross-References

- Contact model & beneficiary coverage: [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md)
- Asset model & AssetContactLink: [05-assets-portfolio.md](05-assets-portfolio.md)
- Dashboard beneficiary widget: [07-dashboard-reporting.md](07-dashboard-reporting.md)
- Authorization & DataAccessGrant: [03-authorization-data-access.md](03-authorization-data-access.md)
- DataCategory.Planning flag: [03-authorization-data-access.md](03-authorization-data-access.md)
- Tier model: [01-platform-infrastructure.md](01-platform-infrastructure.md)

---

*Last Updated: February 2026*
