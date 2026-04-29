# ADO Restructure Proposal — AI Platform + Vertical AI Features

**Status:** Revision 5 (2026-04-26) — execution-ready; doc locked
**Changes from Rev 1 → Rev 2:** §B split into three sibling Features; safety invariants promoted to host-level; MCP transport scoped; §D filing batch trimmed; §F resequenced; #D7 Trust deferred pending RAG.
**Changes from Rev 2 → Rev 3:** §G count corrected (5 Features, not 6); merchant strings moved to denylist (hashed); B2 numeric-traceability reframed to typed `citations[]` block; B2 confirm-token idempotency added; B2 synthetic test surface added; B3 transport switched to Streamable HTTP (SSE fallback); §F B1+B2 sequenced as one release unit; quota infrastructure verified absent and filed as B1 sub-task; §H decisions resolved.
**Changes from Rev 3 → Rev 4:** §F step 2 PR-shape locked (two PRs on same branch train, not independent merges to develop); B2 numeric-traceability AC names the actual Microsoft.Extensions.AI API surface (`ChatOptions.ResponseFormat = ChatResponseFormat.ForJsonSchema(...)`); B2 synthetic-tests AC adds (e) confirm-token TTL-expiry rejection to prove the boundary between TTL (AC #3) and idempotency (AC #4); B1 rate-limit sub-task acknowledges that `Microsoft.AspNetCore.RateLimiting` doesn't natively plug into the isolated-worker Functions model and lists both adaptation paths.
**Changes from Rev 4 → Rev 5:** B1 adds registry-immutability AC (sealed at DI build, no hot-swap); B2 adds provider-capability-matrix AC for structured output (Claude 3.5+/OpenAI/Gemini supported; DeepSeek/Ollama/older models routed through fallback or marked unavailable — not silent); B2 adds distributed-token-storage AC (Cosmos/Redis, in-process rejected at deploy validation, prevents scale-out break); §F step 1 adds explicit "no tools, no user-facing AI surface" gate on the foundation PR (prevents foundation shipping before B2 middleware exists in pipeline).

Notably:

- #309 is Accounts & Transactions (not 336)
- #352 is Contacts & Beneficiaries (not 347)
- #577 is the active Entity Restructure epic (alongside #571 which is the planning epic)
- #596 Trust Advisory Suite is a vertical previously missed
- #602 Engineering Excellence — could host the platform AI work better than #393, but #393 is fine

───

## A. Rename Epic #393

From: `09 - AI Insights & Document Processing`
To: `09 - AI Platform (Providers, Tools, MCP)`
New description: Provider-agnostic AI platform — Microsoft.Extensions.AI factory, BYOK key management, tool/function-calling host, safety invariants (citation enforcement, scope enforcement, preview/confirm protocol), MCP server, telemetry, rate limiting, fallback. Application of AI lives inside each domain epic as a first-class feature. This epic is the plumbing.

**During execution:** quick search for any other "document processing" scope still under #393's children before renaming, to avoid orphaning that scope from search results.

## B. Add THREE new Features under Epic #393 (sibling, shippable independently)

The platform is split into three Features so the safety surface ships as a peer rather than as a trailing AC of a 6-month bundle.

### B1. AI Tool Calling Host — Pri 2, parent #393

> Cross-cutting host that lets each domain register `Microsoft.Extensions.AI.AIFunctions` and threads them into the `IChatClient` pipeline. Owns discovery, registry lifetime, per-call tool selection, and telemetry.

AC:

- [ ] `IAiToolRegistry` lets each domain register `AIFunctions`
- [ ] **Registry immutability:** tool registrations are immutable after host startup (registry is sealed at DI build). Hot-swapping tools at runtime is explicitly out of scope for v1. Rationale: ensures auth/scope decisions are deterministic across a chat session and kills a class of "tool list changed mid-conversation" bugs.
- [ ] `IChatClient` pipeline includes registered tools when configured per-call
- [ ] Tool invocations emit telemetry: tool name, caller (user id), latency, outcome, **argument schema (JSON Schema fragment)**, **redacted-value preview** for fields not on a denylist (denylist initial draft below)
- [ ] **Initial denylist** (sub-task): account numbers, SSN, raw text bodies, file contents, full token strings, **merchant name / payee strings** (routinely contain PII — "Dr. Smith Therapy", named individuals on Zelle/Venmo memos) — never logged in cleartext. Merchant strings may be logged as `sha256(name)[:12]` + first whitespace-delimited token only. Allowed value preview: category, date range, enum values, GUIDs, currency code.
- [ ] **Rate limiting** — **verified during review (2026-04-26):** no rate-limit / quota infrastructure exists in `src/Api` today. Filed as a **sub-task under B1**: introduce per-user/tier rate limiting. Implementation approach to be evaluated during the sub-task — `Microsoft.AspNetCore.RateLimiting` is ASP.NET middleware and does **not** natively plug into the isolated-worker Azure Functions model, so options are (a) adapt it as Functions worker middleware, or (b) Functions-native approach with a custom middleware backed by a distributed counter (Cosmos DB / Redis). Without this sub-task, the Tool Calling Host ships without abuse protection.

### B2. AI Safety Invariants — Pri 1, parent #393

> Host-level enforcement of three invariants that every vertical MUST inherit, not re-implement. Implemented as middleware in the `IChatClient` pipeline so vertical Features cannot bypass them.

AC:

- [ ] **Numeric traceability (typed-citations approach):** Model is constrained to emit numerics inside a typed `citations[]` block via structured output — `ChatOptions.ResponseFormat = ChatResponseFormat.ForJsonSchema(...)` in Microsoft.Extensions.AI, falling back to provider-native JSON mode where the SDK doesn't expose schema-bound output. Each citation references a tool-call result id and the field path within it. **Free-text numerics in the response body fail validation** and the response is rejected or re-prompted. This is significantly easier to enforce than parsing free-text dollar/percentage/quantity tokens (`$200`, `200 bucks`, `two hundred`, markdown tables) and matches how Anthropic/OpenAI structured-output features are typically wired.
- [ ] **Provider capability matrix for structured output:** `ChatResponseFormat.ForJsonSchema(...)` only works on providers that support schema-bound output (Anthropic Claude 3.5+ tool-use, OpenAI/Azure OpenAI structured output, Gemini schema mode). DeepSeek, Ollama, and older models either silently degrade to free-text or fail the request. The host declares a per-provider capability matrix; providers without schema-bound support are either (a) routed through a JSON-mode-only fallback path with post-hoc validation, or (b) marked unavailable for tool-calling. **Behavior is explicit per-provider, not silent** — prevents BYOK users picking DeepSeek and silently getting hallucinated numerics.
- [ ] **Entity-scope enforcement:** Tool implementations scope queries to the entity claim on the call. Cross-entity aggregation requires an explicit `household` scope claim in the JWT. Host rejects tool registrations that don't declare their scope requirement.
- [ ] **Preview / confirm protocol:** Write-tools return a preview payload + `confirmation_token`; execution is a separate `confirm_*` tool call. Host UI may auto-prompt; external MCP clients see the preview as text and call `confirm_*` themselves. Tokens expire (default 5 min) and are single-use.
- [ ] **Confirm idempotency:** `confirm_*` calls are idempotent on the token. A network retry of the same token returns the original result (cached for the token's TTL), not a second write. Without this, a flaky connection on the confirm step double-writes.
- [ ] **Confirmation token storage is distributed, not in-process:** confirmation tokens + cached previews live in a distributed store (same backing as the future B1 rate limiter — Cosmos or Redis). Single-instance memory cache is acceptable for local dev but **rejected at deploy validation**. Rationale: Functions consumption-plan instances are ephemeral and may scale out mid-session; a token issued on instance A must verify on instance B, otherwise the preview/confirm contract silently breaks the first time the Function scales out.
- [ ] **Synthetic integration tests** ship with B2 — mock tools registered, fake JWT claims, assert: (a) cross-entity rejection without `household` claim, (b) free-text numeric in response triggers re-prompt, (c) write-tool execution without prior preview is rejected, (d) confirm-token reuse within TTL returns cached result, (e) confirm-token use **after** TTL expiry is rejected (not silently re-issued — proves the boundary between AC #3's TTL and AC #4's idempotency). D6 (Entity Structure AI) becomes a real-world validator, not the only test surface.
- [ ] Bypass attempts (write-tool called without prior preview, cross-entity query without household scope, unattributed numeric output, confirm-token reuse beyond cached result) are logged at `warning` with full context for abuse detection.

### B3. MCP Server — Pri 2, parent #393

> External MCP endpoint exposing the registered AIFunctions to MCP-protocol clients. **Sequenced after B1 + B2** so the safety invariants and tool registry exist first.

**v1 scope (this Feature):** HTTP transport with bearer Entra tokens — **Streamable HTTP preferred; HTTP+SSE acceptable as fallback.** SSE is the older transport and is being deprecated in newer MCP spec revisions in favor of Streamable HTTP. Intended for Claude.ai and Codex web clients hitting a remote endpoint.
**v2 scope (deferred, separate Feature when prioritized):** stdio transport for Claude Desktop / Cursor local subprocess integration. Different auth story, different deployment model.

AC:

- [ ] `/mcp` endpoint speaks MCP protocol over HTTP — Streamable HTTP preferred; SSE acceptable as fallback if upstream client support is incomplete
- [ ] Authenticates via bearer Entra token (same validation as REST API)
- [ ] AIFunction → MCP tool schema export is automatic from the registry
- [ ] Inherits all B2 safety invariants (preview/confirm, scope enforcement, citation logging) without per-tool work
- [ ] Session lifecycle documented (timeout, reconnect, tool-list refresh)
- [ ] **Risk acknowledged in Feature description:** MCP spec is still settling on auth flows, session model, and transport upstream. v1 may need revision when spec stabilizes; choice of Streamable HTTP-first reduces but does not eliminate this risk.

## C. Re-parent existing Features

| Feature | From | To | Why |
|---|---|---|---|
| #528 Document Processing & Statement Parsing | #393 | #309 Accounts & Transactions | It's the consumer; AI is a means to the end |
| #544 Auto-create manual account from parsed statement | #528 | (stays under 528 — moves with parent) | follows naturally |

Leave #394 / #395 / #396 / #551 under #393. Why: #394 is the chat surface itself (cross-cutting), #395 is RAG infrastructure (cross-cutting), #396 + #551 are pure platform.

## D. Add "AI-assisted <domain>" Features per vertical — TRIMMED FILING BATCH

**Initial filing batch (this PR):** only #D1 (Assets) and #D2 (Accounts & Transactions). Verticals 3–6 deferred until the first vertical proves the tool shape; #D7 Trust deferred until RAG #395 has a working ingestion pipeline. Filing all seven now reproduces the over-filing problem §D8 already correctly identifies.

Per-vertical AC blocks below intentionally **omit** the safety invariants (numeric traceability, scope enforcement, preview/confirm) — those are inherited from B2 and don't need to be restated per vertical.

**1. AI in Assets — capture, valuation, identification → parent #331** *(file now)*

> First-class AI inside the Assets vertical. Conversational and tool-driven asset capture: VIN decoding (NHTSA vPIC), serial/model lookups, photo identification (vision providers), valuation suggestions, depreciation guidance, beneficiary nudges. Registers `create_asset_from_vin`, `create_asset_from_serial`, `identify_asset_from_photo`, `suggest_valuation`, `find_assets_without_beneficiaries` with the AI Tool host (B1).

AC:

- [ ] User can say "I have a 2019 Tesla Model 3, VIN 5YJ3E1EA7KF317…" and confirm a created asset
- [ ] User can drop a photo of a serialized item and the AI proposes a PersonalProperty or Collectible asset
- [ ] VIN decoder uses NHTSA vPIC (free, no API key)
- [ ] Tool calls go through the existing `POST /api/assets` (no parallel write path)
- [ ] Vision-only paths gracefully degrade for text-only providers
- [ ] Tools declare their scope requirement (entity-scoped, no household needed)

**2. AI in Accounts & Transactions — parsing, categorization, anomalies → parent #309** *(file now)*

> First-class AI inside the Accounts/Transactions vertical. Absorbs Feature #528 scope plus categorization, recurring-charge detection, anomaly flags, merchant cleanup. Registers `parse_statement`, `categorize_transaction`, `detect_recurring`, `flag_anomaly` tools.

AC:

- [ ] Statement upload → extracted transactions land in the user's account
- [ ] AI proposes categories; user can accept/reject in bulk
- [ ] Recurring charges surface in the UI with cancel/budget actions
- [ ] All AI writes go through existing transaction APIs
- [ ] Tools declare their scope requirement (entity-scoped)

**3. AI in Estate Planning — coverage analysis & gap detection → parent #414** *(deferred — file after #D1 ships)*

> First-class AI inside the Estate Planning vertical. Plain-English coverage summaries, beneficiary gap detection, allocation validation explanations, per-stirpes modeling Q&A. Registers `analyze_estate_coverage`, `find_beneficiary_gaps`, `explain_allocation` tools.

Draft AC (not yet filed):

- [ ] User can ask "what happens to my estate if I die today?" and get a structured plain-English answer
- [ ] AI flags assets without primary or contingent beneficiaries
- [ ] AI flags allocations that don't sum to 100% with suggested fixes
- [ ] No legal advice disclaimer language is hard-coded into responses

**4. AI in Budgeting & Debt — coaching & strategy comparison → parent #542** *(deferred)*

> First-class AI inside the Budgeting/Debt vertical. Avalanche-vs-Snowball explanations, what-if simulations ("what if I add $200/mo to highest-rate?"), spending-pattern coaching. Registers `simulate_payoff`, `compare_strategies`, `suggest_budget_adjustment` tools.

Draft AC (not yet filed):

- [ ] User can ask what-if questions and get numerically-correct projections (deterministic math, AI explains)
- [ ] (Numeric traceability is inherited from B2 — not restated)

**5. AI in Contacts & Beneficiaries — relationship & coverage suggestions → parent #352** *(deferred)*

> First-class AI inside the Contacts/Beneficiaries vertical. Detects beneficiary gaps across the relationship graph, suggests adding contingents, plain-English summaries of who's covered for what. Registers `find_uncovered_relationships`, `suggest_contingent_beneficiary` tools.

Draft AC (not yet filed):

- [ ] AI never auto-adds a beneficiary — only suggests (preview/confirm inherited from B2)
- [ ] Suggestions cite which assets/policies are involved

**6. AI in Entity Structure — household Q&A & cross-entity insights → parent #577** *(deferred — also gates on B2 scope-enforcement landing, since this is the first feature that uses `household` scope)*

> First-class AI inside the multi-entity architecture. Cross-entity household Q&A ("how much do I owe across all my entities?"), inter-entity transfer reasoning, entity-level P&L explanations. Registers `query_household`, `explain_inter_entity_transfer` tools.

Draft AC (not yet filed):

- [ ] Tools declare `household` scope requirement; B2 host enforces JWT claim presence
- [ ] First real-world validator of B2's cross-entity enforcement (B2 ships with synthetic tests; D6 exercises it against live data)

**7. AI in Trust Advisory Suite — trust strategy & document Q&A → parent #596** *(NOT FILED — gated on RAG #395)*

**Sequencing problem identified during review:** This Feature's AC #1 ("answers grounded in user's own trust documents via RAG #395") cannot be met until #395 has a working ingestion pipeline. Filing it now with a v1-without-grounding scope would create a "trust strategy Q&A with no document grounding" — a hallucination factory that contradicts its own purpose.

**Decision: do not file this Feature in the initial batch.** File it when:

- #395 RAG has a sponsor and committed implementer, OR
- A user explicitly requests trust AI work and accepts the v1-without-grounding limitation in writing

**8. (skip)** Dashboard #373 / Identity #288 / Authz #433 / User Profile #451 / Production Readiness #543 / Engineering Excellence #602 — no domain-specific AI surface yet. Production Readiness #543 may eventually want `summarize_incident` / `explain_alert` tools for ops-style AI; speculative, defer.

## E. What this PR will NOT do

- Won't touch existing Feature #394 (AI Insights UI) **but will sequence it explicitly:** #394 ships **after** B1 + B2 land, OR is explicitly re-scoped as read-only v1 with that limitation documented in the Feature description. Don't leave it ambiguous.
- Won't move #395 (RAG) — RAG is shared infra. **However:** #395 now has an explicit downstream gate (D7 Trust). If #D7 becomes prioritized, #395 needs a sponsor before #D7 can be filed.
- Won't reassign anything currently In Progress or Done.

## F. Implementation order — REVISED

1. **Foundation PR** under existing Feature #396: tasks #398 (interfaces) → #397 (factory) → #545 (Claude adapter). One cohesive change, gets `IChatClient` into DI. **Step 1 explicitly excludes registering any `AIFunction` or exposing any user-facing AI surface.** The factory is in DI but no tool is ever attached until B1+B2 ship together. Foundation validation uses unit tests, not a smoke endpoint. Rationale: prevents the "let's just demo Claude real quick" failure mode where someone wires up a tool against the foundation pipeline before the safety middleware exists, then B2 has to retrofit middleware into a pipeline already in production use.
2. **B1 + B2 ship in the same milestone** — B1 (Tool Calling Host) provides the registry and pipeline hook; B2's safety middleware lands on top of B1's pipeline. They are technically sequential within a tight window (B2's middleware needs B1's pipeline to exist) but **treated as one release unit**. Neither ships without the other; the safety surface does not trail. **PR shape:** implemented as two PRs in sequence on the same branch train (B1 first, B2 immediately after), not merged independently to `develop`. This isolates the safety-middleware change for review while preserving the "ships as one unit" guarantee.
3. **D1 AI in Assets** — first vertical to consume the platform; validates the tool-shape and the safety surface end-to-end.
4. **D2 AI in Accounts & Transactions** — second vertical, broader scope, exercises statement parsing.
5. **B3 MCP Server (Streamable HTTP v1)** — once function-calling and safety are proven in-process.
6. **Reassess** verticals 3–6 and #D7 (Trust) based on what we learned from D1/D2 and where RAG #395 stands.

## G. Decisions locked

- **Epic name:** `09 - AI Platform (Providers, Tools, MCP)` — encodes the three pillars explicitly.
- **Initial filing batch: 5 new Features** — B1 (Tool Calling Host), B2 (Safety Invariants), B3 (MCP Server, sequenced last), D1 (AI in Assets), D2 (AI in Accounts & Transactions). Plus 1 epic rename (#393) and 1 re-parent (#528 → #309).
- **#394 Insights UI:** keep cross-cutting under #393; sequence **after B1+B2 land**. Read-only v1 ages badly and creates a UX expectation that's hard to walk back.
- **MCP v1 scope:** Streamable HTTP preferred (SSE fallback) + Entra bearer. Stdio transport deferred to v2.
- **Telemetry:** argument schema + redacted value preview; merchant strings hashed. Rate-limit infrastructure filed as sub-task under B1 (verified absent in codebase 2026-04-26).
- **B2 safety surface ships with synthetic tests**, not gated on D6 for first validation.

## H. Resolved items (no further verification needed)

1. **Quota system** — verified during review (2026-04-26): no rate-limit / quota infrastructure exists in `src/Api`. Filed as sub-task under B1 (do not assume the AC; introduce it).
2. **#394 sequencing** — decided: ship after B1+B2 land. No read-only v1.
3. **B3 MCP filing** — decided: file now, sequenced last in §F. Anchors the roadmap without pulling resources prematurely.

───

Approve and I'll execute: rename #393, file B1/B2/B3, file D1/D2, re-parent #528. Returns with new IDs.
