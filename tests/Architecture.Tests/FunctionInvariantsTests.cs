using RajFinancial.Api.Data;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Api.Services.EntityService;

namespace RajFinancial.Architecture.Tests;

// ============================================================================
// Function architectural invariants.
// ----------------------------------------------------------------------------
// Azure Functions (`RajFinancial.Api.Functions.*`) are the outermost HTTP/API
// boundary. They must not take a direct dependency on `ApplicationDbContext`
// or `IAuthorizationService`. All data access and authorization flow through
// services, so authorization, validation, and cross-cutting logic run exactly
// once at the right layer (the service).
// Enforced: AGENT.md "Architecture Conventions (Enforced)" — dependency
// boundaries, ADR 0001 / Mode A authorization.
// ============================================================================
public class FunctionInvariantsTests
{
    [Fact]
    public void Functions_ShouldNotReferenceApplicationDbContext()
    {
        var result = Types
            .InAssembly(typeof(EntityService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("RajFinancial.Api.Functions")
            .Should()
            .NotHaveDependencyOn(typeof(ApplicationDbContext).FullName)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Azure Functions must go through a service layer — do not inject ApplicationDbContext directly. " +
            "Offenders: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Functions_ShouldNotReferenceIAuthorizationService()
    {
        // Mode A authorization (owner-scoped endpoints) is the service's job.
        // Functions wire the request to a service and shape responses; if a
        // function pulls IAuthorizationService directly, auth ends up running
        // outside the database transaction and bypasses the service-layer
        // contract that drives ADR 0001 (IDOR -> 404). Mode B (admin role-gated)
        // endpoints rely on AuthorizationMiddleware, NOT on a Function-level
        // dependency, so the rule applies uniformly across the boundary.
        var result = Types
            .InAssembly(typeof(EntityService).Assembly)
            .That()
            .ResideInNamespaceStartingWith("RajFinancial.Api.Functions")
            .Should()
            .NotHaveDependencyOn(typeof(IAuthorizationService).FullName)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Azure Functions must not depend on IAuthorizationService — authorization belongs in services (ADR 0001 / Mode A). " +
            "Offenders: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }
}

