# AB#545 — Anthropic ChatClientProvider + AI foundation wiring

## Problem

Foundation PR train step 3 of 3. After #398 (contracts) and #397 (factory), the
`AddRajFinancialAi(...)` extension is **defined but not invoked** and there are
**zero `IChatClientProvider` registrations**. AB#545 lands the first concrete provider
(Anthropic Claude), wires the DI extension into `Program.cs`, and proves the foundation
works end-to-end via unit tests only — no `AIFunction`, no chat endpoint, no client UI.

Per umbrella plan §F: **"Step 1 explicitly excludes registering any AIFunction or exposing
any user-facing AI surface."** This plan respects that gate.

## Scope

### In scope

- New provider: `AnthropicChatClientProvider : IChatClientProvider` wrapping `Anthropic.SDK`
  (tghamm), exposing the underlying client as MEAI `IChatClient`.
- Wire `services.AddRajFinancialAi(builder.Configuration)` in `Program.cs` /
  `ApplicationServicesRegistration`, plus singleton registration of the new provider.
- Add `RajFinancial.Api.Ai` to `ObservabilityDomains.All` so the MEAI OpenTelemetry
  decorator's traces/metrics flow into our existing OTel pipeline.
- Wrap the SDK client with `.UseOpenTelemetry(...)` from `Microsoft.Extensions.AI` so
  every `IChatClient` call gets standard activity + token-usage metrics for free.
- Configuration: `Ai` section added to `appsettings.json` (defaults), with env-var-only
  secrets (`ANTHROPIC_API_KEY`). Document in `local.settings.json.example`.
- Unit tests for `AnthropicChatClientProvider` (env-var resolution, BaseUrl override,
  null/empty guards, provider Id correctness).
- Round-trip test: factory dispatches to the new provider when configured for `Anthropic`.
- Architecture test: provider implementations live under `src/Api/Services/Ai/Providers/`
  and are sealed.

### Out of scope (explicit non-goals)

- Any `AIFunction` registration, tool-calling host, or chat HTTP endpoint (B1).
- Safety middleware, citation enforcement, structured-output gates (B2).
- BYOK / Key Vault / per-user API keys (#551). Foundation uses env-var only.
- Streaming, prompt-caching, MCP — all available in the SDK but not exposed yet.
- Production health-check probe of the live AI endpoint. Adding a probe that calls
  Anthropic on every `/health/ready` would burn quota and create an external dependency
  for liveness. Defer until B1 introduces user-facing AI traffic.
- Functional/integration tests against the real Anthropic API. Foundation is
  unit-tested per umbrella plan §F.

## Approach

### Files to create

| File | Purpose |
|---|---|
| `src/Api/Services/Ai/Providers/AnthropicChatClientProvider.cs` | `IChatClientProvider` impl. Resolves `ApiKeyEnvVar` at `CreateClient` time, constructs `Anthropic.SDK` client, wraps with MEAI OTel decorator, returns as `IChatClient`. Sealed, internal. |
| `src/Api/Services/Ai/Providers/AnthropicChatClientProvider.Logging.cs` | Source-gen `[LoggerMessage]` partials. EventId range 8020–8029 (within reserved Ai domain 8000–8999, distinct from factory's 8001–8010). |
| `tests/Api.Tests/Services/Ai/AnthropicChatClientProviderTests.cs` | Unit tests — see Test Plan below. |

### Files to modify

| File | Change |
|---|---|
| `src/Api/RajFinancial.Api.csproj` | Add `<PackageReference Include="Anthropic.SDK" Version="..." />` (latest 5.x stable). Keep deterministic — pin to a specific version, not a range. |
| `src/Api/Configuration/ApplicationServicesRegistration.cs` | Call `services.AddRajFinancialAi(configuration)`. Register `AnthropicChatClientProvider` as `IChatClientProvider` singleton. |
| `src/Api/Configuration/ObservabilityDomains.cs` | Add `Ai = "RajFinancial.Api.Ai"` constant + append to `All`. |
| `src/Api/appsettings.json` | Add `Ai:DefaultProvider = "Anthropic"`, `Ai:Providers:Anthropic:Model = "claude-sonnet-4-5"`, `Ai:Providers:Anthropic:ApiKeyEnvVar = "ANTHROPIC_API_KEY"`. Non-secret defaults only — no key value. |
| `src/Api/local.settings.json.example` | Document `ANTHROPIC_API_KEY` under `Values` so contributors know how to seed it locally. |
| `src/Shared/Contracts/Ai/README.md` | Mark #545 done; note `AddRajFinancialAi` is now invoked from `Program.cs`. |
| `src/Shared/Contracts/Ai/AiProviderId.cs` | Update XML comment from "Task #545" wording (no enum value change — `Anthropic = 1` already exists). |
| `tests/Architecture.Tests/...` | Add tests: providers under `Services/Ai/Providers` namespace, sealed, implement `IChatClientProvider`. |

### Why a separate `Providers/` folder

Existing `Services/Ai/` holds factory + abstractions + options validator (provider-agnostic
plumbing). Each concrete provider lives in `Services/Ai/Providers/<Name>/` so the directory
listing communicates extensibility — adding OpenAI/Azure-OpenAI later is a sibling folder,
not a sprinkle of `OpenAIChatClientProvider.cs` next to the factory.

### OpenTelemetry decoration

Per AGENTS.md / observability rules, wrap the raw SDK `IChatClient` with MEAI's
`UseOpenTelemetry(sourceName: ObservabilityDomains.Ai)` so:

- Token-usage histograms are emitted with our domain source name, not the MEAI default.
- Existing `ObservabilityRegistration` picks up the source via `ObservabilityDomains.All`.
- The provider does **not** declare its own `ActivitySource` — MEAI's decorator
  is the single source of AI telemetry truth, matching how we treat HTTP/SQL via
  upstream auto-instrumentation packages.

### Key resolution path (security)

```
AiOptions:Providers:Anthropic:ApiKeyEnvVar  →  "ANTHROPIC_API_KEY"
                                                       ↓
                                Environment.GetEnvironmentVariable(name)
                                                       ↓
                                       AnthropicClient(apiKey: value)
```

- The factory **never** sees the secret value, only the env-var name (already config-public).
- Throws `InvalidOperationException` (per `IChatClientProvider` contract) if env var is unset
  or empty. Message names the env var — never logs the value.
- Future BYOK (#551) replaces `ApiKeyEnvVar` resolution with a Key Vault / per-user lookup
  inside this provider, no callers change.

## Test plan

Per umbrella plan §F, foundation validation is unit tests only.

### `AnthropicChatClientProviderTests` (new, ~10 cases)

- `Id_returns_Anthropic`
- `CreateClient_with_valid_options_returns_non_null_IChatClient`
- `CreateClient_throws_when_ApiKeyEnvVar_is_missing_from_environment`
- `CreateClient_throws_when_ApiKeyEnvVar_is_set_to_empty_string`
- `CreateClient_does_not_log_the_api_key_value` (assert against `FakeLogger` content)
- `CreateClient_honours_BaseUrl_override_when_provided`
- `CreateClient_uses_default_BaseUrl_when_not_provided`
- `CreateClient_returns_an_IChatClient_decorated_with_OTel` (assert source name = `RajFinancial.Api.Ai` via reflection on the wrapper, or by starting an `ActivityListener` on that source and confirming it observes the wrapper)
- `CreateClient_options_null_throws_ArgumentNullException`
- `CreateClient_model_empty_throws_InvalidOperationException`

Use `Environment.SetEnvironmentVariable` per-test with proper teardown. Tests run sequentially
within their collection (xUnit `[Collection]`) to avoid env-var cross-talk.

### Factory round-trip test (extend existing `ChatClientFactoryTests`)

- Add 1 case: factory configured with `[AnthropicChatClientProvider]` and matching options
  for `AiProviderId.Anthropic` returns the provider's client unchanged.

### Architecture tests (new, in `tests/Architecture.Tests/`)

- Every type implementing `IChatClientProvider` resides in
  `RajFinancial.Api.Services.Ai.Providers` namespace.
- Every type implementing `IChatClientProvider` is `sealed`.
- Every type implementing `IChatClientProvider` has a single public constructor.

## Acceptance criteria

- [ ] `dotnet build src/RajFinancial.sln` clean (no new warnings).
- [ ] `dotnet test tests/Api.Tests` — all green, including new `AnthropicChatClientProviderTests`.
- [ ] `dotnet test tests/Architecture.Tests` — all green, including new provider-shape rules.
- [ ] `dotnet test tests/IntegrationTests` — still green (no functional change to integration surface).
- [ ] Host startup with valid `Ai` section + `ANTHROPIC_API_KEY` set: success, no
      AI calls made until B1 ships.
- [ ] Host startup with `Ai:Providers:Anthropic:ApiKeyEnvVar` naming an unset env var: 
      `ValidateOnStart` still succeeds (shape OK); first `GetClient(Anthropic)` call throws
      `InvalidOperationException` with actionable message. (No proactive eager probe per scope.)
- [ ] Host startup with `Ai:Providers` empty: `OptionsValidationException` from `ValidateOnStart`
      with the existing `AiOptionsValidator` message. (Pre-existing; verify still fires after wiring.)
- [ ] No secret values appear in logs at any level (`FakeLogger` assertion + manual
      `func start` smoke during dev verification).
- [ ] OTel pipeline picks up `RajFinancial.Api.Ai` source — confirmed via Console exporter
      in dev when a test explicitly invokes the decorated client.

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| `Anthropic.SDK` major version churn breaks our adapter | Pin to specific stable 5.x version. Adapter is one ~80 LOC file; rewriting against a future version is bounded. |
| MEAI `UseOpenTelemetry` API changes | We're already on `Microsoft.Extensions.AI.Abstractions` 10.5.0; pin the implementation package to the same minor. |
| Env-var leak via SDK's own logging | SDK doesn't log secrets by default. Add architecture test that `AnthropicChatClientProvider` does not pass an `ILogger` into the SDK constructor (foundation has no need to). |
| Someone wires up an `AIFunction` / smoke endpoint "just to test it" | Code review gate. Plan and PR description both call this out as a B1 deliverable, not foundation. |
| Test env-var pollution across cases | Use a single `[Collection("AnthropicEnv")]` xUnit collection + `IDisposable` per-test teardown that restores prior value. |

## Verification sequence (during implementation)

1. Add package, build → green
2. Add provider class (no DI) + unit tests → run unit tests → green
3. Wire in `ApplicationServicesRegistration` → build → green
4. Add architecture tests → run → green
5. `dotnet test` full suite locally → green
6. `func start` smoke — confirm no AI traffic at boot, host listens, `/health/ready` 200
7. Commit, push, open PR targeting `develop`

## Open questions

None — all resolved in conversation:

- ✅ First provider = Anthropic Claude (per original AB#545 plan)
- ✅ SDK = `Anthropic.SDK` (tghamm) — best documented, production-tested
- ✅ No eager startup probe of the live API (umbrella §F gate)
- ✅ No HTTP endpoint, no `AIFunction` (umbrella §F gate)

## Follow-ups (not in this PR)

- **Ollama provider (tracked as [AB#668](https://dev.azure.com/jamesconsulting/RAJ%20Financial%20Planner/_workitems/edit/668))** — Add second `IChatClientProvider`
  using `Microsoft.Extensions.AI.OpenAI` pointed at `http://localhost:11434/v1` (Ollama
  exposes OpenAI-compatible HTTP; API key ignored). Adds `AiProviderId.Ollama = 2`. ~80
  LOC, no new SDK dependency.

  Strategic context: user is exploring a **desktop app that packages Ollama** for
  privacy-first local inference (no client PII over the wire, zero per-token cost,
  air-gappable for compliance).

  Desktop-friendly model candidates (16–32 GB PC):

  | Model | Q4 RAM | License | Notes |
  |---|---|---|---|
  | Phi-4 14B | ~9 GB | MIT | Best reasoning per parameter; **recommended default** |
  | Qwen 3.5 8B | ~6 GB | Apache 2.0 | Strong on legal/finance, multi-lingual |
  | Llama 3.3 8B | ~5 GB | Llama Community (restricted) | Familiar; license caveats for SaaS |

  Decision deferred until AB#545 ships and desktop-app direction is firmer.
