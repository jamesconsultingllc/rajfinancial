using System.Collections;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

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
    /// <summary>
    /// Gets or sets a custom FunctionDefinition for testing middleware that uses
    /// <see cref="FunctionDefinition.EntryPoint"/> to resolve target methods.
    /// Defaults to null (same behavior as before for existing tests).
    /// </summary>
    public FunctionDefinition? FunctionDefinitionValue { get; set; }

    public override FunctionDefinition FunctionDefinition => FunctionDefinitionValue!;
    public override IDictionary<object, object> Items
    {
        get => items;
        set { /* Items is read-only in this test implementation */ }
    }
    public override IInvocationFeatures Features => features;

    /// <summary>
    /// Configures the test context to return an <see cref="HttpRequestData"/>
    /// with the specified headers. Used for testing middleware that reads
    /// the Authorization header (e.g., JWT parsing path).
    /// </summary>
    public void SetHttpRequestHeaders(HttpHeadersCollection headers)
    {
        var mockContext = new Mock<FunctionContext>();
        var mockRequest = new Mock<HttpRequestData>(mockContext.Object);
        mockRequest.SetupGet(r => r.Url).Returns(new Uri("https://localhost/api/test"));
        mockRequest.SetupGet(r => r.Headers).Returns(headers);

        features.Set<IHttpRequestDataFeature>(new TestHttpRequestDataFeature(mockRequest.Object));
    }
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
/// Returns a pre-configured <see cref="HttpRequestData"/> for testing middleware
/// that reads HTTP request data (e.g., Authorization header for JWT parsing).
/// </summary>
internal class TestHttpRequestDataFeature(HttpRequestData requestData) : IHttpRequestDataFeature
{
    public ValueTask<HttpRequestData?> GetHttpRequestDataAsync(FunctionContext context)
    {
        return ValueTask.FromResult<HttpRequestData?>(requestData);
    }
}

/// <summary>
/// Minimal IInvocationFeatures implementation for unit testing.
/// </summary>
internal class TestInvocationFeatures : IInvocationFeatures
{
    private readonly Dictionary<Type, object> features = new();

    public void Set<T>(T instance)
    {
        features[typeof(T)] = instance!;
    }

    public T? Get<T>()
    {
        return features.TryGetValue(typeof(T), out var value) ? (T)value : default;
    }

    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => features.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
