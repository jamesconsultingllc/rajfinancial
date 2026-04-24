# Phase 1c capture — Insomnia / `inso` CLI

Paired artifact for [`../phase-1c-span-validation.md`](../phase-1c-span-validation.md).
This folder ships:

- `phase-1c.insomnia.yaml` — committable Insomnia workspace named
  `Phase 1c Span Capture` (this is the string you pass to
  `inso run collection`). It contains a `Phase 1c Capture` folder holding
  the 6 capture requests: one per primary API domain
  (Auth/UserProfile/Assets/Entities/ClientManagement), plus one
  intentionally-denied request that also exercises Authorization +
  middleware. A `00 Healthcheck` worker-liveness ping sits at the
  workspace top level, outside that folder — 7 requests total. The
  healthcheck is not pasted into §6 of the span-validation doc.
- `README.md` — this file. How to run the capture via `inso` (or the GUI)
  without leaking secrets.

The requests hit the locally-running Functions worker (Dev-only OTel **Console**
exporter, see [`ObservabilityRegistration.cs`](../../../src/Api/Configuration/ObservabilityRegistration.cs)).
Dev / prod environments route spans to Application Insights and are out of
scope for 1c.

---

## 1. Prerequisites

1. Functions Core Tools (`func --version` ≥ 4.x).
2. `inso` CLI — `npm i -g insomnia-inso` (or download the standalone binary
   from the Insomnia releases page).
3. Everything already called out in
   [`phase-1c-span-validation.md`](../phase-1c-span-validation.md#3-prerequisites):
   DB container up, `src/Api/local.settings.json` configured,
   `ASPNETCORE_ENVIRONMENT=Development` (+ `AZURE_FUNCTIONS_ENVIRONMENT=Development`
   for code paths that check it), build succeeds, a seed user + owned asset +
   owned entity + one client assignment exist.

   Both env vars must be available to the worker, either by setting them in the
   shell before starting `func` or by adding them under
   `src/Api/local.settings.json` → `Values`. Without these,
   `AuthenticationMiddleware` rejects unsigned JWTs and every authenticated
   request returns `401 AUTH_REQUIRED`.
4. A bearer token minted the same way `tests/IntegrationTests/Support/TestAuthHelper.cs`
   mints one (local unsigned JWT for localhost, or `RopcTokenProvider` for
   Entra).

## 2. Populate your private env file

The committed workspace has blank values for `bearer`, `asset_id`, `entity_id`,
and `denied_asset_id` — on purpose. Put your real values in a **private**
Insomnia sub-env that never gets committed.

### Option A — in the Insomnia GUI

1. `File → Import` → select `phase-1c.insomnia.yaml`.
2. Manage Environments → Local → set `bearer`, `asset_id`, `entity_id`,
   `denied_asset_id`.
3. The sub-env is marked `isPrivate: true`; Insomnia keeps it out of exports.

### Option B — with `inso` on the command line

`inso` reads the workspace from the Insomnia app data dir (`~/.config/Insomnia`
on Linux / `%APPDATA%\Insomnia` on Windows). After importing once via the GUI
the CLI can drive it directly. For fully scripted runs, point
`-w` / `--workingDir` at the **directory** that contains
`phase-1c.insomnia.yaml` and any private env override — for this repo that
directory is `docs/plans/phase-1c/`. Pass the folder path, not the YAML file
path. Note: the repo root `.gitignore` rule `/docs/Insomnia_*.yaml`
matches only files directly under `/docs/` (not under
`docs/plans/phase-1c/`), so place any private override under `/docs/`
(e.g. `docs/Insomnia_local.yaml`) to ensure it stays gitignored — or
broaden the rule to `docs/**/Insomnia_*` before putting overrides in
subdirectories.

## 3. Run the capture

Two terminals:

```powershell
# Terminal 1 — worker + stdout tail
cd <repo-root>/src/Api
func start --useHttps | Tee-Object -FilePath ..\..\phase1c-spans.log
```

Wait for `Host started` and `Functions:` listing.

Requests run in sortKey order: `00 Healthcheck` → `01 Auth` → `02 UserProfile`
→ `03 Assets` → `04 Entities` → `05 ClientManagement` → `06 Denied`.

Two ways to feed the bearer + ids without leaking secrets into shell history:

- **Preferred — populate the private `Local` sub-env once via the GUI import**
  (see §2, Option A). `inso run collection` will read them from there and
  nothing sensitive appears on the command line.
- **Scripted** — export shell env vars up front, then let `inso` interpolate
  them into `--env-var`:

  ```powershell
  # Terminal 2 — set once per shell, never echo to logs
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
    --ci `
    -w docs\plans\phase-1c `
    | Tee-Object -FilePath phase1c-requests.log
  ```

  `Get-Credential` hides the token entry from the shell prompt so it never
  hits PSReadLine history (works on both Windows PowerShell 5.1 and PS 7+).
  Unset the vars (`Remove-Item Env:BEARER,Env:ASSET_ID,Env:ENTITY_ID,Env:DENIED_ASSET_ID`)
  when you're done.

Alternatively, step through each request in the Insomnia GUI (right-click →
Send) and read the stdout tail — useful if you want to inspect each trace in
isolation.

### `inso` quirks (v12.5.0)

- **Collection identifier** passed to `run collection` is the top-level `name`
  (`Phase 1c Span Capture`), not the `Phase 1c Capture` folder inside.
- A workspace YAML must contain at least one top-level (non-folder) request
  for `inso` to recognize it as a workspace — that's the purpose of the
  `00 Healthcheck` request. Don't remove it.
- `inso` aborts the batch on the first network timeout regardless of
  `--bail=false`. That's fine against a live worker; against a cold/missing
  worker you'll only see the first request attempted.
- On Windows, if `npm i -g insomnia-inso` fails on the `@kong/insomnia-plugin-*`
  native builds, download the standalone `inso-windows-<ver>.zip` from
  [Kong/insomnia releases](https://github.com/Kong/insomnia/releases) (tag
  `core@<ver>`) and invoke `inso.exe` directly.

## 4. Paste into the scaffold

- Copy the full stdout block for each request into §6 of
  [`../phase-1c-span-validation.md`](../phase-1c-span-validation.md#6-capture-paste-here-during-execution).
- Fill in §7 (decision block) of `../phase-1c-span-validation.md` and
  §3.1 (Activity names — HTTP span TBD) of
  [`../../patterns/service-function-pattern.md`](../../patterns/service-function-pattern.md#31-activity-names).
- Attach `phase1c-spans.log` to ADO #633.

## 5. Secrets hygiene

- Real tokens go only in the private Local sub-env (or a shell env var).
- Never `Save All` + export an Insomnia workspace that contains the populated
  bearer back into this folder — the repo root `.gitignore` blocks
  `/docs/Insomnia_*.yaml`, but a misplaced paste into this file would bypass
  that.
- If a token leaks anyway, revoke the Entra app registration credentials and
  any dev-user passwords rather than trying to scrub git history.
