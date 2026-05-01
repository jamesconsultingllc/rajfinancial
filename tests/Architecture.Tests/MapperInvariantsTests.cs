using RajFinancial.Api.Services.EntityService;

namespace RajFinancial.Architecture.Tests;

// ============================================================================
// Mapper architectural invariants.
// ----------------------------------------------------------------------------
// Any type named `*Mapper` must be a pure static class (no state, no
// instance, no DI). Mappers are value translations and must be trivially
// reusable without touching a container.
// Enforced: AGENTS.md "Architecture Conventions (Enforced)" rule #2.
// ============================================================================
public class MapperInvariantsTests
{
    [Fact]
    public void Mappers_ShouldBeStaticClasses()
    {
        var result = Types
            .InAssembly(typeof(EntityService).Assembly)
            .That()
            .HaveNameEndingWith("Mapper")
            .Should()
            .BeAbstract() // static classes compile as abstract+sealed
            .And()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "types named *Mapper must be static (abstract + sealed). Offenders: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }
}
