using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Middleware.RateLimit;
using RajFinancial.Api.Services.RateLimit;

namespace RajFinancial.Api.Tests.Middleware.RateLimit;

public sealed class RateLimitMiddlewareTests
{
    private static readonly RateLimitPolicy AiPolicy = new(
        RateLimitPolicyKind.AiToolCalling, 60, 1000, RateLimitFailureMode.FailClosed);

    private static (RateLimitMiddleware mw, Mock<IRateLimitPolicyResolver> resolver, Mock<IRateLimitStore> store)
        Build()
    {
        var resolver = new Mock<IRateLimitPolicyResolver>();
        var store = new Mock<IRateLimitStore>();
        var mw = new RateLimitMiddleware(resolver.Object, store.Object,
            NullLogger<RateLimitMiddleware>.Instance);
        return (mw, resolver, store);
    }

    private static TestFunctionContext Ctx(string functionName, string? userId)
    {
        var ctx = new TestFunctionContext { FunctionDefinitionValue = new TestFunctionDefinition(functionName) };
        if (userId is not null) ctx.Items[FunctionContextKeys.UserId] = userId;
        return ctx;
    }

    private static FunctionExecutionDelegate Next(Action<FunctionContext>? observe = null) =>
        ctx =>
        {
            observe?.Invoke(ctx);
            return Task.CompletedTask;
        };

    [Fact]
    public async Task Invoke_NoOpPolicy_PassesThrough()
    {
        var (mw, resolver, store) = Build();
        resolver.Setup(r => r.Resolve(It.IsAny<FunctionContext>())).Returns(RateLimitPolicy.None);
        var called = false;

        await mw.Invoke(Ctx("Foo", "u1"), Next(_ => called = true));

        called.Should().BeTrue();
        store.Verify(s => s.TryConsumeAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_BypassPolicy_PassesThrough()
    {
        var (mw, resolver, store) = Build();
        resolver.Setup(r => r.Resolve(It.IsAny<FunctionContext>())).Returns(RateLimitPolicy.Bypass);

        await mw.Invoke(Ctx("Health", "u1"), Next());

        store.Verify(s => s.TryConsumeAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_MissingUserId_PassesThroughWithoutCallingStore()
    {
        var (mw, resolver, store) = Build();
        resolver.Setup(r => r.Resolve(It.IsAny<FunctionContext>())).Returns(AiPolicy);

        await mw.Invoke(Ctx("AiFn", userId: null), Next());

        store.Verify(s => s.TryConsumeAsync(It.IsAny<string>(), It.IsAny<RateLimitPolicy>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_AllowedDecision_CallsNext()
    {
        var (mw, resolver, store) = Build();
        resolver.Setup(r => r.Resolve(It.IsAny<FunctionContext>())).Returns(AiPolicy);
        store.Setup(s => s.TryConsumeAsync(It.IsAny<string>(), AiPolicy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RateLimitDecision.Allow());
        var called = false;

        await mw.Invoke(Ctx("AiFn", "user-42"), Next(_ => called = true));

        called.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_HashesUserIdBeforeCallingStore()
    {
        var (mw, resolver, store) = Build();
        resolver.Setup(r => r.Resolve(It.IsAny<FunctionContext>())).Returns(AiPolicy);
        string? capturedKey = null;
        store.Setup(s => s.TryConsumeAsync(It.IsAny<string>(), AiPolicy, It.IsAny<CancellationToken>()))
            .Callback<string, RateLimitPolicy, CancellationToken>((k, _, _) => capturedKey = k)
            .ReturnsAsync(RateLimitDecision.Allow());

        await mw.Invoke(Ctx("AiFn", "user-42"), Next());

        capturedKey.Should().NotBeNullOrEmpty();
        capturedKey.Should().NotBe("user-42", "raw user id must be hashed before persistence");
    }

    [Fact]
    public async Task Invoke_RejectedDecision_ThrowsRateLimitedException()
    {
        var (mw, resolver, store) = Build();
        resolver.Setup(r => r.Resolve(It.IsAny<FunctionContext>())).Returns(AiPolicy);
        var rejection = RateLimitDecision.RejectWindow(RateLimitWindow.Minute, TimeSpan.FromSeconds(45));
        store.Setup(s => s.TryConsumeAsync(It.IsAny<string>(), AiPolicy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rejection);

        var act = async () => await mw.Invoke(Ctx("AiFn", "u"), Next());

        var ex = await act.Should().ThrowAsync<RateLimitedException>();
        ex.Which.RetryAfter.Should().Be(TimeSpan.FromSeconds(45));
        ex.Which.StoreUnavailable.Should().BeFalse();
        ex.Which.Window.Should().Be(RateLimitWindow.Minute);
    }

    [Fact]
    public async Task Invoke_FailClosedOnStoreException_ThrowsWithStoreUnavailable()
    {
        var (mw, resolver, store) = Build();
        resolver.Setup(r => r.Resolve(It.IsAny<FunctionContext>())).Returns(AiPolicy);
        store.Setup(s => s.TryConsumeAsync(It.IsAny<string>(), AiPolicy, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage offline"));

        var act = async () => await mw.Invoke(Ctx("AiFn", "u"), Next());

        var ex = await act.Should().ThrowAsync<RateLimitedException>();
        ex.Which.StoreUnavailable.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_FailOpenOnStoreException_PassesThrough()
    {
        var (mw, resolver, store) = Build();
        var failOpen = AiPolicy with { FailureMode = RateLimitFailureMode.FailOpen };
        resolver.Setup(r => r.Resolve(It.IsAny<FunctionContext>())).Returns(failOpen);
        store.Setup(s => s.TryConsumeAsync(It.IsAny<string>(), failOpen, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage offline"));
        var called = false;

        await mw.Invoke(Ctx("AiFn", "u"), Next(_ => called = true));

        called.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_OperationCanceled_Propagates()
    {
        var (mw, resolver, store) = Build();
        resolver.Setup(r => r.Resolve(It.IsAny<FunctionContext>())).Returns(AiPolicy);
        store.Setup(s => s.TryConsumeAsync(It.IsAny<string>(), AiPolicy, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await mw.Invoke(Ctx("AiFn", "u"), Next());

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class TestFunctionDefinition : FunctionDefinition
    {
        public TestFunctionDefinition(string name) => Name = name;

        public override string PathToAssembly => string.Empty;
        public override string EntryPoint => string.Empty;
        public override string Id => Name;
        public override string Name { get; }
        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; } =
            ImmutableDictionary<string, BindingMetadata>.Empty;
        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; } =
            ImmutableDictionary<string, BindingMetadata>.Empty;
        public override ImmutableArray<FunctionParameter> Parameters { get; } =
            ImmutableArray<FunctionParameter>.Empty;
    }
}
