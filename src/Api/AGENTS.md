# Server-Side Agent Instructions (C# / TypeScript)

> **Backend-specific directives.** The shared rules at the [repo root AGENTS.md](../AGENTS.md) also apply.
>
> **Primary language: C# (.NET)**
> **Secondary language: TypeScript** (Cloudflare Workers, edge functions)

---

## Required Packages — C# (.NET)

Install these when scaffolding a new .NET backend project.

### Core Runtime

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.OpenApi` | OpenAPI / Swagger doc generation |
| `FluentValidation.AspNetCore` | Request validation pipeline |
| `MemoryPack` | High-performance binary serialization (cache, queues, inter-service) |
| `Mapster` or `AutoMapper` | DTO mapping |
| `Polly` | Resilience / retry / circuit breaker |
| `Microsoft.Extensions.Http.Resilience` | HttpClient resilience (wraps Polly) |

### BDD / TDD / Unit Testing

| Package | Purpose |
|---------|---------|
| `xUnit` | Unit test framework |
| `xUnit.runner.visualstudio` | VS Test Explorer integration |
| `FluentAssertions` | Readable assertion syntax |
| `NSubstitute` | Mocking framework |
| `Bogus` | Realistic test data generation |
| `Reqnroll` | BDD / Gherkin `.feature` file runner (SpecFlow successor) |
| `Reqnroll.xUnit` | Reqnroll + xUnit integration |
| `Verify.Xunit` | Snapshot testing for complex outputs |

### Architecture Testing

| Package | Purpose |
|---------|--------|
| `NetArchTest.Rules` | Enforce layer boundaries and naming conventions at test time |

### Integration Testing

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory` for in-process API tests |
| `Testcontainers` | Spin up real DB/Redis/queue in Docker for tests |
| `Testcontainers.MsSql` | SQL Server test container |
| `Testcontainers.CosmosDb` | Cosmos DB emulator container |
| `Respawn` | Fast database reset between tests |
| `WireMock.Net` | Mock external HTTP dependencies |

### Security Testing

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth |
| `NetEscapades.AspNetCore.SecurityHeaders` | Security header middleware |
| `Microsoft.AspNetCore.RateLimiting` | Rate limiting middleware |

### Accessibility Testing (API-driven UI testing)

| Package | Purpose |
|---------|---------|
| `Deque.AxeCore.Playwright` | axe accessibility checks in .NET Playwright tests |
| `Microsoft.Playwright` | Cross-browser E2E from .NET |

### Logging & Telemetry

| Package | Purpose |
|---------|---------|
| `Serilog.AspNetCore` | Structured logging provider |
| `Serilog.Sinks.Console` | Console sink (dev) |
| `Serilog.Sinks.Seq` | Seq sink (structured log viewer) |
| `Serilog.Sinks.OpenTelemetry` | Export logs via OTel protocol |
| `OpenTelemetry.Extensions.Hosting` | OTel host integration |
| `OpenTelemetry.Instrumentation.AspNetCore` | Auto-instrument HTTP requests |
| `OpenTelemetry.Instrumentation.Http` | Auto-instrument outbound HttpClient |
| `OpenTelemetry.Instrumentation.SqlClient` | Auto-instrument SQL queries |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | Auto-instrument EF Core |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | OTLP exporter (traces + metrics) |
| `Azure.Monitor.OpenTelemetry.Exporter` | Export to Application Insights |

### Data Access

| Package | Purpose |
|---------|---------|
| `Microsoft.EntityFrameworkCore` | ORM |
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server provider |
| `Microsoft.Azure.Cosmos` | Cosmos DB SDK |
| `Dapper` | Micro-ORM for performance-critical queries |

### Health Checks

| Package | Purpose |
|---------|---------|
| `AspNetCore.HealthChecks.SqlServer` | SQL Server health check |
| `AspNetCore.HealthChecks.CosmosDb` | Cosmos DB health check |
| `AspNetCore.HealthChecks.Redis` | Redis health check |
| `AspNetCore.HealthChecks.UI.Client` | Health check UI response writer |

---

## Required Packages — TypeScript (Cloudflare Workers)

For edge/serverless backends on Cloudflare.

### Core Runtime

| Package | Purpose |
|---------|---------|
| `hono` | Lightweight web framework (Cloudflare-native) |
| `zod` | Schema validation |
| `drizzle-orm` | Type-safe ORM (D1, Postgres, etc.) |
| `@cloudflare/workers-types` | Worker type definitions |

### BDD / TDD / Testing

| Package | Purpose |
|---------|---------|
| `vitest` | Unit test runner |
| `miniflare` | Local Cloudflare Workers simulator for integration tests |
| `@cucumber/cucumber` | Gherkin BDD (if using feature files) |
| `msw` | Mock Service Worker for HTTP mocking |
| `@faker-js/faker` | Test data generation |

### Logging & Telemetry

| Package | Purpose |
|---------|---------|
| `@opentelemetry/api` | OTel API |
| `@microlabs/otel-cf-workers` | OTel SDK adapted for Cloudflare Workers |

---

## Project Structure — C# (Vertical Slices)

Organize by **feature**, not by layer:

```
src/
├── MyApp.Api/
│   ├── Features/
│   │   ├── Orders/                    # One vertical slice
│   │   │   ├── CreateOrder.cs         # Endpoint + handler + request/response
│   │   │   ├── GetOrders.cs
│   │   │   ├── OrderService.cs        # Business logic
│   │   │   ├── OrderRepository.cs     # Data access
│   │   │   ├── OrderDto.cs            # DTOs
│   │   │   └── OrderValidator.cs      # FluentValidation rules
│   │   └── Auth/
│   │       └── ...
│   ├── Shared/                        # Cross-cutting only
│   │   ├── Middleware/
│   │   ├── Extensions/
│   │   └── Behaviors/                 # MediatR/pipeline behaviors
│   └── Program.cs
├── MyApp.Domain/                      # If needed: entities, value objects
├── MyApp.Infrastructure/              # If needed: EF DbContext, external integrations
tests/
├── MyApp.UnitTests/
│   └── Features/
│       └── Orders/
│           ├── OrderServiceTests.cs
│           └── CreateOrderTests.cs
├── MyApp.IntegrationTests/
│   └── Features/
│       └── Orders/
│           └── OrdersEndpointTests.cs
├── MyApp.BddTests/
│   └── Features/
│       └── Orders/
│           ├── Orders.feature          # Gherkin specs
│           └── OrdersStepDefinitions.cs
└── MyApp.E2eTests/
    └── ...
```

### Project Structure — TypeScript (Cloudflare Workers)

```
src/
├── features/
│   ├── orders/
│   │   ├── orders-handler.ts
│   │   ├── orders-service.ts
│   │   ├── orders-repository.ts
│   │   ├── orders-schema.ts          # Zod schemas
│   │   └── orders-handler.test.ts
│   └── auth/
│       └── ...
├── shared/
│   ├── middleware/
│   └── lib/
├── index.ts                           # Worker entry point
tests/
├── features/
│   └── orders/
│       ├── orders.feature
│       └── orders.steps.ts
└── setup.ts
```

---

## Azure Functions Testing Strategy

Azure Functions do **not** support `WebApplicationFactory`. Use the **thin trigger + thick service** pattern so Reqnroll tests the logic without starting the Functions host.

### Pattern: Thin Triggers, Thick Services

```csharp
// ✅ Thin trigger — just plumbing, no logic
public class OrderFunctions
{
    private readonly IOrderService _orderService;
    public OrderFunctions(IOrderService orderService) => _orderService = orderService;

    [Function("CreateOrder")]
    public async Task<HttpResponseData> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
    {
        var request = await req.ReadFromJsonAsync<CreateOrderRequest>();
        var result = await _orderService.CreateAsync(request!);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(result);
        return response;
    }
}

// ✅ All logic here — fully testable without Functions host
public class OrderService : IOrderService
{
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        // validation, business rules, persistence — everything testable
    }
}
```

### Reqnroll Targets the Service, Not the Function

```csharp
[Binding]
public class OrderSteps
{
    private readonly IOrderService _orderService;
    private OrderDto? _result;

    public OrderSteps()
    {
        _orderService = BuildTestServiceProvider().GetRequiredService<IOrderService>();
    }

    [When("a new order is placed for {int} items")]
    public async Task WhenOrderPlaced(int itemCount)
    {
        _result = await _orderService.CreateAsync(new CreateOrderRequest { /* ... */ });
    }

    [Then("the order should be created successfully")]
    public void ThenOrderCreated()
    {
        _result.Should().NotBeNull();
        _result!.Id.Should().NotBeEmpty();
    }
}
```

### Testing the HTTP Wiring (No Host Needed)

For the few tests verifying trigger routing, auth level, and serialization:

```csharp
var mockRequest = MockHttpRequestData.Create("POST", "/api/orders", body);
var function = new OrderFunctions(mockOrderService);
var response = await function.CreateOrder(mockRequest);

response.StatusCode.Should().Be(HttpStatusCode.Created);
```

### What to Test Where

| What | How | Functions host needed? |
|------|-----|------------------------|
| Business logic + rules | Reqnroll → `IOrderService` | No |
| Data access | Reqnroll → repository + Testcontainers | No |
| HTTP wiring / routing | xUnit → mock `HttpRequestData` | No |
| Full E2E smoke test | Playwright or `HttpClient` | Yes (1-2 tests only) |

---

## Authorization (Backend)

1. **Tenant Isolation** — Every request scoped to authenticated tenant
2. **Role Validation** — Return `403 Forbidden` for unauthorized access
3. **Deny by Default** — No implicit permissions
4. **Audit Logging** — Log all authorization failures and data modifications

### C# Pattern

```csharp
/// <summary>
/// Attribute-based authorization with tenant scoping.
/// </summary>
[Authorize(Policy = "RequireAdminRole")]
[HttpDelete("{id:guid}")]
public async Task<IActionResult> DeleteOrder(Guid id)
{
    var tenantId = User.GetTenantId();
    var order = await _orderService.GetByIdAsync(id, tenantId);
    if (order is null)
        return NotFound(new ApiError { Code = "ORDER_NOT_FOUND" });

    await _orderService.DeleteAsync(id, tenantId);

    _logger.LogInformation(
        "Order {OrderId} deleted by {UserId} in tenant {TenantId}",
        id, User.GetUserId(), tenantId);

    return NoContent();
}
```

---

## Error Handling (Backend)

- Never expose stack traces to clients
- Return structured error responses with error codes
- Log detailed errors internally with correlation IDs

### C# Pattern

```csharp
/// <summary>
/// Structured API error response for client-side localization.
/// </summary>
public record ApiError
{
    public required string Code { get; init; }
    public string? Message { get; init; }
    public object? Details { get; init; }
}

// Standard error codes:
// AUTH_REQUIRED, AUTH_FORBIDDEN, RESOURCE_NOT_FOUND,
// VALIDATION_FAILED, RATE_LIMITED, SERVER_ERROR
```

### TypeScript Pattern

```typescript
/** Structured API error for client-side localization. */
interface ApiError {
  code: string;
  message?: string;
  details?: Record<string, unknown>;
}

// Return error codes, not messages
return c.json<ApiError>({ code: "ORDER_NOT_FOUND" }, 404);
```

---

## Logging & Telemetry (Backend)

### C# Structured Logging

```csharp
// ✅ Correct: Named placeholders — structured data preserved
_logger.LogInformation(
    "Order {OrderId} placed by {UserId} for {Amount:C} — {ItemCount} items",
    order.Id, userId, order.Total, order.Items.Count);

// ✅ Correct: Scoped context
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = correlationId,
    ["TenantId"] = tenantId
}))
{
    _logger.LogInformation("Processing payment for Order {OrderId}", order.Id);
}

// ❌ Incorrect: String interpolation — breaks structured logging
_logger.LogInformation($"Order {order.Id} placed by {userId}");
```

### C# Distributed Tracing

```csharp
private static readonly ActivitySource s_activitySource = new("MyApp.Orders");

/// <summary>
/// Places an order with full distributed tracing.
/// </summary>
public async Task<Order> PlaceOrderAsync(OrderRequest request)
{
    using var activity = s_activitySource.StartActivity("PlaceOrder");
    activity?.SetTag("order.tenant_id", request.TenantId);
    activity?.SetTag("order.item_count", request.Items.Count);

    try
    {
        var order = await _repository.CreateAsync(request);
        activity?.SetTag("order.id", order.Id.ToString());
        return order;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```

### C# Metrics

```csharp
private static readonly Meter s_meter = new("MyApp.Orders");
private static readonly Counter<long> s_ordersPlaced = s_meter.CreateCounter<long>(
    "app.orders.placed", "orders", "Total orders placed");
private static readonly Histogram<double> s_orderDuration = s_meter.CreateHistogram<double>(
    "app.orders.processing_duration", "ms", "Order processing time");

public async Task<Order> PlaceOrderAsync(OrderRequest request)
{
    var sw = Stopwatch.StartNew();
    try
    {
        var order = await _repository.CreateAsync(request);
        s_ordersPlaced.Add(1, new KeyValuePair<string, object?>("tenant", request.TenantId));
        return order;
    }
    finally
    {
        s_orderDuration.Record(sw.Elapsed.TotalMilliseconds);
    }
}
```

### TypeScript Logging & Tracing (Cloudflare Workers)

```typescript
// Structured logging
logger.info("Order placed", {
  orderId: order.id,
  userId,
  tenantId,
  amount: order.total,
  correlationId: ctx.correlationId,
});

// Tracing
import { trace } from "@opentelemetry/api";
const tracer = trace.getTracer("my-worker");

async function placeOrder(request: OrderRequest): Promise<Order> {
  return tracer.startActiveSpan("PlaceOrder", async (span) => {
    span.setAttribute("order.tenant_id", request.tenantId);
    try {
      const order = await repository.create(request);
      span.setAttribute("order.id", order.id);
      return order;
    } catch (err) {
      span.recordException(err as Error);
      span.setStatus({ code: SpanStatusCode.ERROR });
      throw err;
    } finally {
      span.end();
    }
  });
}
```

---

## Architecture Tests (C#)

**Enforce layer boundaries at test time, not code review time.** Use `NetArchTest` to prevent architectural drift.

```csharp
/// <summary>
/// Functions (HTTP triggers) must never access DbContext or authorization services directly.
/// They delegate to service classes only.
/// </summary>
[Fact]
public void Functions_ShouldNot_DependOn_DataLayer()
{
    Types.InAssembly(typeof(Program).Assembly)
        .That().ResideInNamespace("MyApp.Api.Functions")
        .ShouldNot()
        .HaveDependencyOn("MyApp.Api.Data")
        .GetResult()
        .IsSuccessful.Should().BeTrue();
}

/// <summary>
/// Services must never depend on HTTP types — they are transport-agnostic.
/// </summary>
[Fact]
public void Services_ShouldNot_DependOn_HttpTypes()
{
    Types.InAssembly(typeof(Program).Assembly)
        .That().ResideInNamespace("MyApp.Api.Services")
        .ShouldNot()
        .HaveDependencyOnAny(
            "Microsoft.Azure.Functions.Worker.Http",
            "Microsoft.Azure.Functions.Worker.FunctionContext")
        .GetResult()
        .IsSuccessful.Should().BeTrue();
}
```

### What to Enforce

| Rule | Why |
|------|-----|
| Functions → Services only (no DbContext) | Thin triggers, thick services |
| Services → no HTTP types | Transport-agnostic business logic |
| Shared project → no Api dependency | Contracts must be standalone |
| All domain telemetry sources registered | Prevent silent instrumentation gaps |

---

## Domain Telemetry Architecture (C#)

**Register domain-specific `ActivitySource` and `Meter` instances, not one catch-all.** This enables per-feature filtering, sampling, and dashboards.

### Pattern: ObservabilityDomains Registry

```csharp
/// <summary>
/// Central registry of all observability domains.
/// Architecture tests verify every domain is registered here.
/// </summary>
public static class ObservabilityDomains
{
    public const string Auth = "MyApp.Auth";
    public const string Orders = "MyApp.Orders";
    public const string Payments = "MyApp.Payments";
    public const string Middleware = "MyApp.Middleware";

    /// <summary>All domains — used by OTel registration to ensure nothing is missed.</summary>
    public static readonly IReadOnlyList<string> All = [Auth, Orders, Payments, Middleware];
}

// Registration — add ALL ActivitySources and Meters
services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource(ObservabilityDomains.All.ToArray()))
    .WithMetrics(b => b.AddMeter(ObservabilityDomains.All.ToArray()));
```

### Pattern: BusinessEventsInterceptor

Emit domain counters from EF Core `SaveChanges` — impossible to forget instrumentation:

```csharp
/// <summary>
/// EF Core interceptor that emits OTel counters for created/modified/deleted entities.
/// </summary>
public class BusinessEventsInterceptor : SaveChangesInterceptor
{
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct)
    {
        if (eventData.Context is not null)
        {
            foreach (var entry in eventData.Context.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                    TelemetryMeters.EntityCreated(entry.Entity.GetType().Name);
            }
        }
        return base.SavedChangesAsync(eventData, result, ct);
    }
}
```

### Pattern: Source-Generated LoggerMessage

**Always use `[LoggerMessage]` source generator** — zero-alloc, compile-time checked, unique EventIds:

```csharp
// ✅ Correct: Source-generated, zero-alloc
public static partial class OrderLogs
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Order {OrderId} created by {UserId} in tenant {TenantId}")]
    public static partial void OrderCreated(ILogger logger, Guid orderId, string userId, string tenantId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Order {OrderId} creation failed: {Reason}")]
    public static partial void OrderCreationFailed(ILogger logger, Guid orderId, string reason);
}

// Usage
OrderLogs.OrderCreated(_logger, order.Id, userId, tenantId);

// ❌ Avoid: String interpolation (loses structured data, allocates)
_logger.LogInformation($"Order {order.Id} created");
```

### Pattern: Context Keys Constants

**No magic strings.** Use a constants class for all middleware-shared keys:

```csharp
/// <summary>
/// Constants for keys stored in FunctionContext.Items across the middleware pipeline.
/// </summary>
public static class FunctionContextKeys
{
    public const string UserId = nameof(UserId);
    public const string TenantId = nameof(TenantId);
    public const string UserRoles = nameof(UserRoles);
    public const string IsAuthenticated = nameof(IsAuthenticated);
    public const string ClaimsPrincipal = nameof(ClaimsPrincipal);
    public const string RequestBody = nameof(RequestBody);
    public const string ContentType = nameof(ContentType);
}
```

---

## Typed Exception Hierarchy (C#)

**Map domain exceptions to HTTP status codes in a global exception middleware.** Services throw typed exceptions; middleware translates to HTTP responses. Never throw raw `Exception` or return status codes from services.

```csharp
public class NotFoundException : Exception { public string Code { get; init; } }
public class ValidationException : Exception { public IDictionary<string, string> Errors { get; init; } }
public class UnauthorizedException : Exception { }
public class ForbiddenException : Exception { }
public class ConflictException : Exception { public string Code { get; init; } }
public class BusinessRuleException : Exception { public string Code { get; init; } }
```

| Exception | HTTP | Error Code |
|-----------|------|------------|
| `NotFoundException` | 404 | Per-instance |
| `ValidationException` | 400 | `VALIDATION_FAILED` |
| `UnauthorizedException` | 401 | `AUTH_REQUIRED` |
| `ForbiddenException` | 403 | `AUTH_FORBIDDEN` |
| `ConflictException` | 409 | Per-instance |
| `BusinessRuleException` | 422 | Per-instance |
| `DbUpdateConcurrencyException` | 409 | `DB_CONCURRENCY_CONFLICT` |
| Unhandled | 500 | `INTERNAL_ERROR` |

### Structured Error Response

```csharp
/// <summary>
/// Standard API error response. Error code enables client-side i18n.
/// TraceId enables support correlation.
/// </summary>
public record ApiErrorResponse
{
    public required string Code { get; init; }
    public string? Message { get; init; }
    public object? Details { get; init; }
    public string? TraceId { get; init; } = Activity.Current?.TraceId.ToString();
}
```

---

## Health Check Validation (C#)

**Health checks should validate configuration, not just connectivity.** Catch misconfigured deployments at startup.

```csharp
/// <summary>
/// Validates that the correct auth validator is wired for the current environment.
/// Catches unsigned JWT validators in production before the first request.
/// </summary>
public class AuthConfigHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct)
    {
        var validatorType = _validator.GetType().Name;

        if (_env.IsProduction() && validatorType.Contains("Unsigned"))
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "CRITICAL: Unsigned JWT validator active in production"));

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
```

---

## Serialization (C#)

Use **MemoryPack** for internal communication; JSON for external APIs.

```csharp
using MemoryPack;

/// <summary>
/// Version-tolerant serializable DTO for cache/queue use.
/// </summary>
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class CachedOrder
{
    [MemoryPackOrder(0)] public Guid Id { get; set; }
    [MemoryPackOrder(1)] public string TenantId { get; set; } = string.Empty;
    [MemoryPackOrder(2)] public decimal Total { get; set; }
    [MemoryPackOrder(3)] public List<OrderItem> Items { get; set; } = [];
}

// Serialize / Deserialize
byte[] bytes = MemoryPackSerializer.Serialize(order);
CachedOrder? result = MemoryPackSerializer.Deserialize<CachedOrder>(bytes);
```

### MemoryPack + TypeScript Generation

Use `[GenerateTypeScript]` to auto-generate TypeScript types from C# DTOs:

```csharp
/// <summary>
/// DTO with both MemoryPack serialization and TypeScript type generation.
/// </summary>
[MemoryPackable(SerializeLayout.Explicit)]
[GenerateTypeScript]
public partial class OrderDto
{
    [MemoryPackOrder(0)] public Guid Id { get; set; }
    [MemoryPackOrder(1)] public string Name { get; set; } = string.Empty;
    [MemoryPackOrder(2)] public decimal Total { get; set; }
}

// Generated TypeScript appears in Client/src/generated/memorypack/
// Keep API contracts in sync automatically
```

| Use Case | Format | Reason |
|----------|--------|--------|
| Redis / Cache | MemoryPack | Speed + compact size |
| Message Queues | MemoryPack | High throughput |
| Inter-service RPC | MemoryPack | Low latency |
| Public REST APIs | JSON | Browser compatibility |
| Config files | JSON / YAML | Human readable |

---

## Documentation

### C# — XML Documentation

```csharp
/// <summary>
/// Retrieves all orders for the authenticated tenant.
/// </summary>
/// <remarks>
/// Results are paginated and sorted by CreatedAt descending.
/// Requires <c>orders:read</c> permission.
/// </remarks>
/// <param name="page">Page number (1-indexed).</param>
/// <param name="pageSize">Items per page (max 100).</param>
/// <returns>Paginated list of orders.</returns>
/// <response code="200">Returns the order list.</response>
/// <response code="403">Insufficient permissions.</response>
[HttpGet]
[ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
public async Task<IActionResult> GetOrders(int page = 1, int pageSize = 20) { ... }
```

### TypeScript — JSDoc

```typescript
/**
 * Retrieves all orders for the authenticated tenant.
 *
 * @param page - Page number (1-indexed)
 * @param pageSize - Items per page (max 100)
 * @returns Paginated list of orders
 * @throws {ApiError} AUTH_FORBIDDEN if insufficient permissions
 */
export async function getOrders(page = 1, pageSize = 20): Promise<PagedResult<Order>> { ... }
```

---

## OWASP Cheat Sheets (Server-Specific)

In addition to the shared OWASP table, always consult these for backend work:

| Area | Cheat Sheet |
|------|------------|
| **.NET Specific** | `DotNet_Security_Cheat_Sheet.md` |
| **Node.js Specific** | `Nodejs_Security_Cheat_Sheet.md`, `NPM_Security_Cheat_Sheet.md` |
| **REST APIs** | `REST_Security_Cheat_Sheet.md` |
| **SQL/Database** | `SQL_Injection_Prevention_Cheat_Sheet.md`, `Query_Parameterization_Cheat_Sheet.md` |
| **Microservices** | `Microservices_Security_Cheat_Sheet.md`, `Docker_Security_Cheat_Sheet.md`, `Kubernetes_Security_Cheat_Sheet.md` |
| **Secrets** | `Secrets_Management_Cheat_Sheet.md` |

---

## Server-Side Checklist (Pre-Merge)

- [ ] All endpoints enforce tenant isolation and role-based authorization
- [ ] Structured error responses with error codes (no raw exceptions to clients)
- [ ] Typed exception hierarchy — services throw domain exceptions, middleware maps to HTTP
- [ ] Structured logging via `[LoggerMessage]` source generator — no string interpolation
- [ ] OTel tracing spans on all external calls (DB, HTTP, queue)
- [ ] RED metrics (rate, errors, duration) for new endpoints
- [ ] Domain-specific `ActivitySource` + `Meter` registered in `ObservabilityDomains`
- [ ] Health-check endpoints (`/health`, `/ready`) tested — including config validation
- [ ] Input validation via FluentValidation with `.WithErrorCode()` on every rule
- [ ] Parameterized queries — no SQL concatenation
- [ ] No secrets in code, logs, or config files
- [ ] BDD `.feature` files written before implementation
- [ ] Architecture tests pass (layer boundaries, naming conventions)
- [ ] Integration tests — services tested without running host (thin trigger pattern)
- [ ] 90%+ test coverage on new code (enforced in CI)
- [ ] Security headers configured (CSP, X-Content-Type-Options, X-Frame-Options)
- [ ] Rate limiting configured for public endpoints
- [ ] MemoryPack for internal serialization, JSON for public APIs (C#)
- [ ] No magic strings — use constants classes for shared keys
