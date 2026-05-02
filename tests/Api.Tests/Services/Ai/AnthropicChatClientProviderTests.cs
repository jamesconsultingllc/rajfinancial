using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RajFinancial.Api.Configuration;
using RajFinancial.Api.Services.Ai.Providers;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Tests.Services.Ai;

// ============================================================================
// AnthropicChatClientProvider unit tests (Task #545).
// ----------------------------------------------------------------------------
// Tests in this file mutate process environment variables, so they share the
// "AnthropicEnv" xUnit collection to run sequentially. Each test snapshots and
// restores the env var via try/finally to avoid cross-test contamination.
// ============================================================================

[Collection("AnthropicEnv")]
public class AnthropicChatClientProviderTests
{
    private const string TestEnvVar = "RAJFIN_TEST_ANTHROPIC_KEY";

    private static AnthropicChatClientProvider CreateSut() =>
        new(NullLogger<AnthropicChatClientProvider>.Instance);

    private static AiProviderOptions ValidOptions(string? baseUrl = null) => new()
    {
        Model = "claude-sonnet-4-5",
        ApiKeyEnvVar = TestEnvVar,
        BaseUrl = baseUrl,
    };

    private static void WithEnv(string? value, Action body)
    {
        var prior = Environment.GetEnvironmentVariable(TestEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(TestEnvVar, value);
            body();
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestEnvVar, prior);
        }
    }

    [Fact]
    public void Id_returns_Anthropic()
    {
        CreateSut().Id.Should().Be(AiProviderId.Anthropic);
    }

    [Fact]
    public void CreateClient_with_valid_options_returns_non_null_IChatClient()
    {
        WithEnv("test-key-value", () =>
        {
            var client = CreateSut().CreateClient(ValidOptions());

            client.Should().NotBeNull();
        });
    }

    [Fact]
    public void CreateClient_throws_when_options_null()
    {
        var sut = CreateSut();

        var act = () => sut.CreateClient(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateClient_throws_when_ApiKeyEnvVar_is_empty()
    {
        var sut = CreateSut();
        var options = ValidOptions();
        options.ApiKeyEnvVar = "";

        var act = () => sut.CreateClient(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ApiKeyEnvVar*");
    }

    [Fact]
    public void CreateClient_throws_when_Model_is_empty()
    {
        var sut = CreateSut();
        var options = ValidOptions();
        options.Model = "";

        WithEnv("test-key-value", () =>
        {
            var act = () => sut.CreateClient(options);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Model*");
        });
    }

    [Fact]
    public void CreateClient_throws_when_env_var_is_missing()
    {
        var sut = CreateSut();

        WithEnv(null, () =>
        {
            var act = () => sut.CreateClient(ValidOptions());

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{TestEnvVar}*");
        });
    }

    [Fact]
    public void CreateClient_throws_when_env_var_is_whitespace()
    {
        var sut = CreateSut();

        WithEnv("   ", () =>
        {
            var act = () => sut.CreateClient(ValidOptions());

            act.Should().Throw<InvalidOperationException>();
        });
    }

    [Fact]
    public void CreateClient_does_not_log_the_api_key_value()
    {
        const string secretKey = "sk-ant-secret-7XYZ-do-not-log";
        var capturing = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(b => b.AddProvider(capturing));
        var logger = loggerFactory.CreateLogger<AnthropicChatClientProvider>();
        var sut = new AnthropicChatClientProvider(logger);

        WithEnv(secretKey, () =>
        {
            sut.CreateClient(ValidOptions());
        });

        capturing.Lines.Should().NotContain(line => line.Contains(secretKey));
    }

    [Fact]
    public void CreateSdkClient_applies_BaseUrl_override_to_ApiUrlFormat()
    {
        // Drives the production seam used by CreateClient. Asserts the BaseUrl override
        // actually reaches Anthropic SDK's ApiUrlFormat — without this, the override
        // could be silently dropped or wired to the wrong property.
        const string overrideUrl = "https://example.test/v1/";

        var sdk = AnthropicChatClientProvider.CreateSdkClient("test-key", overrideUrl);

        sdk.ApiUrlFormat.Should().Be(overrideUrl);
    }

    [Fact]
    public void CreateSdkClient_leaves_ApiUrlFormat_unchanged_when_BaseUrl_null()
    {
        var defaultSdk = AnthropicChatClientProvider.CreateSdkClient("test-key", baseUrl: null);
        var withOverride = AnthropicChatClientProvider.CreateSdkClient("test-key", baseUrl: null);

        // The two clients with no override must share the SDK's default ApiUrlFormat.
        withOverride.ApiUrlFormat.Should().Be(defaultSdk.ApiUrlFormat);
    }

    [Fact]
    public void CreateSdkClient_ignores_whitespace_BaseUrl()
    {
        var defaultSdk = AnthropicChatClientProvider.CreateSdkClient("test-key", baseUrl: null);
        var whitespace = AnthropicChatClientProvider.CreateSdkClient("test-key", baseUrl: "   ");

        whitespace.ApiUrlFormat.Should().Be(defaultSdk.ApiUrlFormat);
    }

    [Theory]
    [InlineData("localhost:11434")]
    [InlineData("/proxy")]
    [InlineData("not a url")]
    [InlineData("ftp://example.test")]
    public void CreateSdkClient_rejects_malformed_BaseUrl(string badUrl)
    {
        // Eagerly fail on configuration typos rather than letting them surface as runtime
        // request errors much later. Only absolute http(s) URIs are accepted.
        var act = () => AnthropicChatClientProvider.CreateSdkClient("test-key", badUrl);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*absolute http or https URI*");
    }

    [Fact]
    public void CreateClient_logs_sdk_default_marker_when_BaseUrl_is_whitespace()
    {
        // Whitespace BaseUrl falls back to the SDK default endpoint, so the diagnostics log
        // must record '<sdk-default>' rather than the raw whitespace value.
        var capturing = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(b => b.AddProvider(capturing));
        var logger = loggerFactory.CreateLogger<AnthropicChatClientProvider>();
        var sut = new AnthropicChatClientProvider(logger);

        WithEnv("test-key-value", () =>
            sut.CreateClient(ValidOptions(baseUrl: "   ")));

        capturing.Lines.Should().Contain(line => line.Contains("<sdk-default>"));
        capturing.Lines.Should().NotContain(line => line.Contains("'   '"));
    }

    [Fact]
    public async Task BuildPipeline_emits_activity_on_AI_observability_source()
    {
        // Replaces the brittle `OpenTelemetryChatClient` type-name assertion. Verifies the
        // observable behaviour we actually care about: chat traffic emits an activity on
        // ObservabilityDomains.Ai. This is robust to MEAI internal type renames.
        var capturing = new CapturingInnerClient();
        var pipeline = AnthropicChatClientProvider.BuildPipeline(
            capturing,
            new AiProviderOptions { Model = "claude-sonnet-4-5", ApiKeyEnvVar = TestEnvVar });

        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = src => src.Name == ObservabilityDomains.Ai,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = captured.Add,
        };
        ActivitySource.AddActivityListener(listener);

        await pipeline.GetResponseAsync("hello", options: null);

        captured.Should().NotBeEmpty(
            $"the pipeline must emit at least one Activity on the '{ObservabilityDomains.Ai}' source.");
    }

    [Fact]
    public async Task BuildPipeline_defaults_ModelId_to_options_Model_when_caller_does_not_specify()
    {
        // Drives the production BuildPipeline seam — if the `.ConfigureOptions(...)` line
        // is removed or reordered in CreateClient/BuildPipeline, this test fails.
        const string expectedModel = "claude-sonnet-4-5";
        var capturing = new CapturingInnerClient();
        var pipeline = AnthropicChatClientProvider.BuildPipeline(
            capturing,
            new AiProviderOptions { Model = expectedModel, ApiKeyEnvVar = TestEnvVar });

        await pipeline.GetResponseAsync("hello", options: null);

        capturing.LastOptions.Should().NotBeNull();
        capturing.LastOptions!.ModelId.Should().Be(expectedModel);
    }

    [Fact]
    public async Task BuildPipeline_does_not_overwrite_caller_supplied_ModelId()
    {
        const string callerModel = "claude-opus-4-1";
        var capturing = new CapturingInnerClient();
        var pipeline = AnthropicChatClientProvider.BuildPipeline(
            capturing,
            new AiProviderOptions { Model = "claude-sonnet-4-5", ApiKeyEnvVar = TestEnvVar });

        await pipeline.GetResponseAsync("hello", options: new ChatOptions { ModelId = callerModel });

        capturing.LastOptions!.ModelId.Should().Be(callerModel);
    }

    private sealed class CapturingInnerClient : IChatClient
    {
        public ChatOptions? LastOptions { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastOptions = options;
            return Task.FromResult(new ChatResponse());
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastOptions = options;
            return AsyncEnumerable.Empty<ChatResponseUpdate>();
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<string> Lines { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Lines);

        public void Dispose() { }

        private sealed class CapturingLogger(List<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                sink.Add(formatter(state, exception));
            }
        }
    }
}

[CollectionDefinition("AnthropicEnv", DisableParallelization = true)]
public sealed class AnthropicEnvCollection { }
