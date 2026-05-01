---
name: apply-agent-instructions
description: >-
  Apply or merge AI agent instruction templates (AGENTS.md, CLAUDE.md, copilot-instructions.md)
  into the current repository. Auto-detects the repo stack (client, server, library), pulls
  the latest template from GitHub, and intelligently merges with existing repo-specific content.
  USE FOR: set up agent instructions, add AGENTS.md, apply template, bootstrap repo instructions,
  update agent rules, merge agent instructions, sync agent templates.
  DO NOT USE FOR: editing template content in the template repos themselves.
license: MIT
compatibility: Requires gh CLI authenticated with access to jamesconsultingllc org repos.
metadata:
  author: jamesconsultingllc
  version: "1.0"
---

# Apply Agent Instructions

Applies standardized AI agent instruction templates to the current repository, with intelligent
merge support so repo-specific customizations are never lost.

## When to Use

- Setting up a new repo with agent instructions
- Updating an existing repo's shared rules to the latest version
- Adding missing instruction files (CLAUDE.md, copilot-instructions.md)
- Merging new template sections into an existing AGENTS.md without losing repo-specific content

## Architecture

See [references/architecture.md](references/architecture.md) for the template repo layout
and file mapping details.

## Procedure

### Step 1: Detect the Stack

Scan the current workspace to determine the repo type:

| Signal | Stack |
|--------|-------|
| `package.json` with `react`, `next`, `vue`, `angular`, `svelte`, or `vite` | **client** |
| `*.csproj` with `Microsoft.NET.Sdk.Web`, `Microsoft.Azure.Functions`, or ASP.NET packages | **server** |
| `tsconfig.json` with `hono`, `@cloudflare/workers-types`, or `wrangler.toml` present | **server** |
| `*.csproj` with `Microsoft.NET.Sdk` (no web/functions), produces a NuGet package | **library** |
| `package.json` that is a publishable npm package (has `main`/`exports`, no framework) | **library** |

If detection is ambiguous, present findings and ask the user to confirm.

### Step 2: Pull Template Files from GitHub

Download four files from the matching template repo's `main` branch:

```
Owner: jamesconsultingllc
Repos:  template-client | template-server | template-library
```

| Template repo path | Target repo path |
|---|---|
| `AGENTS.md` | `AGENTS.md` (root — shared rules) |
| `src/AGENTS.md` | `src/AGENTS.md` (stack-specific rules) |
| `CLAUDE.md` | `CLAUDE.md` (pointer file) |
| `.github/copilot-instructions.md` | `.github/copilot-instructions.md` (pointer file) |

Use the GitHub MCP tools to read file contents from the template repo.
If GitHub MCP tools are unavailable, fall back to `gh api` in the terminal:

```bash
gh api "repos/jamesconsultingllc/template-{stack}/contents/{path}" \
  -H "Accept: application/vnd.github.raw"
```

### Step 3: Merge Strategy

#### Pointer files (CLAUDE.md, .github/copilot-instructions.md)

These are single-line pointers (`Read and follow AGENTS.md at the repository root.`).

- If missing → create them
- If they already exist and match → skip
- If they exist with different content → show the user and ask whether to replace

#### Root AGENTS.md (shared rules)

This file contains universal rules that apply to ALL repos (vertical slices, BDD/TDD, security,
observability, GitFlow, etc.).

- If missing → create from template
- If exists → compare section-by-section:
  1. Read both the existing file and the template
  2. Identify sections by `## Heading` markers
  3. List sections that are:
     - **Missing** from the repo (present in template, absent in repo)
     - **Modified** in the repo (heading exists but content differs)
     - **Extra** in the repo (present in repo, absent in template — these are repo-specific)
  4. Present a summary table to the user:
     ```
     | Section | Status | Action |
     |---------|--------|--------|
     | Vertical Slice Implementation | ✅ Up to date | Skip |
     | Security Requirements | ⚠️ Modified locally | Keep local |
     | Session Management | ❌ Missing | Add from template |
     | [Repo-specific section] | 🔵 Custom | Keep |
     ```
  5. Ask the user to approve the merge plan before making changes
  6. Apply approved changes — append missing sections, preserve local modifications

#### src/AGENTS.md (stack-specific rules)

Same merge strategy as root AGENTS.md. This file has stack-specific content (packages,
project structure, testing patterns, checklists).

### Step 4: Report

After applying changes, show a summary:

```
✅ Applied template-server instructions to E:\rajfinancial

  AGENTS.md              — 2 sections added, 8 unchanged, 1 kept (custom)
  src/AGENTS.md          — Created (new file)
  CLAUDE.md              — Created (pointer)
  .github/copilot-instructions.md — Already exists, skipped

Run 'git diff' to review all changes before committing.
```

## Important Rules

1. **NEVER blindly overwrite** an existing AGENTS.md — always merge
2. **Preserve repo-specific sections** — any heading not in the template is custom content; keep it
3. **Ask before acting** — always show the merge plan and get user approval
4. **One commit** — all changes should be committed together with message: `chore: apply {stack} agent instruction template`
5. **Don't commit automatically** — stage the changes and let the user review and commit
