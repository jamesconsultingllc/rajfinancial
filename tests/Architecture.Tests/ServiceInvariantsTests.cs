using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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
//
// Services must also stay pure of HTTP-boundary types. `HttpRequestData`,
// `HttpResponseData`, and `FunctionContext` belong to the Functions worker
// boundary; if a service depends on them it has either inverted the layering
// (the Function should be shaping the HTTP, not the service) or it is reading
// auth/correlation state by leaning on the boundary instead of accepting
// arguments. Enforce the boundary here so it cannot drift back in.
// Enforced: AGENT.md "Architecture Conventions (Enforced)" rule #3 + canonical
// service/function pattern (docs/patterns/service-function-pattern.md).
// ============================================================================
public class ServiceInvariantsTests
{
    private const string ServicesNamespacePrefix = "RajFinancial.Api.Services.";

    private static readonly string HttpRequestDataFullName = typeof(HttpRequestData).FullName!;
    private static readonly string HttpResponseDataFullName = typeof(HttpResponseData).FullName!;
    private static readonly string FunctionContextFullName = typeof(FunctionContext).FullName!;

    private static readonly Assembly ApiAssembly = typeof(EntityService).Assembly;

    [Fact]
    public void Services_ShouldNotHavePrivateStaticMethods()
    {
        var offenders = ApiAssembly.GetTypes()
            .Where(t => t.Namespace is not null
                        && t.Namespace.StartsWith(ServicesNamespacePrefix, StringComparison.Ordinal)
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

    [Fact]
    public void Services_ShouldNotReferenceHttpRequestData()
    {
        AssertServicesDoNotDependOn(
            HttpRequestDataFullName,
            "services must not depend on HttpRequestData — keep HTTP-boundary types in Functions and pass plain CLR arguments into services.");
    }

    [Fact]
    public void Services_ShouldNotReferenceHttpResponseData()
    {
        AssertServicesDoNotDependOn(
            HttpResponseDataFullName,
            "services must not depend on HttpResponseData — services return DTOs/throw exceptions; Functions shape the HTTP response.");
    }

    [Fact]
    public void Services_ShouldNotReferenceFunctionContext()
    {
        AssertServicesDoNotDependOn(
            FunctionContextFullName,
            "services must not depend on FunctionContext — pass user/correlation state through explicit arguments, not by reaching into the worker boundary.");
    }

    private static void AssertServicesDoNotDependOn(string forbiddenTypeFullName, string because)
    {
        var result = Types
            .InAssembly(ApiAssembly)
            .That()
            .ResideInNamespaceStartingWith(ServicesNamespacePrefix)
            .Should()
            .NotHaveDependencyOn(forbiddenTypeFullName)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because + " Offenders: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    private static bool IsCompilerGenerated(MethodInfo method) =>
        method.Name.StartsWith('<')
        || method.GetCustomAttribute<CompilerGeneratedAttribute>() is not null
        || method.GetCustomAttribute<GeneratedRegexAttribute>() is not null
        || method.GetCustomAttribute<LoggerMessageAttribute>() is not null;
}

