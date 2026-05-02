using RajFinancial.Api.Services.Ai.Abstractions;

namespace RajFinancial.Architecture.Tests;

// ============================================================================
// AI provider architectural invariants.
// ----------------------------------------------------------------------------
// Every IChatClientProvider implementation:
//   1. Lives in `RajFinancial.Api.Services.Ai.Providers` (or a sub-namespace).
//   2. Is `sealed` — providers are leaf types; behaviour is composed via
//      Microsoft.Extensions.AI middleware (UseOpenTelemetry, etc.), not via
//      inheritance.
//   3. Has a single public constructor — DI clarity, no ambiguous activation.
// Enforced: AGENTS.md observability rules (per-domain ActivitySource/Meter)
// + plan docs/plans/2026-05-01-ab545-anthropic-provider.md §"Approach".
// ============================================================================
public class AiProviderInvariantsTests
{
    // Anchor on the API assembly via the abstraction (IChatClientProvider lives in
    // RajFinancial.Api.Services.Ai.Abstractions). This decouples the architecture test
    // from any one concrete provider — Anthropic can be removed or renamed without
    // breaking the invariant suite, while a new offending provider in the same assembly
    // is still caught.
    private static readonly System.Reflection.Assembly ApiAssembly =
        typeof(IChatClientProvider).Assembly;

    private const string ProvidersNamespace = "RajFinancial.Api.Services.Ai.Providers";

    [Fact]
    public void Providers_ShouldLiveUnderProvidersNamespace()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IChatClientProvider))
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceStartingWith(ProvidersNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"every IChatClientProvider implementation must live under '{ProvidersNamespace}'. " +
            "Offenders: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Providers_ShouldBeSealed()
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IChatClientProvider))
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "every IChatClientProvider implementation must be sealed. Offenders: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Providers_ShouldHaveSinglePublicConstructor()
    {
        var providerTypes = ApiAssembly
            .GetTypes()
            .Where(t => t.IsClass
                && !t.IsAbstract
                && typeof(IChatClientProvider).IsAssignableFrom(t))
            .ToList();

        providerTypes.Should().NotBeEmpty(
            "at least one IChatClientProvider implementation must exist (Anthropic).");

        var offenders = providerTypes
            .Where(t => t.GetConstructors(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Length != 1)
            .Select(t => t.FullName)
            .ToList();

        offenders.Should().BeEmpty(
            "every IChatClientProvider implementation must declare exactly one public constructor.");
    }
}
