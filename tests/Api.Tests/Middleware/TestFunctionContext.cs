using System.Collections;
using System.Collections.Specialized;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace RajFinancial.Api.Tests.Middleware;

/// <summary>
/// Minimal test implementation of FunctionContext for unit testing.
/// Provides fluent builder methods for setting up authentication, request
/// bodies, query parameters, and HTTP response creation for function-level tests.
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

    // =========================================================================
    // Legacy method – kept for backward compatibility with existing tests
    // =========================================================================

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

    // =========================================================================
    // Fluent builder methods for function-level unit tests
    // =========================================================================

    /// <summary>
    /// Configures the context as an authenticated user with the specified user ID.
    /// Sets <c>Items["UserIdGuid"]</c>, <c>Items["UserId"]</c>, and
    /// <c>Items["IsAuthenticated"]</c> so that <c>GetUserIdAsGuid()</c>
    /// and <c>IsAuthenticated()</c> return the expected values.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="email">Optional email address stored in <c>Items["UserEmail"]</c>.</param>
    /// <param name="name">Optional display name stored in <c>Items["UserName"]</c>.</param>
    /// <param name="roles">Optional roles stored in <c>Items["UserRoles"]</c>.</param>
    /// <returns>This context for fluent chaining.</returns>
    public TestFunctionContext WithAuthentication(
        Guid userId,
        string? email = null,
        string? name = null,
        params string[] roles)
    {
        Items["UserIdGuid"] = userId;
        Items["UserId"] = userId.ToString();
        Items["IsAuthenticated"] = true;

        if (email is not null)
            Items["UserEmail"] = email;
        if (name is not null)
            Items["UserName"] = name;
        if (roles.Length > 0)
            Items["UserRoles"] = roles;

        return this;
    }

    /// <summary>
    /// Sets the request body in the context as a JSON-serialized string.
    /// The <see cref="RajFinancial.Api.Middleware.ValidationExtensions.GetValidatedBodyAsync{T}"/>
    /// extension reads from <c>Items["RequestBody"]</c>.
    /// </summary>
    /// <typeparam name="T">The type of the request body.</typeparam>
    /// <param name="body">The object to serialize as the request body.</param>
    /// <returns>This context for fluent chaining.</returns>
    public TestFunctionContext WithRequestBody<T>(T body)
    {
        Items["RequestBody"] = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Ensure InstanceServices is set so GetValidatedBodyAsync can resolve
        // IValidator<T> without throwing ArgumentNullException.
        instanceServices ??= new ServiceCollection().BuildServiceProvider();

        return this;
    }

    /// <summary>
    /// Sets the response content type for content negotiation.
    /// Defaults to <c>application/json</c> if not called.
    /// </summary>
    /// <param name="contentType">The response content type.</param>
    /// <returns>This context for fluent chaining.</returns>
    public TestFunctionContext WithResponseContentType(string contentType = "application/json")
    {
        Items["ResponseContentType"] = contentType;
        return this;
    }

    /// <summary>
    /// Configures <see cref="InstanceServices"/> with the specified service provider.
    /// Use this when the function under test resolves services from DI
    /// (e.g., <c>IValidator&lt;T&gt;</c> for body validation).
    /// </summary>
    /// <param name="provider">The service provider to use.</param>
    /// <returns>This context for fluent chaining.</returns>
    public TestFunctionContext WithServices(IServiceProvider provider)
    {
        InstanceServices = provider;
        return this;
    }

    /// <summary>
    /// Builds a <see cref="ServiceCollection"/>, registers the given services,
    /// and configures <see cref="InstanceServices"/>.
    /// </summary>
    /// <param name="configure">Action to register services.</param>
    /// <returns>This context for fluent chaining.</returns>
    public TestFunctionContext WithServices(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        InstanceServices = services.BuildServiceProvider();
        return this;
    }

    /// <summary>
    /// Creates a fully configured <see cref="Mock{HttpRequestData}"/> with support
    /// for query parameters, headers, and <see cref="HttpRequestData.CreateResponse"/>.
    /// The returned mock's <c>CreateResponse(statusCode)</c> produces a
    /// <see cref="TestHttpResponseData"/> that has a writable <c>Body</c> stream,
    /// settable <c>StatusCode</c>, and a real <c>Headers</c> collection.
    /// </summary>
    /// <param name="queryParams">Optional query string parameters.</param>
    /// <param name="route">Optional route URL (defaults to <c>https://localhost/api/assets</c>).</param>
    /// <returns>A configured mock request ready for function invocation.</returns>
    public Mock<HttpRequestData> CreateMockHttpRequest(
        NameValueCollection? queryParams = null,
        string route = "https://localhost/api/assets")
    {
        var mockRequest = new Mock<HttpRequestData>(this);

        mockRequest.SetupGet(r => r.Url).Returns(new Uri(route));
        mockRequest.SetupGet(r => r.Query).Returns(queryParams ?? new NameValueCollection());
        mockRequest.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());

        // CreateResponse(HttpStatusCode) is an extension method that internally
        // calls the abstract CreateResponse() then sets StatusCode on the result.
        // We mock the parameterless version since that's the only virtual member.
        mockRequest
            .Setup(r => r.CreateResponse())
            .Returns(() => new TestHttpResponseData(this, HttpStatusCode.OK));

        // Also register as the IHttpRequestDataFeature so middleware
        // calling GetHttpRequestDataAsync() gets this request.
        features.Set<IHttpRequestDataFeature>(
            new TestHttpRequestDataFeature(mockRequest.Object));

        return mockRequest;
    }
}

// =============================================================================
// TestHttpResponseData
// =============================================================================

/// <summary>
/// Concrete <see cref="HttpResponseData"/> implementation for unit tests.
/// Provides a writable <see cref="MemoryStream"/> body and real
/// <see cref="HttpHeadersCollection"/> so that serialization extensions
/// can write response data without any additional mock setup.
/// </summary>
internal class TestHttpResponseData : HttpResponseData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestHttpResponseData"/> class.
    /// </summary>
    /// <param name="context">The function context that owns this response.</param>
    /// <param name="statusCode">The initial HTTP status code.</param>
    public TestHttpResponseData(FunctionContext context, HttpStatusCode statusCode)
        : base(context)
    {
        StatusCode = statusCode;
        Headers = new HttpHeadersCollection();
        Body = new MemoryStream();
    }

    /// <inheritdoc/>
    public override HttpStatusCode StatusCode { get; set; }

    /// <inheritdoc/>
    public override HttpHeadersCollection Headers { get; set; }

    /// <inheritdoc/>
    public override Stream Body { get; set; }

    /// <inheritdoc/>
    public override HttpCookies Cookies => throw new NotImplementedException(
        "Cookies are not supported in TestHttpResponseData");
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
