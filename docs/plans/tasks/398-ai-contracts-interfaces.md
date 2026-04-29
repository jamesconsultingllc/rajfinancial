# Task #398 — AI Contracts & Interfaces

**Parent:** Feature #396 — AI Provider Integration (Microsoft.Extensions.AI)
**Epic:** #393 — 09 - AI Platform (Providers, Tools, MCP)
**Branch:** `feature/396-ai-provider-integration`
**Status:** In Progress
**Author / agent:** Codex via OpenClaw, 2026-04-26

## Why this task

Foundation for the AI Platform. Establish the abstractions every later piece (B1 Tool Calling Host, B2 Safety Invariants, B3 MCP Server, vertical AI Features) will depend on. No runtime behavior, no DI registration, no providers — just shape. Lets #397 (factory) and #545 (Claude adapter) be built and reviewed independently against a stable contract.

This is **step 1 of plan §F**:
> Foundation PR — interfaces → factory → Claude adapter. One cohesive change, gets `IChatClient` into DI. **Step 1 explicitly excludes registering any AIFunction or exposing any user-facing AI surface.**

## Scope

**In scope:**
- Configuration POCOs in `src/Shared/Contracts/Ai/`:
  - `AiOptions` — root configuration POCO bound from `appsettings:Ai` section.
  - `AiProviderOptions` — per-provider config (model, API-key env-var reference, base URL override).
  - `AiProviderId` — enum of supported providers. Initial values: `Anthropic`. (Open future: `AzureOpenAI`, `OpenAI`, `Gemini`, `Ollama`, `DeepSeek`.)
- Factory contract in `src/Api/Services/Ai/Abstractions/`:
  - `IChatClientFactory` — resolves an `IChatClient` for a named provider. Lives in `Api` (not `Shared`) because it returns the `Microsoft.Extensions.AI.IChatClient` type, and `Shared` is multi-targeted (`net9.0;net10.0`) and intentionally free of MEAI dependencies. See §1 below.
- XML doc comments on every public type — these become the spec for downstream tasks.
- A single `README.md` in `src/Shared/Contracts/Ai/` that links back to the umbrella plan and this task plan.

**Out of scope (explicitly NOT in #398):**
- No `ChatClientFactory` implementation (that's #397).
- No Anthropic / Claude provider wiring (#545).
- No `IAiToolRegistry` / `AIFunction` surface (B1, Feature #648).
- No `IChatClient` middleware for citations / scope / preview-confirm (B2, Feature #649).
- No DI extension methods (`AddRajFinancialAi`) — those land with #397 once there's something to register.
- No HTTP endpoints, no Functions, no client-facing surface.

## Design choices

### 1. Config POCOs in `Shared`, factory contract in `Api`

The split is deliberate:

- **Config POCOs (`AiOptions`, `AiProviderOptions`, `AiProviderId`) → `src/Shared/Contracts/Ai/`.** They have zero AI runtime deps. Putting them in `Shared` lets `tests/Api.Tests/` and any future test project bind/mock them without dragging in Api internals, and matches the pattern already used for entity contracts in `src/Shared/Contracts/`.
- **`IChatClientFactory` → `src/Api/Services/Ai/Abstractions/`.** It returns `Microsoft.Extensions.AI.IChatClient`. Putting it in `Shared` would force `Microsoft.Extensions.AI.Abstractions` to be a dependency of the multi-targeted (`net9.0;net10.0`) `Shared` project, polluting every consumer of `Shared` — including tests and any non-AI feature project — with the MEAI surface. Keeping the factory contract in `Api` confines the MEAI dependency to the project that actually uses AI.

Downstream tasks (#397 factory implementation, #545 Anthropic adapter, B1/B2/B3 Features) implement against `RajFinancial.Api.Services.Ai.Abstractions.IChatClientFactory` and bind config from `RajFinancial.Shared.Contracts.Ai.*`.

### 2. Use `Microsoft.Extensions.AI` abstractions, don't redefine

We do **not** define our own `IAiChat`, `ChatMessage`, etc. The whole point of MEAI is provider-agnosticism — redefining its types would be a layer of indirection with no value. Our contracts are the **factory** and the **configuration**, not the chat surface itself.

Concretely: `IChatClientFactory.GetClient(...)` returns `Microsoft.Extensions.AI.IChatClient`. Callers use MEAI's standard surface from there.

### 3. `AiProviderId` as enum vs string

Picking **enum** (`AiProviderId.Anthropic`) for v1, with the understanding it converts to/from string for config binding. Reasons:
- Compile-time safety in factory dispatch (switch expression with completeness analysis).
- IDE autocomplete for callers.
- BYOK / user-configurable providers (when that feature lands) can layer a string-keyed lookup on top of the enum-based built-in dispatch — the enum doesn't preclude flexibility.

If the BYOK Feature (#551) reveals a need for fully-dynamic provider IDs, the enum can be deprecated then. For now, enum is the simpler default.

### 4. Configuration shape

```jsonc
// appsettings.Development.json (illustrative — not added in this task)
{
  "Ai": {
    "DefaultProvider": "Anthropic",
    "Providers": {
      "Anthropic": {
        "Model": "claude-sonnet-4-5",
        "ApiKeyEnvVar": "ANTHROPIC_API_KEY",   // foundation-PR temp; real BYOK is #551
        "BaseUrl": null                          // null → SDK default
      }
    }
  }
}
```

`AiOptions` binds to `Ai`. `AiProviderOptions` binds to one entry under `Ai:Providers`. Key resolution **does not** happen in the contract types — that's a factory concern (#397) and a BYOK concern (#551).

### 5. Why no `IAiInsightsService` here

The original ADO task title mentions `IAiInsightsService`. That name belongs to the chat surface / insights UI (Feature #394), not to the platform foundation. Defining it here would either (a) leak UI concerns into platform contracts, or (b) define an empty marker interface nobody uses.

**Decision: drop `IAiInsightsService` from this task.** When #394 lands (sequenced after B1+B2 per plan §G), it can define its own service interface scoped to the chat/insights use case. This task delivers the platform contracts only.

I'll update the ADO task description to reflect this.

## AC mapping

| AC | How met |
|---|---|
| Interface contracts exist for AI provider resolution | `IChatClientFactory` in `src/Api/Services/Ai/Abstractions/` |
| Configuration POCOs are in a project both Api and tests can reference | `src/Shared/Contracts/Ai/` |
| Configuration shape is bindable from `appsettings` | `AiOptions` + `AiProviderOptions` POCOs with parameterless ctors and settable properties |
| No runtime behavior introduced | Zero implementations; nothing in DI |
| Foundation does not expose user-facing AI surface | No endpoints, no Functions, no AIFunction registrations — verified by grep in PR description |

## Test plan

- Build must remain green (zero new warnings, zero errors).
- No new tests in this task — there's nothing to test in pure interfaces and POCOs. Tests land with #397 (factory resolution behavior).
- Architecture.Tests: if there's a "no public types in Shared without XML doc" rule, our types must comply. Will verify before commit.

## Files added

```
src/Shared/Contracts/Ai/
  AiOptions.cs
  AiProviderOptions.cs
  AiProviderId.cs
  README.md

src/Api/Services/Ai/Abstractions/
  IChatClientFactory.cs
```

Plus the umbrella plan committed once on this branch:

```
docs/plans/2026-04-26-ado-restructure-ai-platform.md
docs/plans/tasks/398-ai-contracts-interfaces.md   # this file
```

## Risks / open questions

- **MEAI package versions on net9.0 vs net10.0** — `Shared` is multi-targeted, and we want it to stay free of MEAI. Decision (locked): the factory contract `IChatClientFactory` lives in `Api/Services/Ai/Abstractions/` (public, mockable from `Api.Tests` directly — they reference `Api`); only the config POCOs go in `Shared/Contracts/Ai/`. See §1 above.
- **Anthropic SDK package availability for net10.0** — verified during #545, not this task.

## Done when

- All files in `src/Api/Services/Ai/Abstractions/` and `src/Shared/Contracts/Ai/` compile.
- `dotnet build RajFinancial.sln` succeeds with no new warnings.
- `dotnet test` passes (no new tests, but full suite must stay green).
- ADO #398 description updated to reflect `IAiInsightsService` removal.
- This plan attached to ADO #398.
