# Contact Model Specification

> **Last Updated**: February 27, 2026

## Design Principle

A **Contact** is the base entity for people and organizations. **"Beneficiary" is a *role*** a contact plays when linked to an asset — not a separate entity type. This allows the same contact to serve multiple roles:
- Beneficiary on a 401k
- Trustee on a trust
- Co-owner on real estate
- Emergency contact
- Power of attorney

> **Implementation note**: Contact entities are mutable `class` types (not `sealed record`) because EF Core requires mutable state for change tracking. This is an intentional difference from the `sealed record` pattern used for DTOs and metadata types in `ASSET_TYPE_SPECIFICATIONS.md`.

---

## Entity Hierarchy

```
Contact (base: Id, UserId, ContactType, Email?, Phone?, Address?, Notes?, CreatedAt, UpdatedAt)
├── IndividualContact (firstName, lastName, dob?, ssn?, relationship?)
├── TrustContact (trustName, ein?, category, purpose, specificType?, trustDate?,
│                 stateOfFormation?, isGrantorTrust, hasCrummeyProvisions, isGstExempt)
│                 + Roles[] → links to other Contacts
└── OrganizationContact (orgName, ein?, orgType, Is501C3?)
```

---

## Contact (Base Entity)

> **Tenant isolation**: `UserId` serves as the tenant scope. All queries filter by `UserId` via middleware — the same pattern used on the `Asset` entity. No separate `TenantId` is needed because each user owns their own contacts.

```csharp
public abstract class Contact
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ContactType ContactType { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Owned entity — stored as columns on the Contact table, not a JSON blob.
    // Enables querying by state/zip and consistent validation with AddressDto.
    public ContactAddress? Address { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<AssetContactLink> AssetLinks { get; set; } = new List<AssetContactLink>();

    public abstract string DisplayName { get; }
}

/// <summary>
/// EF Core owned entity type for contact addresses.
/// Configured via .OwnsOne(c => c.Address) in the entity configuration.
/// </summary>
public class ContactAddress
{
    public string Street1 { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "US";
}

public enum ContactType
{
    Individual,
    Trust,
    Organization
}
```

---

## IndividualContact

```csharp
public class IndividualContact : Contact
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    
    [SensitiveData(VisibleChars = 4)]
    public string? Ssn { get; set; }  // Encrypted at rest, masked in API responses
    // NOTE: Consider storing only last 4 digits if full SSN is not needed for
    // downstream integrations. If full SSN is stored, ensure data retention
    // and deletion policies are documented and enforced.
    
    public RelationshipType? Relationship { get; set; }
    
    public override string DisplayName => $"{FirstName} {LastName}".Trim();
}

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

Uses a **flexible Category + Purpose + SpecificType** model rather than enumerating all 50+ trust types.

```csharp
public class TrustContact : Contact
{
    public string TrustName { get; set; } = string.Empty;
    
    [SensitiveData(VisibleChars = 4)]
    public string? Ein { get; set; }  // Optional - revocable living trusts often don't have EINs
    // Format: XX-XXXXXXX (9 digits with hyphen after first 2)
    
    public TrustCategory Category { get; set; }
    public TrustPurpose Purpose { get; set; }
    
    /// <summary>
    /// Specific trust type name (e.g., "QTIP", "GRAT", "ILIT").
    /// Freeform to allow for 50+ trust variants without schema changes.
    /// </summary>
    public string? SpecificType { get; set; }
    
    public DateOnly? TrustDate { get; set; }
    public string? StateOfFormation { get; set; }
    
    // Tax treatment flags
    public bool IsGrantorTrust { get; set; }
    public bool HasCrummeyProvisions { get; set; }
    public bool IsGstExempt { get; set; }
    
    // Navigation - roles link to other contacts
    public ICollection<TrustRole> Roles { get; set; } = new List<TrustRole>();
    
    public override string DisplayName => TrustName;
}
```

### TrustCategory (Base Structural Type)

```csharp
public enum TrustCategory
{
    RevocableLiving,
    Irrevocable,
    Testamentary
}
```

### TrustPurpose (Primary Purpose)

```csharp
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

### Common SpecificType Values (Freeform Field)

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

---

## TrustRole

Links a contact to a trust with a specific role.

> **Cascade behavior**: When a `TrustContact` is deleted, its `TrustRole` records cascade-delete (the roles are meaningless without the trust).

```csharp
public class TrustRole
{
    public Guid Id { get; set; }
    public Guid TrustContactId { get; set; }
    public Guid ContactId { get; set; }  // The person/entity in this role
    public TrustRoleType RoleType { get; set; }

    /// <summary>
    /// For successor trustees: 1 = first successor, 2 = second, etc.
    /// </summary>
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

```csharp
public class OrganizationContact : Contact
{
    public string OrganizationName { get; set; } = string.Empty;
    
    [SensitiveData(VisibleChars = 4)]
    public string? Ein { get; set; }  // Encrypted at rest
    // Format: XX-XXXXXXX (9 digits with hyphen after first 2)

    public OrganizationType OrganizationType { get; set; }

    /// <summary>
    /// For charities: whether they have 501(c)(3) status.
    /// </summary>
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

> **Cascade behavior**: When an `Asset` is deleted, its `AssetContactLink` records cascade-delete (the link is meaningless without the asset). When a `Contact` is deleted, cascade is **blocked** — the user must remove asset links first (see validation rules). This prevents accidental orphaning of beneficiary designations.

```csharp
public class AssetContactLink
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid ContactId { get; set; }

    public AssetContactRole Role { get; set; }

    /// <summary>
    /// For beneficiaries: Primary or Contingent.
    /// Null for non-beneficiary roles.
    /// </summary>
    public DesignationType? Designation { get; set; }

    /// <summary>
    /// Allocation percentage (0-100). Required for beneficiaries.
    /// </summary>
    public decimal? AllocationPercent { get; set; }

    /// <summary>
    /// Per stirpes: if beneficiary dies, their share passes to their descendants.
    /// </summary>
    public bool PerStirpes { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public Asset Asset { get; set; } = null!;
    public Contact Contact { get; set; } = null!;
}

public enum AssetContactRole
{
    Beneficiary,
    CoOwner,
    Trustee,        // "Trustee on this asset" — e.g. trust holds this real estate.
                    // Distinct from TrustRoleType.Trustee which means "trustee OF a trust entity."
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

## API Endpoints

```
# Contact CRUD
GET    /api/contacts                     → GetContacts (?type=Individual|Trust|Organization)
GET    /api/contacts/{id}                → GetContactById
POST   /api/contacts                     → CreateContact
PUT    /api/contacts/{id}                → UpdateContact
DELETE /api/contacts/{id}                → DeleteContact

# Trust Roles (for TrustContact type only)
GET    /api/contacts/{id}/roles          → GetTrustRoles
POST   /api/contacts/{id}/roles          → AddTrustRole
PUT    /api/contacts/{id}/roles/{roleId} → UpdateTrustRole
DELETE /api/contacts/{id}/roles/{roleId} → RemoveTrustRole

# Asset-Contact Links (how contacts become beneficiaries, co-owners, etc.)
GET    /api/assets/{id}/contacts         → GetAssetContacts
POST   /api/assets/{id}/contacts         → LinkContactToAsset
PUT    /api/asset-links/{linkId}         → UpdateAssetContactLink
DELETE /api/asset-links/{linkId}         → RemoveAssetContactLink

# Coverage Analysis
GET    /api/contacts/coverage            → GetBeneficiaryCoverageSummary
```

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

The API returns a discriminated DTO based on `contactType`. The base fields are always present; subtype fields are only populated for their respective type.

```csharp
/// <summary>
/// Polymorphic contact response. Discriminated by ContactType.
/// Subtypes include their specific fields alongside the base fields.
/// </summary>
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
    public bool? SsnMasked { get; init; }            // True when Ssn is masked
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
    // Ein reused from Trust section (same field, same masking)
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

Discriminated by `contactType`. Validators enforce that the correct fields are populated for each type.

```csharp
/// <summary>
/// Polymorphic create request. The contactType field determines which
/// subtype fields are required/validated.
/// </summary>
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
    // Ein reused from Trust section
    public OrganizationType? OrganizationType { get; init; }
    public bool? Is501C3 { get; init; }
}
```

**TypeScript equivalent** (discriminated union):

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

Same shape as `CreateContactRequest` but all fields optional (partial update). The `contactType` cannot be changed after creation.

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
| Individual requires firstName + lastName | Error |
| Trust requires trustName + category + purpose | Error |
| Organization requires organizationName + organizationType | Error |

---

## UI Flow

**Creating a Contact:**
1. Select type: Individual / Trust / Organization
2. Fill type-specific fields
3. For Trust: pick Category → Purpose → (optional) SpecificType
4. For Trust: add Roles (Grantor, Trustees, Beneficiaries, etc.)

**Linking to Asset:**
1. Navigate to Asset detail or Contacts page
2. Select contact to link
3. Choose role (Beneficiary, CoOwner, etc.)
4. If Beneficiary: set Designation (Primary/Contingent) + Allocation % + Per Stirpes

**Two Views (on Contacts page):**
- **By Contact**: List of contacts → shows what assets they're linked to
- **By Asset**: List of assets → shows who's linked to each

---

## Benefits of This Model

| Benefit | Description |
|---------|-------------|
| **Reusable** | Same contact used across multiple assets/roles |
| **Flexible Trust Support** | Handles 50+ trust types without schema changes |
| **No Redundant Data** | Spouse entered once, linked many places |
| **Future-Proof** | Supports professional access, family sharing |
| **Clean Separation** | Contact = who, AssetContactLink = what role on which asset |

---

## Cascade Delete Summary

| Parent Deleted | Child Records | Behavior |
|----------------|---------------|----------|
| `Asset` | `AssetContactLink` | **Cascade delete** — links are meaningless without the asset |
| `TrustContact` | `TrustRole` | **Cascade delete** — roles are meaningless without the trust |
| `Contact` | `AssetContactLink` | **Restrict** — must remove links first (validation rule) |
| `Contact` | `TrustRole` (as role holder) | **Restrict** — must remove trust roles first |

Configure in EF Core:

```csharp
// In entity configuration
builder.HasMany(a => a.ContactLinks)
    .WithOne(l => l.Asset)
    .HasForeignKey(l => l.AssetId)
    .OnDelete(DeleteBehavior.Cascade);

builder.HasMany(t => t.Roles)
    .WithOne(r => r.TrustContact)
    .HasForeignKey(r => r.TrustContactId)
    .OnDelete(DeleteBehavior.Cascade);

builder.HasMany<AssetContactLink>()
    .WithOne(l => l.Contact)
    .HasForeignKey(l => l.ContactId)
    .OnDelete(DeleteBehavior.Restrict);

builder.HasMany<TrustRole>()
    .WithOne(r => r.Contact)
    .HasForeignKey(r => r.ContactId)
    .OnDelete(DeleteBehavior.Restrict);
```

---

## Cross-References

- **Asset Types**: See [`ASSET_TYPE_SPECIFICATIONS.md`](ASSET_TYPE_SPECIFICATIONS.md) for all 12 asset types and their metadata
- **Sensitive Fields**: See `ASSET_TYPE_SPECIFICATIONS.md` — Sensitive Field Handling section for masking/reveal strategy (applies to SSN, EIN)
- **API Details**: See [`RAJ_FINANCIAL_INTEGRATIONS_API.md`](RAJ_FINANCIAL_INTEGRATIONS_API.md) for full API specification
- **API Tracking**: See [`RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md`](RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md) for implementation status
