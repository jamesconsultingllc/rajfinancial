# Template Architecture Reference

## GitHub Org & Repos

| Repo | Purpose | URL |
|------|---------|-----|
| `jamesconsultingllc/template-client` | TypeScript / React / MSAL template | `https://github.com/jamesconsultingllc/template-client` |
| `jamesconsultingllc/template-server` | C# / .NET / Azure Functions template | `https://github.com/jamesconsultingllc/template-server` |
| `jamesconsultingllc/template-library` | C# / NuGet class library template | `https://github.com/jamesconsultingllc/template-library` |
| `jamesconsultingllc/misc-tools` | This repo — scripts, shared master, skills | `https://github.com/jamesconsultingllc/misc-tools` |

## File Layout in Each Template Repo

```
AGENTS.md                          ← Shared rules (identical across all 3 repos)
CLAUDE.md                          ← Pointer → AGENTS.md (for Claude auto-discovery)
.github/copilot-instructions.md   ← Pointer → AGENTS.md (for Copilot Chat auto-discovery)
src/AGENTS.md                      ← Stack-specific rules (unique per template)
```

## File Purposes

### `AGENTS.md` (root)

Universal shared rules that apply to ALL repos regardless of stack:

- Vertical slice implementation workflow
- BDD/TDD methodology
- Session management
- Security requirements (OWASP)
- Observability (telemetry, metrics, logging)
- GitFlow branching model
- Azure DevOps integration
- Core principles priority order

### `src/AGENTS.md` (stack-specific)

Rules unique to the stack type:

**Client** (18KB): React/TS packages, MSAL auth, vertical slice component structure,
MemoryPack DTO generation, accessibility (a11y), localization (i18n), mobile-first
CSS, React Query error boundaries, auth test mocks, pre-merge checklist.

**Server** (27KB): C#/.NET + TypeScript/Cloudflare packages, project structure,
Azure Functions thin-trigger pattern, authorization, error handling, typed exceptions,
structured logging via `[LoggerMessage]`, distributed tracing, architecture tests,
domain telemetry, MemoryPack serialization, health checks, pre-merge checklist.

**Library** (19KB): NuGet golden rule (zero config required to function), IOptions
pattern, multi-targeting, `[LoggerMessage]` source generator, named ActivitySource/Meter,
error codes as constants, FluentValidation with error codes, architecture tests,
NuGet packaging, SemVer, PublicApiAnalyzers, pre-merge checklist.

### `CLAUDE.md` (pointer)

Single line: `Read and follow AGENTS.md at the repository root.`

### `.github/copilot-instructions.md` (pointer)

Single line: `Read and follow AGENTS.md at the repository root.`

## Stack Detection Signals

### Client

| Signal | Weight |
|--------|--------|
| `package.json` has `react`, `next`, `vue`, `angular`, `svelte` | Strong |
| `package.json` has `vite`, `webpack`, `tailwindcss` | Medium |
| `tsconfig.json` exists with `jsx` compiler option | Medium |
| `src/` contains `.tsx` or `.jsx` files | Medium |

### Server

| Signal | Weight |
|--------|--------|
| `*.csproj` references `Microsoft.NET.Sdk.Web` | Strong |
| `*.csproj` references `Microsoft.Azure.Functions.Worker` | Strong |
| `host.json` exists (Azure Functions) | Strong |
| `wrangler.toml` exists (Cloudflare Workers) | Strong |
| `package.json` has `hono`, `@cloudflare/workers-types` | Strong |
| `*.csproj` references ASP.NET Core packages | Medium |

### Library

| Signal | Weight |
|--------|--------|
| `*.csproj` with `Microsoft.NET.Sdk` (no Web/Functions) | Strong |
| `*.csproj` has `<PackageId>` or `<IsPackable>true</IsPackable>` | Strong |
| `*.csproj` has `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>` | Strong |
| `package.json` has `main`/`exports` but no framework deps | Medium |
| Project produces a `.nupkg` or npm package | Medium |

## Section Headings for Merge Comparison

When comparing existing AGENTS.md against the template, use `## ` (h2) headings as
section boundaries. These are the expected headings in the shared AGENTS.md:

1. `## Variables`
2. `## Vertical Slice Implementation`
3. `## Workflow: Plan Before Coding`
4. `## Session Management`
5. `## Development Methodology: BDD/TDD First`
6. `## Security Requirements (OWASP)`
7. `## Observability: Telemetry, Metrics & Logging`
8. `## Code Documentation`
9. `## GitFlow Branching`
10. `## Azure DevOps Integration`
11. `## Core Principles (Priority Order)`

Any heading NOT in this list is repo-specific custom content and must be preserved.
