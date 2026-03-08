# 03 — Authorization & Data Access

> RBAC roles, data sharing with DataAccessGrant, CanAccessUserDataAsync pattern, audit logging.

**ADO Tracking:** [Epic #433 — 03 - Authorization & Data Access](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/433)

| # | Feature | State |
|---|---------|-------|
| [470](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/470) | Authorization Middleware & Resource Access Control | Done |
| [452](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/452) | Security Hardening | New |
| [520](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/520) | DataAccessGrant System | New |
| [521](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/521) | Audit Logging | New |

---

## Overview

Authorization in RAJ Financial is enforced at two levels:
1. **Role-Based Access Control (RBAC)** — Entra App Roles determine what actions a user can perform
2. **Data-Level Access Control** — DataAccessGrant determines whose data a user can see

The UI **hides** unauthorized features entirely (never disabled). The API **denies** unauthorized requests with `403 Forbidden`.

---

## App Roles

Roles are defined in the Entra API app registration manifest and assigned to users.

### Role Definitions

| Role | Value | Description |
|------|-------|-------------|
| **Administrator** | `admin` | Full system access, user management, configuration |
| **Advisor** | `advisor` | View/manage client data (via DataAccessGrant), reports, planning tools |
| **Client** | `client` | Manage own financial data, view dashboards |
| **Viewer** | `viewer` | Read-only access to shared data |

### Combined Roles

Users may hold multiple roles:
- **AdminAdvisor** — Administrator + Advisor capabilities
- **AdminClient** — Administrator + Client capabilities (e.g., admin who also tracks own finances)

### App Manifest JSON

```json
{
  "appRoles": [
    {
      "allowedMemberTypes": ["User"],
      "displayName": "Administrator",
      "description": "Full system access — user management, configuration, all data.",
      "id": "a1b2c3d4-1234-5678-9abc-000000000001",
      "isEnabled": true,
      "value": "admin"
    },
    {
      "allowedMemberTypes": ["User"],
      "displayName": "Advisor",
      "description": "View and manage client data via grants. Create reports and plans.",
      "id": "a1b2c3d4-1234-5678-9abc-000000000002",
      "isEnabled": true,
      "value": "advisor"
    },
    {
      "allowedMemberTypes": ["User"],
      "displayName": "Client",
      "description": "Manage own financial data, assets, contacts, and dashboards.",
      "id": "a1b2c3d4-1234-5678-9abc-000000000003",
      "isEnabled": true,
      "value": "client"
    },
    {
      "allowedMemberTypes": ["User"],
      "displayName": "Viewer",
      "description": "Read-only access to shared data. Cannot modify any records.",
      "id": "a1b2c3d4-1234-5678-9abc-000000000004",
      "isEnabled": true,
      "value": "viewer"
    }
  ]
}
```

---

## Data Access Control

### DataAccessGrant Entity

Data sharing between users is governed by `DataAccessGrant`. An **owner** grants a **grantee** access to specific data categories with a chosen access level.

```csharp
namespace RAJFinancial.Core.Entities;

/// <summary>
/// Represents a data sharing grant from one user (owner) to another (grantee).
/// Supports scoped access by data category and access level.
/// </summary>
public class DataAccessGrant : IHasTimestamps
{
    public Guid Id { get; set; }

    /// <summary>The user who owns the data (grantor).</summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>The user who receives access (grantee).</summary>
    public string GranteeId { get; set; } = string.Empty;

    /// <summary>Level of access granted.</summary>
    public AccessType AccessType { get; set; }

    /// <summary>What data categories are shared.</summary>
    public DataCategory DataCategories { get; set; }

    /// <summary>When the grant expires (null = permanent until revoked).</summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>Level of data access granted.</summary>
public enum AccessType
{
    /// <summary>View access to shared categories.</summary>
    Read = 0,

    /// <summary>View + limited edit (e.g., add notes).</summary>
    Limited = 1,

    /// <summary>Full CRUD on shared categories.</summary>
    Full = 2,

    /// <summary>Full access to ALL data categories.</summary>
    Owner = 3
}

/// <summary>
/// Data categories that can be selectively shared.
/// Flags enum — combine with bitwise OR.
/// </summary>
[Flags]
public enum DataCategory
{
    None = 0,
    Assets = 1,
    Accounts = 2,
    Contacts = 4,
    Documents = 8,
    Insights = 16,
    Planning = 32,
    All = Assets | Accounts | Contacts | Documents | Insights | Planning
}
```

### Access Check Pattern

Every data-accessing endpoint calls `CanAccessUserDataAsync` to verify the requesting user has access to the target user's data:

```csharp
namespace RAJFinancial.Api.Services;

/// <summary>
/// Central authorization check for data access.
/// Used by all endpoints that read or modify user-scoped data.
/// </summary>
public class DataAccessService : IDataAccessService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataAccessService> _logger;

    /// <summary>
    /// Determines whether <paramref name="requestingUserId"/> can access data
    /// owned by <paramref name="targetUserId"/> for the given category and level.
    /// </summary>
    /// <returns>True if access is allowed, false otherwise.</returns>
    public async Task<bool> CanAccessUserDataAsync(
        string requestingUserId,
        string targetUserId,
        DataCategory requiredCategory,
        AccessType minimumAccess,
        CancellationToken ct = default)
    {
        // Owner always has access to own data
        if (requestingUserId == targetUserId)
            return true;

        var grant = await _context.DataAccessGrants
            .Where(g => g.OwnerId == targetUserId
                     && g.GranteeId == requestingUserId
                     && g.IsActive
                     && (g.ExpiresAt == null || g.ExpiresAt > DateTime.UtcNow))
            .FirstOrDefaultAsync(ct);

        if (grant is null)
        {
            _logger.LogWarning(
                "Access denied: {RequestingUser} attempted to access {TargetUser} data ({Category})",
                requestingUserId, targetUserId, requiredCategory);
            return false;
        }

        // Check category access
        if (!grant.DataCategories.HasFlag(requiredCategory))
            return false;

        // Check access level
        return grant.AccessType >= minimumAccess;
    }
}
```

### Usage in API Functions

```csharp
[Function("GetAssets")]
[Authorize]
public async Task<IActionResult> GetAssets(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userId}/assets")] HttpRequestData req,
    string userId)
{
    var requestingUserId = GetCurrentUserId(req);

    if (!await _dataAccess.CanAccessUserDataAsync(
        requestingUserId, userId, DataCategory.Assets, AccessType.Read))
    {
        return new ForbidResult();
    }

    var assets = await _assetService.GetByUserAsync(userId);
    return new OkObjectResult(assets);
}
```

---

## Sharing Flow

The data sharing lifecycle follows four stages:

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  INVITE  │────▶│  ACCEPT  │────▶│  ACCESS  │────▶│  REVOKE  │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
```

### Sharing API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/sharing/invite` | Owner sends sharing invitation |
| `GET` | `/api/sharing/invitations` | List pending invitations for current user |
| `PUT` | `/api/sharing/invitations/{id}/accept` | Accept an invitation |
| `PUT` | `/api/sharing/invitations/{id}/decline` | Decline an invitation |
| `GET` | `/api/sharing/grants` | List active grants (as owner or grantee) |
| `DELETE` | `/api/sharing/grants/{id}` | Revoke a grant |

### Sharing DTOs

```csharp
/// <summary>Request to send a data sharing invitation.</summary>
public class ShareInvitationRequest
{
    public required string InviteeEmail { get; set; }
    public AccessType AccessType { get; set; }
    public DataCategory DataCategories { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>Active sharing grant visible to owner and grantee.</summary>
public class DataAccessGrantDto
{
    public Guid Id { get; set; }
    public string OwnerDisplayName { get; set; } = string.Empty;
    public string GranteeDisplayName { get; set; } = string.Empty;
    public AccessType AccessType { get; set; }
    public DataCategory DataCategories { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime GrantedAt { get; set; }
}
```

> Data sharing is a **Premium** feature. Free-tier users cannot send or accept sharing invitations. See [01-platform-infrastructure.md](01-platform-infrastructure.md) for tier limits.

---

## Frontend Authorization

### Permission Hook

```tsx
// src/hooks/use-permissions.ts
import { useMsal } from '@azure/msal-react';

/**
 * Returns the current user's roles from MSAL token claims.
 * Provides permission-checking helpers for conditional rendering.
 */
export function usePermissions() {
  const { accounts } = useMsal();
  const account = accounts[0];
  const roles: string[] = account?.idTokenClaims?.roles ?? [];

  return {
    roles,
    isAdmin: roles.includes('admin'),
    isAdvisor: roles.includes('advisor'),
    isClient: roles.includes('client'),
    isViewer: roles.includes('viewer'),
    hasRole: (role: string) => roles.includes(role),
    hasAnyRole: (...required: string[]) =>
      required.some((r) => roles.includes(r)),
  };
}
```

### Conditional Rendering

```tsx
// ✅ Hide unauthorized features entirely — never disable
function AdminNav() {
  const { isAdmin } = usePermissions();

  return (
    <nav aria-label={t('nav.main')}>
      <NavLink to="/dashboard">{t('nav.dashboard')}</NavLink>
      <NavLink to="/assets">{t('nav.assets')}</NavLink>
      {isAdmin && <NavLink to="/admin">{t('nav.admin')}</NavLink>}
    </nav>
  );
}
```

---

## Audit Logging

All authorization-relevant events are logged for security compliance:

```csharp
/// <summary>
/// Records security-relevant actions for audit trail.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Audited Events

| Event | Level | Details Captured |
|-------|-------|-----------------|
| Login Success | Info | UserId, IP, UserAgent |
| Login Failure | Warning | Email attempted, IP |
| Grant Created | Info | OwnerId, GranteeId, AccessType, Categories |
| Grant Revoked | Info | GrantId, RevokedBy |
| Data Access Denied | Warning | RequestingUser, TargetUser, Category |
| Asset Created/Updated/Deleted | Info | AssetId, UserId, ChangeType |
| Role Changed | Warning | UserId, OldRole, NewRole, ChangedBy |
| Sensitive Field Accessed | Info | UserId, Field (e.g., AccountNumber) |

---

## EF Core Tenant Isolation

Global query filters enforce data scoping at the database level:

```csharp
// ApplicationDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Ensure users only see their own assets
    modelBuilder.Entity<Asset>()
        .HasQueryFilter(a => a.UserId == CurrentUserId);

    // Ensure users only see their own contacts
    modelBuilder.Entity<Contact>()
        .HasQueryFilter(c => c.UserId == CurrentUserId);

    // Ensure users only see their own linked accounts
    modelBuilder.Entity<LinkedAccount>()
        .HasQueryFilter(la => la.UserId == CurrentUserId);

    // Sharing grants: visible to owner or grantee
    modelBuilder.Entity<DataAccessGrant>()
        .HasQueryFilter(g =>
            g.OwnerId == CurrentUserId || g.GranteeId == CurrentUserId);
}
```

> `CurrentUserId` is set from the JWT `oid` claim at the start of each request. For shared-data queries, the filter is temporarily bypassed using `IgnoreQueryFilters()` after `CanAccessUserDataAsync` verification.

---

## Cross-References

- Identity & MFA: [02-identity-authentication.md](02-identity-authentication.md)
- Tier limits (sharing = premium): [01-platform-infrastructure.md](01-platform-infrastructure.md)
- Asset access control: [05-assets-portfolio.md](05-assets-portfolio.md)
- Contact access control: [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md)

---

*Last Updated: February 2026*
