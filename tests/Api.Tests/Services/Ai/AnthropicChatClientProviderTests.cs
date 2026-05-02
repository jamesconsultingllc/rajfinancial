using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    public void CreateClient_with_BaseUrl_override_returns_non_null_client()
    {
        WithEnv("test-key-value", () =>
        {
            var client = CreateSut().CreateClient(ValidOptions(baseUrl: "https://example.test/v1/"));

            client.Should().NotBeNull();
        });
    }

    [Fact]
    public void CreateClient_returns_OpenTelemetry_decorated_client()
    {
        WithEnv("test-key-value", () =>
        {
            var client = CreateSut().CreateClient(ValidOptions());

            // The MEAI builder pipeline returns OpenTelemetryChatClient as the outermost
            // wrapper when UseOpenTelemetry is configured. Type-name match is sufficient —
            // we don't depend on a specific public surface.
            client.GetType().FullName.Should().Be(
                "Microsoft.Extensions.AI.OpenTelemetryChatClient");
        });
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
