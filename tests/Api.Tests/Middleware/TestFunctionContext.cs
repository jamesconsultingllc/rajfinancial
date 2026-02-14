using System.Collections;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace RajFinancial.Api.Tests.Middleware;

/// <summary>
/// Minimal test implementation of FunctionContext for unit testing.
/// </summary>
internal class TestFunctionContext : FunctionContext
{
    private readonly Dictionary<object, object> items = new();
    private readonly TestInvocationFeatures features = new();
    private IServiceProvider instanceServices = null!;

    public TestFunctionContext()
    {
        // Register a no-op IHttpRequestDataFeature so GetHttpRequestDataAsync()
        // returns null instead of throwing NRE via DefaultHttpRequestDataFeature.
        features.Set<IHttpRequestDataFeature>(new NullHttpRequestDataFeature());
    }

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
    public override IInvocationFeatures Features => features;
}

/// <summary>
/// Returns null for HTTP request data, simulating a non-HTTP trigger or missing request.
/// Prevents NullReferenceException when middleware calls GetHttpRequestDataAsync().
/// </summary>
internal class NullHttpRequestDataFeature : IHttpRequestDataFeature
{
    public ValueTask<HttpRequestData?> GetHttpRequestDataAsync(FunctionContext context)
    {
        return ValueTask.FromResult<HttpRequestData?>(null);
    }
}

/// <summary>
/// Minimal IInvocationFeatures implementation for unit testing.
/// </summary>
internal class TestInvocationFeatures : IInvocationFeatures
{
    private readonly Dictionary<Type, object> _features = new();

    public void Set<T>(T instance)
    {
        _features[typeof(T)] = instance!;
    }

    public T? Get<T>()
    {
        return _features.TryGetValue(typeof(T), out var value) ? (T)value : default;
    }

    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => _features.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
