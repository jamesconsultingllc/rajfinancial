# Entity Restructure Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Restructure RAJ Financial from a flat "My Account" layout to an Entity-First Architecture where Personal, Business, and Trust entities each scope their own income, expenses, assets, liabilities, accounts, transactions, and documents.

**Architecture:** TPH (Table-Per-Hierarchy) Entity table with JSON metadata columns for Business/Trust-specific fields (matching existing asset metadata pattern). EntityRole table unifies business org chart and trust roles. Every existing feature gains an `EntityId` FK. Frontend uses EntityProvider context so the same page components render in different entity contexts.

**Tech Stack:** .NET 10 (Azure Functions isolated worker), EF Core 10 + SQL Server, MemoryPack serialization, FluentValidation, React 18 + TypeScript + Vite, TanStack Query, Tailwind CSS, shadcn/ui, Recharts, i18next. Tests: xUnit + FluentAssertions (unit), Reqnroll + Gherkin (API integration), Cucumber.js + Playwright + Gherkin (E2E).

**Design Doc:** `docs/features/12-entity-structure.md`

---

## Phase Overview

This plan is divided into phases. Each phase is independently deployable and testable. **Phase 1 is fully detailed below with BDD-first tasks. Phases 2–8 are outlined — each gets its own detailed plan when we reach it.**

| Phase | Description | Dependencies |
|-------|-------------|--------------|
| **Phase 1** | Core Entity Model + EntityRole + Migration | None |
| **Phase 2** | Entity Service + API Endpoints | Phase 1 |
| **Phase 3** | Asset EntityId Backfill + Scoped Queries | Phase 2 |
| **Phase 4** | Frontend Entity Navigation + Routing | Phase 2 |
| **Phase 5** | Income Sources (new feature) | Phase 2 |
| **Phase 6** | Bills & Recurring Expenses (new feature) | Phase 2 |
| **Phase 7** | Documents & Storage Connections | Phase 2 |
| **Phase 8** | Insurance, Debt Payoff, Alerts, Statements, Transfers, Household | Phases 5–7 |

---

## Phase 1: Core Entity Model + EntityRole + Migration

### Task 1: Write Entity BDD Feature (API Integration)

**Files:**
- Create: `tests/IntegrationTests/Features/Entities.feature`

**Step 1: Write the Gherkin feature file**

```gherkin
@api @entities @security
Feature: Entity CRUD Operations
    As an authenticated user
    I want to create and manage financial entities (Personal, Business, Trust)
    So that I can organize my finances by entity

    Security coverage:
    - OWASP A01:2025 - Broken Access Control (IDOR prevention)
    - OWASP A07:2025 - Authentication Failures

    Background:
        Given the API is running

    # =========================================================================
    # Authentication Guard
    # =========================================================================

    @security @A01
    Scenario: Unauthenticated request to list entities returns 401
        Given I am not authenticated
        When I send a GET request to "/api/entities"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    @security @A01
    Scenario: Unauthenticated request to create entity returns 401
        Given I am not authenticated
        When I send a POST request to "/api/entities" with an empty body
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    # =========================================================================
    # Auto-Provisioned Personal Entity
    # =========================================================================

    @smoke
    Scenario: New user automatically gets a Personal entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I send a GET request to "/api/entities"
        Then the response status should be 200
        And the response should contain at least 1 entity
        And the response should contain an entity of type "Personal"

    # =========================================================================
    # POST /api/entities — Create Business Entity
    # =========================================================================

    @smoke
    Scenario: Create a new Business entity with required fields
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an entity with the following details:
            | Name     | Type     |
            | Acme LLC | Business |
        Then the response status should be 201
        And the response should contain the entity name "Acme LLC"
        And the response should contain the entity type "Business"
        And the response should contain a non-empty slug

    Scenario: Create a Business entity with metadata
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create a business entity "Acme LLC" with:
            | EntityFormationType | Ein        | Industry   | StateOfFormation |
            | MultiMemberLLC      | 12-3456789 | Technology | Delaware         |
        Then the response status should be 201
        And the response should contain EIN "12-3456789"

    # =========================================================================
    # POST /api/entities — Create Trust Entity
    # =========================================================================

    @smoke
    Scenario: Create a new Trust entity with required fields
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an entity with the following details:
            | Name         | Type  |
            | Family Trust | Trust |
        Then the response status should be 201
        And the response should contain the entity name "Family Trust"
        And the response should contain the entity type "Trust"

    Scenario: Create a Trust entity with metadata
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create a trust entity "Family Trust" with:
            | Category  | Purpose        | IsGrantorTrust | Jurisdiction |
            | Revocable | EstatePlanning | true           | California   |
        Then the response status should be 201
        And the response should contain jurisdiction "California"

    # =========================================================================
    # Validation
    # =========================================================================

    Scenario: Cannot create entity without name
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an entity with the following details:
            | Name | Type     |
            |      | Business |
        Then the response status should be 400
        And the error code should be "VALIDATION_FAILED"

    Scenario: Cannot create a second Personal entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I create an entity with the following details:
            | Name        | Type     |
            | My Personal | Personal |
        Then the response status should be 400
        And the error code should be "ENTITY_PERSONAL_ALREADY_EXISTS"

    Scenario: Cannot create entity with duplicate slug
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Acme LLC"
        When I create an entity with the following details:
            | Name     | Type     |
            | Acme LLC | Business |
        Then the response status should be 409
        And the error code should be "ENTITY_SLUG_DUPLICATE"

    # =========================================================================
    # GET /api/entities — List Entities
    # =========================================================================

    @smoke
    Scenario: User retrieves all their entities
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Acme LLC"
        And I have created a trust entity "Family Trust"
        When I send a GET request to "/api/entities"
        Then the response status should be 200
        And the response should contain at least 3 entities

    Scenario: Filter entities by type
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Acme LLC"
        And I have created a trust entity "Family Trust"
        When I send a GET request to "/api/entities?type=Business"
        Then the response status should be 200
        And all returned entities should have type "Business"

    # =========================================================================
    # GET /api/entities/{id} — Get Entity By ID
    # =========================================================================

    @smoke
    Scenario: User retrieves entity by ID
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Acme LLC"
        When I request the entity by its ID
        Then the response status should be 200
        And the response should contain the entity name "Acme LLC"

    Scenario: Request non-existent entity returns 404
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I send a GET request to "/api/entities/00000000-0000-0000-0000-000000000099"
        Then the response status should be 404

    # =========================================================================
    # PUT /api/entities/{id} — Update Entity
    # =========================================================================

    @smoke
    Scenario: User updates their business entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Acme LLC"
        When I update the entity with name "Acme Holdings LLC"
        Then the response status should be 200
        And the response should contain the entity name "Acme Holdings LLC"

    Scenario: Cannot update Personal entity name
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I update my Personal entity with name "Custom Name"
        Then the response status should be 400
        And the error code should be "ENTITY_PERSONAL_NAME_IMMUTABLE"

    # =========================================================================
    # DELETE /api/entities/{id} — Delete Entity
    # =========================================================================

    @smoke
    Scenario: User deletes their business entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have created a business entity "Temp LLC"
        When I delete the entity by its ID
        Then the response status should be 204

    Scenario: Cannot delete Personal entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        When I try to delete my Personal entity
        Then the response status should be 400
        And the error code should be "ENTITY_PERSONAL_CANNOT_DELETE"

    # =========================================================================
    # IDOR Prevention — Cross-User Access
    # =========================================================================

    @security @A01 @idor
    Scenario: User cannot access another user's entity by ID
        Given I am authenticated as user "other@rajfinancial.com" with role "Client"
        And user "owner@rajfinancial.com" has a business entity
        When I request that entity by ID
        Then access should be denied by the service tier

    @security @A01
    Scenario: Administrator can access any user's entities
        Given I am authenticated as user "admin@rajfinancial.com" with role "Administrator"
        And user "owner@rajfinancial.com" has entities
        When I send a GET request to "/api/entities?ownerUserId={ownerUserId}"
        Then the response status should be 200
```

**Step 2: Commit**

```bash
git add tests/IntegrationTests/Features/Entities.feature
git commit -m "feat(entities): add BDD feature file for Entity CRUD"
```

---

### Task 2: Write EntityRole BDD Feature (API Integration)

**Files:**
- Create: `tests/IntegrationTests/Features/EntityRoles.feature`

**Step 1: Write the Gherkin feature file**

```gherkin
@api @entity-roles @security
Feature: Entity Role Management
    As a user with entities
    I want to assign roles (owners, trustees, beneficiaries) to contacts on my entities
    So that I can track who is involved with each entity

    Background:
        Given the API is running

    # =========================================================================
    # Authentication Guard
    # =========================================================================

    @security @A01
    Scenario: Unauthenticated request to list entity roles returns 401
        Given I am not authenticated
        When I send a GET request to "/api/entities/00000000-0000-0000-0000-000000000001/roles"
        Then the response status should be 401
        And the error code should be "AUTH_REQUIRED"

    # =========================================================================
    # POST /api/entities/{entityId}/roles — Assign Role
    # =========================================================================

    @smoke
    Scenario: Add an owner role to a business entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a business entity "Acme LLC"
        And I have a contact "John Doe"
        When I assign role "Owner" to contact "John Doe" on entity "Acme LLC" with:
            | OwnershipPercent | Title          | IsSignatory |
            | 50.00            | Managing Member | true        |
        Then the response status should be 201
        And the response should contain role type "Owner"
        And the response should contain ownership percent 50.00

    @smoke
    Scenario: Add a trustee role to a trust entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a trust entity "Family Trust"
        And I have a contact "Jane Smith"
        When I assign role "Trustee" to contact "Jane Smith" on entity "Family Trust" with:
            | IsPrimary | Title         |
            | true      | Primary Trustee |
        Then the response status should be 201
        And the response should contain role type "Trustee"

    Scenario: Add a beneficiary with beneficial interest percent
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a trust entity "Family Trust"
        And I have a contact "Child Doe"
        When I assign role "Beneficiary" to contact "Child Doe" on entity "Family Trust" with:
            | BeneficialInterestPercent |
            | 33.33                     |
        Then the response status should be 201
        And the response should contain beneficial interest percent 33.33

    # =========================================================================
    # Validation
    # =========================================================================

    Scenario: Cannot assign trust-only role to business entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a business entity "Acme LLC"
        And I have a contact "John Doe"
        When I assign role "Grantor" to contact "John Doe" on entity "Acme LLC"
        Then the response status should be 400
        And the error code should be "ENTITY_ROLE_INVALID_FOR_TYPE"

    Scenario: Cannot assign business-only role to trust entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a trust entity "Family Trust"
        And I have a contact "Jane Smith"
        When I assign role "Officer" to contact "Jane Smith" on entity "Family Trust"
        Then the response status should be 400
        And the error code should be "ENTITY_ROLE_INVALID_FOR_TYPE"

    Scenario: Ownership percent must not exceed 100 across all owners
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a business entity "Acme LLC" with an owner at 80%
        And I have a contact "New Partner"
        When I assign role "Owner" to contact "New Partner" on entity "Acme LLC" with:
            | OwnershipPercent |
            | 30.00            |
        Then the response status should be 400
        And the error code should be "ENTITY_ROLE_OWNERSHIP_EXCEEDS_100"

    # =========================================================================
    # GET /api/entities/{entityId}/roles — List Roles
    # =========================================================================

    @smoke
    Scenario: List all roles for a business entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a business entity "Acme LLC" with roles assigned
        When I send a GET request to "/api/entities/{entityId}/roles"
        Then the response status should be 200
        And the response should contain at least 1 role

    # =========================================================================
    # DELETE /api/entities/{entityId}/roles/{roleId} — Remove Role
    # =========================================================================

    @smoke
    Scenario: Remove a role from an entity
        Given I am authenticated as user "owner@rajfinancial.com" with role "Client"
        And I have a business entity "Acme LLC" with a role assigned
        When I delete the role by its ID
        Then the response status should be 204
```

**Step 2: Commit**

```bash
git add tests/IntegrationTests/Features/EntityRoles.feature
git commit -m "feat(entities): add BDD feature file for EntityRole management"
```

---

### Task 3: Write Entity E2E Feature (Cucumber.js + Playwright)

**Files:**
- Create: `tests/e2e/features/Entities.feature`

**Step 1: Write the E2E Gherkin feature file**

```gherkin
@entities @requires-auth
Feature: Entity Management UI
    As a logged-in user
    I want to see my entities in the sidebar and manage them
    So that I can organize my financial data by entity

    Background:
        Given I am logged in as a "Client"

    # =========================================================================
    # Sidebar Navigation
    # =========================================================================

    @smoke
    Scenario: User sees Personal section in sidebar
        When I view the navigation menu
        Then I should see the "Personal" section
        And I should see the "Overview" link under "Personal"

    @smoke
    Scenario: User sees Business section in sidebar
        When I view the navigation menu
        Then I should see the "Business" section

    @smoke
    Scenario: User sees Trusts section in sidebar
        When I view the navigation menu
        Then I should see the "Trusts" section

    # =========================================================================
    # Entity Creation
    # =========================================================================

    Scenario: User creates a new Business entity
        When I click the "Add Business" button
        Then I should see the "Create Business Entity" form
        When I fill in "Entity Name" with "Acme LLC"
        And I select "Multi-Member LLC" for "Formation Type"
        And I click the "Create" button
        Then I should see "Acme LLC" in the Business section
        And I should be on the "business/acme-llc/overview" page

    Scenario: User creates a new Trust entity
        When I click the "Add Trust" button
        Then I should see the "Create Trust Entity" form
        When I fill in "Entity Name" with "Family Trust"
        And I select "Revocable" for "Category"
        And I click the "Create" button
        Then I should see "Family Trust" in the Trusts section

    # =========================================================================
    # Entity Overview Page
    # =========================================================================

    @smoke
    Scenario: Personal overview shows summary cards
        When I navigate to "/personal/overview"
        Then I should see the "Net Worth" card
        And I should see the "Monthly Income" card
        And I should see the "Monthly Expenses" card

    # =========================================================================
    # Entity Sub-Navigation
    # =========================================================================

    Scenario: Personal entity has all sub-navigation links
        When I navigate to "/personal/overview"
        Then I should see the "Income" link
        And I should see the "Bills & Expenses" link
        And I should see the "Assets" link
        And I should see the "Accounts" link
        And I should see the "Insurance" link
        And I should see the "Documents" link

    # =========================================================================
    # Mobile
    # =========================================================================

    @mobile
    Scenario: Entity navigation works on mobile
        Given the viewport is set to mobile size
        When I click the hamburger menu button
        Then I should see the "Personal" section
        And I should see the "Business" section
        And I should see the "Trusts" section

    # =========================================================================
    # Accessibility
    # =========================================================================

    @accessibility
    Scenario: Entity sections are keyboard navigable
        When I press Tab multiple times
        Then I should be able to navigate through menu items
        And each focused item should have a visible focus indicator
```

**Step 2: Commit**

```bash
git add tests/e2e/features/Entities.feature
git commit -m "feat(entities): add E2E BDD feature file for Entity navigation"
```

---

### Task 4: Create Entity and EntityRole Enums

**Files:**
- Create: `src/Shared/Entities/EntityType.cs`
- Create: `src/Shared/Entities/EntityRoleType.cs`
- Create: `src/Shared/Entities/BusinessFormationType.cs`
- Create: `src/Shared/Entities/TaxClassification.cs`
- Create: `src/Shared/Entities/TrustCategory.cs`
- Create: `src/Shared/Entities/TrustPurpose.cs`
- Create: `src/Shared/Entities/StorageProvider.cs`

**Step 1: Create all enum files**

`src/Shared/Entities/EntityType.cs`:
```csharp
namespace RajFinancial.Shared.Entities;

/// <summary>
///     Classification of a financial entity.
/// </summary>
public enum EntityType
{
    /// <summary>Individual/family finances. One per user, auto-created.</summary>
    Personal = 0,

    /// <summary>LLC, corporation, partnership, sole proprietorship.</summary>
    Business = 1,

    /// <summary>Revocable, irrevocable, special needs, charitable trust.</summary>
    Trust = 2,
}
```

`src/Shared/Entities/EntityRoleType.cs`:
```csharp
namespace RajFinancial.Shared.Entities;

/// <summary>
///     Unified role type for business org chart and trust role assignments.
///     Business roles (0–9), Trust roles (10–19), Shared (99).
/// </summary>
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

`src/Shared/Entities/BusinessFormationType.cs`:
```csharp
namespace RajFinancial.Shared.Entities;

/// <summary>
///     Legal formation type for business entities.
/// </summary>
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
```

`src/Shared/Entities/TaxClassification.cs`:
```csharp
namespace RajFinancial.Shared.Entities;

/// <summary>
///     IRS tax classification for business entities.
/// </summary>
public enum TaxClassification
{
    SoleProprietor    = 0,
    Partnership       = 1,
    SCorporation      = 2,
    CCorporation      = 3,
    NonProfit501c3    = 4,
    DisregardedEntity = 5,
}
```

`src/Shared/Entities/TrustCategory.cs`:
```csharp
namespace RajFinancial.Shared.Entities;

/// <summary>
///     Primary classification of a trust entity.
/// </summary>
public enum TrustCategory
{
    Revocable   = 0,
    Irrevocable = 1,
}
```

`src/Shared/Entities/TrustPurpose.cs`:
```csharp
namespace RajFinancial.Shared.Entities;

/// <summary>
///     Purpose classification for trust entities.
/// </summary>
public enum TrustPurpose
{
    AssetProtection    = 0,
    EstatePlanning     = 1,
    Charitable         = 2,
    SpecialNeeds       = 3,
    Education          = 4,
    BusinessSuccession = 5,
    TaxPlanning        = 6,
    Other              = 7,
}
```

`src/Shared/Entities/StorageProvider.cs`:
```csharp
namespace RajFinancial.Shared.Entities;

/// <summary>
///     Cloud storage providers supported for document storage.
/// </summary>
public enum StorageProvider
{
    OneDrive    = 0,
    GoogleDrive = 1,
    Dropbox     = 2,
}
```

**Step 2: Verify build**

Run: `dotnet build src/Shared/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Shared/Entities/EntityType.cs src/Shared/Entities/EntityRoleType.cs \
  src/Shared/Entities/BusinessFormationType.cs src/Shared/Entities/TaxClassification.cs \
  src/Shared/Entities/TrustCategory.cs src/Shared/Entities/TrustPurpose.cs \
  src/Shared/Entities/StorageProvider.cs
git commit -m "feat(entities): add Entity, EntityRole, and related enums"
```

---

### Task 5: Create Entity and Related Entity Classes

**Files:**
- Create: `src/Shared/Entities/Entity.cs`
- Create: `src/Shared/Entities/EntityRole.cs`
- Create: `src/Shared/Entities/BusinessEntityMetadata.cs`
- Create: `src/Shared/Entities/TrustEntityMetadata.cs`
- Create: `src/Shared/Entities/StateRegistration.cs`

**Step 1: Create Entity.cs**

```csharp
using MemoryPack;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Core organizing unit for financial data. Every asset, income source, bill,
///     account, and document is scoped to an entity.
/// </summary>
/// <remarks>
///     <para>
///         Three entity types exist: Personal (one per user, auto-created),
///         Business (LLCs, corporations, etc.), and Trust (revocable, irrevocable, etc.).
///     </para>
///     <para>
///         Uses TPH (Table-Per-Hierarchy) with JSON metadata columns for type-specific
///         fields via EF Core <c>ToJson()</c>.
///     </para>
/// </remarks>
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class Entity
{
    /// <summary>Unique identifier.</summary>
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }

    /// <summary>The user who owns this entity (Entra Object ID).</summary>
    [MemoryPackOrder(1)]
    public Guid UserId { get; set; }

    /// <summary>Entity classification: Personal, Business, or Trust.</summary>
    [MemoryPackOrder(2)]
    public EntityType Type { get; set; }

    /// <summary>Display name (e.g., "Personal", "Acme LLC", "Family Trust").</summary>
    [MemoryPackOrder(3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe slug derived from name (e.g., "acme-llc").</summary>
    [MemoryPackOrder(4)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional parent entity for nesting (e.g., holding company → subsidiary).</summary>
    [MemoryPackOrder(5)]
    public Guid? ParentEntityId { get; set; }

    /// <summary>Cloud storage connection for this entity's documents.</summary>
    [MemoryPackOrder(6)]
    public Guid? StorageConnectionId { get; set; }

    /// <summary>Whether this entity is currently active.</summary>
    [MemoryPackOrder(7)]
    public bool IsActive { get; set; } = true;

    /// <summary>Date and time the entity was created.</summary>
    [MemoryPackOrder(8)]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Date and time the entity was last updated.</summary>
    [MemoryPackOrder(9)]
    public DateTimeOffset? UpdatedAt { get; set; }

    // ── Type-specific metadata (JSON columns via ToJson()) ──

    /// <summary>Business-specific metadata. Null for non-Business entities.</summary>
    [MemoryPackOrder(10)]
    public BusinessEntityMetadata? Business { get; set; }

    /// <summary>Trust-specific metadata. Null for non-Trust entities.</summary>
    [MemoryPackOrder(11)]
    public TrustEntityMetadata? Trust { get; set; }

    // ── Navigation properties (ignored by MemoryPack) ──

    /// <summary>Parent entity (for nested entities).</summary>
    [MemoryPackIgnore]
    public Entity? ParentEntity { get; set; }

    /// <summary>Child entities (subsidiaries, sub-trusts).</summary>
    [MemoryPackIgnore]
    public ICollection<Entity> ChildEntities { get; set; } = [];

    /// <summary>Roles assigned to contacts on this entity.</summary>
    [MemoryPackIgnore]
    public ICollection<EntityRole> Roles { get; set; } = [];
}
```

**Step 2: Create EntityRole.cs**

```csharp
using MemoryPack;

namespace RajFinancial.Shared.Entities;

/// <summary>
///     Assigns a contact to a role on an entity. Handles both business org chart
///     positions (Owner, Officer, Director) and trust roles (Grantor, Trustee, Beneficiary).
/// </summary>
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class EntityRole
{
    /// <summary>Unique identifier.</summary>
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }

    /// <summary>The entity this role belongs to.</summary>
    [MemoryPackOrder(1)]
    public Guid EntityId { get; set; }

    /// <summary>The contact assigned to this role.</summary>
    [MemoryPackOrder(2)]
    public Guid ContactId { get; set; }

    /// <summary>Type of role (Owner, Trustee, Beneficiary, etc.).</summary>
    [MemoryPackOrder(3)]
    public EntityRoleType RoleType { get; set; }

    /// <summary>Display title (e.g., "CEO", "Managing Member", "Primary Trustee").</summary>
    [MemoryPackOrder(4)]
    public string? Title { get; set; }

    /// <summary>Ownership percentage for business entities (0–100).</summary>
    [MemoryPackOrder(5)]
    public decimal? OwnershipPercent { get; set; }

    /// <summary>Beneficial interest percentage for trust entities (0–100).</summary>
    [MemoryPackOrder(6)]
    public decimal? BeneficialInterestPercent { get; set; }

    /// <summary>Whether this person can sign on behalf of the entity.</summary>
    [MemoryPackOrder(7)]
    public bool IsSignatory { get; set; }

    /// <summary>Whether this is the primary role holder (e.g., primary trustee).</summary>
    [MemoryPackOrder(8)]
    public bool IsPrimary { get; set; }

    /// <summary>Sort order for succession planning or display ordering.</summary>
    [MemoryPackOrder(9)]
    public int SortOrder { get; set; }

    /// <summary>Date when this role became effective.</summary>
    [MemoryPackOrder(10)]
    public DateTimeOffset? EffectiveDate { get; set; }

    /// <summary>Date when this role ended (null if still active).</summary>
    [MemoryPackOrder(11)]
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>Optional notes about this role assignment.</summary>
    [MemoryPackOrder(12)]
    public string? Notes { get; set; }

    // ── Navigation properties ──

    /// <summary>The entity this role belongs to.</summary>
    [MemoryPackIgnore]
    public Entity Entity { get; set; } = null!;
}
```

**Step 3: Create BusinessEntityMetadata.cs**

```csharp
namespace RajFinancial.Shared.Entities;

/// <summary>
///     Business-specific metadata stored as a JSON column on the Entity table.
/// </summary>
public sealed record BusinessEntityMetadata
{
    /// <summary>Legal formation type (LLC, S-Corp, etc.).</summary>
    public BusinessFormationType EntityFormationType { get; init; }

    /// <summary>Employer Identification Number.</summary>
    public string? Ein { get; init; }

    /// <summary>Dun & Bradstreet DUNS number.</summary>
    public string? DunsNumber { get; init; }

    /// <summary>North American Industry Classification System code.</summary>
    public string? NaicsCode { get; init; }

    /// <summary>Industry description.</summary>
    public string? Industry { get; init; }

    /// <summary>State where the entity was formed.</summary>
    public string? StateOfFormation { get; init; }

    /// <summary>Date of formation or incorporation.</summary>
    public DateTimeOffset? FormationDate { get; init; }

    /// <summary>Fiscal year end month (1–12). Null defaults to December.</summary>
    public int? FiscalYearEnd { get; init; }

    /// <summary>Registered agent name.</summary>
    public string? RegisteredAgentName { get; init; }

    /// <summary>Annual revenue (for classification purposes).</summary>
    public decimal? AnnualRevenue { get; init; }

    /// <summary>Number of employees.</summary>
    public int? NumberOfEmployees { get; init; }

    /// <summary>IRS tax classification.</summary>
    public TaxClassification? TaxClassification { get; init; }

    /// <summary>State registrations (SOS filings, annual reports).</summary>
    public StateRegistration[]? Registrations { get; init; }
}
```

**Step 4: Create TrustEntityMetadata.cs**

```csharp
namespace RajFinancial.Shared.Entities;

/// <summary>
///     Trust-specific metadata stored as a JSON column on the Entity table.
/// </summary>
public sealed record TrustEntityMetadata
{
    /// <summary>Revocable or Irrevocable.</summary>
    public TrustCategory Category { get; init; }

    /// <summary>Purpose of the trust.</summary>
    public TrustPurpose Purpose { get; init; }

    /// <summary>Specific trust type (e.g., "GRAT", "QPRT", "ILIT").</summary>
    public string? SpecificType { get; init; }

    /// <summary>Trust's EIN (if applicable).</summary>
    public string? Ein { get; init; }

    /// <summary>Date the trust was established.</summary>
    public DateTimeOffset? TrustDate { get; init; }

    /// <summary>Jurisdiction (state) governing the trust.</summary>
    public string? Jurisdiction { get; init; }

    /// <summary>Whether the grantor is treated as the owner for tax purposes.</summary>
    public bool IsGrantorTrust { get; init; }

    /// <summary>Whether the trust has Crummey withdrawal provisions.</summary>
    public bool HasCrummeyProvisions { get; init; }

    /// <summary>Whether the trust is exempt from generation-skipping transfer tax.</summary>
    public bool IsGstExempt { get; init; }

    /// <summary>Initial or total funding amount.</summary>
    public decimal? FundingAmount { get; init; }

    /// <summary>Successor trustee succession plan description.</summary>
    public string? SuccessorTrusteePlan { get; init; }
}
```

**Step 5: Create StateRegistration.cs**

```csharp
namespace RajFinancial.Shared.Entities;

/// <summary>
///     State registration record for a business entity (SOS filing, annual report).
/// </summary>
public sealed record StateRegistration
{
    /// <summary>State abbreviation (e.g., "DE", "CA").</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>State registration or entity number.</summary>
    public string? RegistrationNumber { get; init; }

    /// <summary>Secretary of State filing number.</summary>
    public string? SosFilingNumber { get; init; }

    /// <summary>Date registered in this state.</summary>
    public DateTimeOffset? RegisteredDate { get; init; }

    /// <summary>Next annual report due date.</summary>
    public DateTimeOffset? AnnualReportDueDate { get; init; }

    /// <summary>Whether the entity is in good standing in this state.</summary>
    public bool IsInGoodStanding { get; init; }
}
```

**Step 6: Verify build**

Run: `dotnet build src/Shared/`
Expected: Build succeeded

**Step 7: Commit**

```bash
git add src/Shared/Entities/Entity.cs src/Shared/Entities/EntityRole.cs \
  src/Shared/Entities/BusinessEntityMetadata.cs src/Shared/Entities/TrustEntityMetadata.cs \
  src/Shared/Entities/StateRegistration.cs
git commit -m "feat(entities): add Entity, EntityRole, and metadata entity classes"
```

---

### Task 6: Create Entity DTOs and Error Codes

**Files:**
- Create: `src/Shared/Contracts/Entities/EntityDto.cs`
- Create: `src/Shared/Contracts/Entities/EntityDetailDto.cs`
- Create: `src/Shared/Contracts/Entities/CreateEntityRequest.cs`
- Create: `src/Shared/Contracts/Entities/UpdateEntityRequest.cs`
- Create: `src/Shared/Contracts/Entities/EntityRoleDto.cs`
- Create: `src/Shared/Contracts/Entities/CreateEntityRoleRequest.cs`
- Create: `src/Shared/Contracts/Entities/EntityErrorCodes.cs`

**Step 1: Create DTOs**

Follow existing pattern from `AssetDto.cs` — MemoryPackable sealed partial records with `[MemoryPackOrder(n)]`, `[GenerateTypeScript]`. Use `double` for money fields, `DateTime` for dates (MemoryPack convention).

`EntityDto.cs` — list view (name, type, slug, isActive, metadata summaries).
`EntityDetailDto.cs` — extends EntityDto with full Business/Trust metadata and roles list.
`CreateEntityRequest.cs` — name, type, optional business/trust metadata fields.
`UpdateEntityRequest.cs` — name, optional metadata updates.
`EntityRoleDto.cs` — role type, contact info, ownership/beneficial interest percents.
`CreateEntityRoleRequest.cs` — contactId, roleType, title, percentages, isSignatory, etc.
`EntityErrorCodes.cs` — all error code constants.

**Step 2: Create EntityErrorCodes.cs**

```csharp
namespace RajFinancial.Shared.Contracts.Entities;

/// <summary>
///     Error codes returned by entity validation and service operations.
/// </summary>
public static class EntityErrorCodes
{
    // Required fields
    public const string NAME_REQUIRED = "ENTITY_NAME_REQUIRED";
    public const string TYPE_REQUIRED = "ENTITY_TYPE_REQUIRED";

    // Max length
    public const string NAME_MAX_LENGTH = "ENTITY_NAME_MAX_LENGTH";

    // Business rules
    public const string PERSONAL_ALREADY_EXISTS = "ENTITY_PERSONAL_ALREADY_EXISTS";
    public const string PERSONAL_CANNOT_DELETE = "ENTITY_PERSONAL_CANNOT_DELETE";
    public const string PERSONAL_NAME_IMMUTABLE = "ENTITY_PERSONAL_NAME_IMMUTABLE";
    public const string SLUG_DUPLICATE = "ENTITY_SLUG_DUPLICATE";
    public const string NOT_FOUND = "ENTITY_NOT_FOUND";
    public const string TYPE_INVALID = "ENTITY_TYPE_INVALID";

    // Role errors
    public const string ROLE_NOT_FOUND = "ENTITY_ROLE_NOT_FOUND";
    public const string ROLE_INVALID_FOR_TYPE = "ENTITY_ROLE_INVALID_FOR_TYPE";
    public const string ROLE_OWNERSHIP_EXCEEDS_100 = "ENTITY_ROLE_OWNERSHIP_EXCEEDS_100";
    public const string ROLE_CONTACT_REQUIRED = "ENTITY_ROLE_CONTACT_REQUIRED";
    public const string ROLE_TYPE_REQUIRED = "ENTITY_ROLE_TYPE_REQUIRED";
}
```

**Step 3: Verify build**

Run: `dotnet build src/Shared/`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/Shared/Contracts/Entities/
git commit -m "feat(entities): add Entity DTOs, request models, and error codes"
```

---

### Task 7: Write Unit Tests for Entity Service

**Files:**
- Create: `tests/Api.Tests/Services/EntityService/EntityServiceTests.cs`

**Step 1: Write failing unit tests**

Follow the existing `AssetServiceTests.cs` pattern — xUnit + FluentAssertions + InMemoryDatabase + mocked IAuthorizationService. Test all service behaviors:

- `CreateEntity_Business_ReturnsDto`
- `CreateEntity_Trust_ReturnsDto`
- `CreateEntity_PersonalAlreadyExists_ThrowsBusinessRuleException`
- `CreateEntity_DuplicateSlug_ThrowsConflictException`
- `CreateEntity_GeneratesSlugFromName`
- `GetEntities_ReturnsAllUserEntities`
- `GetEntities_FilterByType_ReturnsFiltered`
- `GetEntityById_Exists_ReturnsDetailDto`
- `GetEntityById_NotFound_ThrowsNotFoundException`
- `GetEntityById_OtherUser_ThrowsForbiddenException`
- `UpdateEntity_Business_UpdatesMetadata`
- `UpdateEntity_PersonalName_ThrowsBusinessRuleException`
- `DeleteEntity_Business_Succeeds`
- `DeleteEntity_Personal_ThrowsBusinessRuleException`
- `DeleteEntity_OtherUser_ThrowsForbiddenException`

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Api.Tests/ --filter "FullyQualifiedName~EntityServiceTests" -v n`
Expected: All tests FAIL (service not yet implemented)

**Step 3: Commit failing tests**

```bash
git add tests/Api.Tests/Services/EntityService/
git commit -m "test(entities): add failing unit tests for EntityService"
```

---

### Task 8: Write Unit Tests for EntityRole Service

**Files:**
- Create: `tests/Api.Tests/Services/EntityService/EntityRoleServiceTests.cs`

**Step 1: Write failing unit tests**

- `AssignRole_BusinessOwner_ReturnsDto`
- `AssignRole_TrustTrustee_ReturnsDto`
- `AssignRole_TrustRoleOnBusiness_ThrowsBusinessRuleException`
- `AssignRole_BusinessRoleOnTrust_ThrowsBusinessRuleException`
- `AssignRole_OwnershipExceeds100_ThrowsBusinessRuleException`
- `AssignRole_BeneficialInterest_ReturnsDto`
- `GetRoles_ReturnsAllRolesForEntity`
- `RemoveRole_Exists_Succeeds`
- `RemoveRole_NotFound_ThrowsNotFoundException`

**Step 2: Run to verify failures, commit**

```bash
git add tests/Api.Tests/Services/EntityService/EntityRoleServiceTests.cs
git commit -m "test(entities): add failing unit tests for EntityRole operations"
```

---

### Task 9: Create EF Configuration and DbContext Updates

**Files:**
- Create: `src/Api/Data/Configurations/EntityConfiguration.cs`
- Create: `src/Api/Data/Configurations/EntityRoleConfiguration.cs`
- Modify: `src/Api/Data/ApplicationDbContext.cs`

**Step 1: Create EntityConfiguration.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Data.Configurations;

/// <summary>
///     EF Core configuration for the <see cref="Entity"/> class.
///     Configures JSON metadata columns, indexes, and relationships.
/// </summary>
public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.ToTable("Entities");
        builder.HasKey(e => e.Id);

        // Unique slug per user
        builder.HasIndex(e => new { e.UserId, e.Slug }).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.Type });

        // Property constraints
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);

        // JSON metadata columns
        builder.OwnsOne(e => e.Business, b => b.ToJson());
        builder.OwnsOne(e => e.Trust, t => t.ToJson());

        // Self-referencing parent-child
        builder.HasOne(e => e.ParentEntity)
            .WithMany(e => e.ChildEntities)
            .HasForeignKey(e => e.ParentEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to UserProfile
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**Step 2: Create EntityRoleConfiguration.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajFinancial.Shared.Entities;

namespace RajFinancial.Api.Data.Configurations;

/// <summary>
///     EF Core configuration for the <see cref="EntityRole"/> class.
/// </summary>
public class EntityRoleConfiguration : IEntityTypeConfiguration<EntityRole>
{
    public void Configure(EntityTypeBuilder<EntityRole> builder)
    {
        builder.ToTable("EntityRoles");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RoleType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.OwnershipPercent).HasPrecision(5, 2);
        builder.Property(e => e.BeneficialInterestPercent).HasPrecision(5, 2);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasOne(e => e.Entity)
            .WithMany(e => e.Roles)
            .HasForeignKey(e => e.EntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**Step 3: Add DbSets to ApplicationDbContext**

Add to `src/Api/Data/ApplicationDbContext.cs`:
```csharp
/// <summary>
/// Financial entities (Personal, Business, Trust) owned by users.
/// </summary>
public DbSet<Entity> Entities => Set<Entity>();

/// <summary>
/// Role assignments linking contacts to entities (owners, trustees, etc.).
/// </summary>
public DbSet<EntityRole> EntityRoles => Set<EntityRole>();
```

**Step 4: Verify build**

Run: `dotnet build src/Api/`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add src/Api/Data/Configurations/EntityConfiguration.cs \
  src/Api/Data/Configurations/EntityRoleConfiguration.cs \
  src/Api/Data/ApplicationDbContext.cs
git commit -m "feat(entities): add EF Core configurations and DbContext updates"
```

---

### Task 10: Create and Apply EF Migration

**Step 1: Generate migration**

Run: `cd src/Api && dotnet ef migrations add AddEntities`

**Step 2: Review generated migration**

Read the generated migration file. Verify it creates `Entities` and `EntityRoles` tables with correct columns, indexes, and JSON columns.

**Step 3: Apply migration locally (if local DB available)**

Run: `dotnet ef database update` (or verify with `dotnet build`)

**Step 4: Commit**

```bash
git add src/Api/Data/Migrations/
git commit -m "feat(entities): add EF migration for Entities and EntityRoles tables"
```

---

### Task 11: Implement Entity Service (Make Unit Tests Pass)

**Files:**
- Create: `src/Api/Services/EntityService/IEntityService.cs`
- Create: `src/Api/Services/EntityService/EntityService.cs`

**Step 1: Create IEntityService interface**

Follow `IAssetService.cs` pattern. Methods:
- `GetEntitiesAsync(Guid requestingUserId, Guid ownerUserId, EntityType? filterType)`
- `GetEntityByIdAsync(Guid requestingUserId, Guid entityId)`
- `CreateEntityAsync(Guid userId, CreateEntityRequest request)`
- `UpdateEntityAsync(Guid requestingUserId, Guid entityId, UpdateEntityRequest request)`
- `DeleteEntityAsync(Guid requestingUserId, Guid entityId)`
- `EnsurePersonalEntityAsync(Guid userId)` — called during user provisioning
- `AssignRoleAsync(Guid requestingUserId, Guid entityId, CreateEntityRoleRequest request)`
- `GetRolesAsync(Guid requestingUserId, Guid entityId)`
- `RemoveRoleAsync(Guid requestingUserId, Guid entityId, Guid roleId)`

**Step 2: Implement EntityService.cs**

Follow `AssetService.cs` pattern:
- Constructor injection: `ApplicationDbContext`, `IAuthorizationService`, `ILogger<EntityService>`
- Three-tier authorization on all operations
- Slug generation from name (lowercase, replace spaces with hyphens, strip special chars)
- Personal entity: cannot create duplicate, cannot delete, cannot rename
- Business rules: validate role types match entity type, ownership sum <= 100

**Step 3: Run unit tests**

Run: `dotnet test tests/Api.Tests/ --filter "FullyQualifiedName~EntityServiceTests" -v n`
Expected: All tests PASS

**Step 4: Commit**

```bash
git add src/Api/Services/EntityService/
git commit -m "feat(entities): implement EntityService with CRUD and role management"
```

---

### Task 12: Create Validators

**Files:**
- Create: `src/Api/Validators/CreateEntityRequestValidator.cs`
- Create: `src/Api/Validators/UpdateEntityRequestValidator.cs`
- Create: `src/Api/Validators/CreateEntityRoleRequestValidator.cs`

**Step 1: Implement validators**

Follow `CreateAssetRequestValidator.cs` pattern. Use error codes from `EntityErrorCodes`.

**Step 2: Verify build**

Run: `dotnet build src/Api/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Api/Validators/
git commit -m "feat(entities): add FluentValidation validators for Entity requests"
```

---

### Task 13: Create Azure Functions Endpoints

**Files:**
- Create: `src/Api/Functions/Entities/EntityFunctions.cs`
- Create: `src/Api/Functions/Entities/EntityRoleFunctions.cs`

**Step 1: Implement EntityFunctions.cs**

Follow `AssetFunctions.cs` pattern:
- `GET /api/entities` — list with optional `?type=` and `?ownerUserId=` filters
- `GET /api/entities/{id}` — get by ID
- `POST /api/entities` — create
- `PUT /api/entities/{id}` — update
- `DELETE /api/entities/{id}` — delete

**Step 2: Implement EntityRoleFunctions.cs**

- `GET /api/entities/{entityId}/roles` — list roles
- `POST /api/entities/{entityId}/roles` — assign role
- `DELETE /api/entities/{entityId}/roles/{roleId}` — remove role

**Step 3: Register in Program.cs**

Add to `src/Api/Program.cs`:
```csharp
builder.Services.AddScoped<IEntityService, EntityService>();
builder.Services.AddScoped<IValidator<CreateEntityRequest>, CreateEntityRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateEntityRequest>, UpdateEntityRequestValidator>();
builder.Services.AddScoped<IValidator<CreateEntityRoleRequest>, CreateEntityRoleRequestValidator>();
```

**Step 4: Verify build**

Run: `dotnet build src/Api/`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add src/Api/Functions/Entities/ src/Api/Program.cs
git commit -m "feat(entities): add Azure Functions HTTP endpoints for Entity CRUD and roles"
```

---

### Task 14: Add DataCategories Constant for Entities

**Files:**
- Modify: `src/Shared/Entities/DataCategories.cs`

**Step 1: Add entity data category**

```csharp
public const string Entities = "entities";
```

**Step 2: Commit**

```bash
git add src/Shared/Entities/DataCategories.cs
git commit -m "feat(entities): add Entities data category for authorization"
```

---

### Task 15: Auto-Provision Personal Entity on User Login

**Files:**
- Modify: `src/Api/Middleware/UserProfileProvisioningMiddleware.cs` (or `UserProfileService.cs`)

**Step 1: After provisioning UserProfile, call EntityService.EnsurePersonalEntityAsync**

When a user's profile is created or updated, ensure a Personal entity exists for that user. If none exists, create one with `Name = "Personal"` and `Slug = "personal"`.

**Step 2: Verify unit test for EnsurePersonalEntityAsync**

Run: `dotnet test tests/Api.Tests/ --filter "FullyQualifiedName~EnsurePersonalEntity" -v n`
Expected: PASS

**Step 3: Commit**

```bash
git add src/Api/Services/UserProfiles/ src/Api/Middleware/
git commit -m "feat(entities): auto-provision Personal entity on user login"
```

---

### Task 16: Write Integration Test Step Definitions

**Files:**
- Create: `tests/IntegrationTests/StepDefinitions/EntitySteps.cs`
- Create: `tests/IntegrationTests/StepDefinitions/EntityRoleSteps.cs`

**Step 1: Implement step definitions**

Follow `AssetSteps.cs` pattern — HTTP requests against the live Functions host, JSON parsing with `System.Text.Json`, `FluentAssertions` for assertions.

**Step 2: Run integration tests locally**

Run: `cd tests/IntegrationTests && dotnet test --filter "Tag=entities" -v n`
Expected: All scenarios PASS (requires `func start` running)

**Step 3: Commit**

```bash
git add tests/IntegrationTests/StepDefinitions/EntitySteps.cs \
  tests/IntegrationTests/StepDefinitions/EntityRoleSteps.cs
git commit -m "test(entities): add Reqnroll step definitions for Entity integration tests"
```

---

### Task 17: Run All Tests and Verify

**Step 1: Run unit tests**

Run: `dotnet test tests/Api.Tests/ -v n`
Expected: All 177+ existing tests + new entity tests PASS

**Step 2: Run integration tests**

Run: `cd tests/IntegrationTests && dotnet test -v n`
Expected: All 10+ existing scenarios + new entity scenarios PASS

**Step 3: Commit final Phase 1**

```bash
git commit -m "feat(entities): Phase 1 complete — Entity model, service, API, and tests"
```

---

## Phase 2: Entity Service + API Endpoints (Outline)

> **Detailed plan will be written when Phase 1 is complete.**

- Frontend TypeScript types for Entity/EntityRole (`src/Client/src/types/entities.ts`)
- Entity service hooks (`src/Client/src/services/entity-service.ts`)
- EntityProvider React context (`src/Client/src/contexts/EntityContext.tsx`)
- E2E step definitions for Entity UI (`tests/e2e/step-definitions/entity.steps.ts`)

---

## Phase 3: Asset EntityId Backfill + Scoped Queries (Outline)

> **Detailed plan will be written when Phase 2 is complete.**

- Add `EntityId` column to Asset table (nullable initially)
- EF migration
- Backfill script: create Personal entity per user, set EntityId on all existing assets
- Make EntityId non-nullable migration
- Update AssetService to accept entityId parameter
- Update AssetFunctions to route by entity
- Update all asset unit tests and integration tests
- BDD scenarios for entity-scoped asset queries

---

## Phase 4: Frontend Entity Navigation + Routing (Outline)

> **Detailed plan will be written when Phase 3 is complete.**

- Restructure DashboardLayout.tsx sidebar (Personal / Business / Trusts sections)
- Entity selector component for Business/Trust sections
- URL routing: `/personal/assets`, `/business/:slug/assets`, `/trust/:slug/assets`
- EntityProvider wraps entity sub-routes
- Update App.tsx with new route structure
- E2E BDD scenarios for navigation, entity creation flows, mobile hamburger menu

---

## Phase 5: Income Sources (Outline)

> **Detailed plan will be written when Phase 4 is complete.**

- BDD feature files (API integration + E2E)
- Backend: IncomeSource entity, DTOs, service, functions, validators, EF config, migration
- Frontend: types, service hooks, Income page, Income form sheet, breakdown chart
- i18n translation keys
- Integration with Insurance Calculator and Dashboard

---

## Phase 6: Bills & Recurring Expenses (Outline)

> **Detailed plan will be written when Phase 5 is complete.**

- BDD feature files (API integration + E2E)
- Backend: Bill entity, DTOs, service, functions, validators, EF config, migration
- Frontend: types, service hooks, Bills page, upcoming bills banner, Plaid import dialog
- i18n translation keys
- Integration with Debt Payoff Calculator and Insurance Calculator

---

## Phase 7: Documents & Storage Connections (Outline)

> **Detailed plan will be written when Phase 6 is complete.**

- BDD feature files (API integration + E2E)
- Backend: StorageConnection entity, Document entity, DTOs, services, functions
- Cloud provider OAuth integration (OneDrive Graph API, Google Drive API, Dropbox API)
- Frontend: storage connection setup, document upload/browse, per-entity storage
- i18n translation keys

---

## Phase 8: Insurance, Debt Payoff, Alerts, Statements, Transfers, Household (Outline)

> **Detailed plan will be written when Phase 7 is complete.**

This phase covers the remaining features from the design doc:
- Entity-scoped Insurance Coverage Manager
- Entity-scoped Debt Payoff Plans (persisted)
- Unified Alert System
- Auto-generated Financial Statements (P&L, Balance Sheet, Cash Flow)
- Inter-Entity Transfers
- Household Dashboard with cross-entity aggregation

Each sub-feature may be broken into its own phase if scope warrants it.

---

## Conventions Reference

| Layer | Pattern | Example |
|-------|---------|---------|
| Entity | `[MemoryPackable(GenerateType.VersionTolerant)]` partial class in `src/Shared/Entities/` | `Asset.cs` |
| DTO | `[MemoryPackable(SerializeLayout.Explicit)]` sealed partial record in `src/Shared/Contracts/{Feature}/` | `AssetDto.cs` |
| Error codes | Static class with `const string` in `src/Shared/Contracts/{Feature}/` | `AssetErrorCodes.cs` |
| EF Config | `IEntityTypeConfiguration<T>` in `src/Api/Data/Configurations/` | `AssetConfiguration.cs` |
| Service | Interface + implementation in `src/Api/Services/{Feature}/` | `IAssetService.cs` |
| Functions | One file per resource in `src/Api/Functions/{Feature}/` | `AssetFunctions.cs` |
| Validator | `AbstractValidator<T>` in `src/Api/Validators/` | `CreateAssetRequestValidator.cs` |
| Unit test | xUnit + FluentAssertions + InMemoryDb in `tests/Api.Tests/Services/` | `AssetServiceTests.cs` |
| BDD (API) | Reqnroll `.feature` + step defs in `tests/IntegrationTests/` | `Assets.feature` |
| BDD (E2E) | Cucumber.js + Playwright `.feature` + step defs in `tests/e2e/` | `Navigation.feature` |
| DI registration | `Program.cs` — `AddScoped<I,T>()` for services, `AddScoped<IValidator<T>,V>()` for validators | See `Program.cs` |
| Money | Entity: `decimal` (18,2) → DTO: `double` via `ToMoney()`/`FromMoney()` | `AssetService.cs` |
| Dates | Entity: `DateTimeOffset` → DTO: `DateTime` (UTC) | Convention |
| Enums | Stored as string via `.HasConversion<string>()` | `AssetConfiguration.cs` |
