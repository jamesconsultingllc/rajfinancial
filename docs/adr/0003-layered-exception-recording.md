# ADR 0003 — Layered exception recording on per-layer activities

- **Status:** Accepted
- **Date:** 2026-04-24
- **Deciders:** Rudy James Jr
- **Related:**
  - Pattern: [`docs/patterns/service-function-pattern.md §4`](../patterns/service-function-pattern.md)
  - Code: [`src/Api/Observability/ActivityExceptionExtensions.cs`](../../src/Api/Observability/ActivityExceptionExtensions.cs), [`src/Api/Middleware/Exception/ExceptionClassification.cs`](../../src/Api/Middleware/Exception/ExceptionClassification.cs)
  - Work items: [AB#637](https://dev.azure.com/jamesconsulting/_workitems/edit/637); Feature [#632](https://dev.azure.com/jamesconsulting/_workitems/edit/632)
  - Specs: [OpenTelemetry — Recording Errors](https://opentelemetry.io/docs/specs/otel/trace/exceptions/), [HTTP span status](https://opentelemetry.io/docs/specs/semconv/http/http-spans/#status)

## Context

A single request that throws passes through multiple layers in this codebase, each of which has its own `Activity`:

1. **Service-layer span** (`<Domain>.<Op>.Service`) — the domain call where the exception originated.
2. **Function-layer span** (`<Domain>.<Op>`) — the HTTP handler that delegated to the service (when one is opened; thin handlers per ADR 0002 may skip this).
3. **`AuthorizationMiddleware`** — owns its own `Authorization.CheckAccess` span when the failure is an authorization denial.
4. **`ExceptionMiddleware`** — records on the outer Functions Invoke span (the span the host gives each invocation, retrieved via `FunctionContext`).

Naïve implementations centralize exception recording in the outermost middleware only: "we have exception middleware, it records the exception, we're done." Under that shape, per-domain trace UIs, KQL grouped by service span, and span-level error-rate metrics all come up empty on the service span — the exception traveled through it without being recorded. Domain observability then degrades to "look at the outer Invoke span and read the stack trace."

We also need handled-client exceptions (validation errors, not-found, forbidden, business-rule violations, optimistic-concurrency) to behave differently from unexpected exceptions. OpenTelemetry's HTTP conventions say 4xx **server-originated** outcomes should *not* mark the span status as `Error` — they are intentional business outcomes, not server failures — while 5xx-class / unexpected exceptions *should*. Every recording site needs to follow the same rule, not improvise.

## Decision

Every layer that opens its own `Activity` records the exception on that activity, via a single helper:

```csharp
catch (Exception ex)
{
    activity?.RecordExceptionOutcome(ex);
    throw;
}
```

`ActivityExceptionExtensions.RecordExceptionOutcome` ([source](../../src/Api/Observability/ActivityExceptionExtensions.cs)) is the single place where the W3C / OTel classification rule is applied:

- `OperationCanceledException` (cooperative cancellation) is **neither** recorded **nor** marked `Error`. Caller cancellation is not a server fault.
- Handled client exceptions (`ValidationException`, `NotFoundException`, `ForbiddenException`, `UnauthorizedException`, `ConflictException`, `BusinessRuleException`, `DbUpdateConcurrencyException` — the set identified by `ExceptionClassification.IsHandledClientException`) are recorded as an `exception` event on the span but **do not** set the span status to `Error`.
- All other exceptions (5xx-class, unexpected) are recorded as an `exception` event **and** set the span status to `Error`.

The recording is intentionally duplicated — service, function, authorization middleware, and `ExceptionMiddleware` each record on their own span. `ExceptionMiddleware` applies the same helper to the outer Functions Invoke span (captured via `Activity.Current` *before* it starts its own `Middleware.Exception` activity — see the [Consequences](#consequences) section), so the host-emitted invocation span carries the same classification. `ExceptionMiddleware` then converts the exception into the HTTP response body.

Direct calls to `activity.SetStatus(...)`, `activity.RecordException(...)`, or `activity.AddException(...)` outside this helper are **not allowed** in call sites. All recording is routed through `RecordExceptionOutcome` so the classification rule lives in one place.

## Alternatives considered

### A. Record once, in `ExceptionMiddleware` only

Rejected. It centralizes the code but loses per-layer visibility. Queries that group by the service-layer span name (`operation.name == "Assets.GetById.Service"`) see no exceptions under this shape — the service span completes "cleanly" even though it threw. Domain-level error-rate dashboards become impossible without reconstructing parent/child relationships from the outer Invoke span.

### B. Record once, in the service only

Rejected for the opposite reason. The function-layer span and the `AuthorizationMiddleware` span would complete without the exception event, and `ExceptionMiddleware` would have no signal on the outer Invoke span at all — the span the host emits would look successful right up to the moment a non-2xx response appears.

### C. Record everywhere but let each site implement its own classification

Rejected. The rule for "is this a handled 4xx or a 5xx" is non-trivial (seven exception types today, with room for more) and must stay consistent across four recording sites. Letting each site re-implement the check is how you end up with handled-4xx outcomes polluting your `status=Error` error-rate charts for one service and not another.

### D. Use a generic middleware that walks the span chain and records once per Activity

Rejected as premature. It requires reaching into the active `Activity` chain, which is brittle under async state machines and sampled spans, and it solves a problem (consistency) that the shared helper already solves more cheaply. The current shape — each layer has its own `try/catch` that calls one helper — is simple, explicit, and line-of-sight readable at every recording site.

## Consequences

### Gained

- **Per-layer trace UIs work.** Service-span dashboards, function-span dashboards, and the outer Invoke span all show the exception as expected.
- **One classification rule, one place.** Updating the set of handled-client exceptions is a one-line edit in `ExceptionClassification` and every recording site picks it up automatically.
- **Handled-4xx outcomes don't inflate error rates.** Validation failures and not-found responses don't turn green charts red.
- **Cancellation is silent on spans.** Background cancellation during shutdown or client abort is not a recording event; it stays out of telemetry noise.

### Paid

- **The exception event appears on multiple spans in a trace.** This looks like double-reporting on first read, which is why [pattern §4](../patterns/service-function-pattern.md) explicitly names it out and marks "collapse these catches" as a rejection criterion. A reviewer inclined to dedupe them needs to be pointed at this ADR.
- **Every `try/catch` at a recording site has the same two lines.** It is intentional boilerplate and cheap at the point of use; the helper keeps the classification hidden.
- **`ExceptionMiddleware` captures the outer Invoke span before starting its own.** It does so by reading `Activity.Current` at the top of `Invoke` — *before* it starts its own `Middleware.Exception` activity — and calling `RecordExceptionOutcome` on that captured reference. If it called `Activity.Current?.RecordExceptionOutcome(ex)` inside the `catch`, it would record on its own middleware span, not the Functions Invoke span. The current shape is documented in [`ExceptionMiddleware.cs`](../../src/Api/Middleware/Exception/ExceptionMiddleware.cs); architecture tests should cover that the capture-before-activity sequence is preserved.

## References

- [`docs/patterns/service-function-pattern.md §4`](../patterns/service-function-pattern.md) — the four-layer table and the "do not collapse" rule.
- [`src/Api/Observability/ActivityExceptionExtensions.cs`](../../src/Api/Observability/ActivityExceptionExtensions.cs) — `RecordExceptionOutcome` helper.
- [`src/Api/Middleware/Exception/ExceptionClassification.cs`](../../src/Api/Middleware/Exception/ExceptionClassification.cs) — the handled-client-exception set.
- [`src/Api/Middleware/Exception/ExceptionMiddleware.cs`](../../src/Api/Middleware/Exception/ExceptionMiddleware.cs) — outermost recording site + HTTP mapping.
- [OpenTelemetry — Recording Errors](https://opentelemetry.io/docs/specs/otel/trace/exceptions/), [HTTP span status](https://opentelemetry.io/docs/specs/semconv/http/http-spans/#status).
