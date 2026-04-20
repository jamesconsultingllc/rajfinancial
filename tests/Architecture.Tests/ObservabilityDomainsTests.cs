using System.Reflection;
using RajFinancial.Api.Configuration;

namespace RajFinancial.Architecture.Tests;

// ============================================================================
// Drift guard for ObservabilityDomains.
// ----------------------------------------------------------------------------
// ObservabilityDomains declares one internal const per instrumentation domain
// and exposes them through a read-only `All` list consumed by
// ObservabilityRegistration (AddSource / AddMeter). Adding a new const without
// updating `All` silently drops the domain from OTel registration — spans and
// metrics would be emitted but never exported.
//
// This test reflects over the const fields and asserts the `All` set matches.
// Adding an 8th domain should require only updating the backing list; this
// test stays green without edits.
// ============================================================================
public class ObservabilityDomainsTests
{
    [Fact]
    public void All_Reflects_Every_Declared_Const_String_Field()
    {
        var declared = typeof(ObservabilityDomains)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        ObservabilityDomains.All
            .ToHashSet(StringComparer.Ordinal)
            .SetEquals(declared)
            .Should().BeTrue(
                "ObservabilityDomains.All must stay in lockstep with the declared const fields. " +
                "Declared: {0}. In .All: {1}.",
                string.Join(", ", declared.OrderBy(x => x)),
                string.Join(", ", ObservabilityDomains.All.OrderBy(x => x)));
    }
}
