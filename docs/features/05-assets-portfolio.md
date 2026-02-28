# 05 — Assets & Portfolio

> Asset entity hierarchy, 12 asset types with typed metadata, depreciation, disposal, valuation, documents, sensitive field handling, and tier limits.

**ADO Tracking:** [Epic #331 — 05 - Assets & Portfolio Management](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/331)

| # | Feature | State |
|---|---------|-------|
| [332](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/332) | Assets Page UI | In Progress |
| [333](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/333) | Asset Form & CRUD UI | New |
| [334](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/334) | Asset Service & API | Done |

---

## Overview

RAJ Financial tracks **12 asset types** through a typed inheritance hierarchy — **not** a flat entity with a JSON metadata column. Each type has its own `sealed record` metadata class. The hierarchy enables:

- **FinancialAccount** intermediate layer sharing `accountNumber` + `institutionName`
- **Per-type validation** enforced at compile time (required fields per metadata record)
- **Per-type forms** in the UI (each asset type renders its own metadata section)
- **Type-safe serialization** via discriminated `AssetType` enum

---

## Type Hierarchy

```
BaseAsset (abstract — core fields + depreciation, disposal, valuation, audit)
├── Vehicle               → VehicleMetadata
├── RealEstate            → RealEstateMetadata
├── Business              → BusinessMetadata
├── PersonalProperty      → PersonalPropertyMetadata
├── Collectible           → CollectibleMetadata
├── IntellectualProperty  → IntellectualPropertyMetadata
├── Other                 → OtherAssetMetadata
└── FinancialAccount (abstract — accountNumber, institutionName)
    ├── Investment        → InvestmentMetadata (with Holding, VestingEvent)
    ├── Retirement        → RetirementMetadata (with EmployerMatchTier)
    ├── BankAccount       → BankAccountMetadata
    ├── Insurance         → InsuranceMetadata (with PolicyRider)
    └── Cryptocurrency    → CryptocurrencyMetadata (with CryptoHolding)
```

### AssetType Enum

```csharp
public enum AssetType
{
    RealEstate           = 0,
    Vehicle              = 1,
    Investment           = 2,
    Retirement           = 3,
    BankAccount          = 4,
    Insurance            = 5,
    Business             = 6,
    PersonalProperty     = 7,
    Collectible          = 8,
    Cryptocurrency       = 9,
    IntellectualProperty = 10,
    Other                = 99   // Values 11–98 reserved
}
```

---

## BaseAsset (Abstract)

EF Core maps all 12 types using **TPH (Table-Per-Hierarchy)** with `AssetType` as discriminator.

### Tenant Isolation

BaseAsset carries **both** `UserId` and `TenantId` (unlike Contact which uses `UserId` only). This enables future advisor-client data sharing within a tenant.

### Core Fields

| Field | C# Type | Required | Notes |
|-------|---------|----------|-------|
| `id` | `Guid` | Auto | Primary key |
| `userId` | `string` | Yes | Owner (Entra Object ID) |
| `tenantId` | `string` | Yes | Tenant scope |
| `name` | `string` | **Yes** | Max 200 chars |
| `type` | `AssetType` | **Yes** | Enum discriminator |
| `currentValue` | `decimal?` | No | ≥ 0. UI shows "Value Unknown" when null |
| `description` | `string?` | No | Max 2000 chars |
| `purchasePrice` | `decimal?` | No | ≥ 0 |
| `purchaseDate` | `DateTimeOffset?` | No | |
| `location` | `string?` | No | Max 500 chars |
| `isDepreciable` | `bool` | Yes | Computed from type + user toggle |
| `isDisposed` | `bool` | Yes | Default: false |
| `hasBeneficiaries` | `bool` | Yes | Computed from AssetContactLink records where Role == Beneficiary |
| `createdAt` | `DateTimeOffset` | Auto | |
| `updatedAt` | `DateTimeOffset?` | Auto | |

### Depreciation Fields (only when `isDepreciable`)

| Field | C# Type | Notes |
|-------|---------|-------|
| `depreciationMethod` | `DepreciationMethod?` | None, StraightLine, DecliningBalance, Macrs |
| `salvageValue` | `decimal?` | ≥ 0 |
| `usefulLifeMonths` | `int?` | > 0 |
| `inServiceDate` | `DateTimeOffset?` | |
| `accumulatedDepreciation` | `decimal?` | Computed at read time |
| `bookValue` | `decimal?` | Computed at read time |
| `monthlyDepreciation` | `decimal?` | Computed at read time |
| `depreciationPercentComplete` | `decimal?` | 0.0–1.0, computed |

```csharp
public enum DepreciationMethod
{
    None             = 0,
    StraightLine     = 1,
    DecliningBalance = 2,
    Macrs            = 3
}
```

**Depreciable asset types** (user can toggle):

| Type | Default Depreciable | Rationale |
|------|---------------------|-----------|
| Vehicle | Yes | Standard auto depreciation |
| RealEstate | Yes (rental/commercial) | IRS schedules |
| PersonalProperty | Yes (some) | Electronics, furniture depreciate; jewelry doesn't |
| IntellectualProperty | Yes | Patents amortize over useful life |
| Business | No | Business value isn't depreciated directly |
| Other | Toggle | Depends on what it is |
| All Financial types | No | Financial instruments don't depreciate |

### Disposal Fields (only when `isDisposed`)

| Field | C# Type | Notes |
|-------|---------|-------|
| `disposalDate` | `DateTimeOffset?` | |
| `disposalPrice` | `decimal?` | |
| `disposalNotes` | `string?` | |

### Valuation Fields

| Field | C# Type | Notes |
|-------|---------|-------|
| `marketValue` | `decimal?` | ≥ 0 |
| `lastValuationDate` | `DateTimeOffset?` | |

### Navigation Properties

```csharp
/// <summary>Navigation to AssetContactLink records (beneficiaries, co-owners, etc.).</summary>
public ICollection<AssetContactLink> ContactLinks { get; set; } = [];

/// <summary>Navigation to AssetDocument records.</summary>
public ICollection<AssetDocument> Documents { get; set; } = [];
```

---

## FinancialAccount (Abstract)

Intermediate abstract type for asset types that represent accounts at financial institutions.

**Inheritors**: Investment, Retirement, BankAccount, Insurance, Cryptocurrency

| Field | C# Type | Required | Notes |
|-------|---------|----------|-------|
| `accountNumber` | `string?` | No | Max 100. **Encrypted at rest**, masked in API responses |
| `institutionName` | `string` | **Yes** (for financial types) | Max 200. e.g. Fidelity, Chase, Coinbase |

```csharp
public abstract class FinancialAccount : BaseAsset
{
    [SensitiveData(VisibleChars = 4)]
    public string? AccountNumber { get; set; }

    public string InstitutionName { get; set; } = string.Empty;
}
```

---

## 12 Asset Types — Metadata Records

Each type has a `sealed record` metadata class. Metadata is stored as a **typed property** on the DTO, discriminated by `AssetType`. See the full field listings and C# class definitions in [ASSET_TYPE_SPECIFICATIONS.md](../ASSET_TYPE_SPECIFICATIONS.md).

### Summary Table

| # | Type | Metadata Record | Inherits From | Key Fields | Depreciable |
|---|------|-----------------|---------------|------------|-------------|
| 1 | Vehicle | `VehicleMetadata` | BaseAsset | make, model, year, vin, mileage | Yes |
| 2 | RealEstate | `RealEstateMetadata` | BaseAsset | address, propertyType, sqft, beds/baths | Yes |
| 3 | Investment | `InvestmentMetadata` | FinancialAccount | accountType, holdings[], RSU vesting | No |
| 4 | Retirement | `RetirementMetadata` | FinancialAccount | accountType, employerMatchTiers[], vesting% | No |
| 5 | BankAccount | `BankAccountMetadata` | FinancialAccount | bankAccountType, routingNumber, apy | No |
| 6 | Insurance | `InsuranceMetadata` | FinancialAccount | policyType, cashValue, deathBenefit, riders[] | No |
| 7 | Business | `BusinessMetadata` | BaseAsset | entityType, ownership%, ein, registrations[] | No |
| 8 | PersonalProperty | `PersonalPropertyMetadata` | BaseAsset | category, condition, brand, insuredValue | Toggle |
| 9 | Collectible | `CollectibleMetadata` | BaseAsset | category, condition, grade, provenance | No |
| 10 | Cryptocurrency | `CryptocurrencyMetadata` | FinancialAccount | walletType, cryptoHoldings[] | No |
| 11 | IntellectualProperty | `IntellectualPropertyMetadata` | BaseAsset | ipType, registrationNumber, royaltyRate | Yes |
| 12 | Other | `OtherAssetMetadata` | BaseAsset | category, customFields (key-value) | Toggle |

### Metadata Architecture

Metadata records are **immutable DTOs** (`sealed record`), not EF entities. In the database, per-type metadata is stored as a **JSON column** mapped via EF Core's `ToJson()`. In the API, metadata is a discriminated property:

```csharp
public class AssetDto
{
    // ... base fields ...
    public AssetType Type { get; init; }

    // Discriminated metadata — only the matching type is non-null
    public VehicleMetadata? Vehicle { get; init; }
    public RealEstateMetadata? RealEstate { get; init; }
    public InvestmentMetadata? Investment { get; init; }
    public RetirementMetadata? Retirement { get; init; }
    public BankAccountMetadata? BankAccount { get; init; }
    public InsuranceMetadata? Insurance { get; init; }
    public BusinessMetadata? Business { get; init; }
    public PersonalPropertyMetadata? PersonalProperty { get; init; }
    public CollectibleMetadata? Collectible { get; init; }
    public CryptocurrencyMetadata? Cryptocurrency { get; init; }
    public IntellectualPropertyMetadata? IntellectualProperty { get; init; }
    public OtherAssetMetadata? Other { get; init; }
}
```

### Nested Records (Arrays within Metadata)

Several metadata types contain nested arrays of sub-records:

| Metadata | Nested Record | Purpose |
|----------|---------------|---------|
| `InvestmentMetadata` | `Holding` | Brokerage positions (ticker, shares, costBasis, currentPrice) |
| `InvestmentMetadata` | `VestingEvent` | RSU vesting schedule entries (date, shares) |
| `RetirementMetadata` | `EmployerMatchTier` | Tiered employer match (matchPercent, onFirst) |
| `InsuranceMetadata` | `PolicyRider` | Policy add-ons (riderType, value, annualCost) |
| `BusinessMetadata` | `StateRegistration` | State filings (state, sosNumber, isFormationState) |
| `CryptocurrencyMetadata` | `CryptoHolding` | Coin/token positions (symbol, quantity, staking) |

### Shared Enum

```csharp
/// <summary>
/// Shared condition enum for PersonalProperty and Collectible types.
/// </summary>
public enum ItemCondition
{
    Mint, Excellent, Good, Fair, Poor
}
```

---

## Sensitive Field Handling

### Principle

**Never return raw sensitive values by default.** The API returns masked representations; full values are revealed via a separate secure endpoint.

### SensitiveData Attribute

```csharp
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveDataAttribute : Attribute
{
    /// <summary>Number of trailing characters to show unmasked.</summary>
    public int VisibleChars { get; init; } = 4;

    /// <summary>The mask character to use.</summary>
    public char MaskChar { get; init; } = '•';
}
```

### Sensitive Fields Registry

| Asset Type | Field | Mask Format | Example |
|------------|-------|-------------|---------|
| FinancialAccount (all subtypes) | `accountNumber` | Last 4 digits | `••••1234` |
| BankAccount | `routingNumber` | Last 4 digits | `••••5678` |
| Insurance | `accountNumber` (policy #) | Last 4 chars | `••••A789` |
| Vehicle | `vin` | Last 6 chars | `•••••••••••234567` |

### API Response Pattern

```json
{
  "accountNumber": "••••1234",
  "accountNumberMasked": true
}
```

### Reveal Endpoint

`GET /api/assets/{id}/sensitive/{fieldName}`

- Same authorization as parent asset (tenant + ownership)
- Returns decrypted, unmasked value
- **Audit-logged**: userId, assetId, fieldName, timestamp
- Response: `{ "fieldName": "accountNumber", "value": "123456781234" }`

### Storage

- Column-level encryption (Azure SQL Always Encrypted or application-layer AES-256)
- Keys managed via Azure Key Vault
- Masking operates after decryption in API layer

### UI Pattern

- **Eye icon** (👁) → fetch & reveal via separate API call
- Auto-hides after 30s (configurable)
- **Lock icon** (🔒) → re-mask immediately
- Opt-in "always show" per field in Settings > Privacy
- Full value never persisted in client state beyond active viewport

---

## Asset Documents

Any asset type can have N linked documents.

### AssetDocument Entity

```csharp
public sealed record AssetDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid AssetId { get; init; }
    public required AssetDocumentType DocumentType { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
    public required string StorageUrl { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
```

### AssetDocumentType Enum

```csharp
public enum AssetDocumentType
{
    OperatingAgreement, ArticlesOfIncorporation, Amendment,
    SOSRegistration, Deed, Title, Registration, BillOfSale,
    PolicyDocument, BeneficiaryDesignation, AccountStatement,
    TaxDocument, Appraisal, Survey, ClosingDisclosure,
    PatentFiling, TrademarkCertificate, LicenseAgreement,
    Receipt, Warranty, Other
}
```

**Storage**: Azure Blob Storage. URLs not exposed to client — generated SAS tokens for download.

### Document API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/assets/{id}/documents` | List documents for an asset |
| `POST` | `/api/assets/{id}/documents` | Upload document |
| `GET` | `/api/assets/{id}/documents/{docId}` | Get document metadata |
| `GET` | `/api/assets/{id}/documents/{docId}/download` | Download (SAS URL redirect) |
| `DELETE` | `/api/assets/{id}/documents/{docId}` | Delete document |

---

## API Endpoints

### Asset CRUD

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/assets` | List all assets (optional `?type=Vehicle&disposed=false`) |
| `GET` | `/api/assets/{id}` | Get asset detail with metadata |
| `POST` | `/api/assets` | Create asset (includes metadata by type) |
| `PUT` | `/api/assets/{id}` | Update asset + metadata |
| `DELETE` | `/api/assets/{id}` | Delete asset (cascades to AssetContactLink + Documents) |

### Sensitive Fields

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/assets/{id}/sensitive/{fieldName}` | Reveal masked field value |

### Asset-Contact Links

Defined in [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md) — included here for cross-reference:

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/assets/{id}/contacts` | List contacts linked to asset |
| `POST` | `/api/assets/{id}/contacts` | Link contact to asset |

### Depreciation

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/assets/{id}/depreciation` | Get computed depreciation schedule |

---

## Service Interface

```csharp
public interface IAssetService
{
    // CRUD
    Task<IEnumerable<AssetSummaryDto>> GetAssetsAsync(string userId, AssetType? filterType = null, bool? isDisposed = null);
    Task<AssetDto> GetAssetByIdAsync(string userId, Guid assetId);
    Task<AssetDto> CreateAssetAsync(string userId, CreateAssetRequest request);
    Task<AssetDto> UpdateAssetAsync(string userId, Guid assetId, UpdateAssetRequest request);
    Task DeleteAssetAsync(string userId, Guid assetId);

    // Documents
    Task<IEnumerable<AssetDocumentDto>> GetDocumentsAsync(string userId, Guid assetId);
    Task<AssetDocumentDto> UploadDocumentAsync(string userId, Guid assetId, UploadDocumentRequest request);
    Task<string> GetDownloadUrlAsync(string userId, Guid assetId, Guid documentId);
    Task DeleteDocumentAsync(string userId, Guid assetId, Guid documentId);

    // Sensitive fields
    Task<SensitiveFieldValue> RevealSensitiveFieldAsync(string userId, Guid assetId, string fieldName);

    // Depreciation
    Task<DepreciationScheduleDto> GetDepreciationScheduleAsync(string userId, Guid assetId);

    // Portfolio summary
    Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(string userId);
}
```

---

## Request/Response DTOs

### CreateAssetRequest

```csharp
public sealed record CreateAssetRequest
{
    public required string Name { get; init; }
    public required AssetType Type { get; init; }
    public decimal? CurrentValue { get; init; }
    public string? Description { get; init; }
    public decimal? PurchasePrice { get; init; }
    public DateTimeOffset? PurchaseDate { get; init; }
    public string? Location { get; init; }

    // FinancialAccount fields (required when Type is a financial type)
    public string? AccountNumber { get; init; }
    public string? InstitutionName { get; init; }

    // Depreciation
    public bool? IsDepreciable { get; init; }
    public DepreciationMethod? DepreciationMethod { get; init; }
    public decimal? SalvageValue { get; init; }
    public int? UsefulLifeMonths { get; init; }
    public DateTimeOffset? InServiceDate { get; init; }

    // Per-type metadata (only the one matching Type is provided)
    public VehicleMetadata? Vehicle { get; init; }
    public RealEstateMetadata? RealEstate { get; init; }
    public InvestmentMetadata? Investment { get; init; }
    public RetirementMetadata? Retirement { get; init; }
    public BankAccountMetadata? BankAccount { get; init; }
    public InsuranceMetadata? Insurance { get; init; }
    public BusinessMetadata? Business { get; init; }
    public PersonalPropertyMetadata? PersonalProperty { get; init; }
    public CollectibleMetadata? Collectible { get; init; }
    public CryptocurrencyMetadata? Cryptocurrency { get; init; }
    public IntellectualPropertyMetadata? IntellectualProperty { get; init; }
    public OtherAssetMetadata? Other { get; init; }
}
```

### AssetSummaryDto (List View)

```csharp
public sealed record AssetSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public AssetType Type { get; init; }
    public decimal? CurrentValue { get; init; }
    public bool IsDisposed { get; init; }
    public bool HasBeneficiaries { get; init; }
    public int DocumentCount { get; init; }
    public int ContactLinkCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

### PortfolioSummaryDto

```csharp
public sealed record PortfolioSummaryDto
{
    public decimal TotalValue { get; init; }
    public int TotalAssets { get; init; }
    public int DisposedAssets { get; init; }
    public Dictionary<AssetType, AssetTypeSummary> ByType { get; init; } = new();
}

public sealed record AssetTypeSummary
{
    public int Count { get; init; }
    public decimal TotalValue { get; init; }
}
```

---

## Validation Rules

### Base Asset Validation

| Rule | Level |
|------|-------|
| `name` is required, max 200 chars | Error |
| `type` must be a valid `AssetType` enum value | Error |
| `currentValue` must be ≥ 0 when provided | Error |
| `purchasePrice` must be ≥ 0 when provided | Error |
| Financial types require `institutionName` | Error |
| Metadata for the declared `type` must be provided | Error |
| Cannot provide metadata for a different type | Warning |

### Depreciation Validation

| Rule | Level |
|------|-------|
| `usefulLifeMonths` must be > 0 when provided | Error |
| `salvageValue` must be ≥ 0 when provided | Error |
| Depreciation fields only valid when `isDepreciable` | Warning |
| Non-depreciable types (all financial) cannot enable depreciation | Error |

### Document Validation

| Rule | Level |
|------|-------|
| File size ≤ 20MB per document | Error |
| ContentType must be allowed (PDF, PNG, JPG, DOCX, XLSX) | Error |
| Free tier: ≤ 5 document uploads per month | Error |

---

## Liabilities (Future)

A `Liability` entity linked to assets via `linkedAssetId` FK. Enables net-worth calculations.

| Type | Example Link |
|------|-------------|
| Mortgage | → RealEstate |
| AutoLoan | → Vehicle |
| PolicyLoan | → Insurance |
| CreditCard | (no asset link) |
| StudentLoan | (no asset link) |
| PersonalLoan | (varies) |
| HELOC | → RealEstate |

**Not in MVP** — Asset detail pages show an empty "Linked Liabilities" card with "Coming Soon".

```csharp
public enum LiabilityType
{
    Mortgage, AutoLoan, StudentLoan, CreditCard,
    PolicyLoan, PersonalLoan, HELOC, Other
}
```

---

## UI Patterns

### Asset List Page

- Grid/table with: Name, Type (icon + label), Value, Status (Active/Disposed)
- Filter by type, sort by value/name/date
- Card layout on mobile, table on desktop

### Asset Detail Page

- **Header**: Name, type badge, current value, edit/dispose/delete actions
- **Metadata card**: Type-specific fields rendered by per-type form component
- **Contacts card**: Linked beneficiaries, co-owners, etc. (from [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md))
- **Documents card**: Upload, view, download, delete
- **Depreciation card** (when applicable): Schedule, book value, chart
- **Liabilities card** (future): Linked debts

### Per-Type Form Sections

Each asset type renders its own metadata form section. The UI selects the component based on `AssetType`:

```tsx
const METADATA_FORMS: Record<AssetType, React.ComponentType> = {
  Vehicle: VehicleForm,
  RealEstate: RealEstateForm,
  Investment: InvestmentForm,
  // ... etc.
};
```

---

## Tier Limits

| Limit | Free | Premium |
|-------|------|---------|
| Total assets | 10 | Unlimited |
| Document uploads | 5/month | Unlimited |
| Document storage | 100 MB | Unlimited |

When limits are reached, API returns `429` with error code `TIER_LIMIT_REACHED`.

---

## Cross-References

- Per-type field definitions & C# metadata records: [../ASSET_TYPE_SPECIFICATIONS.md](../ASSET_TYPE_SPECIFICATIONS.md)
- Contact linking & beneficiary designations: [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md)
- Sensitive field masking (shared with contacts): [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md) § Sensitive Field Handling
- Data sharing (Assets category in DataAccessGrant): [03-authorization-data-access.md](03-authorization-data-access.md)
- Platform infrastructure & tech stack: [01-platform-infrastructure.md](01-platform-infrastructure.md)

---

*Last Updated: February 2026*
