using System.Diagnostics;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
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
    public async Task Tags_allow_listed_route_values_from_binding_data()
    {
        var assetId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var context = new BindingDataFunctionContext(new Dictionary<string, object?>
        {
            ["assetId"] = assetId,
            ["entityId"] = entityId,
            ["someUnrelatedKey"] = "ignored",
        });

        using var activity = TestActivitySource.StartActivity("test");
        var middleware = new TelemetryEnrichmentMiddleware();
        await middleware.Invoke(context, _ => Task.CompletedTask);

        activity!.GetTagItem("route.assetId").Should().Be(assetId.ToString());
        activity.GetTagItem("route.entityId").Should().Be(entityId.ToString());
        activity.GetTagItem("route.someUnrelatedKey").Should().BeNull();
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

    public override IDictionary<object, object> Items
    {
        get => items;
        set => _ = value;
    }

    public override BindingContext BindingContext => bindingContext;

    public override string InvocationId => Guid.NewGuid().ToString();
    public override string FunctionId => "test-fn";
    public override TraceContext TraceContext => null!;
#pragma warning disable CS8764
    public override RetryContext? RetryContext => null;
#pragma warning restore CS8764
    public override IServiceProvider InstanceServices { get; set; } = null!;
    public override FunctionDefinition FunctionDefinition => null!;
    public override IInvocationFeatures Features => null!;
    public override CancellationToken CancellationToken => CancellationToken.None;

    private sealed class TestBindingContext(IReadOnlyDictionary<string, object?> data) : BindingContext
    {
        public override IReadOnlyDictionary<string, object?> BindingData => data;
    }
}
