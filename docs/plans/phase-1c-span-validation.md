# Phase 1c — 30-minute span validation procedure

> **Status:** Scaffolded. Execution pending — capture must be run against a live `func start` + seeded DB. Output gets pasted into the *Decision capture* section at the bottom of this file and then copied into ADR 0002 when Phase 1a authors it.
>
> **Feature:** [AB#632](https://dev.azure.com/jamesconsulting/_workitems/edit/632)
> **Task:** [AB#633](https://dev.azure.com/jamesconsulting/_workitems/edit/633)
> **Related:** [AB#628](https://dev.azure.com/jamesconsulting/_workitems/edit/628) (centralized HTTP-span enrichment) — Option (a) below is coupled to this work.

---

## 1. What this phase decides

The canonical pattern doc ([`docs/patterns/service-function-pattern.md`](../patterns/service-function-pattern.md) §3.1) lists the HTTP-layer span name as **TBD per Phase 1c**. This procedure resolves that TBD by capturing a real local trace and picking between two options.

### Option (a) — Drop per-domain HTTP spans

- Rely on the auto-emitted Functions Invoke span (or whatever the isolated worker host emits unaided).
- `TelemetryEnrichmentMiddleware` from [AB#628](https://dev.azure.com/jamesconsulting/_workitems/edit/628) tags that span with `domain` / `operation` attributes.
- Dashboards group by tag, not by span name.
- Removes `Assets.Http.*` from [`AssetsTelemetry.cs`](../../src/Api/Services/AssetService/AssetsTelemetry.cs); no equivalent added to Entities / ClientManagement.
- **Gating constraint:** if Option (a) wins, Phases 3a / 3b / 4 / 7 are blocked on #628 landing first — otherwise the enrichment tags that replace per-domain HTTP spans won't exist.

### Option (b) — Keep per-domain HTTP spans as `<Domain>.Http.<Op>`

- Every function body keeps a `using var activity = StartActivity("<Domain>.Http.<Op>")` + try/catch + `RecordExceptionOutcome`.
- Pro: clearer domain grouping in trace UIs where tag-based filtering is awkward.
- Con: one redundant span per request; three domains must each maintain their own `*Telemetry.cs` HTTP-span constants.
- **No dependency on #628.** Phases 3a / 3b / 4 can proceed independently.

**Expectation:** Option (a) wins because #628's enrichment tags subsume the domain-grouping use case — but this procedure is authoritative; neither option is locked until the trace is captured and reviewed against the criteria in §5.

---

## 2. Why a local trace is required

Reading the OTel registration code ([`ObservabilityRegistration.cs`](../../src/Api/Configuration/ObservabilityRegistration.cs)) tells us which `ActivitySource` / `Meter` names we subscribed to (`AddSource(DomainSources)` on line 84, `AddMeter(DomainSources)` on line 107). It does **not** tell us what the Functions isolated worker host emits for the HTTP layer on its own — that depends on the host version, the worker package (`Microsoft.Azure.Functions.Worker.OpenTelemetry`), and the trigger binding. A real trace capture is the only way to see:

1. The actual span name the host emits for each HTTP request (candidates include `Microsoft.Azure.Functions.Worker.Invoke`, `Invoke`, `Function.<Name>`, or an `ActivityKind.Server` span with no explicit name).
2. The parent/child relationship between that span and the domain's service span (`Assets.GetById`, etc.).
3. Whether the host-emitted span already carries tags (`code.function`, `faas.trigger`, HTTP method/route) that would make Option (a)'s per-call `domain`/`operation` tags redundant or complementary.
4. Whether the per-domain HTTP span added today (`Assets.Http.<Op>`) is nested *inside* the host span (making it a 3-level trace) or at the same level (2-level).

The observability stack already has `AddConsoleExporter` registered for both traces and metrics when `env.IsDevelopment()` (see `ObservabilityRegistration.cs:89, 110`), so **no external collector is required** — spans stream to the `func start` stdout.

---

## 3. Prerequisites

- [ ] Docker running with the SQL container (connection string via `ConnectionStrings__SqlConnectionString` env var or `src/Api/local.settings.json`; see repo README / `scripts/infra/` for the compose file and seed scripts).
- [ ] `src/Api/local.settings.json` exists and provides the connection string plus Entra-related values the Functions host needs at boot. (`appsettings.local.json` is a tests-only override consumed by `tests/IntegrationTests/Support/FunctionsHostFixture.cs`, not by the running worker.)
- [ ] Development environment is signaled to the host. `src/Api/Properties/launchSettings.json` sets both `ASPNETCORE_ENVIRONMENT=Development` and `AZURE_FUNCTIONS_ENVIRONMENT=Development`; if you start the worker outside those profiles (e.g. a bare `func start` in a plain shell), set both env vars explicitly — a few code paths key off `AZURE_FUNCTIONS_ENVIRONMENT` (`DesignTimeDbContextFactory`, `SerializationFactory`) and the Console exporters + file logging gate on `IHostEnvironment.IsDevelopment()` (`ObservabilityRegistration.cs:59, 86, 109, 120`).
- [ ] `dotnet build src/Api/RajFinancial.Api.csproj -c Debug` succeeds.
- [ ] A seeded user + one asset + one entity + one client assignment exist in the DB with known IDs, and a bearer token for that user is in hand — see §3.1 below for how to obtain each.
- [ ] `curl` (or PowerShell `Invoke-WebRequest`) available, plus `jq` for token sanity-checks (optional).

### 3.1 Obtaining the bearer token + IDs

Keep these values handy — they go into the curl commands in §4.

| Variable | How to get it |
|---|---|
| `BEARER` | For local (unsigned-JWT) runs, mint a dev token the same way the integration tests do — see `tests/IntegrationTests/Support/TestAuthHelper.cs` plus `tests/IntegrationTests/appsettings.json` (the `Entra:TestUsers:*` entries it reads) and `appsettings.local.json` if present. For a real Entra token against the running worker, use `RopcTokenProvider` with `TEST_{ROLE}_PASSWORD` env vars set (same as the integration suite). **The fixture itself does not mint tokens** — only `TestAuthHelper` / `RopcTokenProvider` do. |
| `ASSET_ID` | After `func start --useHttps` is up (see §4.1), `curl -k -H "Authorization: Bearer $BEARER" https://localhost:7071/api/assets` and pick any `id` from the response. |
| `ENTITY_ID` | Same pattern against `/api/entities`. |
| `DENIED_ASSET_ID` | An asset id owned by a **different** user than the one `BEARER` represents. Seed a second user with an asset, then pick that asset's id. Required for the denied-request capture (§4.3 request 06). |

The `FunctionsHostFixture` used by the integration suite **does not seed data or start the host** — it only builds an `HttpClient` and checks that something is already listening at `FunctionsHost:BaseUrl` (from `tests/IntegrationTests/appsettings.json`, default `https://localhost:7071`). Feature data comes from either (a) the test scenarios making their own HTTP calls, or (b) helper endpoints like `/api/testing/seed-contact`. If the DB is empty, create a user / asset / entity / assignment via the normal create endpoints before starting this procedure, or run one integration scenario that exercises those create paths first.

---

## 4. Capture procedure

There are two supported paths. Both require the worker running with `--useHttps`; they differ only in how the 7 requests (1 healthcheck + 6 captures) are fired.

- **Path A — `inso` CLI (recommended for reproducibility).** See [`phase-1c/README.md`](./phase-1c/README.md). Uses the committed [`phase-1c/phase-1c.insomnia.yaml`](./phase-1c/phase-1c.insomnia.yaml) workspace so the same set of requests runs the same way every time, regardless of who's at the keyboard.
- **Path B — ad-hoc curl (fallback).** Kept in §4.3 below for anyone who can't install `inso` or just wants to tweak a single request.

Either way, the stdout of `func start --useHttps` is the capture surface — §4.1 + §4.2 apply to both paths.

### 4.1 Start the worker with the Console exporter active

From repo root:

```powershell
cd src\Api
func start --useHttps | Tee-Object -FilePath ..\..\phase1c-spans.log
```

`--useHttps` is not optional — `tests/IntegrationTests/appsettings.json` has `FunctionsHost:BaseUrl=https://localhost:7071`, `src/Api/Properties/launchSettings.json` declares HTTPS profiles, and the fixture's cert validator only trusts the dev cert on `localhost`. (`func start` alone defaults to HTTP and breaks integration tests + this procedure's requests, which all hit HTTPS.)

`Tee-Object` both prints to the console and persists the full span stream to `phase1c-spans.log` at the repo root — do not skip this: §6 of this doc needs the full log attached to ADO #633.

Wait for `Host lock lease acquired` and the endpoint list. Leave the window visible — the Console exporter dumps every span to it.

### 4.2 Fire the 7 requests (healthcheck + 6 captures)

**Path A — `inso`:**

Feed the bearer + ids via shell env vars (or populate the private `Local`
sub-env in Insomnia once — see [`phase-1c/README.md`](./phase-1c/README.md#2-populate-your-private-env-file)).
**Do not paste literal tokens on the command line** — they land in
PSReadLine history and any CI job log.

```powershell
# Second terminal (from repo root):
$env:BEARER = (Get-Credential -UserName bearer -Message 'Paste bearer token').GetNetworkCredential().Password
$env:ASSET_ID = "<guid>"
$env:ENTITY_ID = "<guid>"
$env:DENIED_ASSET_ID = "<guid-owned-by-other-user>"

inso run collection "Phase 1c Span Capture" `
  --env Local `
  --env-var bearer=$env:BEARER `
  --env-var asset_id=$env:ASSET_ID `
  --env-var entity_id=$env:ENTITY_ID `
  --env-var denied_asset_id=$env:DENIED_ASSET_ID `
  --disableCertValidation `
  -w docs\plans\phase-1c `
  | Tee-Object -FilePath phase1c-requests.log
```

`-w` points at the committed workspace so the run is reproducible without a
prior Insomnia GUI import. The committed YAML intentionally ships the `bearer`
/ `asset_id` / `entity_id` / `denied_asset_id` values blank, so you **must**
either (a) pass them via `--env-var` as shown above (reading from shell env
vars populated via `Get-Credential` / manual assignment, never a literal
token on the command line), or (b) import the workspace into Insomnia once
and populate the private `Local` sub-env there. Skipping both will cause
401s/404s and an incomplete capture. `--disableCertValidation` is required
for the localhost dev cert on `https://localhost:7071`. Unset the env vars
(`Remove-Item Env:BEARER,Env:ASSET_ID,Env:ENTITY_ID,Env:DENIED_ASSET_ID`)
when you're done.

Seven requests execute in order, with ~2 s between them (the runner serializes): `00 Healthcheck` first as a worker-liveness sanity ping, then `01`–`06` as the actual capture set. Ignore the healthcheck trace in §6 — it hits `/health/live` and is not one of the six captures. See [`phase-1c/README.md`](./phase-1c/README.md#2-populate-your-private-env-file) for how to populate the `bearer` / `asset_id` / `entity_id` / `denied_asset_id` env vars (they live in a private `isPrivate: true` Insomnia sub-env — not in the committed YAML).

**Path B — curl fallback:** see §4.3.

### 4.3 Curl fallback (equivalent to Path A's 6 captures)

Run each from a second terminal. Between requests, let stdout settle for ~2 seconds. Path A's `00 Healthcheck` request is a worker-liveness sanity ping only and is intentionally omitted here — if you want the equivalent, `curl -k https://localhost:7071/api/health/live` before the capture set. It is not one of the six traces copied into §6.

**01 Auth — GET /auth/me:**

```powershell
# Prompt securely once for the bearer token so it is not written to PSReadLine history.
$env:BEARER = (Get-Credential -UserName bearer -Message 'Paste bearer token').GetNetworkCredential().Password
curl.exe -k -H "Authorization: Bearer $env:BEARER" "https://localhost:7071/api/auth/me"
```

**02 UserProfile — GET /profile/me:**

```powershell
curl.exe -k -H "Authorization: Bearer $env:BEARER" "https://localhost:7071/api/profile/me"
```

**03 Assets — GET /assets/{id} (Mode A):**

```powershell
$env:ASSET_ID="..."
curl.exe -k -H "Authorization: Bearer $env:BEARER" "https://localhost:7071/api/assets/$env:ASSET_ID"
```

**04 Entities — GET /entities/{id} (Mode A):**

```powershell
$env:ENTITY_ID="..."
curl.exe -k -H "Authorization: Bearer $env:BEARER" "https://localhost:7071/api/entities/$env:ENTITY_ID"
```

**05 ClientManagement — GET /auth/clients (Mode B):**

```powershell
curl.exe -k -H "Authorization: Bearer $env:BEARER" "https://localhost:7071/api/auth/clients"
```

**06 Denied — GET /assets/{id} for non-owned id:**

```powershell
$env:DENIED_ASSET_ID="..."   # an asset id owned by a different user
curl.exe -k -H "Authorization: Bearer $env:BEARER" "https://localhost:7071/api/assets/$env:DENIED_ASSET_ID"
# Expected: 403 (ForbiddenException). Traces should show Authorization.CheckAccess
# marked AccessDenied with status=Error, while the service/Invoke spans should
# record the handled exception event but should not be marked status=Error.
```

### 4.4 Collect the relevant `Activity` dumps

The Console exporter writes one block per completed span, of the form:

```
Activity.TraceId:            <...>
Activity.SpanId:             <...>
Activity.ParentSpanId:       <...>
Activity.TraceFlags:          Recorded
Activity.ActivitySourceName:  <source>
Activity.DisplayName:         <name>
Activity.Kind:                <Server|Internal|Client>
Activity.StartTime:           <...>
Activity.Duration:            <...>
Activity.Tags:
    <key>: <value>
    ...
```

For each of the six requests, copy every `Activity` block whose `TraceId` matches the outermost request's trace id (all child spans share the same trace id). Anonymize anything that looks like a real user id or email.

Paste the captures into §6 below.

---

## 5. Decision criteria

Review the captures and answer each question. The answers together pick Option (a) or (b).

| # | Question | Option (a) wins if... | Option (b) wins if... |
|---|---|---|---|
| 1 | Does the Functions host emit a span for each HTTP request without our help? | **Yes** — there's already a server-kind span per request. | **No** — there's no server span and our `<Domain>.Http.*` span is the only HTTP-layer record. |
| 2 | If yes, does that host span carry usable tags (HTTP method, route template, status code)? | **Yes** — tagging it with `domain`/`operation` (from #628) gives us everything Option (b) would. | **No** — the host span is a blank `Invoke` with no correlating attributes, and tag enrichment would still leave important facets missing. |
| 3 | Is the per-domain `Assets.Http.<Op>` span a sibling or a child of the host span? | **Child** (wrapping the host span adds a useless outer layer). | **Sibling / separate trace** (no natural parenting — then the per-domain span is the only coherent server-side view). |
| 4 | Is #628's `TelemetryEnrichmentMiddleware` on a concrete timeline? | **Yes** — pick (a) and accept the blocking dependency on 3a / 3b / 4 / 7. | **No / indefinite** — Option (b) ships without waiting for #628 and we retire the per-domain HTTP span later if #628 lands. |

The final call lives in §7.

---

## 6. Capture (paste here during execution)

> **Placeholder — replace before closing ADO #633.** One subsection per request in the order fired by [`phase-1c/phase-1c.insomnia.yaml`](./phase-1c/phase-1c.insomnia.yaml).

### 6.1 `01 Auth — GET /auth/me`

```text
<paste full Activity blocks for the trace here>
```

### 6.2 `02 UserProfile — GET /profile/me`

```text
<paste>
```

### 6.3 `03 Assets — GET /assets/{id}` (Mode A)

```text
<paste>
```

### 6.4 `04 Entities — GET /entities/{id}` (Mode A)

```text
<paste>
```

### 6.5 `05 ClientManagement — GET /auth/clients` (Mode B)

```text
<paste>
```

### 6.6 `06 Denied — GET /assets/{id}` for non-owned id

```text
<paste>
```

### 6.7 Observations

- Host-emitted server span name: `<TBD>`
- Host-emitted server span tags: `<TBD>`
- Parent/child relationship of `<Domain>.<Op>` service span to the host span: `<TBD>`
- Parent/child relationship of existing `Assets.Http.<Op>` span (Assets only) to the host span: `<TBD>`
- Exception recording on the denied request (§6.6): confirm `exception.type` / `exception.message` are recorded on the service span and the host span without setting `status=Error`, and confirm the `Authorization.CheckAccess` span is explicitly marked `status=Error` with an access-denied description (via `AuthorizationTelemetry.RecordDenied`) rather than recording an exception event: `<TBD>`

---

## 7. Decision

> **To be filled in after §6 is complete.**

- **Chosen option:** `<Option (a) | Option (b)>`
- **Rationale (one paragraph):** `<...>`
- **Blocking dependencies created:** `<none | #628 must land before Phases 3a / 3b / 4 / 7 can proceed>`

This §7 block is the exact content that gets copied into ADR 0002 (*Activity naming convention*) when Phase 1a authors it, with the ADR adding only its standard frontmatter (Context / Decision / Consequences / Alternatives considered / References).

---

## 8. Execution checklist

- [ ] §3 prerequisites satisfied (DB up, build passing, token + IDs in hand).
- [ ] §4.1 `func start --useHttps` running; Console exporter producing output.
- [ ] §4.2 all 6 capture requests executed (+ the `00 Healthcheck` liveness ping if using Path A); each returned the expected status code.
- [ ] §4.4 Activity blocks captured and anonymized.
- [ ] §6 sections 6.1 – 6.6 filled in this file, committed on a feature branch.
- [ ] §5 decision criteria evaluated; answers recorded in §6.7.
- [ ] §7 decision recorded.
- [ ] Phase 1a opened (ADO #637) to author ADR 0002 with §7's content.
- [ ] This file linked from ADO #633; work item moved to Done.
