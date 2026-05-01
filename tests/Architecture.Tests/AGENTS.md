# Architecture.Tests Agent Instructions (Layering Invariants)

> **Project:** `RajFinancial.Architecture.Tests` — `NetArchTest.Rules`-driven invariant tests.
> The repo-wide rules at the [repo root AGENTS.md](../../AGENTS.md) apply.
> **Test Layer:** Architecture (executable rules, not behavioral tests).

---

## Purpose

Encode architectural rules as **executable invariants** so violations fail CI instead of
relying on reviewer vigilance. These tests fail fast and produce a list of offending types in
the assertion message.

Examples currently enforced:
- Functions must not depend on `ApplicationDbContext` or `IAuthorizationService` (Mode A
  authorization, ADR 0001).
- Services / Functions / Mappers / DTOs follow naming and namespace conventions.
- Observability domains are uniquely registered.

---

## Required Packages (already wired)

| Package | Purpose |
|---------|---------|
| `NetArchTest.Rules` | Fluent rule API over reflected types |
| `xunit` | Test runner |
| `FluentAssertions` | Assertion DSL — **always use this**, not `Assert.*` |

The csproj references both `RajFinancial.Api` and `RajFinancial.Shared` so rules can target
either assembly's types.

---

## Layout

Files are flat under the project root, one per invariant family:

```
tests/Architecture.Tests/
├── DtoInvariantsTests.cs           # Contracts/DTOs: no entity refs, immutable shape
├── FunctionInvariantsTests.cs      # Functions layer: no DbContext, no IAuthorizationService
├── MapperInvariantsTests.cs        # Mappers: stateless, no service/DbContext deps
├── ObservabilityDomainsTests.cs    # Unique ActivitySource/Meter names
├── ServiceInvariantsTests.cs       # Services: no Function refs, no HTTP types
└── GlobalUsings.cs                 # Xunit, FluentAssertions, NetArchTest.Rules
```

Add a new file when introducing a new rule family. Don't bloat existing files past ~10
related rules.

---

## Conventions

1. **One `[Fact]` per rule.** Each rule has a single, narrow assertion.
2. **Failure message names offenders.** Always append the failing types to the assertion
   message so `dotnet test` output is actionable:
   ```csharp
   result.IsSuccessful.Should().BeTrue(
       "Functions must not reference ApplicationDbContext. Offenders: "
       + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
   ```
3. **Cite the source.** Every test's leading comment block identifies which AGENTS.md /
   ADR section the rule comes from. Future contributors must be able to find the prose
   rationale.
4. **Use `Types.InAssembly(typeof(KnownType).Assembly)`** — never `Assembly.LoadFrom`
   strings. The project's `ProjectReference`s pull the assemblies in directly.
5. **Pin to namespace prefixes**, not exact namespaces:
   `ResideInNamespaceStartingWith("RajFinancial.Api.Functions")` survives sub-namespace
   restructures.
6. **No I/O, no async.** These tests run reflection on already-loaded assemblies and must be
   deterministic and instantaneous.
7. **Do not duplicate behavioral tests.** If a rule can only be enforced by running code,
   it belongs in `Api.Tests` or `IntegrationTests`, not here.

---

## When to add a rule here vs. relying on review

Add an architecture test when **all** of the following hold:

- The rule is **structural** (depends-on, namespace, naming, attribute presence).
- The rule has been violated more than once already, or is plausible to violate by accident.
- The rule has a clear narrow definition (no judgment calls).

Don't add architecture tests for stylistic preferences, performance heuristics, or rules
that require understanding the *meaning* of code. Use code review or analyzers for those.

---

## Running

```bash
dotnet test tests/Architecture.Tests/RajFinancial.Architecture.Tests.csproj
```

These run as part of the standard `dotnet test` sweep and **must stay green on `develop`**.
A red architecture test means the codebase has drifted from a documented invariant — fix
the code, not the test, unless the invariant itself has been formally retired (PR + ADR
update).

---

## What does NOT belong here

| Concern | Goes here |
|---------|-----------|
| Service / business logic correctness | `tests/Api.Tests/` |
| HTTP behavior | `tests/IntegrationTests/` |
| Browser / a11y | `tests/e2e/` |
| Performance / load | (separate; not yet established) |
| OWASP scenario coverage | BDD features in `tests/IntegrationTests/Features/` |
