using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RajFinancial.Api.Services.EntityService;

namespace RajFinancial.Architecture.Tests;

// ============================================================================
// Services architectural invariants.
// ----------------------------------------------------------------------------
// Types whose name ends with `Service` (i.e., our DI-registered service classes)
// must delegate pure logic to dedicated helper classes (Mappers, Rules,
// Factories, Calculators) instead of hiding it in private static methods on
// the service itself. Private static methods are a strong SRP smell: pure
// functions that the service does not need state for, but that are not
// available to the rest of the codebase either. Helper types in the same
// namespace (`*Mapper`, `*Calculator`, `*Rules`, etc.) are explicitly allowed
// to have private statics — that is their whole purpose.
// Enforced: AGENT.md "Architecture Conventions (Enforced)" rule #3.
// ============================================================================
public class ServiceInvariantsTests
{
    private static readonly Assembly ApiAssembly = typeof(EntityService).Assembly;

    [Fact]
    public void Services_ShouldNotHavePrivateStaticMethods()
    {
        var offenders = ApiAssembly.GetTypes()
            .Where(t => t.Namespace is not null
                        && t.Namespace.StartsWith("RajFinancial.Api.Services.", StringComparison.Ordinal)
                        && t.Name.EndsWith("Service", StringComparison.Ordinal))
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
        || method.GetCustomAttribute<CompilerGeneratedAttribute>() is not null
        || method.GetCustomAttribute<GeneratedRegexAttribute>() is not null
        || method.GetCustomAttribute<LoggerMessageAttribute>() is not null;
}
