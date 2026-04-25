---
name: new-api-function
description: Scaffold a new Azure Function HTTP endpoint for RajFinancial following the 4-layer pattern (Function → Service → Validator → Shared Contracts). Usage: /new-api-function <ResourceName>
disable-model-invocation: true
---

# New Azure Function Endpoint

## Usage

```
/new-api-function <ResourceName>
```

**Example:** `/new-api-function Payment`

This scaffolds all four layers for the given resource following the exact conventions established in this codebase.

---

## Conventions to Follow

Before creating any file, read the following reference implementations:

| Layer | Reference File |
|-------|---------------|
| Function | `src/Api/Functions/ClientManagementFunctions.cs` |
| Service interface | `src/Api/Services/ClientManagement/IClientManagementService.cs` |
| Service implementation | `src/Api/Services/ClientManagement/ClientManagementService.cs` |
| Validator | `src/Api/Validators/AssignClientRequestValidator.cs` |
| Contracts | `src/Shared/Contracts/Auth/` |
| Registration | `src/Api/Configuration/ApplicationServicesRegistration.cs` |

---

## Files to Create

### 1. `src/Api/Functions/<Resource>Functions.cs`

Pattern:
- `partial class` (required — logging source generator uses `.Logging.cs` sibling)
- Primary constructor injecting `ILogger<T>` and `I<Resource>Service`
- Decorated with `[RequireRole(...)]` using string role names. The convention is to define them as `internal const string` on the per-feature telemetry class (e.g., `ClientManagementTelemetry.RoleAdvisor` / `RoleAdministrator`) and reference those constants — never reference `AppRoleOptions` (that holds role *GUIDs* for options binding, not names).
- One method per HTTP verb/route
- Extract request body via `FunctionHelpers` middleware — never re-read `HttpRequestData.Body` directly
- Return typed `HttpResponseData` with appropriate `HttpStatusCode`
- Use `ObservabilityDomains` and `ObservabilityConstants` for activity/metric names
- XML doc on the class and every public method

```csharp
// src/Api/Functions/<Resource>Functions.cs
namespace RajFinancial.Api.Functions;

[RequireRole(<Resource>Telemetry.RoleAdvisor, <Resource>Telemetry.RoleAdministrator)]
public partial class <Resource>Functions(
    ILogger<<Resource>Functions> logger,
    I<Resource>Service <resource>Service)
{
    // GET /api/<resource>
    [Function("Get<Resource>")]
    public async Task<HttpResponseData> Get<Resource>(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "<resource>")] HttpRequestData req,
        FunctionContext context)
    { ... }
}
```

### 2. `src/Api/Functions/<Resource>Functions.Logging.cs`

Partial class with `[LoggerMessage]` source-generated log methods:

```csharp
namespace RajFinancial.Api.Functions;

public partial class <Resource>Functions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Getting <resource> for user {UserId}")]
    private partial void LogGetting<Resource>(string userId);
}
```

### 3. `src/Api/Services/<Resource>/I<Resource>Service.cs`

```csharp
namespace RajFinancial.Api.Services.<Resource>;

public interface I<Resource>Service
{
    Task<IReadOnlyList<<Resource>Response>> Get<Resource>sAsync(string userId, CancellationToken ct = default);
    // Add Create/Update/Delete as needed
}
```

### 4. `src/Api/Services/<Resource>/<Resource>Service.cs`

- Inject `ApplicationDbContext` and `ILogger<T>` via primary constructor
- No private static methods — pure utility goes in its own class (architecture rule)
- Parameterized EF queries only — never raw SQL string concatenation
- Add XML doc on the class and all public methods

### 5. `src/Api/Validators/<Resource>RequestValidator.cs`

```csharp
namespace RajFinancial.Api.Validators;

public class <Resource>RequestValidator : AbstractValidator<<Resource>Request>
{
    public <Resource>RequestValidator()
    {
        RuleFor(x => x.SomeField).NotEmpty().MaximumLength(200);
    }
}
```

### 6. `src/Shared/Contracts/<Resource>/`

Create the following record types:

```csharp
// <Resource>Request.cs — inbound DTO
// <Resource>Response.cs — outbound DTO
```

- Use `record` types (immutable)
- Decorate with `[MemoryPackable]` and `[MemoryPackUnion]` where needed (see existing contracts)
- No entity references — Contracts must not import from `RajFinancial.Shared.Entities` (architecture rule; enums are allowed)

---

## Registration

Add to `src/Api/Configuration/ApplicationServicesRegistration.cs`:

```csharp
services.AddScoped<I<Resource>Service, <Resource>Service>();
```

---

## Tests to Create (BDD-first)

Per the project's TDD/BDD mandate, create these **before or alongside** the implementation:

1. **`tests/IntegrationTests/Features/<Resource>.feature`** — Gherkin scenarios (happy path, validation errors, auth failures, IDOR attempts)
2. **`tests/Api.Tests/<Resource>/<Resource>ServiceTests.cs`** — xUnit unit tests for the service
3. **`tests/Api.Tests/<Resource>/<Resource>ValidatorTests.cs`** — FluentValidation rule tests

---

## Build Verification

After creating all files:

```bash
dotnet build src/RajFinancial.sln --nologo -v:q
dotnet test tests/Api.Tests --nologo -v:q
```

Both must pass before presenting the result to the user.
