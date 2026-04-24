# ADR 0002 — Activity naming convention (`<Domain>.<Op>` / `<Domain>.<Op>.Service`)

- **Status:** Accepted
- **Date:** 2026-04-24
- **Deciders:** Rudy James Jr
- **Related:**
  - Pattern: [`docs/patterns/service-function-pattern.md §3.1`](../patterns/service-function-pattern.md)
  - Evidence: [`docs/plans/phase-1c-span-validation.md §6, §7`](../plans/phase-1c-span-validation.md) (12 captured traces, Option b2 decision)
  - Work items: Phase 1c — [AB#633](https://dev.azure.com/jamesconsulting/_workitems/edit/633); Phase 1a ADRs — [AB#637](https://dev.azure.com/jamesconsulting/_workitems/edit/637); Feature [#632](https://dev.azure.com/jamesconsulting/_workitems/edit/632)
  - Specs: [OpenTelemetry HTTP spans](https://opentelemetry.io/docs/specs/semconv/http/http-spans/), [OpenTelemetry FaaS spans](https://opentelemetry.io/docs/specs/semconv/faas/faas-spans/)

## Context

Before Phase 1c, three domains in this codebase named their activities three different ways:

| Domain | Function-layer span | Service-layer span |
|---|---|---|
| Entities | `Entities.CreateEntity` | `Entities.CreateEntity.Service` |
| ClientManagement | `ClientMgmt.AssignClient` | `ClientMgmt.AssignClient.Service` (plus odd outliers like `ClientMgmt.GetClientAssignments`) |
| Assets | `Assets.Http.Create` | `Assets.Create` |

A full inventory and a set of 12 end-to-end trace captures are recorded in [`phase-1c-span-validation.md §6`](../plans/phase-1c-span-validation.md). The captures confirmed that this inconsistency made cross-domain trace queries awkward (different name shapes per domain) and made the pattern doc impossible to state without three special cases.

There was also a secondary question — whether per-domain HTTP-layer spans should exist at all, or whether they could be dropped in favor of tagging the Functions-host-emitted invocation span (`faas.name` / `faas.trigger`) via a telemetry-enrichment middleware ("Option (a)"). The Phase 1c captures showed that the host-emitted invocation span is **not visible** to the worker's Console exporter under Azure Functions Core Tools; its presence, attributes, and parent/child relationship to worker spans can only be validated against a deployed Azure Functions environment. Option (a) therefore could not be committed to on local evidence alone.

## Decision

Use the following two-layer naming rule for every domain:

- **Function-layer span:** `<Domain>.<Op>` — e.g. `Assets.GetById`, `Entities.CreateEntity`, `ClientManagement.AssignClient`.
- **Service-layer span:** `<Domain>.<Op>.Service` — e.g. `Assets.GetById.Service`, `Entities.CreateEntity.Service`, `ClientManagement.AssignClient.Service`.
- **Thin-handler exception:** if a handler has no meaningful work of its own and is a pure delegation to the service (e.g. `Auth.GetMe`, `UserProfile.GetById`, `UserProfile.UpdateProfile`), a **single function-layer span** is allowed. Do not manufacture a `<Domain>.<Op>.Service` child purely for symmetry.
- **Provisioning / internal side-effects:** activities emitted from a service in response to a middleware trigger use the `.Service` form (`UserProfile.EnsureProfileExists.Service`, `Entities.EnsurePersonalEntity.Service`).
- **No transport or protocol in the name.** Never use `.Http.`, `.Rest.`, `.Grpc.`, or similar. Put protocol context in attributes (`http.request.method`, `http.route`, `faas.trigger`).
- **No abbreviations in the domain segment.** `ClientManagement`, never `ClientMgmt`. `UserProfile`, never `UserProf`.

This is "Option (b2)" in the Phase 1c plan — we keep per-domain HTTP-entry spans, we keep the service layer distinct with a `.Service` suffix, and we leave any host-span-based alternative for a separate, Azure-validated decision.

The Phase 1c PR applied the rename (`Assets.Http.*` removed, `ClientMgmt.*` → `ClientManagement.*`, `.Service` added where missing). This ADR ratifies that naming as normative going forward.

## Alternatives considered

### Option (a) — Drop per-domain HTTP spans; enrich the host invocation span

Rejected (for now). The host-emitted invocation span is not observable from the worker-side Console exporter, so we can't confirm on local evidence that `faas.name` / `faas.trigger` tags from a `TelemetryEnrichmentMiddleware` would actually land on the span we expect. Committing to this shape would also gate Phases 3a, 3b, and 4 on shipping that middleware first ([AB#628](https://dev.azure.com/jamesconsulting/_workitems/edit/628)). Option (a) remains available as a future decision once Azure-hosted validation is possible; at that point a follow-up ADR would supersede this one.

### Option (b1) — `<Domain>.Http.<Op>` function / `<Domain>.<Op>` service (the Assets shape)

Rejected. It encodes transport into the name — which OpenTelemetry guidance specifically discourages in favor of attributes — and it adds churn across Entities, ClientManagement, and future domains without buying useful query power beyond what `http.request.method` / `http.route` / `faas.trigger` already provide.

### Option (c) — Single span per endpoint, no service layer at all

Rejected for anything beyond thin handlers. The function-plus-service shape gives us a clear place to attach service-internal tags (authorization outcome, domain IDs resolved post-auth, EF operation counts), and it matches how exceptions are recorded at each layer (ADR 0003). Thin handlers are the documented exception, not the default.

## Consequences

### Gained

- **One naming rule for every domain.** Reviewers, dashboards, and KQL can use a single pattern.
- **Low-cardinality, stable span names** — domain and operation are the only sources of variance; route parameters and query strings stay in attributes where OTel wants them.
- **Clear function-vs-service split** makes per-layer exception recording (ADR 0003) unambiguous.
- **Thin handlers don't lie.** A single `Auth.GetMe` span accurately reflects a handler that does nothing but return; inserting an empty `.Service` child would be noise.

### Paid

- **Two-layer recording cost.** Every non-thin endpoint pays for two `ActivitySource.StartActivity` calls per request instead of one. Acceptable — `ActivitySource` is sampler-aware, and the cost is dwarfed by the DB round-trip on the same call path.
- **Dashboards and saved KQL built against old names are stale.** The Phase 1c PR renamed `Assets.Http.*`, `ClientMgmt.*`, `ClientMgmt.GetClientAssignments`, and `UserProfile.EnsureProfileExists`. A dashboard audit ([AB#638](https://dev.azure.com/jamesconsulting/_workitems/edit/638)) is required before the rename reaches a shared environment. No production dashboards exist today, so the bill is bounded.
- **Host-span question stays open** until we deploy to Azure. The §8.4 checklist item in the Phase 1c plan tracks validating presence, attributes, and parent/child relationship of the host-emitted invocation span, at which point a new ADR may revisit Option (a).

## References

- [`docs/plans/phase-1c-span-validation.md`](../plans/phase-1c-span-validation.md) — the 12 captured traces, the pre-b2 inventory, and the decision record.
- [`docs/patterns/service-function-pattern.md §3.1`](../patterns/service-function-pattern.md) — the normative rule for implementation and review.
- [`src/Api/Services/AssetService/AssetsTelemetry.cs`](../../src/Api/Services/AssetService/AssetsTelemetry.cs), [`src/Api/Observability/ClientManagementTelemetry.cs`](../../src/Api/Observability/ClientManagementTelemetry.cs), [`src/Api/Services/UserProfile/UserProfileTelemetry.cs`](../../src/Api/Services/UserProfile/UserProfileTelemetry.cs) — activity-name constants that embody this rule.
- [OpenTelemetry — HTTP Spans](https://opentelemetry.io/docs/specs/semconv/http/http-spans/), [FaaS Spans](https://opentelemetry.io/docs/specs/semconv/faas/faas-spans/), [General naming guidance](https://opentelemetry.io/docs/specs/semconv/general/naming/).
