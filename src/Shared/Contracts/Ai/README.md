# AI Configuration Contracts

Shared configuration POCOs for the AI platform. Referenced by `RajFinancial.Api` and tests; no
runtime behavior here — just the shape that `IOptions<AiOptions>` binds to.

## Types

| Type | Purpose |
|---|---|
| `AiOptions` | Root config (binds `Ai:` section). Holds `DefaultProvider` + `Providers` dictionary. |
| `AiProviderOptions` | Per-provider config: `Model`, `ApiKeyEnvVar`, optional `BaseUrl`. |
| `AiProviderId` | Enum of supported providers. v1: `Anthropic` only. |

## What lives elsewhere

- `IChatClientFactory` (the factory contract that returns `Microsoft.Extensions.AI.IChatClient`):
  `src/Api/Services/Ai/Abstractions/`. Lives outside `Shared` because returning an MEAI type from
  a `Shared` interface would require pulling MEAI into the multi-targeted (`net9.0;net10.0`)
  shared project. Splitting the contracts (config in Shared, factory in Api) keeps Shared free of
  AI runtime dependencies.
- Factory implementation: `src/Api/Services/Ai/` (Task #397, **done**: `ChatClientFactory`,
  `AiOptionsValidator`, `IChatClientProvider` strategy seam, and `AddRajFinancialAi(IConfiguration)`
  DI extension. The DI extension is **defined but not yet called from `Program.cs`** — it will be
  wired up by #545 when the first concrete `IChatClientProvider` ships).
- Anthropic provider wiring: `src/Api/Services/Ai/Providers/` (Task #545, pending).

## Plans

- **Umbrella:** [`docs/plans/2026-04-26-ado-restructure-ai-platform.md`](../../../../docs/plans/2026-04-26-ado-restructure-ai-platform.md)
- **This task (#398):** [`docs/plans/tasks/398-ai-contracts-interfaces.md`](../../../../docs/plans/tasks/398-ai-contracts-interfaces.md)
