using FluentAssertions;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Services.Ai.Telemetry;

namespace RajFinancial.Api.Tests.Services.Ai.Telemetry;

public class DefaultAiTelemetryRedactorTests
{
    private static DefaultAiTelemetryRedactor Build(string? secret = null, int prefix = 12) =>
        new(Options.Create(new AiTelemetryRedactorOptions
        {
            MerchantHashSecret = secret ?? new string('k', 32),
            HmacPrefixLength = prefix,
        }));

    [Fact]
    public void Null_value_returns_null_marker()
    {
        var r = Build();
        r.Redact("tool", "anything", null).Should().Be("<null>");
    }

    [Theory]
    [InlineData("pageNumber", 5)]
    [InlineData("limit", 50)]
    [InlineData("scope", "assets")]
    [InlineData("year", 2026)]
    public void AllowList_passes_through_value(string arg, object value)
    {
        var r = Build();
        r.Redact("tool", arg, value).Should().NotStartWith("[REDACTED");
    }

    [Fact]
    public void Merchant_like_arg_emits_HMAC_prefix()
    {
        var r = Build();
        var first = r.Redact("tool", "merchantName", "Starbucks");
        var second = r.Redact("tool", "merchantName", "Starbucks");
        var different = r.Redact("tool", "merchantName", "Walmart");

        first.Should().StartWith("[REDACTED:HMAC=");
        first.Should().Be(second, "same input under same secret must hash to same prefix (clustering)");
        first.Should().NotBe(different);
    }

    [Fact]
    public void Merchant_HMAC_prefix_changes_with_secret()
    {
        var a = Build(new string('a', 32));
        var b = Build(new string('b', 32));

        a.Redact("tool", "merchantName", "Starbucks")
            .Should().NotBe(b.Redact("tool", "merchantName", "Starbucks"));
    }

    [Fact]
    public void Account_like_arg_masks_trailing_4()
    {
        var r = Build();
        r.Redact("tool", "accountNumber", "1234567890").Should().Be("******7890");
        r.Redact("tool", "cardNumber", "411111111111").Should().Be("********1111");
    }

    [Fact]
    public void Account_short_value_is_fully_masked()
    {
        var r = Build();
        r.Redact("tool", "accountNumber", "12").Should().Be("**");
    }

    [Fact]
    public void Unknown_argument_falls_through_to_REDACTED()
    {
        var r = Build();
        r.Redact("tool", "freeformQuery", "anything sensitive").Should().Be("[REDACTED]");
        r.Redact("tool", "amount", 1234.56m).Should().Be("[REDACTED]");
    }

    [Fact]
    public void Empty_or_whitespace_tool_name_throws()
    {
        var r = Build();
        var act = () => r.Redact(" ", "arg", "value");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Empty_or_whitespace_argument_name_throws()
    {
        var r = Build();
        var act = () => r.Redact("tool", " ", "value");
        act.Should().Throw<ArgumentException>();
    }
}
