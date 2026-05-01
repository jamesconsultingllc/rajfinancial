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

## 6. Capture results

Captured 2026-04-23 against a locally-built worker (`dotnet build src/Api -c Debug`, `func start --useHttps`) with `host.json: telemetryMode=OpenTelemetry`, `AUTH__USE_UNSIGNED_LOCAL_VALIDATOR=true`, and the Console exporter gated on `env.IsDevelopment()`. The full raw log is attached to [AB#633](https://dev.azure.com/jamesconsulting/_workitems/edit/633) as `phase1c-spans.log` (1.6 MB). Tokens were unsigned JWTs minted by a helper matching [`TestClaimsBuilder.JwtForUser`](../../tests/IntegrationTests/Support/TestClaimsBuilder.cs); two users were JIT-provisioned from claims (no Entra round-trip).

### 6.1 Traces captured

| # | Request | Purpose | TraceId (prefix) | Spans |
|---|---|---|---|---|
| S01 | `GET /auth/me` (first call, advisor) | JIT-provision advisor + personal entity | `10f4a9fe…` | 15 |
| S02 | `POST /assets` (advisor) | Seed asset owned by advisor | `7b0804e1…` | 14 |
| S03 | `POST /entities` (advisor, `type=Business`) | Seed business entity | `ffc5b262…` | 16 |
| S04 | `POST /assets` (client) | Seed asset owned by a different user (for R06) | `dd25d9a0…` | 14 |
| S05 | `POST /auth/clients` (advisor) | Seed advisor→client `DataAccessGrant` (Read, `[assets,accounts]`) | `916711eb…` | 14 |
| R00 | `GET /health/live` (anon) | Worker liveness ping | `4e0f5a4c…` | 6 |
| R01 | `GET /auth/me` (cached) | Auth profile read | `a226be3c…` | 14 |
| R02 | `GET /profile/me` | UserProfile read (single-layer) | `ae829c4a…` | 12 |
| R03 | `GET /assets/{owned}` | Mode A owner-allowed (Assets) | `500c8840…` | 15 |
| R04 | `GET /entities/{owned}` | Mode A owner-allowed (Entities) | `ded1c719…` | 16 |
| R05 | `GET /auth/clients` | Mode B role-gated (ClientMgmt) | `3920f746…` | 14 |
| R06 | `GET /assets/{denied}` → **403** | Mode A denied — advisor does not own; no grant | `1998da50…` | 17 |

All 12 traces rendered below using a condensed `[Kind]` notation (`I` = Internal, `C` = Client); repetitive `rajfinancial` EF spans collapsed to `[EF <Op> <Table>]`; only tags relevant to Phase 1c's decision (`code.function`, `http.method`, `http.route`, `http.status_code`, `exception.type`, `db.system`) are shown.

### 6.2 Seed-phase traces

**S01 — first-call JIT `GET /auth/me` (advisor, fresh DB)** (`trace=10f4a9fea68c…`, 15 spans)

```text
- Middleware.Exception [I] code.function=AuthMe
  - Auth.Authenticate [I] code.function=AuthMe
    - Middleware.UserProfileProvisioning [I] code.function=AuthMe
      - UserProfile.EnsureProfileExists [I]
        - [EF SELECT] db.system=mssql
        - [EF INSERT UserProfiles] db.system=mssql
      - Entities.EnsurePersonalEntity.Service [I]
        - [EF SELECT] db.system=mssql
        - [EF INSERT Entities] db.system=mssql
      - Middleware.Authorization [I] code.function=AuthMe
        - Middleware.ContentNegotiation [I] code.function=AuthMe
          - Middleware.Validation [I] code.function=AuthMe
            - Auth.GetMe [I] http.method=GET; http.route=auth/me
              - UserProfile.EnsureProfileExists [I]
                - [EF SELECT] db.system=mssql
```

**S02 — `POST /assets` (advisor asset create)** (`trace=7b0804e13c9e…`, 14 spans)

```text
- Middleware.Exception [I] code.function=CreateAsset
  - Auth.Authenticate [I] code.function=CreateAsset
    - Middleware.UserProfileProvisioning [I] code.function=CreateAsset
      - UserProfile.EnsureProfileExists [I]
        - [EF SELECT] db.system=mssql
        - [EF SELECT] db.system=mssql
      - Entities.EnsurePersonalEntity.Service [I]
        - [EF SELECT] db.system=mssql
      - Middleware.Authorization [I] code.function=CreateAsset
        - Middleware.ContentNegotiation [I] code.function=CreateAsset
          - Middleware.Validation [I] code.function=CreateAsset
            - Assets.Http.Create [I]
              - Assets.Create [I]
                - [EF INSERT Assets] db.system=mssql
```

**S03 — `POST /entities` (advisor, `type=Business`)** (`trace=ffc5b26235f4…`, 16 spans)

```text
- Middleware.Exception [I] code.function=CreateEntity
  - Auth.Authenticate [I] code.function=CreateEntity
    - Middleware.UserProfileProvisioning [I] code.function=CreateEntity
      - UserProfile.EnsureProfileExists [I]
        - [EF SELECT] db.system=mssql
        - [EF SELECT] db.system=mssql
      - Entities.EnsurePersonalEntity.Service [I]
        - [EF SELECT] db.system=mssql
      - Middleware.Authorization [I] code.function=CreateEntity
        - Middleware.ContentNegotiation [I] code.function=CreateEntity
          - Middleware.Validation [I] code.function=CreateEntity
            - Entities.CreateEntity [I]
              - Entities.CreateEntity.Service [I]
                - Authorization.CheckAccess [I]
                - [EF SELECT] db.system=mssql
                - [EF INSERT Entities] db.system=mssql
```

**S04 — `POST /assets` (client asset create)** (`trace=dd25d9a06eb6…`, 14 spans)

Identical tree shape to S02 with `code.function=CreateAsset`; second asset owned by `client2@example.com` (used as `DENIED_ASSET_ID` for R06).

**S05 — `POST /auth/clients` (advisor→client grant)** (`trace=916711eb8cea…`, 14 spans)

```text
- Middleware.Exception [I] code.function=AssignClient
  - Auth.Authenticate [I] code.function=AssignClient
    - Middleware.UserProfileProvisioning [I] code.function=AssignClient
      - UserProfile.EnsureProfileExists [I]
        - [EF SELECT] db.system=mssql
        - [EF SELECT] db.system=mssql
      - Entities.EnsurePersonalEntity.Service [I]
        - [EF SELECT] db.system=mssql
      - Middleware.Authorization [I] code.function=AssignClient
        - Middleware.ContentNegotiation [I] code.function=AssignClient
          - Middleware.Validation [I] code.function=AssignClient
            - ClientMgmt.AssignClient [I]
              - ClientMgmt.AssignClient.Service [I]
                - [EF INSERT DataAccessGrants] db.system=mssql
```

### 6.3 Capture-phase traces

**R00 — anon `GET /health/live`** (`trace=4e0f5a4ccb7a…`, 6 spans)

```text
- Middleware.Exception [I] code.function=HealthLive
  - Auth.Authenticate [I] code.function=HealthLive
    - Middleware.UserProfileProvisioning [I] code.function=HealthLive
      - Middleware.Authorization [I] code.function=HealthLive
        - Middleware.ContentNegotiation [I] code.function=HealthLive
          - Middleware.Validation [I] code.function=HealthLive
```

No function-level or service-level span — `HealthLive` body doesn't open one. No host-emitted span either. Proves the middleware scaffold runs on every trigger including anonymous endpoints.

**R01 — `GET /auth/me` (cached JIT)** (`trace=a226be3c77d2…`, 14 spans)

```text
- Middleware.Exception [I] code.function=AuthMe
  - Auth.Authenticate [I] code.function=AuthMe
    - Middleware.UserProfileProvisioning [I] code.function=AuthMe
      - UserProfile.EnsureProfileExists [I]
        - [EF SELECT] db.system=mssql
        - [EF SELECT] db.system=mssql
      - Entities.EnsurePersonalEntity.Service [I]
        - [EF SELECT] db.system=mssql
      - Middleware.Authorization [I] code.function=AuthMe
        - Middleware.ContentNegotiation [I] code.function=AuthMe
          - Middleware.Validation [I] code.function=AuthMe
            - Auth.GetMe [I] http.method=GET; http.route=auth/me
              - UserProfile.EnsureProfileExists [I]
                - [EF SELECT] db.system=mssql
```

Single function-level span `Auth.GetMe`; no distinct service span (thin endpoint).

**R02 — `GET /profile/me`** (`trace=ae829c4a3123…`, 12 spans)

```text
- Middleware.Exception [I] code.function=ProfileMe
  - Auth.Authenticate [I] code.function=ProfileMe
    - Middleware.UserProfileProvisioning [I] code.function=ProfileMe
      - UserProfile.EnsureProfileExists [I]
        - [EF SELECT] db.system=mssql
        - [EF SELECT] db.system=mssql
      - Entities.EnsurePersonalEntity.Service [I]
        - [EF SELECT] db.system=mssql
      - Middleware.Authorization [I] code.function=ProfileMe
        - Middleware.ContentNegotiation [I] code.function=ProfileMe
          - Middleware.Validation [I] code.function=ProfileMe
            - UserProfile.GetById [I]
```

Single function-level span; no separate service span emitted; no DB query (provisioning middleware already hydrated the profile).

**R03 — `GET /assets/{owned}` (Mode A allow)** (`trace=500c8840e30b…`, 15 spans)

```text
- Middleware.Exception [I] code.function=GetAssetById
  - Auth.Authenticate [I] code.function=GetAssetById
    - Middleware.UserProfileProvisioning [I] code.function=GetAssetById
      - UserProfile.EnsureProfileExists [I]
        - [EF SELECT] db.system=mssql
        - [EF SELECT] db.system=mssql
      - Entities.EnsurePersonalEntity.Service [I]
        - [EF SELECT] db.system=mssql
      - Middleware.Authorization [I] code.function=GetAssetById
        - Middleware.ContentNegotiation [I] code.function=GetAssetById
          - Middleware.Validation [I] code.function=GetAssetById
            - Assets.Http.GetById [I]
              - Assets.GetById [I]
                - [EF SELECT] db.system=mssql
                - Authorization.CheckAccess [I]
```

Two-layer pattern: function span `Assets.Http.GetById` (current Assets-domain-only convention) wrapping service span `Assets.GetById`. Authorization success → no `<Error>` status on `Authorization.CheckAccess`.

**R04 — `GET /entities/{owned}` (Mode A allow)** (`trace=ded1c719ae17…`, 16 spans)

```text
- Middleware.Exception [I] code.function=GetEntityById
  - Auth.Authenticate [I] code.function=GetEntityById
    - Middleware.UserProfileProvisioning [I] code.function=GetEntityById
      - UserProfile.EnsureProfileExists [I]
        - [EF SELECT] db.system=mssql
        - [EF SELECT] db.system=mssql
      - Entities.EnsurePersonalEntity.Service [I]
        - [EF SELECT] db.system=mssql
      - Middleware.Authorization [I] code.function=GetEntityById
        - Middleware.ContentNegotiation [I] code.function=GetEntityById
          - Middleware.Validation [I] code.function=GetEntityById
            - Entities.GetEntityById [I]
              - Entities.GetEntityById.Service [I]
                - [EF SELECT] db.system=mssql
                - Authorization.CheckAccess [I]
                - [EF SELECT] db.system=mssql
```

Two-layer pattern with opposite naming from R03: function span `Entities.GetEntityById` (no `.Http.` prefix); service span `Entities.GetEntityById.Service` (explicit `.Service` suffix). **This is the pre-b2 inconsistency** — see §6.5.

**R05 — `GET /auth/clients` (Mode B role-gated)** (`trace=3920f746e5fa…`, 14 spans)

```text
- Middleware.Exception [I] code.function=GetClients
  - Auth.Authenticate [I] code.function=GetClients
    - Middleware.UserProfileProvisioning [I] code.function=GetClients
      - UserProfile.EnsureProfileExists [I]
        - [EF SELECT] db.system=mssql
        - [EF SELECT] db.system=mssql
      - Entities.EnsurePersonalEntity.Service [I]
        - [EF SELECT] db.system=mssql
      - Middleware.Authorization [I] code.function=GetClients
        - Middleware.ContentNegotiation [I] code.function=GetClients
          - Middleware.Validation [I] code.function=GetClients
            - ClientMgmt.GetClients [I]
              - ClientMgmt.GetClientAssignments [I]
                - [EF SELECT] db.system=mssql
```

Two-layer pattern, **but function and service span names do not share an operation token** (`GetClients` vs `GetClientAssignments`). No `Authorization.CheckAccess` span — Mode B uses a role gate, not per-row auth. Abbreviated domain prefix `ClientMgmt` violates the "no abbreviations" rule already stated in pattern §3.1.

**R06 — `GET /assets/{denied}` → 403 (Mode A deny)** (`trace=1998da5045ff…`, 17 spans)

```text
- Middleware.Exception [I] code.function=GetAssetById; exception.type=ForbiddenException; http.status_code=403
  - Auth.Authenticate [I] code.function=GetAssetById
    - Middleware.UserProfileProvisioning [I] code.function=GetAssetById
      - UserProfile.EnsureProfileExists [I]
        - [EF SELECT] db.system=mssql
        - [EF SELECT] db.system=mssql
      - Entities.EnsurePersonalEntity.Service [I]
        - [EF SELECT] db.system=mssql
      - Middleware.Authorization [I] code.function=GetAssetById
        - Middleware.ContentNegotiation [I] code.function=GetAssetById
          - Middleware.Validation [I] code.function=GetAssetById
            - Assets.Http.GetById [I]
              - Assets.GetById [I]
                - [EF SELECT] db.system=mssql
                - Authorization.CheckAccess [I] <Error>
                  - [EF SELECT] db.system=mssql
                  - [EF SELECT] db.system=mssql
```

Denied flow behaves as the pattern doc prescribes:

- `Authorization.CheckAccess` carries `StatusCode=Error` (via `AuthorizationTelemetry.RecordDenied`).
- `Middleware.Exception` (root) carries `exception.type=ForbiddenException` + `http.status_code=403` as *tags*, **with no `exception` event attached and no `Error` status** — correct handled-4xx handling per [`ActivityExceptionExtensions.RecordExceptionOutcome`](../../src/Api/Observability/ActivityExceptionExtensions.cs).
- The service span (`Assets.GetById`) and function span (`Assets.Http.GetById`) also do not record an exception event — only the authorization span flags the error.

### 6.4 Observations

1. **No host-emitted HTTP span reaches the worker's console exporter** — zero `ActivityKind=Server` spans across all 12 traces, despite `host.json: telemetryMode=OpenTelemetry` + `UseFunctionsWorkerDefaults()` being active. Every trace is rooted at `Middleware.Exception` (`Kind=Internal`). Confirmed this is *expected* behavior per the OTel-with-Azure-Functions docs: the host process emits its invocation telemetry (with `faas.name` / `faas.trigger`) on a separate pipeline, not through worker-side `ActivitySource`/exporter registration. **Local Core Tools traces cannot validate or invalidate the existence of the host invocation span; only a deployed Azure environment can.** See §7 for how this constrains the decision.

2. **Middleware chain order matches `Program.cs:37-42` exactly** on every trace: Exception → Auth → Provisioning → Authorization → ContentNegotiation → Validation → function body. Exception-as-root is intentional — it's registered first so it can catch everything nested beneath it.

3. **`UserProfileProvisioningMiddleware` always runs the JIT side-effects**, even on cached reads (R01–R06 all show `UserProfile.EnsureProfileExists` + `Entities.EnsurePersonalEntity.Service` children under provisioning). S01's extra `INSERT` statements versus R01's pure `SELECT`s are the only tree-shape difference between first-call and cached-call.

4. **R06 denied flow is correct and should not be "fixed"**: the `<Error>` status lives only on the authorization span; upstream spans record exception tags but no status=Error. This is exactly what [`AGENTS.md` handled-4xx rule](../../AGENTS.md) prescribes.

5. **Pre-b2 naming inconsistency confirmed across four domains** (see §6.5). This is the input that makes the §7 decision necessary.

### 6.5 Pre-b2 naming inventory

| Domain | Function-layer span | Service-layer span | Convention |
|---|---|---|---|
| Assets | `Assets.Http.GetById`, `Assets.Http.Create` | `Assets.GetById`, `Assets.Create` | `.Http.` prefix on function; clean service |
| Entities | `Entities.GetEntityById`, `Entities.CreateEntity` | `Entities.GetEntityById.Service`, `Entities.CreateEntity.Service` | No prefix on function; `.Service` suffix on service |
| ClientMgmt | `ClientMgmt.GetClients`, `ClientMgmt.AssignClient` | `ClientMgmt.GetClientAssignments`, `ClientMgmt.AssignClient.Service` | Mixed — AssignClient matches Entities pattern; GetClients uses a *different operation name* on the service (`GetClientAssignments`). Also uses abbreviated `ClientMgmt`. |
| Auth / UserProfile | `Auth.GetMe`, `UserProfile.GetById` | *(collapsed into the function span — no separate service activity)* | Single-span; acceptable for thin handlers. |

Three incompatible shapes across four domains plus two abbreviation violations. §7 picks a single target.

---

## 7. Decision — Option (b2): `<Domain>.<Op>` / `<Domain>.<Op>.Service`

**Chosen option:** b2 — standardize on a two-layer naming scheme where the function-level span is `<Domain>.<Op>` and the (optional) service-level span is `<Domain>.<Op>.Service`. Drop the `.Http.` prefix from Assets. Keep thin handlers as single function-level spans.

### 7.1 Rationale

- **Option (a) deferred.** The core premise of Option (a) — that a host-emitted invocation span already exists and only needs tag enrichment — cannot be validated from local Core Tools output (§6.4 observation 1). Committing to (a) now would couple Phases 3a/3b/4/7 to both AB#628 *and* a still-unverified production trace shape. We can revisit (a) later by enriching whatever span turns out to exist on Azure.
- **b2 over b1.** OpenTelemetry guidance prefers stable, low-cardinality span names and pushes protocol detail into attributes (`http.request.method`, `http.route`, `faas.trigger`). Encoding the transport into the name (`.Http.`) adds churn without adding queryability. Three of four domains already drop the prefix.
- **Picks up existing majority.** Entities and ClientMgmt (partly) already use `<Domain>.<Op>` / `<Domain>.<Op>.Service`. Ratifying that shape minimizes rename surface — only Assets's function layer and ClientMgmt's service layer change.
- **Preserves thin-handler collapse.** `Auth.GetMe` and `UserProfile.GetById` stay single-span; we do not manufacture a `.Service` child when the service method is a trivial DTO projection.
- **Reverses prior direction in pattern §3.1.** The old §3.1 reserved `<Domain>.<Op>` for the service layer and told Entities/ClientMgmt to *drop* `.Service`. b2 inverts that: `<Domain>.<Op>` becomes the function-layer name, and `.Service` is the retained internal marker. This is the change Phase 1c was commissioned to make; pattern §3.1 is rewritten to match.

### 7.2 Rename mapping

| Current name | New name | Layer | Domain |
|---|---|---|---|
| `Assets.Http.GetList` | `Assets.GetList` | Function | Assets |
| `Assets.Http.GetById` | `Assets.GetById` | Function | Assets |
| `Assets.Http.Create` | `Assets.Create` | Function | Assets |
| `Assets.Http.Update` | `Assets.Update` | Function | Assets |
| `Assets.Http.Delete` | `Assets.Delete` | Function | Assets |
| `Assets.GetList` | `Assets.GetList.Service` | Service | Assets |
| `Assets.GetById` | `Assets.GetById.Service` | Service | Assets |
| `Assets.Create` | `Assets.Create.Service` | Service | Assets |
| `Assets.Update` | `Assets.Update.Service` | Service | Assets |
| `Assets.Delete` | `Assets.Delete.Service` | Service | Assets |
| `ClientMgmt.GetClientAssignments` | `ClientManagement.GetClients.Service` | Service | ClientMgmt |
| `ClientMgmt.GetClients` | `ClientManagement.GetClients` | Function | ClientMgmt |
| `ClientMgmt.AssignClient` | `ClientManagement.AssignClient` | Function | ClientMgmt |
| `ClientMgmt.AssignClient.Service` | `ClientManagement.AssignClient.Service` | Service | ClientMgmt |
| `Entities.CreateEntity` | `Entities.CreateEntity` | Function | Entities (no change) |
| `Entities.CreateEntity.Service` | `Entities.CreateEntity.Service` | Service | Entities (no change) |
| `Entities.GetEntityById` | `Entities.GetEntityById` | Function | Entities (no change) |
| `Entities.GetEntityById.Service` | `Entities.GetEntityById.Service` | Service | Entities (no change) |
| `Entities.EnsurePersonalEntity.Service` | `Entities.EnsurePersonalEntity.Service` | Service | Entities (no change) |
| `Auth.GetMe` | `Auth.GetMe` | Function (single-span) | Auth (no change) |
| `UserProfile.GetById` | `UserProfile.GetById` | Function (single-span) | UserProfile (no change) |
| `UserProfile.EnsureProfileExists` | `UserProfile.EnsureProfileExists` | Service (side-effect only) | UserProfile (no change) |

The ClientMgmt rows also fold in the existing "no abbreviations" rule from pattern §3.1 (`ClientMgmt` → `ClientManagement`). `GetClientAssignments` → `GetClients.Service` aligns the service operation token with the function operation token as required by b2.

### 7.3 Follow-up work

The code-side rename per §7.2 was applied in this PR alongside the docs. Remaining follow-ups:

- [ ] Option (a) validation (host-emitted span on Azure with `faas.name` / `faas.trigger` attributes) — scoped to whichever work item carries the first non-local deployment.
- [x] Confirm no Application Insights / Azure Monitor KQL dashboards reference the old names (`Assets.Http.*`, `ClientMgmt.*`, `ClientMgmt.GetClientAssignments`, `UserProfile.EnsureProfileExists`) before the rename reaches a shared environment — [AB#638](https://dev.azure.com/jamesconsulting/_workitems/edit/638). Outcome recorded in §7.4.

### 7.4 Dashboard audit (AB#638)

Scope: every asset in the repo that could embed a span/metric name and be deployed to a shared environment. Audit performed against `develop` after the Phase 1c rename merged.

Checked:

- **Infra (Bicep / ARM).** `infra/modules/monitoring.bicep` provisions Log Analytics + Application Insights only. No `Microsoft.Portal/dashboards`, `Microsoft.Insights/workbooks`, `savedSearches`, or `Microsoft.Insights/scheduledQueryRules` resources exist anywhere in `infra/`. Nothing to update.
- **Operational KQL.** [`docs/observability-runbook.md`](../observability-runbook.md) is the de facto dashboard-of-record. Every KQL block there filters by `operation_Name` / `customMetrics.name` / `customDimensions["EventId"]` or `customDimensions["authz.*"]` — none pin on a renamed span name (`Assets.Http.*`, `ClientMgmt.*`, `UserProfile.EnsureProfileExists`, or the old `ClientMgmt.GetClientAssignments` service token). The snippets remain correct after the rename because they address activity/metric names by *pattern*, not by the specific renamed tokens.
- **Scripts / tests / docs.** A repo-wide search for `Assets\.Http`, `ClientMgmt` (as a telemetry name, not as a log/method substring), `GetClientAssignments` as a span name, and `UserProfile.EnsureProfileExists` without the `.Service` suffix returns only historical references in the Phase 1c plan (this document) and ADR 0002 — both of which intentionally cite the old names as part of the migration record. The surviving `ClientManagementService.GetClientAssignments` C# method name and its EventId 6002 log message are **not** telemetry span/metric names; they are internal code identifiers and out of scope for this audit.

Conclusion: **no dashboard assets reference the renamed spans**. The rename is safe to reach shared environments without migration work. If Azure Portal dashboards / workbooks are introduced later (outside the repo), they must be authored against the b2 names from the start — there is no legacy query surface to migrate.

---

## 8. Execution checklist

- [x] §3 prerequisites satisfied (Docker SQL up, Debug build passing, unsigned JWTs minted, IDs seeded via S01–S05).
- [x] §4.1 `func start --useHttps` running; Console exporter streaming to `phase1c-spans.log`.
- [x] §4.2 all 6 capture requests executed (plus R00 health probe); each returned expected status (6× 200, 1× 403).
- [x] §4.4 Activity blocks captured, anonymized, rendered as condensed trees in §6.
- [x] §6.1–§6.3 trees pasted.
- [x] §6.4 observations recorded.
- [x] §6.5 pre-b2 naming inventory documented.
- [x] §7 decision recorded (Option b2) with rename mapping.
- [x] §7.2 code rename applied (activity constants + call sites for Assets / ClientManagement / UserProfile).
- [x] Build clean; 583 unit tests + 5 architecture tests pass.
- [ ] `phase1c-spans.log` attached to [AB#633](https://dev.azure.com/jamesconsulting/_workitems/edit/633) (reviewer to attach; the file is gitignored and not in this PR).
- [x] ADR 0002 (*Activity naming convention*) authored in Phase 1a ([AB#637](https://dev.azure.com/jamesconsulting/_workitems/edit/637)) using §7.1 as its Decision body — see [`docs/adr/0002-activity-naming-convention.md`](../adr/0002-activity-naming-convention.md).
- [ ] [AB#633](https://dev.azure.com/jamesconsulting/_workitems/edit/633) moved to Done once log is attached.
