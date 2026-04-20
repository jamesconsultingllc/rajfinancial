using System.Reflection;
using RajFinancial.Shared.Contracts.Entities;

namespace RajFinancial.Architecture.Tests;

// ============================================================================
// DTO architectural invariants.
// ----------------------------------------------------------------------------
// Request/response contracts under `RajFinancial.Shared.Contracts.*` represent
// the wire protocol and must not leak domain entity *types* (the classes
// that back the EF model). Value-type enums from `RajFinancial.Shared.Entities.*`
// (e.g., EntityType, TrustType, AccessType) are deliberately exempt because
// the wire protocol legitimately mirrors those enum values.
// Enforced: AGENT.md "Architecture Conventions (Enforced)" — layering rule.
// ============================================================================
public class DtoInvariantsTests
{
    [Fact]
    public void Contracts_ShouldNotDependOnEntityClasses()
    {
        var contractAssembly = typeof(EntityDto).Assembly;
        var contractTypes = GetContractTypes(contractAssembly);
        var entityClassFullNames = GetEntityClassFullNames(contractAssembly);

        var violations = contractTypes
            .Select(ct => new Violation(
                ct,
                ct.GetDependencies().Where(entityClassFullNames.Contains).ToArray()))
            .Where(v => v.BadRefs.Length > 0)
            .ToArray();

        violations.Should().BeEmpty(
            "DTOs (Contracts.*) must not reference concrete entity classes. " +
            "Mirror fields explicitly or reuse entity *enums*. Offenders: " +
            FormatViolations(violations));
    }

    private static Type[] GetContractTypes(Assembly contractAssembly)
        => Types
            .InAssembly(contractAssembly)
            .That()
            .ResideInNamespaceStartingWith("RajFinancial.Shared.Contracts")
            .GetTypes()
            .ToArray();

    private static HashSet<string> GetEntityClassFullNames(Assembly contractAssembly)
        => contractAssembly
            .GetTypes()
            .Where(IsConcreteEntityClass)
            .Select(t => t.FullName!)
            .ToHashSet(StringComparer.Ordinal);

    private static bool IsConcreteEntityClass(Type t)
        => t.Namespace is not null
            && t.Namespace.StartsWith("RajFinancial.Shared.Entities", StringComparison.Ordinal)
            && t is { IsClass: true, IsAbstract: false };

    private static string FormatViolations(IEnumerable<Violation> violations)
        => string.Join("; ",
            violations.Select(v => $"{v.Contract.FullName} -> [{string.Join(", ", v.BadRefs)}]"));

    private sealed record Violation(Type Contract, string[] BadRefs);
}

// ============================================================================
// Minimal dependency walker for DtoInvariantsTests.
// NetArchTest exposes `HaveDependencyOnAny` but it reports booleans per-type
// without listing the offending dependency names. We need the names for the
// failure message, so we walk MetadataTokens via reflection.
// ============================================================================
internal static class TypeDependencyExtensions
{
    private const BindingFlags ALL_MEMBERS =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private const BindingFlags ALL_CONSTRUCTORS =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static IEnumerable<string> GetDependencies(this Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in EnumerateDependencyTypes(type).Select(t => t.FullName))
        {
            if (name is not null && seen.Add(name))
            {
                yield return name;
            }
        }
    }

    private static IEnumerable<Type> EnumerateDependencyTypes(Type type)
        => Ancestors(type)
            .Concat(FieldTypes(type))
            .Concat(PropertyTypes(type))
            .Concat(ConstructorParameterTypes(type));

    private static IEnumerable<Type> FieldTypes(Type type)
        => type.GetFields(ALL_MEMBERS).SelectMany(f => UnwrapType(f.FieldType));

    private static IEnumerable<Type> PropertyTypes(Type type)
        => type.GetProperties(ALL_MEMBERS).SelectMany(p => UnwrapType(p.PropertyType));

    private static IEnumerable<Type> ConstructorParameterTypes(Type type)
        => type.GetConstructors(ALL_CONSTRUCTORS)
               .SelectMany(c => c.GetParameters())
               .SelectMany(p => UnwrapType(p.ParameterType));

    /// <summary>
    /// Recursively unwraps array element types and generic type arguments so the
    /// dependency walker detects entity references hidden inside <c>List&lt;T&gt;</c>,
    /// <c>T[]</c>, <c>IReadOnlyList&lt;T&gt;</c>, <c>Nullable&lt;T&gt;</c>, etc.
    /// </summary>
    private static IEnumerable<Type> UnwrapType(Type type)
    {
        var visited = new HashSet<Type>();
        var stack = new Stack<Type>();
        stack.Push(type);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
                continue;

            yield return current;

            foreach (var nested in GetNestedTypes(current))
                stack.Push(nested);
        }
    }

    private static IEnumerable<Type> GetNestedTypes(Type type)
    {
        if (type.HasElementType)
        {
            var element = type.GetElementType();
            if (element is not null)
                yield return element;
        }

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
                yield return arg;
        }
    }

    private static IEnumerable<Type> Ancestors(Type type)
    {
        for (var cur = type.BaseType; cur is not null; cur = cur.BaseType)
        {
            yield return cur;
        }

        foreach (var interfaceType in type.GetInterfaces())
        {
            yield return interfaceType;
        }
    }
}
