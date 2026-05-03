using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Services.Ai.Telemetry;
using RajFinancial.Api.Services.Ai.Tools;

namespace RajFinancial.Api.Tests.Services.Ai.Tools;

public class AiToolRegistryTests
{
    private static IAiTelemetryRedactor BuildRedactor() => new DefaultAiTelemetryRedactor(
        Options.Create(new AiTelemetryRedactorOptions
        {
            MerchantHashSecret = new string('x', 32),
            HmacPrefixLength = 12,
        }));

    private static IServiceProvider RootServices() => new ServiceCollection().BuildServiceProvider();

    private static AIFunction MakeTool(string name) =>
        AIFunctionFactory.Create((string input) => $"echo:{input}", name: name);

    [Fact]
    public void Empty_descriptors_yields_empty_registry()
    {
        var registry = new AiToolRegistry([], RootServices(), BuildRedactor());

        registry.IsEmpty.Should().BeTrue();
        registry.Count.Should().Be(0);
        registry.GetAll().Should().BeEmpty();
        registry.GetByScope(AiToolScopes.Diagnostics).Should().BeEmpty();
    }

    [Fact]
    public void Registers_tools_and_groups_by_scope()
    {
        var d1 = new AiToolDescriptor(AiToolScopes.Diagnostics, "diag.ping", _ => MakeTool("diag.ping"));
        var d2 = new AiToolDescriptor(AiToolScopes.Diagnostics, "diag.echo", _ => MakeTool("diag.echo"));
        var d3 = new AiToolDescriptor(AiToolScopes.Assets, "assets.summary", _ => MakeTool("assets.summary"));

        var registry = new AiToolRegistry([d1, d2, d3], RootServices(), BuildRedactor());

        registry.IsEmpty.Should().BeFalse();
        registry.Count.Should().Be(3);
        registry.GetAll().Select(t => t.Name).Should().Equal("diag.ping", "diag.echo", "assets.summary");
        registry.GetByScope(AiToolScopes.Diagnostics).Select(t => t.Name).Should().Equal("diag.ping", "diag.echo");
        registry.GetByScope(AiToolScopes.Assets).Select(t => t.Name).Should().Equal("assets.summary");
    }

    [Fact]
    public void Wraps_each_tool_in_TelemetryAIFunction()
    {
        var d = new AiToolDescriptor(AiToolScopes.Diagnostics, "diag.ping", _ => MakeTool("diag.ping"));
        var registry = new AiToolRegistry([d], RootServices(), BuildRedactor());

        registry.GetAll().Single().Should().BeOfType<TelemetryAIFunction>();
    }

    [Fact]
    public void Duplicate_scope_and_name_throws()
    {
        var d1 = new AiToolDescriptor(AiToolScopes.Diagnostics, "diag.ping", _ => MakeTool("diag.ping"));
        var d2 = new AiToolDescriptor(AiToolScopes.Diagnostics, "diag.ping", _ => MakeTool("diag.ping"));

        var act = () => new AiToolRegistry([d1, d2], RootServices(), BuildRedactor());

        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate AI tool registration*");
    }

    [Fact]
    public void Same_name_in_different_scopes_is_allowed()
    {
        var d1 = new AiToolDescriptor(AiToolScopes.Diagnostics, "ping", _ => MakeTool("ping"));
        var d2 = new AiToolDescriptor(AiToolScopes.Assets, "ping", _ => MakeTool("ping"));

        var registry = new AiToolRegistry([d1, d2], RootServices(), BuildRedactor());

        registry.Count.Should().Be(2);
    }

    [Fact]
    public void Factory_returning_null_throws()
    {
        var d = new AiToolDescriptor(AiToolScopes.Diagnostics, "diag.ping", _ => null!);

        var act = () => new AiToolRegistry([d], RootServices(), BuildRedactor());

        act.Should().Throw<InvalidOperationException>().WithMessage("*returned null*");
    }

    [Fact]
    public void Factory_producing_mismatched_name_throws()
    {
        var d = new AiToolDescriptor(AiToolScopes.Diagnostics, "diag.ping", _ => MakeTool("not_ping"));

        var act = () => new AiToolRegistry([d], RootServices(), BuildRedactor());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AIFunction.Name='not_ping'*registered as 'diag.ping'*");
    }

    [Fact]
    public void Factory_invoked_exactly_once_per_descriptor()
    {
        var calls = 0;
        var d = new AiToolDescriptor(
            AiToolScopes.Diagnostics,
            "diag.ping",
            _ => { Interlocked.Increment(ref calls); return MakeTool("diag.ping"); });

        var registry = new AiToolRegistry([d], RootServices(), BuildRedactor());

        // Multiple reads of the snapshot must not re-invoke the factory.
        _ = registry.GetAll();
        _ = registry.GetByScope(AiToolScopes.Diagnostics);

        calls.Should().Be(1);
    }

    [Fact]
    public void Empty_or_whitespace_scope_throws()
    {
        var d = new AiToolDescriptor(" ", "ping", _ => MakeTool("ping"));
        var act2 = () => new AiToolRegistry([d], RootServices(), BuildRedactor());
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetByScope_with_unknown_scope_returns_empty()
    {
        var d = new AiToolDescriptor(AiToolScopes.Diagnostics, "diag.ping", _ => MakeTool("diag.ping"));
        var registry = new AiToolRegistry([d], RootServices(), BuildRedactor());

        registry.GetByScope("nonexistent").Should().BeEmpty();
    }
}
