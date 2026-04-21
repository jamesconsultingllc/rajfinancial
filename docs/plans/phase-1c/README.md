# Phase 1c capture — Insomnia / `inso` CLI

Paired artifact for [`../phase-1c-span-validation.md`](../phase-1c-span-validation.md).
This folder ships:

- `phase-1c.insomnia.yaml` — committable Insomnia workspace with a single
  collection (`Phase 1c Capture`) holding one request per
  [`ObservabilityDomain`](../../../src/Api/Configuration/ObservabilityDomains.cs)
  plus one intentionally-denied request.
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
the CLI can drive it directly. For fully scripted runs, point `--workingDir`
at a cloned directory that contains both the YAML and your private env
override (gitignored per `/docs/Insomnia_*.yaml` in the repo root
`.gitignore`).

## 3. Run the capture

Two terminals:

```powershell
# Terminal 1 — worker + stdout tail
cd E:\rajfinancial\src\Api
func start --useHttps | Tee-Object -FilePath ..\..\phase1c-spans.log
```

Wait for `Host started` and `Functions:` listing.

```powershell
# Terminal 2 — fire the 6 requests
inso run collection "Phase 1c Capture" --env Local | Tee-Object -FilePath phase1c-requests.log
```

Alternatively, step through each request in the Insomnia GUI (right-click →
Send) and read the stdout tail — useful if you want to inspect each trace in
isolation.

## 4. Paste into the scaffold

- Copy the full stdout block for each request into §6 of
  [`../phase-1c-span-validation.md`](../phase-1c-span-validation.md#6-captured-traces).
- Fill in §7 (decision block) and §4 of the `service-function-pattern.md`
  cross-ref note.
- Attach `phase1c-spans.log` to ADO #633.

## 5. Secrets hygiene

- Real tokens go only in the private Local sub-env (or a shell env var).
- Never `Save All` + export an Insomnia workspace that contains the populated
  bearer back into this folder — the repo root `.gitignore` blocks
  `/docs/Insomnia_*.yaml`, but a misplaced paste into this file would bypass
  that.
- If a token leaks anyway, revoke the Entra app registration credentials and
  any dev-user passwords rather than trying to scrub git history.
