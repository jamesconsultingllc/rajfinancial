# ADR 0001 — Owner-scoped reads return 404 on deny (IDOR)

- **Status:** Accepted
- **Date:** 2026-04-24
- **Deciders:** Rudy James Jr
- **Related:**
  - Pattern: [`docs/patterns/service-function-pattern.md §5`](../patterns/service-function-pattern.md)
  - Work item: [AB#637](https://dev.azure.com/jamesconsulting/_workitems/edit/637) (Phase 1a of Feature [#632](https://dev.azure.com/jamesconsulting/_workitems/edit/632))
  - Threat model: OWASP Top 10 — [A01:2021 Broken Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/) (IDOR)

## Context

"Mode A" endpoints in this codebase (per pattern §2) operate on resources that carry a `UserId` owner column — assets, entities, and similar personal-finance records. Any authenticated user may *attempt* to read or write any id; authorization happens in the service via `AuthorizeReadAsync` / `AuthorizeWriteAsync`.

The classical shape returns `403 Forbidden` when the caller is authenticated but not allowed to touch the resource, and `404 Not Found` when the resource does not exist. That shape is informative — and therefore dangerous. An attacker who can tell the two apart can enumerate which ids exist by iterating over guids and noting which return 403 vs 404; once they have a list of real ids, they have a shortlist of owners to target through other channels (social engineering, credential stuffing, secondary vulnerabilities). This is the classic Insecure Direct Object Reference (IDOR) enumeration pattern.

The data in this system is personal financial data. The existence of a specific asset or entity id, tied implicitly to *some* user, is itself sensitive — the identifier doesn't need to disclose field values to be useful to an attacker.

## Decision

For Mode A endpoints, authorization denials and truly-missing ids return the **same** response:

- HTTP status **`404 Not Found`**.
- JSON body produced by `NotFoundException.<Domain>(id)` (e.g. `NotFoundException.Asset(id)`).
- No diagnostic tail that distinguishes the two (no "you are not the owner" language, no error code variant).

Services implement this by throwing `NotFoundException.<Domain>(id)` from `AuthorizeReadAsync` / `AuthorizeWriteAsync` on deny — **not** `ForbiddenException`. The response to "asset exists but you're not the owner" and "asset does not exist" is byte-identical at the HTTP boundary.

This rule applies **only to Mode A** (owner-scoped) endpoints. Mode B (role-gated admin endpoints) still denies with `403 Forbidden` through `AuthorizationMiddleware`, because the endpoint itself is not accessible to the role — there is no per-row ownership to hide, and role-based denial does not enable id enumeration.

## Alternatives considered

### A. Return `403 Forbidden` on deny (the conventional shape)

Rejected. Distinguishing "exists but denied" from "does not exist" is the enumeration primitive we are specifically trying to remove. Personal financial data is the wrong domain in which to preserve this conventional but leaky shape.

### B. Authorize-in-query (one database round-trip, no timing gap)

Fold the authorization predicate into the EF query so the database returns no row for both "does not exist" and "exists but not granted", and the service throws `NotFoundException.<Domain>(id)` only once:

```csharp
var grants = await authorizationService.GetGrantedOwnersAsync(requestingUserId, DataCategories.Assets);
var asset = await db.Assets
    .AsNoTracking()
    .FirstOrDefaultAsync(a => a.Id == id
                           && (a.UserId == requestingUserId || grants.Contains(a.UserId)));
if (asset is null) throw NotFoundException.Asset(id);
```

This eliminates the timing side-channel that the two-step shape admits (see *Consequences* below). It is **not** the chosen pattern for today's code for two reasons:

1. It requires the authorization service to expose a grant-set projection (`GetGrantedOwnersAsync` returning the set of owner user ids granted to the caller for a data category). We don't have that projection today, and building it for every domain is a significantly larger change than the two-step shape.
2. The residual timing difference (one query vs one query plus a lightweight grant lookup) is an acceptable risk for personal-finance data of this sensitivity.

This ADR records B as the **upgrade path** for high-sensitivity domains we may add later (e.g. a tax-filing or investment-transaction domain). It is a known, documented move, not a rewrite.

### C. Return a generic `404` with no body

Rejected. The existing `NotFoundException.<Domain>(id)` payload is already a stable machine-readable shape used by the client, and collapsing it to a truly empty 404 would break existing client error handling for legitimate missing-id cases. The identical-response property we need is satisfied without going to a stripped-down body.

## Consequences

### Gained

- **IDOR enumeration neutralized** at the HTTP boundary for Mode A endpoints. An attacker iterating guids sees a uniform 404 stream regardless of existence.
- **One error shape** for the Mode A miss case — clients don't branch on "maybe you're unauthorized, maybe it's gone".
- **Clean invariant for architecture tests** — Mode A services must throw `NotFoundException.<Domain>` on deny, enforceable when Phase 6 tests are added.

### Paid

- **Timing side-channel** between path 1 (single DB query returns null) and path 2 (DB query returns a row, then the authorization check runs). A patient, instrumented attacker on a low-latency path could in principle distinguish the two. Accepted residual risk for this data class; alternative B is the fix if we ever need it.
- **Logs still carry the truth.** Server-side structured logs and activity tags record the authenticated user id, the resource id, and the authorization result; an operator with log access can still see that a 404 on the wire was really a denial. This is intentional — the point is to hide enumeration from *unauthenticated network observers and authenticated non-owners*, not from operators.
- **Mode A and Mode B deny differently** (404 vs 403), which is unusual. The pattern doc §2 calls out the mutual-exclusivity rule so the two paths cannot collide on the same endpoint.

## References

- [`docs/patterns/service-function-pattern.md §5`](../patterns/service-function-pattern.md) — canonical service shape and the authorize-in-query upgrade path.
- [`src/Api/Middleware/Exception/NotFoundException.cs`](../../src/Api/Middleware/Exception/NotFoundException.cs) — domain constructors.
- OWASP — [A01:2021 Broken Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/).
