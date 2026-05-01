# Asset Type Specifications

> **Purpose**: Defines the per-type metadata fields for each asset type, the type hierarchy, and cross-cutting design decisions.
> **Status**: All 12 types locked.

---

## Table of Contents

1. [Design Decisions](#design-decisions)
2. [Type Hierarchy](#type-hierarchy)
3. [Base Asset Fields](#base-asset-fields)
4. [Financial Account (Abstract)](#financial-account-abstract)
5. [Asset Types](#asset-types)
   - [1. Vehicle](#1-vehicle)
   - [2. Real Estate](#2-real-estate)
   - [3. Investment](#3-investment)
   - [4. Retirement](#4-retirement)
   - [5. Bank Account](#5-bank-account)
   - [6. Insurance](#6-insurance)
   - [7. Business](#7-business)
   - [8. Personal Property](#8-personal-property)
   - [9. Collectible](#9-collectible)
   - [10. Cryptocurrency](#10-cryptocurrency)
   - [11. Intellectual Property](#11-intellectual-property)
   - [12. Other](#12-other)
6. [Sensitive Field Handling](#sensitive-field-handling)
7. [Asset Documents](#asset-documents)
8. [Liabilities (Future)](#liabilities-future)
9. [Beneficiary Entity (Cross-Reference)](#beneficiary-entity-cross-reference)
10. [Serialization Migration](#serialization-migration)

---

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Monetary field type | `decimal` (C#) / `number` (TS) | Financial precision; `double` rounds incorrectly |
| Serializer | **MemoryPack** (DTOs) / **System.Text.Json** (public APIs only) | MemoryPack for internal API (React client) — fast binary format. STJ reserved for future public REST endpoints. DTOs use `double`/`DateTime` (MemoryPack-compatible); entities use `decimal`/`DateTimeOffset` with service-layer conversion. See `AGENTS.md` §Serialization |
| `currentValue` | **Optional** (`decimal?`) | User may not know value at time of entry |
| Per-type metadata | **Typed metadata object on DTO** | Discriminated by `AssetType` enum; each type has its own interface |
| Financial accounts | **Abstract FinancialAccount layer** | `accountNumber` + `institutionName` shared by Investment, Retirement, BankAccount, Insurance, Cryptocurrency |
| Brokerage → Holdings | **Holdings as metadata array** | Simpler than parent-child asset relationship |
| RSU placement | **Sub-type of Investment** | `accountType: "RSU"` — stock-based equity comp fits under Investment |
| Holding vs CryptoHolding | **Separate records (intentional)** | Structurally similar (symbol, name, quantity, costBasis, currentPrice) but diverge — CryptoHolding has staking fields, Holding has holdingType enum. Shared base record adds complexity without benefit |
| Policy loans | **Separate Liability entity** (future) | Enables full net-worth view; links to asset via `linkedAssetId` |
| Sensitive field masking | **Mask by default, reveal on demand** | API returns masked values (e.g. `••••1234`) unless client explicitly requests unmasked. Eye-icon toggle in UI fetches full value via separate secure endpoint. See [Sensitive Field Handling](#sensitive-field-handling) |
| Form approach | **Per-type form sections** | Each asset type renders its own metadata fields; generic form is too vague |

---

## Type Hierarchy

```
BaseAsset (name, type, currentValue?, description, purchasePrice, purchaseDate, location, depreciation, disposal, valuation, beneficiaries, audit)
├── Vehicle
├── RealEstate
├── Business
├── PersonalProperty
├── Collectible
├── IntellectualProperty
├── Other
└── FinancialAccount (accountNumber, institutionName)
    ├── Investment (includes RSU sub-type)
    ├── Retirement
    ├── BankAccount
    ├── Insurance
    └── Cryptocurrency
```

---

## Base Asset Fields

All 12 asset types inherit these fields.

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `id` | `Guid` | Yes (auto) | Primary key |
| `name` | `string` | **Yes** | Max 200 chars |
| `type` | `AssetType` | **Yes** | Enum discriminator |
| `currentValue` | `decimal?` | **No** | >= 0. UI shows "Value Unknown" when null |
| `description` | `string?` | No | Max 2000 chars |
| `purchasePrice` | `decimal?` | No | >= 0 |
| `purchaseDate` | `DateTimeOffset?` | No | |
| `location` | `string?` | No | Max 500 chars |
| `isDepreciable` | `bool` | Yes | Computed from type + user toggle |
| `isDisposed` | `bool` | Yes | Default: false |
| `hasBeneficiaries` | `bool` | Yes | Computed from beneficiary assignments (see [Beneficiary Entity](#beneficiary-entity-cross-reference)) |
| `createdAt` | `DateTimeOffset` | Yes (auto) | |
| `updatedAt` | `DateTimeOffset?` | Yes (auto) | |

### Depreciation fields (only when `isDepreciable`)

| Field | C# Type | Notes |
|-------|---------|-------|
| `depreciationMethod` | `DepreciationMethod?` | StraightLine, DecliningBalance, Macrs |
| `salvageValue` | `decimal?` | >= 0 |
| `usefulLifeMonths` | `int?` | > 0 |
| `inServiceDate` | `DateTimeOffset?` | |
| `accumulatedDepreciation` | `decimal?` | Computed at read time |
| `bookValue` | `decimal?` | Computed at read time |
| `monthlyDepreciation` | `decimal?` | Computed at read time |
| `depreciationPercentComplete` | `decimal?` | 0.0–1.0, computed |

### Disposal fields (only when `isDisposed`)

| Field | C# Type | Notes |
|-------|---------|-------|
| `disposalDate` | `DateTimeOffset?` | |
| `disposalPrice` | `decimal?` | |
| `disposalNotes` | `string?` | |

### Valuation fields

| Field | C# Type | Notes |
|-------|---------|-------|
| `marketValue` | `decimal?` | >= 0 |
| `lastValuationDate` | `DateTimeOffset?` | |

---

## Financial Account (Abstract)

Used by: **Investment, Retirement, BankAccount, Insurance, Cryptocurrency**

These fields exist on the base DTO but are **only shown/required for financial account subtypes**.

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `accountNumber` | `string?` | No | Max 100. Masked in display (••••1234). **Encrypted at rest** — see note below |
| `institutionName` | `string` | **Yes** (for financial types) | Max 200. e.g. Fidelity, Chase, Coinbase |

> **Security Note**: See [Sensitive Field Handling](#sensitive-field-handling) for the full masking and reveal strategy.

---

## Sensitive Field Handling

### Principle

**Never return raw sensitive values to the client by default.** The API returns masked representations; the full value is only fetched on explicit user action.

### Sensitive Fields Registry

| Asset Type | Field | Mask Format | Example |
|------------|-------|-------------|---------|
| FinancialAccount (all subtypes) | `accountNumber` | Last 4 digits | `••••1234` |
| BankAccount | `routingNumber` | Last 4 digits | `••••5678` |
| Insurance | `accountNumber` (policy #) | Last 4 chars | `••••A789` |
| Vehicle | `vin` | Last 6 chars | `•••••••••••234567` |

> **Extensible**: New sensitive fields can be registered by adding a `[SensitiveData]` attribute. The mask format is configurable per field.

### API Behavior

**Default (list & detail endpoints)**:
```json
{
  "accountNumber": "••••1234",
  "accountNumberMasked": true
}
```

The response includes a `masked` companion flag so the UI knows whether to show the reveal (eye) icon.

**Reveal endpoint** — `GET /api/assets/{id}/sensitive/{fieldName}`:
- Requires the **same authorization** as the asset itself (tenant + ownership)
- Returns the decrypted, unmasked value for a single field
- **Audit-logged**: every reveal is recorded with userId, assetId, fieldName, timestamp
- Response: `{ "fieldName": "accountNumber", "value": "123456781234" }`

### Storage

- All sensitive fields are **encrypted at rest** using column-level encryption (Azure SQL Always Encrypted or application-layer AES-256)
- Encryption keys managed via **Azure Key Vault**
- The masking layer operates **after** decryption in the API layer — the database only stores encrypted values

### UI Pattern

```
┌──────────────────────────────────┐
│ Account Number:  ••••1234  👁    │  ← masked by default
│                                  │
│ (user clicks eye icon)           │
│                                  │
│ Account Number:  123456781234 🔒 │  ← revealed, icon changes to lock
└──────────────────────────────────┘
```

- **Eye icon** (👁) → fetch & reveal. Auto-hides after configurable timeout (default: 30s)
- **Lock icon** (🔒) → re-mask immediately on click
- User can configure "always show" per field in **Settings > Privacy** (opt-in, not default)
- The reveal action is a **separate API call** — the full value is never stored in client state/Redux beyond the active viewport

### C# Implementation Sketch

```csharp
/// <summary>
/// Marks a property as containing sensitive PII that should be masked in API responses.
/// </summary>
/// <remarks>
/// The API serialization layer checks this attribute and replaces the value with
/// a masked representation (e.g., "••••1234") unless the caller explicitly
/// requests the unmasked value via the reveal endpoint.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveDataAttribute : Attribute
{
    /// <summary>Number of trailing characters to show unmasked.</summary>
    public int VisibleChars { get; init; } = 4;

    /// <summary>The mask character to use.</summary>
    public char MaskChar { get; init; } = '•';
}

// Usage on FinancialAccount:
public abstract class FinancialAccount : BaseAsset
{
    [SensitiveData(VisibleChars = 4)]
    public string? AccountNumber { get; set; }
}
```

### TypeScript UI Hook Sketch

```typescript
/**
 * Hook to reveal a masked sensitive field value on demand.
 * Fetches the unmasked value from the secure reveal endpoint
 * and auto-hides after the configured timeout.
 *
 * @param assetId - The asset ID
 * @param fieldName - The sensitive field name to reveal
 * @param autoHideMs - Auto-hide timeout in ms (default 30000)
 * @returns Reveal state and toggle function
 */
function useSensitiveField(assetId: string, fieldName: string, autoHideMs = 30_000) {
  const [revealed, setRevealed] = useState(false);
  const [value, setValue] = useState<string | null>(null);

  const toggle = async () => {
    if (revealed) {
      setValue(null);
      setRevealed(false);
    } else {
      const res = await api.get(`/assets/${assetId}/sensitive/${fieldName}`);
      setValue(res.data.value);
      setRevealed(true);
      setTimeout(() => { setValue(null); setRevealed(false); }, autoHideMs);
    }
  };

  return { revealed, value, toggle };
}
```

---

## Asset Types

---

### 1. Vehicle

**Status**: ✅ Locked

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `vin` | `string?` | No | Vehicle Identification Number (17 chars) |
| `make` | `string` | **Yes** | e.g. Tesla, Ford, BMW |
| `model` | `string` | **Yes** | e.g. Model 3, F-150, X5 |
| `year` | `int` | **Yes** | 4-digit year |
| `mileage` | `int?` | No | Current odometer reading |
| `color` | `string?` | No | Exterior color |
| `licensePlate` | `string?` | No | License plate number |

**Depreciable**: Yes (user toggle)

**C# class**: `VehicleMetadata`

```csharp
public sealed record VehicleMetadata
{
    public string? Vin { get; init; }
    public required string Make { get; init; }
    public required string Model { get; init; }
    public required int Year { get; init; }
    public int? Mileage { get; init; }
    public string? Color { get; init; }
    public string? LicensePlate { get; init; }
}
```

---

### 2. Real Estate

**Status**: ✅ Locked

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `address` | `string` | **Yes** | Street address line 1 |
| `address2` | `string?` | No | Apt, suite, unit, etc. |
| `city` | `string` | **Yes** | City |
| `state` | `string` | **Yes** | State (2-letter code or full name) |
| `zipCode` | `string` | **Yes** | ZIP/postal code |
| `country` | `string` | **Yes** | ISO 3166 country code. Default: `"US"` |
| `propertyType` | `PropertyType` | **Yes** | SingleFamily, Condo, Townhouse, MultiFamily, Land, Commercial, Other |
| `squareFeet` | `int?` | No | Living area sq ft |
| `yearBuilt` | `int?` | No | 4-digit year |
| `lotSize` | `string?` | No | e.g. "0.25 acres" |
| `bedrooms` | `int?` | No | Number of bedrooms |
| `bathrooms` | `decimal?` | No | Supports 1.5, 2.5, etc. |

**Depreciable**: Yes (for rental/commercial, user toggle)

**Enums**:

```csharp
public enum PropertyType
{
    SingleFamily, Condo, Townhouse, MultiFamily, Land, Commercial, Other
}
```

**C# class**: `RealEstateMetadata`

```csharp
public sealed record RealEstateMetadata
{
    public required string Address { get; init; }
    public string? Address2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string ZipCode { get; init; }
    public string Country { get; init; } = "US";
    public required PropertyType PropertyType { get; init; }
    public int? SquareFeet { get; init; }
    public int? YearBuilt { get; init; }
    public string? LotSize { get; init; }
    public int? Bedrooms { get; init; }
    public decimal? Bathrooms { get; init; }
}
```

---

### 3. Investment (Financial Account)

**Status**: ✅ Locked

Represents a **brokerage account** with an optional array of holdings. When `accountType === "RSU"`, shows RSU-specific fields instead of holdings.

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| *inherited*: `accountNumber`, `institutionName` | | | From FinancialAccount |
| `accountType` | `InvestmentAccountType` | **Yes** | Individual, Joint, Custodial, Trust, RSU, Other |
| `holdings` | `List<Holding>?` | No | Array of positions (not for RSU) |

**Each `Holding`:**

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `ticker` | `string` | **Yes** | Symbol (e.g. AAPL, VTSAX) |
| `name` | `string?` | No | Full name (e.g. "Apple Inc.") |
| `holdingType` | `HoldingType` | **Yes** | Stocks, Bonds, MutualFunds, ETF, Options, Other |
| `shares` | `decimal` | **Yes** | Number of shares/units |
| `costBasis` | `decimal?` | No | Total cost basis |
| `currentPrice` | `decimal?` | No | Price per share |

**RSU-specific fields** (only when `accountType === "RSU"`):

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `grantDate` | `DateTimeOffset` | **Yes** | Date RSUs were granted |
| `totalSharesGranted` | `int` | **Yes** | Total shares in the grant |
| `sharesVested` | `int?` | No | Shares vested so far |
| `vestingSchedule` | `List<VestingEvent>?` | No | Vesting events |
| `ticker` | `string?` | No | Company stock ticker |
| `grantPricePerShare` | `decimal?` | No | FMV per share at grant date |

**Each `VestingEvent`:**

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `date` | `DateTimeOffset` | **Yes** | Vesting date |
| `shares` | `decimal` | **Yes** | Shares vesting on that date (decimal to support fractional share vesting) |

**Depreciable**: No

**Enums**:

```csharp
public enum InvestmentAccountType
{
    Individual, Joint, Custodial, Trust, RSU, Other
}

public enum HoldingType
{
    Stocks, Bonds, MutualFunds, ETF, Options, Other
}
```

**C# class**: `InvestmentMetadata`

```csharp
public sealed record InvestmentMetadata
{
    public required InvestmentAccountType AccountType { get; init; }
    public List<Holding>? Holdings { get; init; }

    // RSU-specific (only when AccountType == RSU)
    public DateTimeOffset? GrantDate { get; init; }
    public int? TotalSharesGranted { get; init; }
    public int? SharesVested { get; init; }
    public List<VestingEvent>? VestingSchedule { get; init; }
    public string? Ticker { get; init; }
    public decimal? GrantPricePerShare { get; init; }
}

public sealed record Holding
{
    public required string Ticker { get; init; }
    public string? Name { get; init; }
    public required HoldingType HoldingType { get; init; }
    public required decimal Shares { get; init; }
    public decimal? CostBasis { get; init; }
    public decimal? CurrentPrice { get; init; }
}

public sealed record VestingEvent
{
    public required DateTimeOffset Date { get; init; }
    public required decimal Shares { get; init; }
}
```

---

### 4. Retirement (Financial Account)

**Status**: ✅ Locked

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| *inherited*: `accountNumber`, `institutionName` | | | From FinancialAccount |
| `accountType` | `RetirementAccountType` | **Yes** | Plan401k, IRA, RothIRA, SepIra, Pension, Plan403b, Other |
| `employerMatchTiers` | `List<EmployerMatchTier>?` | No | Tiered employer match structure |
| `vestingPercent` | `decimal?` | No | Current vesting percentage (0–100) |
| `vestingScheduleMonths` | `int?` | No | Total months to full vesting |
| `projectedAnnualContribution` | `decimal?` | No | Expected yearly contribution |
| `salary` | `decimal?` | No | Annual salary (for match calculation) |

**Each `EmployerMatchTier`:**

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `matchPercent` | `decimal` | **Yes** | What % employer matches (e.g. 100, 50) |
| `onFirst` | `decimal` | **Yes** | Of first X% of salary (e.g. 6.0) |

**Example**: "100% on first 6%, 50% on next 2%"

```json
{
  "employerMatchTiers": [
    { "matchPercent": 100, "onFirst": 6.0 },
    { "matchPercent": 50, "onFirst": 2.0 }
  ]
}
```

**Depreciable**: No

**Enums**:

```csharp
/// <summary>
/// Retirement account plan types. Uses Plan prefix for numeric-starting names.
/// Wire format uses [JsonStringEnumMemberName] (.NET 9+) for clean JSON ("401k", "403b").
/// NOTE: System.Text.Json does NOT support [JsonPropertyName] on enum members.
/// Use [JsonStringEnumMemberName("value")] (System.Text.Json .NET 9+) or a custom
/// JsonStringEnumConverter with naming policy for earlier targets.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RetirementAccountType
{
    [JsonStringEnumMemberName("401k")]  Plan401k,
    IRA,
    RothIRA,
    [JsonStringEnumMemberName("sep_ira")] SepIra,
    Pension,
    [JsonStringEnumMemberName("403b")]  Plan403b,
    Other
}
```

**C# class**: `RetirementMetadata`

```csharp
public sealed record RetirementMetadata
{
    public required RetirementAccountType AccountType { get; init; }
    public List<EmployerMatchTier>? EmployerMatchTiers { get; init; }
    public decimal? VestingPercent { get; init; }
    public int? VestingScheduleMonths { get; init; }
    public decimal? ProjectedAnnualContribution { get; init; }
    public decimal? Salary { get; init; }
}

public sealed record EmployerMatchTier
{
    public required decimal MatchPercent { get; init; }
    public required decimal OnFirst { get; init; }
}
```

---

### 5. Bank Account (Financial Account)

**Status**: ✅ Locked

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| *inherited*: `accountNumber`, `institutionName` | | | From FinancialAccount |
| `bankAccountType` | `BankAccountType` | **Yes** | Checking, Savings, MoneyMarket, CD, HYSA, Other |
| `routingNumber` | `string?` | No | 9-digit ABA routing number |
| `apy` | `decimal?` | No | Annual Percentage Yield as percentage (e.g. 4.5) |
| `maturityDate` | `DateTimeOffset?` | No | **CD only** — when the term ends |
| `term` | `int?` | No | **CD only** — term length in months |
| `isJointAccount` | `bool?` | No | Whether jointly held |

**Depreciable**: No

**Conditional fields**: `maturityDate` and `term` only shown when `bankAccountType === "CD"`.

**Enums**:

```csharp
public enum BankAccountType
{
    Checking, Savings, MoneyMarket, CD, HYSA, Other
}
```

**C# class**: `BankAccountMetadata`

```csharp
public sealed record BankAccountMetadata
{
    public required BankAccountType BankAccountType { get; init; }
    public string? RoutingNumber { get; init; }
    public decimal? Apy { get; init; }
    public DateTimeOffset? MaturityDate { get; init; }
    public int? Term { get; init; }
    public bool? IsJointAccount { get; init; }
}
```

---

### 6. Insurance (Financial Account)

**Status**: ✅ Locked

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| *inherited*: `accountNumber`, `institutionName` | | | `accountNumber` = policy number |
| `policyType` | `InsurancePolicyType` | **Yes** | WholeLife, UniversalLife, TermLife, Annuity, Other |
| `cashValue` | `decimal?` | No | Cash surrender value (not for TermLife) |
| `deathBenefit` | `decimal?` | No | Base death benefit |
| `premiumAmount` | `decimal?` | No | Base premium payment |
| `premiumFrequency` | `PremiumFrequency?` | No | Monthly, Quarterly, SemiAnnually, Annually |
| `policyStartDate` | `DateTimeOffset?` | No | When coverage began |
| `policyEndDate` | `DateTimeOffset?` | No | For TermLife — when term expires |
| `riders` | `List<PolicyRider>?` | No | Policy riders / add-ons |
| `dividendOption` | `DividendOption?` | No | WholeLife only — how dividends are applied |
| `annualDividend` | `decimal?` | No | Most recent annual dividend amount |

**Each `PolicyRider`:**

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `riderType` | `PolicyRiderType` | **Yes** | PaidUpAdditions, AccidentalDeath, WaiverOfPremium, ChildTerm, LongTermCare, ChronicIllness, GuaranteedInsurability, Other |
| `name` | `string?` | No | Custom name if type = Other |
| `value` | `decimal?` | No | Rider benefit amount or PUA total value |
| `annualCost` | `decimal?` | No | Annual rider premium/cost |

**Conditional fields**:
- `cashValue`, `riders`, `dividendOption`, `annualDividend` — not shown for TermLife
- `policyEndDate` — only shown for TermLife

**Depreciable**: No

**Enums**:

```csharp
public enum InsurancePolicyType
{
    WholeLife, UniversalLife, TermLife, Annuity, Other
}

public enum PremiumFrequency
{
    Monthly, Quarterly, SemiAnnually, Annually
}

public enum PolicyRiderType
{
    PaidUpAdditions, AccidentalDeath, WaiverOfPremium, ChildTerm,
    LongTermCare, ChronicIllness, GuaranteedInsurability, Other
}

public enum DividendOption
{
    PaidUpAdditions, CashPayment, PremiumReduction, AccumulateAtInterest, Other
}
```

**C# class**: `InsuranceMetadata`

```csharp
public sealed record InsuranceMetadata
{
    public required InsurancePolicyType PolicyType { get; init; }
    public decimal? CashValue { get; init; }
    public decimal? DeathBenefit { get; init; }
    public decimal? PremiumAmount { get; init; }
    public PremiumFrequency? PremiumFrequency { get; init; }
    public DateTimeOffset? PolicyStartDate { get; init; }
    public DateTimeOffset? PolicyEndDate { get; init; }
    public List<PolicyRider>? Riders { get; init; }
    public DividendOption? DividendOption { get; init; }
    public decimal? AnnualDividend { get; init; }
}

public sealed record PolicyRider
{
    public required PolicyRiderType RiderType { get; init; }
    public string? Name { get; init; }
    public decimal? Value { get; init; }
    public decimal? AnnualCost { get; init; }
}
```

---

### 7. Business

**Status**: ✅ Locked

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `entityType` | `BusinessEntityType` | **Yes** | SoleProprietorship, Partnership, LLC, Corporation, SCorp, Other |
| `ownershipPercent` | `decimal` | **Yes** | Ownership stake (> 0 and ≤ 100). Validation: must be greater than 0 |
| `ein` | `string?` | No | Employer Identification Number (XX-XXXXXXX) |
| `naicsCode` | `string?` | No | 6-digit NAICS code |
| `dunsNumber` | `string?` | No | 9-digit D-U-N-S number |
| `industry` | `string?` | No | Free-text description (supplements NAICS) |
| `annualRevenue` | `decimal?` | No | Most recent annual revenue |
| `numberOfEmployees` | `int?` | No | Headcount |
| `foundedDate` | `DateTimeOffset?` | No | Date established |
| `registrations` | `List<StateRegistration>?` | No | State formation + foreign qualifications |

**Each `StateRegistration`:**

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `state` | `string` | **Yes** | 2-letter state code (e.g. DE, TX) |
| `sosNumber` | `string?` | No | Secretary of State filing number |
| `filingDate` | `DateTimeOffset?` | No | Date of filing |
| `isFormationState` | `bool` | **Yes** | True for the state where entity was formed |

**Depreciable**: No

**Enums**:

```csharp
public enum BusinessEntityType
{
    SoleProprietorship, Partnership, LLC, Corporation, SCorp, Other
}
```

**C# class**: `BusinessMetadata`

```csharp
public sealed record BusinessMetadata
{
    public required BusinessEntityType EntityType { get; init; }
    public required decimal OwnershipPercent { get; init; }
    public string? Ein { get; init; }
    public string? NaicsCode { get; init; }
    public string? DunsNumber { get; init; }
    public string? Industry { get; init; }
    public decimal? AnnualRevenue { get; init; }
    public int? NumberOfEmployees { get; init; }
    public DateTimeOffset? FoundedDate { get; init; }
    public List<StateRegistration>? Registrations { get; init; }
}

public sealed record StateRegistration
{
    public required string State { get; init; }
    public string? SosNumber { get; init; }
    public DateTimeOffset? FilingDate { get; init; }
    public required bool IsFormationState { get; init; }
}
```

---

### 8. Personal Property

**Status**: ✅ Locked

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `category` | `PersonalPropertyCategory` | **Yes** | Jewelry, Furniture, Electronics, MusicalInstrument, Firearm, Clothing, Appliance, Tool, SportingGoods, Other |
| `customCategory` | `string?` | No | Free-text category when `category == Other` |
| `condition` | `ItemCondition?` | No | Mint, Excellent, Good, Fair, Poor |
| `serialNumber` | `string?` | No | Manufacturer serial number (electronics, firearms, etc.) |
| `brand` | `string?` | No | e.g. Rolex, Gibson, Apple |
| `modelNumber` | `string?` | No | Manufacturer model |
| `appraiserName` | `string?` | No | Who appraised it |
| `lastAppraisalDate` | `DateTimeOffset?` | No | When last appraised |
| `insuredValue` | `decimal?` | No | Value on insurance rider/schedule |

**Depreciable**: Yes (user toggle — furniture, electronics depreciate; jewelry typically doesn't)

**C# class**: `PersonalPropertyMetadata`

```csharp
public enum PersonalPropertyCategory
{
    Jewelry, Furniture, Electronics, MusicalInstrument, Firearm,
    Clothing, Appliance, Tool, SportingGoods, Other
}
```

```csharp
public sealed record PersonalPropertyMetadata
{
    public required PersonalPropertyCategory Category { get; init; }
    public string? CustomCategory { get; init; }
    public ItemCondition? Condition { get; init; }
    public string? SerialNumber { get; init; }
    public string? Brand { get; init; }
    public string? ModelNumber { get; init; }
    public string? AppraiserName { get; init; }
    public DateTimeOffset? LastAppraisalDate { get; init; }
    public decimal? InsuredValue { get; init; }
}
```

---

### 9. Collectible

**Status**: ✅ Locked

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `category` | `CollectibleCategory` | **Yes** | Coins, Stamps, Wine, Art, Antiques, SportsMemorabilia, Comics, TradingCards, Toys, Other |
| `customCategory` | `string?` | No | Free-text category when `category == Other` |
| `condition` | `ItemCondition?` | No | Mint, Excellent, Good, Fair, Poor |
| `provenance` | `string?` | No | Ownership history / documented origin |
| `serialNumber` | `string?` | No | If applicable (graded coins, authenticated cards) |
| `certificationBody` | `string?` | No | e.g. PSA, NGC, CGC, BGS |
| `certificationNumber` | `string?` | No | Grading/authentication cert number |
| `grade` | `string?` | No | e.g. PSA 10, MS-70, CGC 9.8 |
| `edition` | `string?` | No | e.g. "1st Edition", "Limited 50/500" |
| `artist` | `string?` | No | Creator/maker (for art, crafts) |
| `appraiserName` | `string?` | No | Who appraised it |
| `lastAppraisalDate` | `DateTimeOffset?` | No | When last appraised |
| `insuredValue` | `decimal?` | No | Value on insurance rider/schedule |

**Depreciable**: No (collectibles typically appreciate)

**C# class**: `CollectibleMetadata`

```csharp
public enum CollectibleCategory
{
    Coins, Stamps, Wine, Art, Antiques, SportsMemorabilia,
    Comics, TradingCards, Toys, Other
}
```

```csharp
public sealed record CollectibleMetadata
{
    public required CollectibleCategory Category { get; init; }
    public string? CustomCategory { get; init; }
    public ItemCondition? Condition { get; init; }
    public string? Provenance { get; init; }
    public string? SerialNumber { get; init; }
    public string? CertificationBody { get; init; }
    public string? CertificationNumber { get; init; }
    public string? Grade { get; init; }
    public string? Edition { get; init; }
    public string? Artist { get; init; }
    public string? AppraiserName { get; init; }
    public DateTimeOffset? LastAppraisalDate { get; init; }
    public decimal? InsuredValue { get; init; }
}
```

---

### 10. Cryptocurrency (Financial Account)

**Status**: ✅ Locked

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| *inherited*: `accountNumber`, `institutionName` | | | institutionName = exchange or wallet provider |
| `walletType` | `CryptoWalletType` | **Yes** | Exchange, HardwareWallet, SoftwareWallet, CustodialAccount, Other |
| `holdings` | `List<CryptoHolding>` | **Yes** (≥1) | Array of coin/token positions |

**Each `CryptoHolding`:**

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `coinSymbol` | `string` | **Yes** | e.g. BTC, ETH, SOL |
| `coinName` | `string?` | No | e.g. Bitcoin, Ethereum |
| `quantity` | `decimal` | **Yes** | Number of coins/tokens |
| `costBasis` | `decimal?` | No | Total cost basis |
| `currentPrice` | `decimal?` | No | Price per coin |
| `isStaking` | `bool?` | No | Whether currently staked |
| `stakingApy` | `decimal?` | No | APY percentage (only if staking) |

**Depreciable**: No

**Enums**:

```csharp
public enum CryptoWalletType
{
    Exchange, HardwareWallet, SoftwareWallet, CustodialAccount, Other
}
```

**C# class**: `CryptocurrencyMetadata`

```csharp
public sealed record CryptocurrencyMetadata
{
    public required CryptoWalletType WalletType { get; init; }
    public required List<CryptoHolding> Holdings { get; init; }
}

public sealed record CryptoHolding
{
    public required string CoinSymbol { get; init; }
    public string? CoinName { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? CostBasis { get; init; }
    public decimal? CurrentPrice { get; init; }
    public bool? IsStaking { get; init; }
    public decimal? StakingApy { get; init; }
}
```

---

### 11. Intellectual Property

**Status**: ✅ Locked

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `ipType` | `IpType` | **Yes** | Patent, Trademark, Copyright, TradeSecret, Other |
| `registrationNumber` | `string?` | No | Patent/trademark/copyright registration number |
| `jurisdiction` | `string?` | No | Country or region (e.g. US, EU, WIPO) |
| `filingDate` | `DateTimeOffset?` | No | Date filed |
| `issueDate` | `DateTimeOffset?` | No | Date granted/registered |
| `expirationDate` | `DateTimeOffset?` | No | When protection expires |
| `status` | `IpStatus?` | No | Pending, Active, Expired, Abandoned |
| `licensee` | `string?` | No | Primary licensee |
| `royaltyRate` | `decimal?` | No | Royalty percentage |
| `annualRevenue` | `decimal?` | No | Annual licensing/royalty income |

**Depreciable**: Yes (patents and some IP amortize over useful life)

**Enums**:

```csharp
public enum IpType
{
    Patent, Trademark, Copyright, TradeSecret, Other
}

public enum IpStatus
{
    Pending, Active, Expired, Abandoned
}
```

**C# class**: `IntellectualPropertyMetadata`

```csharp
public sealed record IntellectualPropertyMetadata
{
    public required IpType IpType { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? Jurisdiction { get; init; }
    public DateTimeOffset? FilingDate { get; init; }
    public DateTimeOffset? IssueDate { get; init; }
    public DateTimeOffset? ExpirationDate { get; init; }
    public IpStatus? Status { get; init; }
    public string? Licensee { get; init; }
    public decimal? RoyaltyRate { get; init; }
    public decimal? AnnualRevenue { get; init; }
}
```

---

### 12. Other

**Status**: ✅ Locked

Catch-all type for assets not covered by types 1–11. Uses base asset fields plus optional user-defined metadata.

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `category` | `string?` | No | User-defined category label |
| `customFields` | `Dictionary<string, string>?` | No | Key-value pairs for anything not covered by other types |

**Depreciable**: Yes (user toggle — depends on what the asset is)

**C# class**: `OtherAssetMetadata`

```csharp
public sealed record OtherAssetMetadata
{
    public string? Category { get; init; }
    public Dictionary<string, string>? CustomFields { get; init; }
}
```

---

## Asset Documents

Cross-cutting feature — any asset type can have N linked documents (operating agreements, deeds, titles, policy documents, etc.).

### `AssetDocument` entity

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `id` | `Guid` | Yes (auto) | |
| `assetId` | `Guid` | **Yes** | FK to parent asset |
| `documentType` | `AssetDocumentType` | **Yes** | See enum below |
| `fileName` | `string` | **Yes** | Original file name |
| `contentType` | `string` | **Yes** | MIME type (application/pdf, image/png, etc.) |
| `fileSize` | `long` | **Yes** | Bytes |
| `storageUrl` | `string` | **Yes** | Azure Blob Storage URL (not exposed to client) |
| `description` | `string?` | No | User note about the document |
| `uploadedAt` | `DateTimeOffset` | Yes (auto) | |

**Enum**:

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

**C# class**: `AssetDocument`

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

**UI**: Each asset detail page shows a "Documents" tab/card for upload, view, and delete. Files stored in Azure Blob Storage.

---

## Liabilities (Future)

A `Liability` entity can optionally link to an `Asset` via `linkedAssetId`. This enables full net-worth calculations and tracks debts like mortgages, car loans, policy loans, etc.

| Field | C# Type | Required? | Notes |
|-------|---------|-----------|-------|
| `id` | `Guid` | Yes (auto) | |
| `name` | `string` | **Yes** | e.g. "Home Mortgage", "Policy Loan — NY Life" |
| `liabilityType` | `LiabilityType` | **Yes** | Mortgage, AutoLoan, StudentLoan, CreditCard, PolicyLoan, PersonalLoan, HELOC, Other |
| `linkedAssetId` | `Guid?` | No | FK to asset (e.g. mortgage → real estate, policy loan → insurance) |
| `balance` | `decimal` | **Yes** | Current outstanding balance |
| `apr` | `decimal?` | No | Annual Percentage Rate |
| `monthlyPayment` | `decimal?` | No | |
| `maturityDate` | `DateTimeOffset?` | No | |
| `lender` | `string?` | No | |

**Enum**:

```csharp
public enum LiabilityType
{
    Mortgage, AutoLoan, StudentLoan, CreditCard,
    PolicyLoan, PersonalLoan, HELOC, Other
}
```

**UI Impact**: Asset detail pages will show a "Linked Liabilities" card (empty state with "Coming Soon" until built).

**Net Worth**: `Total Asset Value − Total Liability Balance`

---

## Beneficiary Entity (Cross-Reference)

> **Updated Design**: Beneficiary is now a *role* that a Contact plays when linked to an Asset, not a separate entity type. See [`RAJ_FINANCIAL_INTEGRATIONS_API.md`](RAJ_FINANCIAL_INTEGRATIONS_API.md) for the complete Contact model.

### Key Points

- **Contact** is the base entity (Individual, Trust, or Organization)
- **AssetContactLink** connects a Contact to an Asset with a role (Beneficiary, CoOwner, Trustee, etc.)
- The `HasBeneficiaries` computed property on Asset checks for links where `Role == Beneficiary`
- Same contact can serve multiple roles (beneficiary on one asset, trustee on another)

### Relevant Asset Types for Beneficiary Designations

| Asset Type | Typical Beneficiary Features |
|------------|------------------------------|
| Insurance | Primary + Contingent beneficiaries, per stirpes option |
| Retirement | Primary + Contingent, subject to plan rules |
| BankAccount | POD (Payable on Death) designation |
| Investment | TOD (Transfer on Death) registration |

### AssetContactLink Structure

```csharp
AssetContactLink {
    AssetId,
    ContactId,
    Role,              // Beneficiary, CoOwner, Trustee, etc.
    Designation?,      // Primary or Contingent (for beneficiaries)
    AllocationPercent?, // 0-100
    PerStirpes,        // If beneficiary dies, share goes to descendants
    Notes
}
```

### Validation Rules

- Total Primary allocation for an asset should equal 100% (warning if not)
- Total Contingent allocation should equal 100% (warning if not)
- Cannot link same contact twice to same asset with same role
- Cannot delete contact with active asset links (must remove links first)

---

## Serialization Strategy

### Dual Serialization (decided)

| Layer | Serializer | Types | Rationale |
|-------|-----------|-------|----------|
| **DTOs / Internal API** (React client) | **MemoryPack** | `double`, `DateTime`, `T[]` | Fast binary format; `[MemoryPackable]`, `[MemoryPackOrder]`, `[MemoryPackUnion]` for polymorphic metadata |
| **EF Core Entities** (database) | N/A (EF handles) | `decimal`, `DateTimeOffset` | Financial precision; DB-native types |
| **Public REST APIs** (future) | **System.Text.Json** | `decimal`, `DateTimeOffset` | Browser compatibility; human-readable JSON |

> **Authoritative reference**: `AGENTS.md` §Dual Serialization Strategy

### Type mapping rules

| Entity (DB) | DTO (MemoryPack) | Notes |
|-------------|-----------------|-------|
| `decimal` | `double` | Monetary fields — converted in service/mapping layer |
| `DateTimeOffset` | `DtoDateTime` | Timestamps — implicit conversion via wrapper struct (see below) |
| `List<T>` | `T[]` | MemoryPack prefers arrays |

### DtoDateTime Wrapper

**Problem**: Entities use `DateTimeOffset` for proper timezone handling, while DTOs use `DateTime` for MemoryPack compatibility. Without a helper, every mapping requires manual `.UtcDateTime` calls:

```csharp
// ❌ Without DtoDateTime — verbose, error-prone
var dto = new AssetDto
{
    CreatedAt = asset.CreatedAt.UtcDateTime,
    UpdatedAt = asset.UpdatedAt?.UtcDateTime,
    PurchaseDate = asset.PurchaseDate?.UtcDateTime,
};
```

**Solution**: `DtoDateTime` is a MemoryPack-compatible wrapper struct with implicit conversions:

```csharp
// ✅ With DtoDateTime — clean, automatic
var dto = new AssetDto
{
    CreatedAt = asset.CreatedAt,    // DateTimeOffset → DtoDateTime (implicit)
    UpdatedAt = asset.UpdatedAt,    // DateTimeOffset? → DtoDateTime? (implicit)
};

// Reverse mapping also works
entity.CreatedAt = dto.CreatedAt;   // DtoDateTime → DateTimeOffset (implicit)
```

**Location**: `src/Shared/DtoDateTime.cs`

**Usage in DTOs**:
```csharp
[MemoryPackable]
public sealed partial record UserProfileResponse
{
    [MemoryPackOrder(5)]
    public DtoDateTime CreatedAt { get; init; }

    [MemoryPackOrder(6)]
    public DtoDateTime? UpdatedAt { get; init; }
}
```

**Benefits**:
- Eliminates manual `.UtcDateTime` calls at every mapping point
- Compile-time safety — no runtime overhead
- MemoryPack serializes it as a raw `DateTime` (8 bytes, no overhead)
- Implements `IEquatable<T>`, `IComparable<T>` for proper value semantics

### Metadata serialization

Per-type metadata uses MemoryPack's **union** feature for polymorphism:

```csharp
[MemoryPackable]
[MemoryPackUnion(0, typeof(VehicleMetadata))]
// ... one tag per asset type
public partial interface IAssetMetadata { }
```

Each metadata record: `[MemoryPackable(SerializeLayout.Explicit)]` with explicit `[MemoryPackOrder(n)]`.

### Remaining migration items

1. Add `IAssetMetadata? Metadata` property to DTOs and request objects
2. Update `[GenerateTypeScript]` generation to include metadata types
3. Migrate existing `Beneficiary` + `BeneficiaryAssignment` data to new `Contact` + `AssetContactLink` tables

---

## Shared Enums (for reference)

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
    Other                = 99  // Values 11–98 reserved for future asset types
}

public enum DepreciationMethod
{
    None             = 0,
    StraightLine     = 1,
    DecliningBalance = 2,
    Macrs            = 3
}

/// <summary>
/// Shared enum — place in src/Shared/Enums/ItemCondition.cs
/// Used by PersonalPropertyMetadata and CollectibleMetadata.
/// </summary>
public enum ItemCondition
{
    Mint, Excellent, Good, Fair, Poor
}
```
