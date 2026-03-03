# 04 — Contacts & Beneficiaries

> Contact entity hierarchy, trust roles, asset-contact linking, beneficiary coverage analysis.

**ADO Tracking:** [Epic #352 — 04 - Contacts & Beneficiaries](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/352)

| # | Feature | State |
|---|---------|-------|
| [353](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/353) | Beneficiary Service & API | New |
| [354](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/354) | Beneficiaries Page UI | New |
| [355](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/355) | Beneficiary Assignment & Validation | New |

---

## Overview

RAJ Financial tracks **people and entities** that relate to a user's financial assets — beneficiaries, co-owners, trustees, attorneys, etc. The design principle:

> **"Beneficiary" is a role a contact plays when linked to an asset — not a separate entity type.**

A contact is entered once, then linked to any number of assets in different roles. This eliminates duplicate data entry and keeps the model flexible for estate planning.

---

## Entity Hierarchy

```
Contact (abstract — base fields + address)
├── IndividualContact (people: spouse, child, advisor, etc.)
├── TrustContact (revocable, irrevocable, testamentary trusts)
└── OrganizationContact (charities, businesses, government entities)
```

### ContactType Enum

```csharp
public enum ContactType
{
    Individual,
    Trust,
    Organization
}
```

### Tenant Isolation

Contacts use **`UserId` only** for tenant scoping (no separate `TenantId`). Each user owns their own contacts — there is no shared contact pool. This differs from `BaseAsset` which carries both `UserId` and `TenantId`; the simpler model is appropriate because contacts are never shared across tenants.

---

## Contact (Abstract Base)

All contact types inherit from `Contact`. EF Core maps using **TPH (Table-Per-Hierarchy)** with `ContactType` as discriminator.

```csharp
/// <summary>
/// Abstract base class for all contact types.
/// Uses mutable class (not sealed record) because EF Core requires
/// change tracking on entity instances.
/// </summary>
public abstract class Contact
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ContactType ContactType { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>EF Core owned entity — stored as columns on the Contact table.</summary>
    public ContactAddress? Address { get; set; }

    /// <summary>Navigation to AssetContactLink records.</summary>
    public ICollection<AssetContactLink> AssetLinks { get; set; } = [];

    /// <summary>Computed display name — each subtype provides its own implementation.</summary>
    public abstract string DisplayName { get; }
}
```

### ContactAddress (Owned Entity)

Stored as columns on the Contact table (not a JSON column, not a separate table).

```csharp
/// <summary>
/// EF Core owned entity for contact addresses.
/// Mapped as columns: Address_Street1, Address_City, etc.
/// </summary>
[Owned]
public class ContactAddress
{
    public string Street1 { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "US";
}
```

---

## IndividualContact

Represents a person — spouse, child, parent, professional advisor, etc.

```csharp
public class IndividualContact : Contact
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }

    [SensitiveData(VisibleChars = 4)]
    public string? Ssn { get; set; }  // Encrypted at rest

    public RelationshipType? Relationship { get; set; }

    public override string DisplayName => $"{FirstName} {LastName}";
}
```

### RelationshipType Enum

```csharp
public enum RelationshipType
{
    Spouse,
    Child,
    Parent,
    Sibling,
    Grandchild,
    Grandparent,
    InLaw,
    Friend,
    Attorney,
    Accountant,
    FinancialAdvisor,
    Other
}
```

---

## TrustContact

Represents a trust entity. Uses a **Category + Purpose + SpecificType** model to handle 50+ trust variants without schema explosion.

```csharp
public class TrustContact : Contact
{
    public string TrustName { get; set; } = string.Empty;

    [SensitiveData(VisibleChars = 4)]
    public string? Ein { get; set; }  // XX-XXXXXXX, encrypted at rest

    public TrustCategory Category { get; set; }
    public TrustPurpose Purpose { get; set; }

    /// <summary>
    /// Freeform string for specific trust variants (e.g. "QTIP", "GRAT", "ILIT").
    /// Avoids enumerating 50+ trust types as enum values.
    /// </summary>
    public string? SpecificType { get; set; }

    public DateOnly? TrustDate { get; set; }
    public string? StateOfFormation { get; set; }
    public bool IsGrantorTrust { get; set; }
    public bool HasCrummeyProvisions { get; set; }
    public bool IsGstExempt { get; set; }

    /// <summary>Navigation to TrustRole records (Grantor, Trustee, Beneficiaries, etc.).</summary>
    public ICollection<TrustRole> Roles { get; set; } = [];

    public override string DisplayName => TrustName;
}
```

### Trust Enums

```csharp
public enum TrustCategory
{
    RevocableLiving,
    Irrevocable,
    Testamentary
}

public enum TrustPurpose
{
    General,
    Marital,           // QTIP, Bypass, AB trusts
    AssetProtection,   // DAPT, Spendthrift
    SpecialNeeds,      // SNT, Supplemental Needs
    Charitable,        // CRT, CLT, CRAT, CRUT, CLAT, CLUT
    Insurance,         // ILIT
    TaxPlanning,       // GRAT, QPRT, IDGT, SLAT
    Dynasty,           // GST-exempt, multi-generational
    Business,          // ESBT, QSST
    Other              // Pet, Funeral, Land, etc.
}
```

#### Common SpecificType Values (Freeform Field)

| Purpose | SpecificType Examples |
|---------|----------------------|
| Marital | QTIP, Bypass, AB, Credit Shelter |
| AssetProtection | DAPT, Spendthrift, Ohio Legacy Trust |
| SpecialNeeds | SNT, Supplemental Needs, First-Party SNT, Third-Party SNT |
| Charitable | CRT, CRAT, CRUT, CLT, CLAT, CLUT |
| Insurance | ILIT |
| TaxPlanning | GRAT, QPRT, IDGT, SLAT, ING, NING |
| Dynasty | GST Trust, Generation-Skipping Trust |
| Business | ESBT, QSST |
| Other | Pet Trust, Funeral Trust, Land Trust, Blind Trust |

### TrustRole

Links a contact to a trust with a specific role. Cascade-deletes when the parent `TrustContact` is deleted.

```csharp
public class TrustRole
{
    public Guid Id { get; set; }
    public Guid TrustContactId { get; set; }
    public Guid ContactId { get; set; }  // The person/entity in this role
    public TrustRoleType RoleType { get; set; }

    /// <summary>For successor trustees: 1 = first successor, 2 = second, etc.</summary>
    public int? SuccessionOrder { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public TrustContact TrustContact { get; set; } = null!;
    public Contact Contact { get; set; } = null!;
}

public enum TrustRoleType
{
    Grantor,
    Trustee,
    SuccessorTrustee,
    Beneficiary,
    RemainderBeneficiary,
    TrustProtector,
    InvestmentAdvisor,
    DistributionAdvisor
}
```

---

## OrganizationContact

Represents a business, charity, or other entity.

```csharp
public class OrganizationContact : Contact
{
    public string OrganizationName { get; set; } = string.Empty;

    [SensitiveData(VisibleChars = 4)]
    public string? Ein { get; set; }  // Encrypted at rest

    public OrganizationType OrganizationType { get; set; }

    /// <summary>For charities: whether they have 501(c)(3) status.</summary>
    public bool? Is501C3 { get; set; }

    public override string DisplayName => OrganizationName;
}

public enum OrganizationType
{
    Charity,
    Business,
    Government,
    NonProfit,
    EducationalInstitution,
    ReligiousOrganization,
    Other
}
```

---

## AssetContactLink

Links a contact to an asset with a specific role. **This is how a contact becomes a "beneficiary".**

```csharp
public class AssetContactLink
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid ContactId { get; set; }

    public AssetContactRole Role { get; set; }

    /// <summary>For beneficiaries: Primary or Contingent. Null for non-beneficiary roles.</summary>
    public DesignationType? Designation { get; set; }

    /// <summary>Allocation percentage (0-100). Required for beneficiaries.</summary>
    public decimal? AllocationPercent { get; set; }

    /// <summary>Per stirpes: if beneficiary dies, share passes to descendants.</summary>
    public bool PerStirpes { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public BaseAsset Asset { get; set; } = null!;
    public Contact Contact { get; set; } = null!;
}

public enum AssetContactRole
{
    Beneficiary,
    CoOwner,
    Trustee,        // "Trustee on this asset" — distinct from TrustRoleType.Trustee
    Custodian,
    PowerOfAttorney,
    EmergencyContact,
    Insured,
    Other
}

public enum DesignationType
{
    Primary,
    Contingent
}
```

---

## Cascade Delete Behavior

| Parent Deleted | Child Records | Behavior |
|----------------|---------------|----------|
| `BaseAsset` | `AssetContactLink` | **Cascade delete** — links meaningless without asset |
| `TrustContact` | `TrustRole` | **Cascade delete** — roles meaningless without trust |
| `Contact` | `AssetContactLink` | **Restrict** — must remove links first |
| `Contact` | `TrustRole` (as role holder) | **Restrict** — must remove trust roles first |

### EF Core Configuration

```csharp
// Asset → AssetContactLink (cascade)
builder.HasMany(a => a.ContactLinks)
    .WithOne(l => l.Asset)
    .HasForeignKey(l => l.AssetId)
    .OnDelete(DeleteBehavior.Cascade);

// TrustContact → TrustRole (cascade)
builder.HasMany(t => t.Roles)
    .WithOne(r => r.TrustContact)
    .HasForeignKey(r => r.TrustContactId)
    .OnDelete(DeleteBehavior.Cascade);

// Contact → AssetContactLink (restrict)
builder.HasMany<AssetContactLink>()
    .WithOne(l => l.Contact)
    .HasForeignKey(l => l.ContactId)
    .OnDelete(DeleteBehavior.Restrict);

// Contact → TrustRole as role holder (restrict)
builder.HasMany<TrustRole>()
    .WithOne(r => r.Contact)
    .HasForeignKey(r => r.ContactId)
    .OnDelete(DeleteBehavior.Restrict);
```

---

## API Endpoints

### Contact CRUD

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/contacts` | List contacts (optional `?type=Individual\|Trust\|Organization`) |
| `GET` | `/api/contacts/{id}` | Get contact by ID |
| `POST` | `/api/contacts` | Create contact |
| `PUT` | `/api/contacts/{id}` | Update contact |
| `DELETE` | `/api/contacts/{id}` | Delete contact (fails if active asset links exist) |

### Trust Roles

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/contacts/{id}/roles` | List trust roles (TrustContact only) |
| `POST` | `/api/contacts/{id}/roles` | Add trust role |
| `PUT` | `/api/contacts/{id}/roles/{roleId}` | Update trust role |
| `DELETE` | `/api/contacts/{id}/roles/{roleId}` | Remove trust role |

### Asset-Contact Links

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/assets/{id}/contacts` | List contacts linked to an asset |
| `POST` | `/api/assets/{id}/contacts` | Link contact to asset |
| `PUT` | `/api/asset-links/{linkId}` | Update link (role, allocation, etc.) |
| `DELETE` | `/api/asset-links/{linkId}` | Remove link |

### Coverage Analysis

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/contacts/coverage` | Beneficiary coverage summary across all assets |

---

## Service Interface

```csharp
public interface IContactService
{
    // Contact CRUD
    Task<IEnumerable<ContactDto>> GetContactsAsync(Guid userId, ContactType? filterType = null);
    Task<ContactDto> GetContactByIdAsync(Guid userId, Guid contactId);
    Task<ContactDto> CreateContactAsync(Guid userId, CreateContactRequest request);
    Task<ContactDto> UpdateContactAsync(Guid userId, Guid contactId, UpdateContactRequest request);
    Task DeleteContactAsync(Guid userId, Guid contactId);

    // Trust roles
    Task<IEnumerable<TrustRoleDto>> GetTrustRolesAsync(Guid userId, Guid trustContactId);
    Task<TrustRoleDto> AddTrustRoleAsync(Guid userId, Guid trustContactId, CreateTrustRoleRequest request);
    Task<TrustRoleDto> UpdateTrustRoleAsync(Guid userId, Guid trustContactId, Guid roleId, UpdateTrustRoleRequest request);
    Task RemoveTrustRoleAsync(Guid userId, Guid trustContactId, Guid roleId);

    // Asset-Contact links
    Task<IEnumerable<AssetContactLinkDto>> GetAssetContactsAsync(Guid userId, Guid assetId);
    Task<AssetContactLinkDto> LinkContactToAssetAsync(Guid userId, Guid assetId, CreateAssetContactLinkRequest request);
    Task<AssetContactLinkDto> UpdateAssetContactLinkAsync(Guid userId, Guid linkId, UpdateAssetContactLinkRequest request);
    Task RemoveAssetContactLinkAsync(Guid userId, Guid linkId);

    // Analysis
    Task<BeneficiaryCoverageDto> GetCoverageSummaryAsync(Guid userId);
}
```

---

## DTOs & Request/Response Contracts

### ContactDto (Polymorphic Response)

Discriminated by `contactType`. Base fields are always present; subtype fields are null for non-matching types.

```csharp
public sealed record ContactDto
{
    public Guid Id { get; init; }
    public ContactType ContactType { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public ContactAddressDto? Address { get; init; }
    public string? Notes { get; init; }
    public int AssetLinkCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    // Individual-specific (null for other types)
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Ssn { get; init; }               // Masked: "••••1234"
    public bool? SsnMasked { get; init; }
    public RelationshipType? Relationship { get; init; }

    // Trust-specific (null for other types)
    public string? TrustName { get; init; }
    public string? Ein { get; init; }                // Masked: "••••5678"
    public bool? EinMasked { get; init; }
    public TrustCategory? Category { get; init; }
    public TrustPurpose? Purpose { get; init; }
    public string? SpecificType { get; init; }
    public DateOnly? TrustDate { get; init; }
    public string? StateOfFormation { get; init; }
    public bool? IsGrantorTrust { get; init; }
    public bool? HasCrummeyProvisions { get; init; }
    public bool? IsGstExempt { get; init; }

    // Organization-specific (null for other types)
    public string? OrganizationName { get; init; }
    public OrganizationType? OrganizationType { get; init; }
    public bool? Is501C3 { get; init; }
}

public sealed record ContactAddressDto
{
    public string Street1 { get; init; } = string.Empty;
    public string? Street2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = "US";
}
```

### CreateContactRequest (Polymorphic)

Discriminated by `contactType`. Validators enforce correct fields per type.

```csharp
public sealed record CreateContactRequest
{
    public required ContactType ContactType { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public ContactAddressDto? Address { get; init; }
    public string? Notes { get; init; }

    // Individual fields (required when ContactType == Individual)
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Ssn { get; init; }
    public RelationshipType? Relationship { get; init; }

    // Trust fields (required when ContactType == Trust)
    public string? TrustName { get; init; }
    public string? Ein { get; init; }
    public TrustCategory? Category { get; init; }
    public TrustPurpose? Purpose { get; init; }
    public string? SpecificType { get; init; }
    public DateOnly? TrustDate { get; init; }
    public string? StateOfFormation { get; init; }
    public bool? IsGrantorTrust { get; init; }
    public bool? HasCrummeyProvisions { get; init; }
    public bool? IsGstExempt { get; init; }

    // Organization fields (required when ContactType == Organization)
    public string? OrganizationName { get; init; }
    public OrganizationType? OrganizationType { get; init; }
    public bool? Is501C3 { get; init; }
}
```

### TypeScript Discriminated Union

```typescript
type CreateContactRequest =
  | {
      contactType: "Individual";
      firstName: string;
      lastName: string;
      dateOfBirth?: string;
      ssn?: string;
      relationship?: RelationshipType;
      email?: string;
      phone?: string;
      address?: ContactAddress;
      notes?: string;
    }
  | {
      contactType: "Trust";
      trustName: string;
      ein?: string;
      category: TrustCategory;
      purpose: TrustPurpose;
      specificType?: string;
      trustDate?: string;
      stateOfFormation?: string;
      isGrantorTrust?: boolean;
      hasCrummeyProvisions?: boolean;
      isGstExempt?: boolean;
      email?: string;
      phone?: string;
      address?: ContactAddress;
      notes?: string;
    }
  | {
      contactType: "Organization";
      organizationName: string;
      ein?: string;
      organizationType: OrganizationType;
      is501C3?: boolean;
      email?: string;
      phone?: string;
      address?: ContactAddress;
      notes?: string;
    };
```

### UpdateContactRequest

Same shape as `CreateContactRequest` but all fields optional (partial update). The `contactType` **cannot be changed** after creation.

### AssetContactLinkDto

```csharp
public sealed record AssetContactLinkDto
{
    public Guid Id { get; init; }
    public Guid AssetId { get; init; }
    public string AssetName { get; init; } = string.Empty;
    public Guid ContactId { get; init; }
    public string ContactDisplayName { get; init; } = string.Empty;
    public AssetContactRole Role { get; init; }
    public DesignationType? Designation { get; init; }
    public decimal? AllocationPercent { get; init; }
    public bool PerStirpes { get; init; }
    public string? Notes { get; init; }
}
```

### TrustRoleDto

```csharp
public sealed record TrustRoleDto
{
    public Guid Id { get; init; }
    public Guid TrustContactId { get; init; }
    public Guid ContactId { get; init; }
    public string ContactDisplayName { get; init; } = string.Empty;
    public TrustRoleType RoleType { get; init; }
    public int? SuccessionOrder { get; init; }
    public string? Notes { get; init; }
}
```

### BeneficiaryCoverageDto

```csharp
/// <summary>
/// Summary of beneficiary coverage across all user assets.
/// </summary>
public sealed record BeneficiaryCoverageDto
{
    public int TotalAssets { get; init; }
    public int AssetsWithBeneficiaries { get; init; }
    public int AssetsWithoutBeneficiaries { get; init; }
    public decimal CoveragePercent { get; init; }           // 0-100
    public List<AssetCoverageItem> Assets { get; init; } = [];
}

public sealed record AssetCoverageItem
{
    public Guid AssetId { get; init; }
    public string AssetName { get; init; } = string.Empty;
    public AssetType AssetType { get; init; }
    public decimal? CurrentValue { get; init; }
    public decimal PrimaryAllocationTotal { get; init; }    // Should be 100
    public decimal ContingentAllocationTotal { get; init; } // Should be 100
    public bool HasPrimaryBeneficiary { get; init; }
    public bool HasContingentBeneficiary { get; init; }
    public bool AllocationWarning { get; init; }            // True if totals ≠ 100
}
```

---

## Validation Rules

| Rule | Level |
|------|-------|
| Total Primary allocation per asset should = 100% | Warning |
| Total Contingent allocation per asset should = 100% | Warning |
| Cannot link same contact twice to same asset with same role | Error |
| Cannot delete contact with active asset links | Error (must remove links first) |
| SSN format: XXX-XX-XXXX (9 digits) | Error |
| EIN format: XX-XXXXXXX (9 digits with hyphen after first 2) | Error |
| Trust must have at least one Trustee role | Warning |
| Individual requires `firstName` + `lastName` | Error |
| Trust requires `trustName` + `category` + `purpose` | Error |
| Organization requires `organizationName` + `organizationType` | Error |

### FluentValidation Example

```csharp
public class CreateContactRequestValidator : AbstractValidator<CreateContactRequest>
{
    public CreateContactRequestValidator()
    {
        RuleFor(x => x.ContactType).IsInEnum();
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);

        // Individual-specific
        When(x => x.ContactType == ContactType.Individual, () =>
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Ssn)
                .Matches(@"^\d{3}-\d{2}-\d{4}$")
                .When(x => x.Ssn is not null)
                .WithMessage("SSN must be in format XXX-XX-XXXX");
        });

        // Trust-specific
        When(x => x.ContactType == ContactType.Trust, () =>
        {
            RuleFor(x => x.TrustName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Category).NotNull();
            RuleFor(x => x.Purpose).NotNull();
            RuleFor(x => x.Ein)
                .Matches(@"^\d{2}-\d{7}$")
                .When(x => x.Ein is not null)
                .WithMessage("EIN must be in format XX-XXXXXXX");
        });

        // Organization-specific
        When(x => x.ContactType == ContactType.Organization, () =>
        {
            RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.OrganizationType).NotNull();
        });
    }
}
```

---

## Sensitive Field Handling

SSN and EIN use the same `[SensitiveData]` attribute and reveal pattern as asset account numbers (see [05-assets-portfolio.md](05-assets-portfolio.md)):

| Contact Type | Field | Mask Format | Example |
|--------------|-------|-------------|---------|
| IndividualContact | `Ssn` | Last 4 digits | `••••1234` |
| TrustContact | `Ein` | Last 4 digits | `••••5678` |
| OrganizationContact | `Ein` | Last 4 digits | `••••5678` |

- API returns masked values by default with a `masked` companion flag
- Reveal via: `GET /api/contacts/{id}/sensitive/{fieldName}`
- Every reveal is audit-logged

---

## UI Flow

### Creating a Contact

1. Select type: **Individual** / **Trust** / **Organization**
2. Fill type-specific required fields
3. For Trust: pick Category → Purpose → (optional) SpecificType
4. For Trust: add Roles (Grantor, Trustees, Beneficiaries, etc.)

### Linking Contact to Asset

1. Navigate to asset detail or contacts page
2. Select contact to link
3. Choose role (Beneficiary, CoOwner, Trustee, etc.)
4. If Beneficiary: set Designation (Primary/Contingent) + Allocation % + Per Stirpes

### Two Views (Contacts Page)

| View | Description |
|------|-------------|
| **By Contact** | List of contacts → shows what assets each is linked to |
| **By Asset** | List of assets → shows who is linked to each |

---

## Tier Limits

| Limit | Free | Premium |
|-------|------|---------|
| Contacts | 5 | Unlimited |

When limit is reached, API returns `429` with error code `TIER_LIMIT_REACHED`.

---

## Security & Access Control

### Ownership Model

Contacts use **UserId-based ownership** — every `Contact` record carries a `UserId` (Entra Object ID) and all queries filter by the authenticated user. There is no `TenantId` on Contact entities; ownership is strictly per-user.

```csharp
// All contact queries MUST filter by authenticated user
var contacts = await _context.Contacts
    .Where(c => c.UserId == authenticatedUserId)
    .ToListAsync();
```

### Authorization Model

Contact operations follow the **three-tier authorization pattern** defined in [03-authorization-data-access.md](03-authorization-data-access.md):

| Tier | Access Level | Description |
|------|-------------|-------------|
| **Owner** | Full CRUD | User who created the contact |
| **Granted** | Read-only (scoped) | Users with a `DataAccessGrant` that includes the `Contacts` category |
| **Administrator** | Full read, audit | Platform admins via `Administrator` role |

- **Advisors** access client contacts only through `DataAccessGrant` — the `Advisor` app role alone grants no data access.
- **Viewers** have no contact access unless explicitly granted.

### Operation-Level Access Control

| Operation | Owner | Granted (Advisor) | Administrator | Unauthenticated |
|-----------|-------|--------------------|---------------|-----------------|
| List contacts | ✅ | ✅ (read-only) | ✅ | ❌ 401 |
| Get contact by ID | ✅ | ✅ (read-only) | ✅ | ❌ 401 |
| Create contact | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| Update contact | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| Delete contact | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| Reveal sensitive field | ✅ | ❌ 403 | ✅ (audit-logged) | ❌ 401 |
| Link contact to asset | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| Manage trust roles | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| Get beneficiary coverage | ✅ | ✅ (read-only) | ✅ | ❌ 401 |

### Sensitive Data Classification

| Field | Entity | Classification | Storage | API Response |
|-------|--------|---------------|---------|-------------|
| `SSN` | `IndividualContact` | PII — Personally Identifiable | AES-256 encrypted (Always Encrypted) | Masked: `***-**-1234` |
| `EIN` | `TrustContact` | PII — Tax Identifier | AES-256 encrypted (Always Encrypted) | Masked: `**-***4567` |
| `EIN` | `OrganizationContact` | PII — Tax Identifier | AES-256 encrypted (Always Encrypted) | Masked: `**-***4567` |
| `DateOfBirth` | `IndividualContact` | PII — Personal | Standard column encryption | Full value returned |
| `Email` | `Contact` (base) | PII — Contact Info | Standard column encryption | Full value returned |
| `Phone` | `Contact` (base) | PII — Contact Info | Standard column encryption | Full value returned |

### Audit Logging

All security-sensitive contact operations generate structured audit log entries:

| Event | Logged Data | Severity |
|-------|------------|----------|
| Sensitive field reveal (SSN/EIN) | UserId, ContactId, FieldName, Timestamp | **Warning** |
| Contact deletion (with existing links) | UserId, ContactId, LinkCount, DeniedReason | **Warning** |
| Contact deletion (successful) | UserId, ContactId, ContactType | Information |
| Contact creation | UserId, ContactId, ContactType | Information |
| Authorization failure | UserId, AttemptedAction, ContactId, DeniedReason | **Warning** |
| DataAccessGrant used for contact access | GrantorId, GranteeId, ContactId | Information |

```csharp
// Sensitive field reveal — audit logging
_logger.LogWarning(
    "Sensitive field revealed: {FieldName} on Contact {ContactId} by User {UserId}",
    fieldName, contactId, userId
);

_auditLogger.LogDataAccess(
    action: "SensitiveFieldReveal",
    resourceType: "Contact",
    resourceId: contactId.ToString(),
    userId: userId,
    details: new { FieldName = fieldName }
);
```

### Data Retention

- **Active contacts**: Retained indefinitely while user account is active.
- **Deleted contacts**: Soft-deleted (if implemented) or hard-deleted with cascade rules enforced (links must be removed first due to `RESTRICT` delete behavior).
- **Audit logs**: Retained for minimum 7 years per financial compliance requirements.
- **Sensitive field access logs**: Retained for minimum 7 years.

---

## Address Component Libraries

The `ContactAddress` form uses shared address libraries — see [10-user-profile-settings.md](10-user-profile-settings.md) § Address Component Libraries for details:

- **`react-country-region-selector`** — dynamic country/region dropdowns
- **`i18n-postal-address`** — country-aware display formatting

A shared `<AddressForm />` component is used by both Contact forms and the Profile settings page.

---

## Cross-References

- Asset model & AssetContactLink bridge: [05-assets-portfolio.md](05-assets-portfolio.md)
- Sensitive field masking/reveal strategy: [../ASSET_TYPE_SPECIFICATIONS.md](../ASSET_TYPE_SPECIFICATIONS.md) § Sensitive Field Handling
- Data sharing (Contacts category in DataAccessGrant): [03-authorization-data-access.md](03-authorization-data-access.md)
- Contact specification source (archived): [../archive/CONTACT_MODEL_SPECIFICATION.md](../archive/CONTACT_MODEL_SPECIFICATION.md)

---

*Last Updated: February 2026*
