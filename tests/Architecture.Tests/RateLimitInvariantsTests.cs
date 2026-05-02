using NetArchTest.Rules;
using RajFinancial.Api.Services.RateLimit;

namespace RajFinancial.Architecture.Tests;

// ============================================================================
// Rate-limit architectural invariants.
// ----------------------------------------------------------------------------
// 1. IRateLimitStore implementations live under
//    `RajFinancial.Api.Services.RateLimit.Storage` and are sealed (leaf types).
// 2. The rate-limit middleware depends only on the IRateLimitStore abstraction,
//    not on any concrete implementation — keeps the storage decision swappable.
// Enforced: plan docs/plans (B1 PR-1) + AGENTS.md general "interfaces over
// concretions" + "sealed leaves" patterns.
// ============================================================================
public class RateLimitInvariantsTests
{
    private static readonly System.Reflection.Assembly ApiAssembly =
        typeof(IRateLimitStore).Assembly;

    private const string StorageNamespace = "RajFinancial.Api.Services.RateLimit.Storage";

    [Fact]
    public void RateLimitStoreImplementations_ShouldLiveUnderStorageNamespace()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IRateLimitStore))
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespace(StorageNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "IRateLimitStore implementations must live under "
            + StorageNamespace
            + " so the storage abstraction is swappable. Offenders: "
            + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void RateLimitStoreImplementations_ShouldBeSealed()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IRateLimitStore))
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "IRateLimitStore implementations must be sealed (leaf types). Offenders: "
            + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void RateLimitMiddleware_ShouldNotDependOnConcreteStore()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("RajFinancial.Api.Middleware.RateLimit")
            .Should()
            .NotHaveDependencyOn("RajFinancial.Api.Services.RateLimit.Storage.TableStorageRateLimitStore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Rate-limit middleware must depend on IRateLimitStore, not on the "
            + "concrete TableStorageRateLimitStore. Offenders: "
            + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }
}
