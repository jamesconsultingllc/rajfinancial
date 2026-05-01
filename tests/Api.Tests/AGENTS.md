# Api.Tests Agent Instructions (Unit Tests)

> **Project:** `RajFinancial.Api.Tests` — xUnit unit tests for `RajFinancial.Api`.
> The repo-wide rules at the [repo root AGENTS.md](../../AGENTS.md) and the API rules at
> [`src/Api/AGENTS.md`](../../src/Api/AGENTS.md) apply.
> **Test Layer:** Unit (see root §Testing). **No Gherkin here** — see `tests/IntegrationTests/` for BDD.

---

## Purpose

Fast, in-process tests for service logic, validators, mappers, and middleware. Isolated from
HTTP, real auth, and the Functions host. **No `.feature` files in this project.**

- Service unit tests use **EF Core InMemory** or **SQLite in-memory** for `ApplicationDbContext`
  fakes; they do **not** spin up a real database.
- HTTP-shape tests live in `tests/IntegrationTests/`; cross-layer architecture rules live in
  `tests/Architecture.Tests/`.

---

## Required Packages (already wired in `RajFinancial.Api.Tests.csproj`)

| Package | Purpose |
|---------|---------|
| `xunit` + `xunit.runner.visualstudio` | Test runner |
| `FluentAssertions` | Assertion DSL — **always use this**, not `Assert.*` |
| `Moq` | Mocking framework (project uses Moq today; do not introduce NSubstitute alongside) |
| `Microsoft.EntityFrameworkCore.Sqlite` | In-memory relational fake for `DbContext` |
| `Microsoft.Extensions.Options` | `Options.Create<T>(...)` for `IOptions<T>` test doubles |
| `MemoryPack` | Round-trip tests for binary contracts |
| `coverlet.collector` | Coverage on `dotnet test --collect:"XPlat Code Coverage"` |

Do **not** add: Reqnroll, Testcontainers, Microsoft.AspNetCore.Mvc.Testing — those belong
in `IntegrationTests`.

---

## Layout

```
tests/Api.Tests/
├── Middleware/         # Auth, Authorization, Validation, Exception, Telemetry middleware
├── Services/           # Per-service unit tests (mirror src/Api/Services/ structure)
│   ├── AssetService/
│   ├── Authorization/
│   ├── Auth/
│   ├── EntityService/
│   └── UserProfile/
└── (Serialization/)    # MemoryPack contract round-trip tests when added
```

Mirror the source tree: a service at `src/Api/Services/Foo/FooService.cs` gets tests at
`tests/Api.Tests/Services/Foo/FooServiceTests.cs`.

---

## Conventions

1. **One class per SUT.** `XxxTests` in a namespace mirroring the SUT.
2. **AAA layout** with blank lines between Arrange / Act / Assert. No comments naming the
   sections.
3. **FluentAssertions** for every assertion:
   ```csharp
   result.Should().NotBeNull();
   result.Items.Should().HaveCount(3).And.BeInAscendingOrder(x => x.Name);
   act.Should().ThrowAsync<UnauthorizedException>();
   ```
4. **Test names** describe the behavior, not the method:
   `GetAssetsAsync_returns_403_when_caller_lacks_advisor_role` — not `Test1` / `Should_return_403`.
5. **OWASP coverage is mandatory** (root AGENTS.md §Security):
   - Every service method that takes a user identity gets at least one **A01 IDOR test** —
     a different user attempting the same operation must be denied (404 or 403, never silent
     pass).
   - Auth-bearing services get a **missing-claim** test.
   - Validators get tests for each `RuleFor` (positive + negative + boundary).
6. **Logging is observable, not asserted by string match.** Inject `ILogger<T>` via `NullLogger<T>`
   (don't assert on log content). EventIds and structured fields are validated by integration
   tests / production telemetry, not unit tests.
7. **No real I/O** — no HTTP, no real DB, no `Thread.Sleep`, no `await Task.Delay` outside of
   explicit cancellation tests.
8. **DbContext fakes:** use SQLite in-memory (`Microsoft.EntityFrameworkCore.Sqlite` with
   `:memory:`) when relational behavior matters (FK cascades, unique constraints). Use the
   InMemory provider only for trivial CRUD shape tests — its semantics differ from SQL Server.
9. **Async tests** return `Task` (never `async void`). No `.Result` / `.Wait()`.
10. **Do not** test private methods directly — exercise them through public seams.

---

## TDD Sequence (root §Implementation Order)

For new service code, write the unit test first:

```
1. Write failing security test (A01 IDOR / role check)         → red
2. Write failing happy-path test                                → red
3. Implement minimum code to pass                               → green
4. Add edge-case tests (boundary, null, empty, concurrent)     → red → green
5. Refactor                                                    → still green
```

Coverage target: **90% of new code** (root §Test Coverage).

---

## Running

```bash
# All unit tests
dotnet test tests/Api.Tests/RajFinancial.Api.Tests.csproj

# One service
dotnet test tests/Api.Tests --filter "FullyQualifiedName~AssetService"

# One test
dotnet test tests/Api.Tests --filter "FullyQualifiedName~AssetServiceTests.GetAssetsAsync_returns_403_when_caller_lacks_advisor_role"

# Coverage
dotnet test tests/Api.Tests --collect:"XPlat Code Coverage"
```

Tests must run **without** a Functions host, **without** a SQL Server, **without** network.
A failure to satisfy any of those is a defect in the test.

---

## What does NOT belong here

| Concern | Goes here |
|---------|-----------|
| HTTP request/response shape, status codes, content negotiation | `tests/IntegrationTests/` (BDD) |
| Architecture / layering invariants (e.g., "Functions don't reference DbContext") | `tests/Architecture.Tests/` |
| Real database, real auth tokens, real Functions host | `tests/IntegrationTests/` |
| Browser, navigation, a11y of UI | `tests/e2e/` |
| React component behavior | `src/Client/src/**/__tests__/` (Vitest) |
