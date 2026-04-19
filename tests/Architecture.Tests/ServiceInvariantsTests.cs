using System.Reflection;
using RajFinancial.Api.Services.EntityService;

namespace RajFinancial.Architecture.Tests;

// ============================================================================
// Services architectural invariants.
// ----------------------------------------------------------------------------
// Services under `RajFinancial.Api.Services.*` must delegate pure logic to
// dedicated helper classes (Mappers, Rules, Factories) instead of hiding it
// in private static methods on the service itself. Private static methods
// are a strong SRP smell: pure functions that the service does not need
// state for, but that are not available to the rest of the codebase either.
// Enforced: AGENT.md "Architecture Conventions (Enforced)" rule #3.
// ============================================================================
public class ServiceInvariantsTests
{
    private static readonly Assembly ApiAssembly = typeof(EntityService).Assembly;

    // Types are allowed to have private static methods until extracted into
    // dedicated helper classes under a named SRP cleanup task.
    // Each entry MUST cite the tracking work item.
    private static readonly HashSet<string> PrivateStaticAllowList = new(StringComparer.Ordinal)
    {
        // #623 — EntityService SRP cleanup reference implementation.
        "RajFinancial.Api.Services.EntityService.EntityService",
        // #625 — Service SRP cleanups (UserProfile, Authorization, Asset, DepreciationCalculator).
        "RajFinancial.Api.Services.UserProfiles.UserProfileService",
        "RajFinancial.Api.Services.Authorization.AuthorizationService",
        "RajFinancial.Api.Services.AssetService.AssetService",
        "RajFinancial.Api.Services.AssetService.DepreciationCalculator",
    };

    [Fact]
    public void Services_ShouldNotHavePrivateStaticMethods()
    {
        var offenders = ApiAssembly.GetTypes()
            .Where(t => t.Namespace is not null
                        && t.Namespace.StartsWith("RajFinancial.Api.Services.", StringComparison.Ordinal))
            .Where(t => !PrivateStaticAllowList.Contains(t.FullName ?? string.Empty))
            .Select(t => new
            {
                Type = t,
                PrivateStatics = t.GetMethods(
                        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(m => m is { IsAssembly: false, IsFamily: false } && !IsCompilerGenerated(m))
                    .Select(m => m.Name)
                    .ToArray(),
            })
            .Where(x => x.PrivateStatics.Length > 0)
            .ToArray();

        offenders.Should().BeEmpty(
            "services must not contain private static methods — extract pure logic to named static helper classes. " +
            "Offenders: " + string.Join("; ",
                offenders.Select(o => $"{o.Type.FullName} -> [{string.Join(", ", o.PrivateStatics)}]")));
    }

    private static bool IsCompilerGenerated(MethodInfo method) =>
        method.Name.StartsWith('<')
        || method.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is not null;
}
