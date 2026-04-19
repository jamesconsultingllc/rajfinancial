using RajFinancial.Api.Data;
using RajFinancial.Api.Services.EntityService;

namespace RajFinancial.Architecture.Tests;

// ============================================================================
// Function architectural invariants.
// ----------------------------------------------------------------------------
// Azure Functions (`RajFinancial.Api.Functions.*`) are the outermost HTTP/API
// boundary. They must not take a direct dependency on `ApplicationDbContext`.
// All data access flows through services, so authorization, validation, and
// cross-cutting logic run exactly once.
// Enforced: AGENT.md "Architecture Conventions (Enforced)" — dependency boundaries.
// ============================================================================
public class FunctionInvariantsTests
{
    // Types are permitted to depend on ApplicationDbContext directly.
    // HealthCheckFunction pings the DB as part of its liveness probe; adding
    // a HealthService just for one ping-then-dispose call is over-engineering.
    private static readonly string[] DbContextAllowList =
    [
        "HealthCheckFunction",
    ];

    [Fact]
    public void Functions_ShouldNotReferenceApplicationDbContext()
    {
        var result = Types
            .InAssembly(typeof(EntityService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("RajFinancial.Api.Functions")
            .And()
            .DoNotHaveName(DbContextAllowList)
            .Should()
            .NotHaveDependencyOn(typeof(ApplicationDbContext).FullName)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Azure Functions must go through a service layer — do not inject ApplicationDbContext directly. " +
            "Offenders: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }
}
