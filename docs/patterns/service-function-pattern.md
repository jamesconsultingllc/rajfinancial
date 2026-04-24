# Canonical Function / Service Pattern

> **Status:** Draft (Phase 1 of [ADO Feature #632](https://dev.azure.com/jamesconsulting/_workitems/edit/632))
> **Scope:** Every new or refactored Azure Function endpoint and the service it delegates to.
> **Related ADRs:** 0001 (IDOR → 404), 0002 (activity naming — resolved by Phase 1c, see [`docs/plans/phase-1c-span-validation.md §7`](../plans/phase-1c-span-validation.md)), 0003 (layered exception recording).

This document is the single written standard for how an HTTP endpoint, its service, and the cross-cutting middleware cooperate in this codebase. It exists so reviewers can reject PRs that drift from the pattern by pointing at one document instead of inferring intent from whichever domain they read first. Three domains (Assets, Entities, ClientManagement) currently implement the same cross-cutting concerns three different ways; this pattern is the convergence target.

If something in this doc contradicts [`AGENT.md`](../../AGENT.md), **AGENT.md wins** — file a PR to update this doc.

---

## 1. Responsibility matrix

| Concern | Function (`src/Api/Functions/**`) | Service (`src/Api/Services/**`) | Middleware |
|---|---|---|---|
| Parse query string / route values / headers | ✅ | ❌ | ❌ |
| Parse + validate request body | ✅ via `GetValidatedBodyAsync` | ❌ | `ValidationMiddleware` (if wired) |
| Extract caller identity | ✅ via `context.GetUserIdAsGuid()` | ❌ (receives `Guid requestingUserId`) | `AuthenticationMiddleware` populates it |
| Authentication gate | ✅ via `[RequireAuthentication]` / `[RequireRole]` attribute | ❌ | `AuthenticationMiddleware`, `AuthorizationMiddleware` |
| Authorization (per-resource, owner-scoped) | ❌ — must not reference `IAuthorizationService` | ✅ via `AuthorizeReadAsync` / `AuthorizeWriteAsync` | — |
| Authorization (role gate) | ✅ via `[RequireRole(...)]` | ❌ (trusts the gate) | `AuthorizationMiddleware`, which owns its own `Authorization.CheckAccess` span |
| `DbContext` access | ❌ — enforced by `Functions_ShouldNotReferenceApplicationDbContext` architecture test | ✅ | — |
| Business logic / persistence | ❌ | ✅ | — |
| Return DTO (not entity) | ✅ (serialize) | ✅ (return) | — |
| Write HTTP response | ✅ via `context.CreateSerializedResponseAsync(...)` | ❌ — must not reference `HttpResponseData` / `HttpRequestData` / `FunctionContext` | — |
| Exception → HTTP mapping + JSON body | ❌ | ❌ | `ExceptionMiddleware` |
| Exception recording on own `Activity` | ✅ on the function-layer `<Domain>.<Op>` span it opens | ✅ on the service-layer `<Domain>.<Op>.Service` span | `ExceptionMiddleware` records on the outer Functions Invoke span via `invocationActivity?.RecordExceptionOutcome(ex)` |

The "❌" rows are enforced — or will be, after Phase 6 — by architecture tests in [`tests/Architecture.Tests/FunctionInvariantsTests.cs`](../../tests/Architecture.Tests/FunctionInvariantsTests.cs). Reviewers should point at those tests when rejecting a PR that crosses a line.

---

## 2. Authorization modes

Every endpoint falls into exactly one of two modes. **Do not mix them on the same endpoint.**

### Mode A — Owner-scoped

Applies when the resource has a `UserId` column and anyone authenticated can *attempt* to read or write it, but only the owner (or a user with an explicit grant) is actually allowed to.

- **Function:** `[RequireAuthentication]` only. No `[RequireRole]`.
- **Service:** calls `AuthorizeReadAsync(requestingUserId, resource.UserId, resourceId)` or `AuthorizeWriteAsync(...)` before returning / mutating the resource.
- **Denial:** the service throws `NotFoundException.<Domain>(id)` — **not** `ForbiddenException` — so a forbidden access is indistinguishable from a truly missing id (IDOR-safe, OWASP A01). See §5.
- **Examples:** Entities today; Assets is a convergence target — its `AuthorizeReadAsync` / `AuthorizeWriteAsync` still throw `ForbiddenException` on deny and will be migrated in Phase 5 (see §5).

### Mode B — Role-gated

Applies when the endpoint is inherently restricted to a role (Advisor / Administrator) and the service has no per-row ownership concept to check.

- **Function:** `[RequireRole(Roles.Advisor, Roles.Administrator)]` at the method level (or class level if every endpoint on the class shares the role set).
- **Service:** no authorization call. The gate is `AuthorizationMiddleware`.
- **Denial:** `AuthorizationMiddleware` throws `ForbiddenException` → 403. The service never sees the request.
- **Examples:** ClientManagement (admin-only assignments).

### Why mutually exclusive

If a role-gated endpoint *also* did per-owner authorization in the service, a denial on the owner check would throw `NotFoundException` (Mode A's rule), masking what is actually a role-scoping bug. Conversely, putting `[RequireRole]` on an owner-scoped endpoint would deny legitimate owners who lack the role. The two modes answer different questions — "is this user allowed on this endpoint at all" vs "is this user allowed on this specific row" — and the correct answer for one is never the correct answer for the other.

---

## 3. Observability layering

### 3.1 Activity names

Per Phase 1c (Option b2, see [`docs/plans/phase-1c-span-validation.md §7`](../plans/phase-1c-span-validation.md)):

- **Function-layer span:** `<Domain>.<Op>` — e.g. `Assets.GetById`, `Entities.CreateEntity`, `ClientManagement.AssignClient`.
- **Service-layer span:** `<Domain>.<Op>.Service` — e.g. `Assets.GetById.Service`, `Entities.CreateEntity.Service`, `ClientManagement.AssignClient.Service`.
- **Thin-handler exception:** when an endpoint's handler has no meaningful work of its own and simply delegates to the service (e.g. `Auth.GetMe`, `UserProfile.GetById`), a **single function-layer span** is acceptable. Do not manufacture a `<Domain>.<Op>.Service` child purely for symmetry. If the handler grows a non-trivial body later, promote it to the two-layer shape at that time.
- **Provisioning / internal side-effects:** activities emitted from a service in response to a middleware trigger (e.g. `UserProfileProvisioningMiddleware` calling `EnsureProfileExistsAsync` / `EnsurePersonalEntityAsync`) use the `<Domain>.<Op>.Service` form (`UserProfile.EnsureProfileExists.Service`, `Entities.EnsurePersonalEntity.Service`). The `.Service` suffix identifies these as service-owned, not function entry points.
- **No transport / protocol in the name.** Never add `.Http.`, `.Rest.`, `.Grpc.`, or similar prefixes. Put protocol detail in attributes (`http.request.method`, `http.route`, `faas.trigger`). The `Assets.Http.*` names used up through PR #92 were a staging form and have been removed.
- **No abbreviations in the domain segment.** `ClientManagement`, never `ClientMgmt`; `UserProfile`, never `UserProf`. The prior `ClientMgmt.*` activity names were likewise staging and have been renamed.

### 3.2 Tag keys

Dotted lowercase, namespaced by the domain or the OpenTelemetry semantic convention they follow — `user.id`, `asset.id`, `entity.id`, `asset.type`, `owner.user.id`. Constants live in the domain's `*Telemetry.cs` file (e.g. [`AssetsTelemetry.cs`](../../src/Api/Services/AssetService/AssetsTelemetry.cs)). **Never use magic strings** in call sites.

### 3.3 Post-authorization tagging (security)

Not every identifier follows the same rule:

- **Always safe pre-auth:** `user.id` for the authenticated caller (the middleware already validated it) and the route id of the resource being acted on (e.g. `asset.id` / `entity.id` from the request path). Tagging these early lets the attempted operation be correlated in traces even when it ends up denied.
- **Sensitive — tag only after authorization succeeds:** user-supplied query/body values that reveal *who* owns a resource (e.g. `owner.user.id`), and identifiers derived from storage or related records that were loaded before the authorization check. Tagging these before the authorization call leaks them into telemetry on denied requests, which lets a privileged telemetry reader enumerate resources the caller was not allowed to learn about.

```csharp
// ✅ correct — user.id and the route asset.id are safe pre-auth;
// owner.user.id (user-supplied) is sensitive and only tagged post-auth.
activity?.SetTag(AssetsTelemetry.TagUserId, requestingUserId);
activity?.SetTag(AssetsTelemetry.TagAssetId, assetId);

await AuthorizeReadAsync(requestingUserId, ownerUserId);

activity?.SetTag(AssetsTelemetry.TagOwnerUserId, ownerUserId); // post-auth
```

### 3.4 Metric tags

Prefer the `TagList` overload of `Counter<T>.Add` / `Histogram<T>.Record` over the `params KeyValuePair<string,object?>[]` overload — the `params` overload allocates an array per call. See [`AuthTelemetry.cs:61-66`](../../src/Api/Observability/AuthTelemetry.cs) and [`AssetsTelemetry.cs:68-88`](../../src/Api/Services/AssetService/AssetsTelemetry.cs).

---

## 4. Layered exception recording

Up to four different layers may record the same exception on four different `Activity` objects. **This is intentional — it is not a bug to dedupe.**

| Layer | Activity | Records via |
|---|---|---|
| Function | `<Domain>.<Op>` function-layer span (when opened — thin handlers may skip this) | `activity?.RecordExceptionOutcome(ex); throw;` |
| Service | `<Domain>.<Op>.Service` service-layer span | same pattern |
| `AuthorizationMiddleware` | `Authorization.CheckAccess` span | same pattern |
| `ExceptionMiddleware` | Functions Invoke span (outer; owned by the host) | `invocationActivity?.RecordExceptionOutcome(ex)` inside the middleware |

Each span belongs to a different logical scope. Deleting the service-level `try/catch` because "the middleware already records it" loses the exception on the service span, which breaks domain-level trace UIs and any KQL that groups by the service span's `operation.name`. Do **not** collapse these.

`ActivityExceptionExtensions.RecordExceptionOutcome` is also the single place where "handled client exception" classification (via `ExceptionClassification.IsHandledClientException`) decides whether to set `status=Error` on the span. Keep all recording routed through that helper — do not call `activity.SetStatus(...)` or `activity.RecordException(...)` directly in call sites.

---

## 5. IDOR: owner-scoped reads return 404 on deny

Per ADR 0001 (pending).

For Mode A endpoints, the service's `AuthorizeReadAsync` / `AuthorizeWriteAsync` throws `NotFoundException.<Domain>(id)` on denial — not `ForbiddenException`. The response body, status code, and error code are identical to the truly-missing-id case.

### Canonical service shape (Phase 5 target)

```csharp
public async Task<AssetDetailDto> GetAssetByIdAsync(Guid requestingUserId, Guid assetId)
{
    using var activity = AssetsTelemetry.ActivitySource.StartActivity(AssetsTelemetry.ActivityGetById);
    activity?.SetTag(AssetsTelemetry.TagUserId, requestingUserId);
    activity?.SetTag(AssetsTelemetry.TagAssetId, assetId);

    try
    {
        var asset = await db.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assetId);

        if (asset is null)
            throw NotFoundException.Asset(assetId);              // path 1

        await AuthorizeReadAsync(requestingUserId, asset.UserId, assetId);
                                                                 // path 2 — throws NotFoundException.Asset(assetId) on deny
        activity?.SetTag(AssetsTelemetry.TagOwnerUserId, asset.UserId);
        activity?.SetTag(AssetsTelemetry.TagAssetType, asset.Type.ToString());
        return asset.ToDetailDto();
    }
    catch (Exception ex)
    {
        activity?.RecordExceptionOutcome(ex);
        throw;
    }
}
```

### Accepted residual risk: timing side-channel

Path 1 is one DB query; path 2 is one DB query plus one authorization check. A patient attacker could distinguish them by latency. For personal-finance data this is the accepted residual risk.

### Upgrade path for high-sensitivity domains: authorize-in-query

A future high-sensitivity domain can eliminate the timing side-channel by filtering the authorization predicate into the query so both paths do the same work:

```csharp
var grants = await authorizationService.GetGrantedOwnersAsync(
    requestingUserId, DataCategories.Assets);

var resource = await db.Assets
    .AsNoTracking()
    .FirstOrDefaultAsync(a => a.Id == id
                           && (a.UserId == requestingUserId
                               || grants.Contains(a.UserId)));

if (resource is null)
    throw NotFoundException.Asset(id);

return resource.ToDetailDto();
```

This is **not** the pattern for today's code — the two-step shape is simpler, the timing gap is acceptable for this domain, and one-query authorization requires the authorization service to expose a grant-set projection we don't need for existing domains. ADR 0001 lists this as the upgrade path, not a mandate.

---

## 6. Templates

### 6.1 Function template (Mode A — owner-scoped read)

```csharp
[RequireAuthentication]
[Function("GetAssetById")]
public async Task<HttpResponseData> GetAssetById(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assets/{id}")]
    HttpRequestData req,
    FunctionContext context,
    string id)
{
    var userId = context.GetUserIdAsGuid()
                 ?? throw new UnauthorizedException("User ID not found");

    if (!Guid.TryParse(id, out var assetId))
        throw new ValidationException($"Invalid asset ID format: '{id}'");

    // Function-layer activity — per §3.1 (Phase 1c, Option b2). Opens
    // Assets.GetById; the service delegated to below opens Assets.GetById.Service.
    using var activity = AssetsTelemetry.ActivitySource.StartActivity(AssetsTelemetry.ActivityGetById);
    activity?.SetTag(AssetsTelemetry.TagUserId, userId);
    activity?.SetTag(AssetsTelemetry.TagAssetId, assetId);

    try
    {
        LogFetchingAssetById(assetId, userId);

        var asset = await assetService.GetAssetByIdAsync(userId, assetId);

        activity?.SetTag(AssetsTelemetry.TagAssetType, asset.Type.ToString());
        LogAssetByIdReturned(asset.Id, asset.Name, userId);

        return await context.CreateSerializedResponseAsync(
            req, HttpStatusCode.OK, asset, serializationFactory);
    }
    catch (Exception ex)
    {
        activity?.RecordExceptionOutcome(ex);
        throw;
    }
}
```

### 6.2 Function template (Mode B — role-gated)

```csharp
[RequireRole(Roles.Advisor, Roles.Administrator)]
[Function("AssignClient")]
public async Task<HttpResponseData> AssignClient(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "clients/{id}/assign")]
    HttpRequestData req,
    FunctionContext context,
    string id)
{
    // AuthenticationMiddleware + AuthorizationMiddleware have already
    // established the caller's identity and role membership. The service
    // does not re-check.
    var body = await context.GetValidatedBodyAsync<AssignClientRequest>();
    // ... delegate to IClientManagementService, no per-row auth call.
}
```

### 6.3 Service template (Mode A)

See §5 for the canonical `GetByIdAsync` shape. Writes follow the same shape with `AuthorizeWriteAsync` instead of `AuthorizeReadAsync` and the entity materialized for update before the authorization call.

---

## 7. What reviewers should reject

A PR drifts from this pattern if any of the following is true:

- A service references `HttpRequestData`, `HttpResponseData`, or `FunctionContext` (blocked by Phase 6 architecture tests).
- A function references `IAuthorizationService` for per-row auth (blocked by Phase 6 architecture test).
- A function references `ApplicationDbContext` directly (blocked today by `Functions_ShouldNotReferenceApplicationDbContext`).
- An owner-scoped Mode A service throws `ForbiddenException` on cross-user access instead of `NotFoundException.<Domain>(id)`.
- A call site tags a user-supplied id on an activity **before** the authorization call that would reject the request.
- A service uses the `params KeyValuePair<string,object?>[]` overload of a counter / histogram in a hot path when a `TagList` would do.
- A service omits its `try { … } catch (Exception ex) { activity?.RecordExceptionOutcome(ex); throw; }` wrapper around the `StartActivity` it opened.
- A function opens a service-layer (`<Domain>.<Op>.Service`) span, or a service opens a function-layer (`<Domain>.<Op>`) span — the two layers belong to distinct call sites and must not cross.
- A span name uses an abbreviation in the domain segment (`ClientMgmt.*`, `UserProf.*`).
- A span name encodes transport or protocol (`.Http.`, `.Rest.`, `.Grpc.`, etc.) rather than placing that information in attributes.
- A new per-domain business counter helper (`RecordCreated` / `RecordUpdated` / `RecordDeleted`) is added outside the Assets domain while [ADO #628](https://dev.azure.com/jamesconsulting/_workitems/edit/628) is still in flight.

A reviewer flagging any of the above should link to the specific section of this doc in their comment.

---

## 8. Cross-references

- [`AGENT.md`](../../AGENT.md) — global standards; §789–834 (Observability + Reserved Domain Names) is the companion to §3 here.
- [`src/Api/Middleware/Exception/ExceptionMiddleware.cs`](../../src/Api/Middleware/Exception/ExceptionMiddleware.cs) — exception → HTTP mapping and outer-span recording.
- [`src/Api/Middleware/Authorization/AuthorizationMiddleware.cs`](../../src/Api/Middleware/Authorization/AuthorizationMiddleware.cs) — role gate; owns the `Authorization.CheckAccess` span.
- [`src/Api/Middleware/AuthenticationMiddleware.cs`](../../src/Api/Middleware/AuthenticationMiddleware.cs) — populates `FunctionContext` with the caller's identity.
- [`src/Api/Middleware/Content/ContentNegotiationExtensions.cs`](../../src/Api/Middleware/Content/ContentNegotiationExtensions.cs) — `CreateSerializedResponseAsync`.
- [`src/Api/Middleware/ValidationExtensions.cs`](../../src/Api/Middleware/ValidationExtensions.cs) — `GetValidatedBodyAsync`.
- [`src/Api/Observability/ActivityExceptionExtensions.cs`](../../src/Api/Observability/ActivityExceptionExtensions.cs) — `RecordExceptionOutcome`.
- [`src/Api/Middleware/Exception/ExceptionClassification.cs`](../../src/Api/Middleware/Exception/ExceptionClassification.cs) — `IsHandledClientException`; the single source of truth for which exceptions map to 4xx.
- [`tests/Architecture.Tests/FunctionInvariantsTests.cs`](../../tests/Architecture.Tests/FunctionInvariantsTests.cs) — the architecture tests that enforce this doc.
- Feature plan: ADO #632; phase breakdown lives on the Feature's Description field.
