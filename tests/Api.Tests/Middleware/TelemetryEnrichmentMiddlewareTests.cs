using System.Collections.Immutable;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Moq;
using RajFinancial.Api.Middleware;

namespace RajFinancial.Api.Tests.Middleware;

/// <summary>
///     Unit tests for <see cref="TelemetryEnrichmentMiddleware"/>.
/// </summary>
public sealed class TelemetryEnrichmentMiddlewareTests
{
    private static readonly ActivitySource TestActivitySource = new("TelemetryEnrichmentMiddlewareTests");

    static TelemetryEnrichmentMiddlewareTests()
    {
        // Ensure ActivitySource samples our test spans regardless of listener configuration.
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source.Name == TestActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => { },
            ActivityStopped = _ => { },
        });
    }

    [Fact]
    public async Task Tags_user_id_and_tenant_id_from_function_context()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var context = new BindingDataFunctionContext()
            .WithAuthentication(userId)
            .WithTenantId(tenantId);

        using var activity = TestActivitySource.StartActivity("test");
        activity.Should().NotBeNull(because: "the test ActivityListener samples AllData");

        var middleware = new TelemetryEnrichmentMiddleware();
        await middleware.Invoke(context, _ => Task.CompletedTask);

        activity!.GetTagItem("user.id").Should().Be(userId.ToString());
        activity.GetTagItem("user.tenant_id").Should().Be(tenantId.ToString());
    }

    [Fact]
    public async Task Tags_user_role_from_function_context_user_roles()
    {
        // GetUserRoles + UserRoleMapper.MapHighestPriority maps Administrator > Advisor > Client.
        var context = new BindingDataFunctionContext()
            .WithAuthentication(Guid.NewGuid())
            .WithRoles("Client", "Advisor");

        using var activity = TestActivitySource.StartActivity("test");
        var middleware = new TelemetryEnrichmentMiddleware();
        await middleware.Invoke(context, _ => Task.CompletedTask);

        activity!.GetTagItem("user.role").Should().Be("Advisor");
    }

    [Fact]
    public async Task Skips_user_role_when_no_roles_are_present()
    {
        var context = new BindingDataFunctionContext()
            .WithAuthentication(Guid.NewGuid());

        using var activity = TestActivitySource.StartActivity("test");
        var middleware = new TelemetryEnrichmentMiddleware();
        await middleware.Invoke(context, _ => Task.CompletedTask);

        activity!.GetTagItem("user.role").Should().BeNull();
    }

    [Fact]
    public async Task Tags_allow_listed_route_values_from_binding_data()
    {
        // Real Azure Functions HTTP triggers populate `id` and `roleId` from
        // route templates like `entities/{id}/roles/{roleId}`. Earlier
        // semantic names (assetId/entityId/grantId) never appear in BindingData.
        var id = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var context = new BindingDataFunctionContext(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["roleId"] = roleId,
            ["someUnrelatedKey"] = "ignored",
            ["assetId"] = "should-be-ignored",
        });

        using var activity = TestActivitySource.StartActivity("test");
        var middleware = new TelemetryEnrichmentMiddleware();
        await middleware.Invoke(context, _ => Task.CompletedTask);

        activity!.GetTagItem("route.id").Should().Be(id.ToString());
        activity.GetTagItem("route.roleId").Should().Be(roleId.ToString());
        activity.GetTagItem("route.someUnrelatedKey").Should().BeNull();
        activity.GetTagItem("route.assetId").Should().BeNull();
    }

    [Fact]
    public async Task Tags_route_template_with_function_name_for_http_triggers()
    {
        var context = new BindingDataFunctionContext()
            .WithAuthentication(Guid.NewGuid())
            .WithFunctionDefinition("GetAssetById", "httpTrigger");

        using var activity = TestActivitySource.StartActivity("test");
        var middleware = new TelemetryEnrichmentMiddleware();
        await middleware.Invoke(context, _ => Task.CompletedTask);

        activity!.GetTagItem("route.template").Should().Be("GetAssetById");
    }

    [Fact]
    public async Task Skips_route_template_for_non_http_triggers()
    {
        var context = new BindingDataFunctionContext()
            .WithAuthentication(Guid.NewGuid())
            .WithFunctionDefinition("OnTimerTick", "timerTrigger");

        using var activity = TestActivitySource.StartActivity("test");
        var middleware = new TelemetryEnrichmentMiddleware();
        await middleware.Invoke(context, _ => Task.CompletedTask);

        activity!.GetTagItem("route.template").Should().BeNull();
    }

    [Fact]
    public async Task Is_no_op_when_Activity_Current_is_null()
    {
        // No StartActivity → Activity.Current stays null. The middleware must
        // simply pass through without tagging.
        Activity.Current = null;
        var context = new BindingDataFunctionContext()
            .WithAuthentication(Guid.NewGuid());

        var middleware = new TelemetryEnrichmentMiddleware();
        var act = async () => await middleware.Invoke(context, _ => Task.CompletedTask);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Calls_next_exactly_once()
    {
        var context = new BindingDataFunctionContext().WithAuthentication(Guid.NewGuid());
        var callCount = 0;

        using var activity = TestActivitySource.StartActivity("test");
        var middleware = new TelemetryEnrichmentMiddleware();
        await middleware.Invoke(context, _ =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        callCount.Should().Be(1);
    }
}

/// <summary>
///     Test FunctionContext that exposes an in-memory <see cref="BindingContext"/>
///     so route-value tagging can be exercised.
/// </summary>
internal sealed class BindingDataFunctionContext : FunctionContext
{
    private readonly Dictionary<object, object> items = new();
    private readonly TestBindingContext bindingContext;
    private FunctionDefinition? functionDefinition;

    public BindingDataFunctionContext(IReadOnlyDictionary<string, object?>? bindingData = null)
    {
        bindingContext = new TestBindingContext(bindingData ?? new Dictionary<string, object?>());
    }

    public BindingDataFunctionContext WithAuthentication(Guid userId)
    {
        items[FunctionContextKeys.UserIdGuid] = userId;
        items[FunctionContextKeys.IsAuthenticated] = true;
        return this;
    }

    public BindingDataFunctionContext WithTenantId(Guid tenantId)
    {
        items[FunctionContextKeys.TenantId] = tenantId;
        return this;
    }

    public BindingDataFunctionContext WithRoles(params string[] roles)
    {
        items[FunctionContextKeys.UserRoles] = (IReadOnlyList<string>)roles;
        return this;
    }

    public BindingDataFunctionContext WithFunctionDefinition(string name, string triggerType)
    {
        var binding = new Mock<BindingMetadata>();
        binding.SetupGet(b => b.Name).Returns("req");
        binding.SetupGet(b => b.Type).Returns(triggerType);
        binding.SetupGet(b => b.Direction).Returns(BindingDirection.In);

        var inputBindings = new Dictionary<string, BindingMetadata>
        {
            ["req"] = binding.Object,
        }.ToImmutableDictionary();

        var def = new Mock<FunctionDefinition>();
        def.SetupGet(d => d.Name).Returns(name);
        def.SetupGet(d => d.InputBindings).Returns(inputBindings);

        functionDefinition = def.Object;
        return this;
    }

    public override IDictionary<object, object> Items
    {
        get => items;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            items.Clear();
            foreach (var pair in value)
            {
                items[pair.Key] = pair.Value;
            }
        }
    }

    public override BindingContext BindingContext => bindingContext;

    public override string InvocationId => Guid.NewGuid().ToString();
    public override string FunctionId => "test-fn";
    public override TraceContext TraceContext => null!;
#pragma warning disable CS8764
    public override RetryContext? RetryContext => null;
#pragma warning restore CS8764
    public override IServiceProvider InstanceServices { get; set; } = null!;
    public override FunctionDefinition FunctionDefinition => functionDefinition!;
    public override IInvocationFeatures Features => null!;
    public override CancellationToken CancellationToken => CancellationToken.None;

    private sealed class TestBindingContext(IReadOnlyDictionary<string, object?> data) : BindingContext
    {
        public override IReadOnlyDictionary<string, object?> BindingData => data;
    }
}
