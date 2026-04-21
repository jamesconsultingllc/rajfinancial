# Observability Runbook

> **Audience:** on-call engineers, SREs, developers debugging production/staging issues.
> **Source of truth for instrumentation policy:** [`AGENT.md` §Observability](../AGENT.md#observability).
> This runbook covers **operational usage** — how to diagnose problems using what's been instrumented.

---

## TL;DR — On-Call Triage Ladder

When a symptom is reported, go in this order. **Stop at the first step that gives you the answer.**

1. **Application Insights — Failures & Performance blades.** Application Map shows which component is slow/erroring. KQL + sample traces usually resolve it.
2. **Live Metrics (App Insights).** Real-time CPU, memory, dependency calls, failures. Useful when lag between event and KQL ingestion hurts.
3. **`/api/health/ready`** on the affected instance — returns 503 with a JSON payload showing the overall status (per-check detail is Development-only; Production returns overall status + duration only to avoid leaking internal dependency names).
4. **`dotnet-counters`** — 10-second attach, see runtime counters (CPU, GC, thread pool, our domain counters). No redeploy.
5. **`dotnet-trace`** — 30-second attach, capture EventPipe trace for deep analysis in PerfView / Speedscope. No redeploy.
6. **Log grep (App Insights `traces` table).** Last resort. Logs are noisy; metrics + traces are the primary debugging tools by design.

---

## Prerequisites

### Tools
Install these **locally** (on-call laptop). They attach to remote .NET processes via diagnostic ports over SSH / kubectl exec / Azure Container Apps exec.

```powershell
dotnet tool install --global dotnet-counters
dotnet tool install --global dotnet-trace
dotnet tool install --global dotnet-dump
dotnet tool install --global dotnet-gcdump
```

### Azure CLI / gh access
- `az login` with read access to the Application Insights resource.
- `gh auth status` for PR / issue lookups.
- App Insights resource name + resource group. Keep in your password manager.

### Reading the instrumentation
The Functions API is instrumented per AGENT.md §Observability. Seven domains register an `ActivitySource` + `Meter` (see `ObservabilityDomains.All`); `Testing / diagnostics` is a reserved **log EventId range** only (no OTel source/meter is registered for it today):

| Domain | Source / Meter name | EventId range |
|---|---|---|
| Auth | `RajFinancial.Api.Auth` | 1000–1999 |
| Assets | `RajFinancial.Api.Assets` | 2000–2999 |
| Entities | `RajFinancial.Api.Entities` | 3000–3999 |
| User Profile | `RajFinancial.Api.UserProfile` | 4000–4999 |
| Middleware | `RajFinancial.Api.Middleware` | 5000–5999 |
| Client Management | `RajFinancial.Api.ClientManagement` | 6000–6999 |
| Authorization | `RajFinancial.Api.Authorization` | 7000–7999 |
| Testing / diagnostics | _(no OTel source/meter — EventId range only)_ | 9000–9999 |

All emit to Application Insights via OpenTelemetry (`Azure.Monitor.OpenTelemetry.Exporter` + `Microsoft.Azure.Functions.Worker.OpenTelemetry`). Local dev writes to `logs/rajfinancial-{Date}.log` instead.

---

## Health Endpoints

| Endpoint | Purpose | Caller | Expected |
|---|---|---|---|
| `GET /api/health/live` | Process is alive. Returns 200 **always** unless the process is dead. | Container runtime / App Service ping | 200 `{"status":"alive"}` |
| `GET /api/health/ready` | App can serve traffic: DB reachable + config loaded + dependencies up. | Azure Front Door / load balancer probe | 200 `{"status":"healthy","totalDuration":…}`; **503** `{"status":"unhealthy","totalDuration":…}` if any check fails |

**On 503 from `/api/health/ready`:**
- The response body is intentionally minimal in Production — only `status` and `totalDuration`. Per-check detail (`checks[]` with each check's name, status, description, duration) is included **only when `ASPNETCORE_ENVIRONMENT=Development`** (see `src/Api/Functions/HealthCheckFunction.cs`). In Production, the `RajFinancial.Health.Readiness` warning (EventId 9901) logs the overall status only — **it does not name the failing check**. To identify the failing check use the per-check logs: `Database health check failed` (EventId 9900) and `Configuration validation failed` (EventId 9902) in Application Insights `traces`.
- `database` failure → Azure SQL connection broken (check firewall, connection string, SQL server health).
- `config` failure → `EntraExternalId` or `AppRoles` section missing/placeholder, or `APPLICATIONINSIGHTS_CONNECTION_STRING` unset outside Development.

**Wire Azure Container Apps / App Service probes to `/api/health/ready`** so broken instances drop out of rotation automatically. Do **not** probe `/api/health/live` — a DB-less instance will happily return 200 and receive traffic.

---

## Application Insights — KQL Starter Pack

Application Insights UI → Logs (KQL). Paste and adjust time range.

### Top failures in last hour
```kql
exceptions
| where timestamp > ago(1h)
| summarize count() by type, outerMessage, cloud_RoleName
| order by count_ desc
| take 20
```

### Slowest endpoints (p95)
```kql
requests
| where timestamp > ago(1h)
| summarize
    p50=percentile(duration, 50),
    p95=percentile(duration, 95),
    p99=percentile(duration, 99),
    count_=count()
    by operation_Name
| order by p95 desc
```

### Auth failures vs successes (metric from `RajFinancial.Api.Auth`)
```kql
customMetrics
| where timestamp > ago(1h)
| where name in ("auth.failures.count", "auth.successes.count")
| summarize total=sum(value) by bin(timestamp, 1m), name
| render timechart
```

### Authorization denials by tier (`RajFinancial.Api.Authorization`)
```kql
customMetrics
| where timestamp > ago(1h) and name == "authorization.denied.count"
| extend tier = tostring(customDimensions["authz.tier"])
| extend reason = tostring(customDimensions["authz.reason"])
| summarize count_=sum(value) by tier, reason
| order by count_ desc
```

### User profile concurrency conflicts (`RajFinancial.Api.UserProfile`)
Spike here usually means two requests hitting JIT for same user:
```kql
customMetrics
| where timestamp > ago(6h) and name == "userprofile.concurrent.conflicts.count"
| summarize conflicts=sum(value) by bin(timestamp, 5m)
| render timechart
```

### Middleware exception rate
```kql
customMetrics
| where timestamp > ago(1h) and name == "middleware.exceptions.count"
| summarize total=sum(value) by bin(timestamp, 1m)
| render timechart
```

### Trace waterfall for a single operation
Given an `operation_Id` from a failed request:
```kql
union requests, dependencies, traces, exceptions
| where operation_Id == "<paste-operation-id>"
| project timestamp, itemType, name, duration, resultCode, message, severityLevel
| order by timestamp asc
```

### Follow a user's journey
```kql
let uid = "<user-guid>";
union requests, traces
| where timestamp > ago(1h)
| where customDimensions["user.id"] == uid or tostring(customDimensions) contains uid
| order by timestamp asc
| project timestamp, itemType, name, resultCode, message
```

### Log EventId lookup (which domain?)
Given EventId 3107 in a log entry, that's **Entities** (3000–3999 range). Confirm with:
```kql
traces
| where timestamp > ago(24h) and customDimensions["EventId"] == "3107"
| project timestamp, message, severityLevel, cloud_RoleName, customDimensions
```

---

## `dotnet-counters` — Live Runtime Counters

**When to use:** spike in latency, high CPU, suspected GC pressure, thread pool starvation, want to see our domain counters tick in real time.

**Does NOT require redeploy.** Attach via diagnostic port.

### Local / side-car (has shell access)

```powershell
# List processes
dotnet-counters ps

# Monitor runtime + our domain meters
dotnet-counters monitor --process-id <PID> `
    System.Runtime `
    Microsoft.AspNetCore.Hosting `
    RajFinancial.Api.Auth `
    RajFinancial.Api.Assets `
    RajFinancial.Api.Entities `
    RajFinancial.Api.UserProfile `
    RajFinancial.Api.Middleware `
    RajFinancial.Api.ClientManagement `
    RajFinancial.Api.Authorization
```

### Azure Container Apps (no SSH)

Open a console to the running replica:
```powershell
az containerapp exec `
    --name rajfinancial-api `
    --resource-group rg-rajfinancial `
    --command /bin/bash

# inside the container:
dotnet-counters monitor --name RajFinancial.Api System.Runtime RajFinancial.Api.Entities
```

(If `dotnet-counters` isn't baked into the image, bake it into a side-car debugging image or use `dotnet-monitor` egress.)

### What the counters tell you

| Counter | Normal | Investigate when |
|---|---|---|
| `cpu-usage` | <70% | >85% sustained |
| `gen-2-gc-count` (cumulative) | grows slowly | jumps often → memory pressure |
| `threadpool-queue-length` | near 0 | >100 → starved, blocking I/O somewhere |
| `threadpool-thread-count` | stable | climbing → async done wrong (blocking) |
| `exception-count` | low and flat | any spike → correlate to customDimensions |
| `RajFinancial.Api.Assets.assets.query.duration.ms` | p95 < 200ms | p95 > 1s → DB or N+1 issue |
| `RajFinancial.Api.Authorization.authorization.check.duration.ms` | p95 < 5ms | p95 > 50ms → grant table scan, missing index |

---

## `dotnet-trace` — 30-Second Deep Capture

**When to use:** intermittent latency, CPU hot paths, lock contention, "requests sometimes take 5 seconds and we can't figure out why."

### Capture

```powershell
# Attach for 30 seconds, default profile (CPU + GC + tasks)
dotnet-trace collect --process-id <PID> --duration 00:00:30 --output rajfinancial.nettrace

# Or capture from a remote container:
az containerapp exec --name rajfinancial-api --resource-group rg-rajfinancial --command /bin/bash
# inside:
dotnet-trace collect --name RajFinancial.Api --duration 00:00:30 -o /tmp/t.nettrace
# then scp/az cp out
```

### Analyze

- **PerfView** (Windows, free from Microsoft): open the `.nettrace`, stack view → CPU samples. Find the hot frame.
- **Speedscope** (web): convert with `dotnet-trace convert --format Speedscope rajfinancial.nettrace`, drag the `.speedscope.json` onto https://speedscope.app.
- **Visual Studio** (paid): File → Open → select `.nettrace`. Gives flame graph + call tree.

### Targeted captures

`dotnet-trace --providers` accepts **EventSource / EventPipe** provider names, not OpenTelemetry `Meter` or `ActivitySource` names. Passing `--providers RajFinancial.Api.Entities` does **not** filter to our domain — there's no EventSource with that name.

This API runs as an Azure Functions **isolated worker** (no ASP.NET Core server), so ASP.NET Core hosting providers won't emit anything against the worker process. To discover what providers are actually available on the target process:

```powershell
# List all EventPipe providers registered in the target PID
dotnet-trace list-providers --process-id <PID>
```

Useful providers commonly present on the isolated worker:
```powershell
# EF Core queries (emitted by the worker)
dotnet-trace collect --process-id <PID> `
    --providers Microsoft-EntityFrameworkCore `
    --duration 00:01:00 -o ef.nettrace

# .NET runtime counters / GC / threadpool
dotnet-trace collect --process-id <PID> `
    --providers System.Runtime `
    --duration 00:01:00 -o runtime.nettrace
```

To filter OpenTelemetry spans by `ActivitySource` name you need `Microsoft-Diagnostics-DiagnosticSource` with a `FilterAndPayloadSpecs` argument — that's complex and rarely worth it for ad-hoc diagnostics. Prefer the **Application Insights `dependencies` / `requests` tables** (filtered on `cloud_RoleName` + `operation_Name`) for domain-scoped query work.

---

## `dotnet-dump` / `dotnet-gcdump` — Memory Problems

### Suspected memory leak
```powershell
# Lightweight — types and counts only, ~100 MB output
dotnet-gcdump collect --process-id <PID> --output leak.gcdump

# View in Visual Studio (File → Open → gcdump) or PerfView
```

### Full process dump (deadlock, crash analysis)
```powershell
dotnet-dump collect --process-id <PID> --output rajfinancial.dmp

# Interactive analysis
dotnet-dump analyze rajfinancial.dmp
# at the SOS prompt:
> threads
> clrstack -all
> dumpheap -stat
> pe -nested  # current exception
```

**Warning:** full dumps are large (GBs), may contain PII. Scrub before sharing. Do not commit.

---

## EventPipe — Custom Capture Pipelines

EventPipe is the in-process tracing transport. On by default in .NET 10. **Do not disable.**

### Capture via configuration (no tool)

Useful when you need traces to survive a pod restart. Set env var before process start:

```bash
DOTNET_DiagnosticPorts=/tmp/diag.sock
COMPlus_EnableEventPipe=1
```

Then attach externally via the socket. Primarily used for automated startup-tracing or pinned capture around a known failing test.

### Listen for a specific ActivitySource from inside code

For building custom diagnostics dashboards — see MS Learn [EventPipe guide](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe).

---

## Scenario Playbooks

### 1. "Requests are slow."
1. App Insights → Performance blade. Sort by p95.
2. Identify the slow operation. Look at **Operation details** → Dependencies timeline. Usually a slow SQL call or external HTTP.
3. If SQL: check EF-generated query, add index.
4. If unclear: `dotnet-trace collect --duration 00:00:30` on a hot instance, load in Speedscope, find the hot frame.

### 2. "Users getting 500 errors."
1. App Insights → Failures → top exception type.
2. Click through to a sample exception → it'll have `operation_Id`. Use the trace waterfall KQL above.
3. If it's a `DbUpdateException` with `SqlException`: check DB health, FK/unique constraints.
4. If it's our `BusinessRuleException` / `ValidationException`: **not** a bug — client sent bad input. Correlate to source system.

### 3. "Authentication is failing."
1. KQL `customMetrics | where name == "auth.failures.count"` — confirm rate.
2. KQL `traces | where customDimensions["EventId"] between ("1001" .. "1999")` — see the auth log messages for the failure reason (token expired, invalid audience, principal not found, etc.).
3. Check Entra External ID health portal if broad outage.

### 4. "Authorization is denying legitimate users."
1. KQL for `authorization.denied.count` grouped by `authz.tier` + `authz.reason`.
2. If tier=`grant` and reason=`no coverage`: the user's `DataAccessGrants` don't cover the requested resource. Check the grant table.
3. If tier=`denied` and reason=`wrong tenant`: the resource belongs to another tenant. Good — that's the system working.

### 5. "Memory is growing."
1. `dotnet-counters monitor` → watch `gc-heap-size` + `gen-2-gc-count`.
2. If Gen 2 heap climbing monotonically → true leak. Capture `dotnet-gcdump` at T and T+10min. Compare type counts.
3. If Gen 2 counts high but heap stable → allocation-heavy code. `dotnet-trace` with `--profile gc-collect` profile.

### 6. "CPU is pegged at 100%."
1. `dotnet-trace collect --duration 00:00:30`.
2. Open in PerfView, filter to non-framework frames.
3. Top frame is the culprit — usually a tight loop, regex backtracking, or JSON serialization of large object.

### 7. "One instance is broken, others are fine."
1. Hit `/api/health/ready` on the bad instance. In Development the 503 body lists each failing check; in Production the body is minimal. The `RajFinancial.Health.Readiness` warning (EventId 9901) in Application Insights `traces` confirms that readiness failed overall, but it does **not** identify the failing check. Use the per-check warnings to identify what broke: EventId 9900 (`Database health check failed`) or EventId 9902 (`Configuration validation failed`).
2. If `database` — that pod lost DB connectivity (NSG, private endpoint, DNS). Restart pod.
3. If `config` — config drift. Check environment variables on that instance vs others.

---

## Don'ts

- **Do NOT add `logger.LogInformation(...)` directly** in production troubleshooting. Use metrics (cheap, sampled, aggregated) or traces (sampled, linked to operation_Id). See AGENT.md §Logging Pattern.
- **Do NOT use the classic `TelemetryClient` / `TrackDependency()` API** — maintenance mode, breaks OpenTelemetry correlation.
- **Do NOT ship new code without instrumentation.** Retrofitting is painful. Per AGENT.md §Observability, every new service/module declares its own `ActivitySource` + `Meter` from day one.
- **Do NOT probe `/api/health/live` for load-balancer rotation** — it stays 200 even when DB is dead. Use `/api/health/ready`.
- **Do NOT set prod log level below `Warning`** — see AGENT.md §Log Level Policy. Verbose logs in prod are cost (ingestion) + noise (alert fatigue).
- **Do NOT disable EventPipe.** It's the transport for all the tools above.

---

## Further Reading

- [.NET Diagnostics Overview](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/) — MS Learn entry point
- [OpenTelemetry .NET docs](https://opentelemetry.io/docs/languages/net/)
- [Application Insights KQL reference](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/query/)
- [High-performance logging with source-generators](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/high-performance-logging)
- [EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe)
- **Local spec & policy:** [`AGENT.md` §Observability](../AGENT.md#observability) — the authoritative instrumentation standards.
