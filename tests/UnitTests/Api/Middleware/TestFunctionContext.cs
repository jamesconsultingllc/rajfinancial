using Microsoft.Azure.Functions.Worker;

namespace RajFinancial.UnitTests.Api.Middleware;

/// <summary>
/// Minimal test implementation of FunctionContext for unit testing.
/// </summary>
internal class TestFunctionContext : FunctionContext
{
    private readonly Dictionary<object, object> items = new();
    private IServiceProvider instanceServices = null!;

    public override string InvocationId => Guid.NewGuid().ToString();
    public override string FunctionId => "test-function";
    public override TraceContext TraceContext => null!;
    public override BindingContext BindingContext => null!;
#pragma warning disable CS8764 // Nullability of return type doesn't match overridden member
    public override RetryContext? RetryContext => null;
    public override IServiceProvider InstanceServices
    {
        get => instanceServices;
        set => instanceServices = value;
    }
#pragma warning restore CS8764
    public override FunctionDefinition FunctionDefinition => null!;
    public override IDictionary<object, object> Items
    {
        get => items;
        set { /* Items is read-only in this test implementation */ }
    }
    public override IInvocationFeatures Features => null!;
}