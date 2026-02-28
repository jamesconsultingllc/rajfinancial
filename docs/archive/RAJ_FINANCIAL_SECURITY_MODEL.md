# RAJ Financial - Security & Authorization Model

## Overview

RAJ Financial implements a **user-centric authorization model** where:
- **Roles** determine **feature access** (what UI/tools you can use)
- **Data access** is **explicitly granted by data owners** (not by roles)

This ensures that even administrators cannot see user financial data unless the user explicitly shares it.

---

## Core Principle

> **Data access is NEVER implicit - it must be explicitly granted by the data owner.**

An Administrator can manage the system, but cannot see your portfolio. A Financial Advisor can only see clients who have chosen to share their data with them.

---

## Role Definitions

### System Roles (Feature Access)

| Role | Description | Features | Data Access |
|------|-------------|----------|-------------|
| `Administrator` | System configuration and user management | Admin panel, audit logs, system settings | ? No user data |
| `Advisor` | Financial advisor tools | Client management, portfolio analysis tools | Only clients who grant access |
| `Client` | Primary account holder | Full portfolio management, sharing controls | Own data + controls all sharing |
| `Viewer` | Read-only access to shared data | View shared accounts/documents | Only what's shared with them |

### Combined Roles

| Role | Description | Includes |
|------|-------------|----------|
| `AdminAdvisor` | System admin + financial advisor | Administrator + Advisor features |
| `AdminClient` | System admin + personal account | Administrator + Client features |

---

## Authorization Policies

```csharp
// SYSTEM ADMINISTRATION (no user data access)
options.AddPolicy("RequireAdministrator", policy =>
    policy.RequireRole("Administrator", "AdminAdvisor", "AdminClient"));

// ADVISOR FEATURES (data access granted BY clients)
options.AddPolicy("RequireAdvisor", policy =>
    policy.RequireRole("Advisor", "AdminAdvisor"));

// CLIENT FEATURES (own data, controls sharing)
options.AddPolicy("RequireClient", policy =>
    policy.RequireRole("Client", "Advisor", "AdminAdvisor", "AdminClient"));

// VIEWER FEATURES (read-only shared data)
options.AddPolicy("RequireViewer", policy =>
    policy.RequireRole("Viewer", "Client", "Advisor", "AdminAdvisor", "AdminClient"));

// ANY AUTHENTICATED USER
options.AddPolicy("RequireAuthenticated", policy =>
    policy.RequireAuthenticatedUser());
```

---

## Data Access Model

### Access Types

| Type | Code | Description | Use Case |
|------|------|-------------|----------|
| **Owner** | `Owner` | Full control - implicit for own data | Data owner |
| **Full** | `Full` | Read + write, cannot delete or share | Spouse, trusted family |
| **Read** | `Read` | View only, no modifications | Professional review |
| **Limited** | `Limited` | Specific categories only | CPA (financial only) |

### Data Categories (for Limited Access)

| Category | Code | Includes |
|----------|------|----------|
| Accounts | `accounts` | Linked bank accounts, balances, transactions |
| Assets | `assets` | Manual assets (property, vehicles, valuables) |
| Liabilities | `liabilities` | Debts, loans, mortgages |
| Beneficiaries | `beneficiaries` | Beneficiary assignments, estate planning |
| Documents | `documents` | Uploaded strategy documents |
| Analysis | `analysis` | AI insights, financial analysis |

---

## Data Sharing Flow

```
???????????????????????????????????????????????????????????????????????????
? 1. INVITE: Client grants access via email                              ?
?    - Creates DataAccessGrant with Status = Pending                      ?
?    - Sends email invitation with secure token                           ?
???????????????????????????????????????????????????????????????????????????
                                    ?
                                    ?
???????????????????????????????????????????????????????????????????????????
? 2. ACCEPT: Recipient clicks link, logs in (or registers)               ?
?    - GranteeUserId is set                                               ?
?    - Status = Active, AcceptedAt = now                                  ?
???????????????????????????????????????????????????????????????????????????
                                    ?
                                    ?
???????????????????????????????????????????????????????????????????????????
? 3. ACCESS: Recipient can now see shared data                           ?
?    - API checks DataAccessGrant before returning data                   ?
?    - UI shows "Viewing as [Client Name]" indicator                      ?
???????????????????????????????????????????????????????????????????????????
                                    ?
                                    ?
???????????????????????????????????????????????????????????????????????????
? 4. REVOKE: Client can revoke access instantly                          ?
?    - Status = Revoked, RevokedAt = now                                  ?
?    - Recipient immediately loses access                                 ?
???????????????????????????????????????????????????????????????????????????
```

---

## DataAccessGrant Entity

```csharp
[MemoryPackable]
public partial class DataAccessGrant
{
    public Guid Id { get; set; }
    public Guid GrantorUserId { get; set; }      // Data owner
    public Guid? GranteeUserId { get; set; }     // Recipient (null until accepted)
    public string GranteeEmail { get; set; }     // Invitation email
    public AccessType AccessType { get; set; }   // Owner, Full, Read, Limited
    public List<string> Categories { get; set; } // For Limited access
    public string? RelationshipLabel { get; set; } // "Spouse", "CPA", etc.
    public string? InvitationToken { get; set; }
    public DateTimeOffset? InvitationExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public GrantStatus Status { get; set; }      // Pending, Active, Expired, Revoked
}
```

---

## API Authorization Pattern

### Check Data Access

```csharp
public async Task<DataAccessResult> CanAccessUserDataAsync(
    Guid currentUserId, 
    Guid targetUserId, 
    string category = "all",
    AccessType minimumAccess = AccessType.Read)
{
    // Owner always has access
    if (currentUserId == targetUserId)
        return DataAccessResult.Allowed(AccessType.Owner);
    
    // Check for active grant
    var grant = await _db.DataAccessGrants
        .Where(g => g.GrantorUserId == targetUserId 
                 && g.GranteeUserId == currentUserId
                 && g.Status == GrantStatus.Active
                 && (g.ExpiresAt == null || g.ExpiresAt > DateTimeOffset.UtcNow))
        .FirstOrDefaultAsync();
    
    if (grant == null)
        return DataAccessResult.Denied("No access grant found");
    
    // Check access level
    if (grant.AccessType > minimumAccess) // Higher enum = less access
        return DataAccessResult.Denied("Insufficient access level");
    
    // Check category for limited access
    if (grant.AccessType == AccessType.Limited 
        && category != "all" 
        && !grant.Categories.Contains(category))
        return DataAccessResult.Denied($"No access to {category}");
    
    return DataAccessResult.Allowed(grant.AccessType);
}
```

### Applying to Controllers

```csharp
[HttpGet("users/{userId}/portfolio")]
[Authorize(Policy = "RequireViewer")]
public async Task<IActionResult> GetPortfolio(Guid userId)
{
    var currentUserId = User.GetUserId();
    var accessResult = await _accessService.CanAccessUserDataAsync(
        currentUserId, userId, DataCategories.Accounts);
    
    if (!accessResult.IsAllowed)
        return Forbid();
    
    var portfolio = await _portfolioService.GetAsync(userId);
    
    // Apply read-only projection if needed
    if (accessResult.AccessType == AccessType.Read || 
        accessResult.AccessType == AccessType.Limited)
    {
        return Ok(portfolio.ToReadOnlyView());
    }
    
    return Ok(portfolio);
}
```

---

## API Endpoints

### Sharing Management

```
POST   /api/access/grants              ? CreateGrant (invite someone)
GET    /api/access/grants              ? GetMyGrants (who I've shared with)
GET    /api/access/grants/received     ? GetReceivedGrants (who shared with me)
POST   /api/access/grants/{id}/accept  ? AcceptGrant (accept invitation)
PATCH  /api/access/grants/{id}         ? UpdateGrant (change access level)
DELETE /api/access/grants/{id}         ? RevokeGrant (revoke access)
```

---

## UI Requirements

### Sharing Controls (Client View)
- List of people with access to your data
- Add new share (email, access level, categories, expiration)
- Revoke access instantly
- View access activity log

### Shared Data View (Advisor/Viewer)
- Account switcher dropdown
- Clear indicator: "Viewing as [Client Name]"
- Visual distinction (different header color, badge)
- Respect read-only mode (disable edit buttons)

### Access Denied Handling
- Clear message explaining why access was denied
- Link to request access if appropriate
- No information leakage about whether data exists

---

## Audit Logging

All data access events must be logged:

| Event | Data Logged |
|-------|-------------|
| Grant Created | Grantor, grantee email, access type, categories |
| Grant Accepted | Grantee, timestamp |
| Grant Revoked | Revoker, reason |
| Data Accessed | Who, whose data, what category, timestamp |
| Access Denied | Who, whose data, reason |

---

## Security Considerations

1. **Invitation tokens** expire after 7 days
2. **Rate limiting** on grant creation (prevent spam)
3. **Email verification** before grant can be accepted
4. **No information leakage** - don't reveal if user exists
5. **Audit trail** for all access events
6. **Immediate revocation** - no caching delays
7. **Clear UI indicators** when viewing shared data

---

## Azure App Roles (Entra ID)

Add to App Registration manifest:

```json
"appRoles": [
    {
        "id": "00000000-0000-0000-0000-000000000001",
        "allowedMemberTypes": ["User"],
        "displayName": "Administrator",
        "value": "Administrator",
        "description": "System administration (no user data access)"
    },
    {
        "id": "00000000-0000-0000-0000-000000000002",
        "allowedMemberTypes": ["User"],
        "displayName": "Advisor",
        "value": "Advisor",
        "description": "Financial advisor features"
    },
    {
        "id": "00000000-0000-0000-0000-000000000003",
        "allowedMemberTypes": ["User"],
        "displayName": "Client",
        "value": "Client",
        "description": "Client account holder"
    },
    {
        "id": "00000000-0000-0000-0000-000000000004",
        "allowedMemberTypes": ["User"],
        "displayName": "Viewer",
        "value": "Viewer",
        "description": "Read-only access to shared data"
    },
    {
        "id": "00000000-0000-0000-0000-000000000005",
        "allowedMemberTypes": ["User"],
        "displayName": "AdminAdvisor",
        "value": "AdminAdvisor",
        "description": "Administrator + Advisor"
    },
    {
        "id": "00000000-0000-0000-0000-000000000006",
        "allowedMemberTypes": ["User"],
        "displayName": "AdminClient",
        "value": "AdminClient",
        "description": "Administrator + Client"
    }
]
```

---

*Last Updated: December 2024*
