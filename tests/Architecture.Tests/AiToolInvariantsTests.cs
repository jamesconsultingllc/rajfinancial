using RajFinancial.Api.Services.Ai.Tools;

namespace RajFinancial.Architecture.Tests;

// ============================================================================
// AI tool registry / telemetry architectural invariants.
// ----------------------------------------------------------------------------
// Per docs/plans b1-tool-calling-host §PR-2 and AGENTS.md provider isolation
// rules:
//   1. AiToolRegistry is sealed (single immutable snapshot, no subclass override).
//   2. Tool registry & telemetry types live under Services.Ai.Tools / .Telemetry
//      and DO NOT reference Anthropic SDK namespaces (provider isolation).
//   3. Tool registry & telemetry types DO NOT reference Microsoft.Azure.Functions
//      types (FunctionContext etc.) — they must work in any host.
// ============================================================================
public class AiToolInvariantsTests
{
    private static readonly System.Reflection.Assembly ApiAssembly =
        typeof(IAiToolRegistry).Assembly;

    [Fact]
    public void AiToolRegistry_ShouldBeSealedAndNonPublic()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IAiToolRegistry))
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .And()
            .NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "every IAiToolRegistry implementation must be sealed AND non-public " +
            "(internal). Public exposure would let consumers depend on the concrete " +
            "type and bypass the read-only interface contract. Offenders: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void AiServices_ShouldNotReferenceAnthropicSdk()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceStartingWith("RajFinancial.Api.Services.Ai.Tools")
            .Or()
            .ResideInNamespaceStartingWith("RajFinancial.Api.Services.Ai.Telemetry")
            .ShouldNot()
            .HaveDependencyOn("Anthropic.SDK")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "tool registry & telemetry types must remain provider-agnostic. Offenders: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void AiServices_ShouldNotReferenceFunctionContext()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceStartingWith("RajFinancial.Api.Services.Ai.Tools")
            .Or()
            .ResideInNamespaceStartingWith("RajFinancial.Api.Services.Ai.Telemetry")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Azure.Functions.Worker")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "tool registry & telemetry types must not couple to the Functions host. Offenders: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void IAiToolRegistry_ShouldNotExposeMutationMethods()
    {
        var mutators = typeof(IAiToolRegistry).GetMethods()
            .Where(m => m.Name.StartsWith("Register", StringComparison.Ordinal)
                || m.Name.StartsWith("Add", StringComparison.Ordinal)
                || m.Name.StartsWith("Build", StringComparison.Ordinal)
                || m.Name.StartsWith("Remove", StringComparison.Ordinal))
            .ToList();

        mutators.Should().BeEmpty(
            "IAiToolRegistry must be read-only — descriptors are bound at DI build time. " +
            "Mutation methods found: " + string.Join(", ", mutators.Select(m => m.Name)));
    }
}
