using FluentAssertions;
using RajFinancial.Api.Middleware.Exception;
using RajFinancial.Api.Services.RateLimit;
using Xunit;

namespace RajFinancial.Api.Tests.Middleware.Exceptions;

public class RateLimitedExceptionTests
{
    [Theory]
    [InlineData(0, 1)]            // sub-second remainder rounds up to min 1s
    [InlineData(0.4, 1)]
    [InlineData(0.99, 1)]
    [InlineData(1.0, 1)]
    [InlineData(1.01, 2)]         // >1s rounds up via ceiling — was the truncation bug
    [InlineData(59.5, 60)]        // canonical "59.5s" case from Copilot review
    [InlineData(60.0, 60)]
    [InlineData(60.01, 61)]
    public void Message_RetryAfterSeconds_MatchesResponseHelperHeader(double totalSeconds, int expectedSeconds)
    {
        var retryAfter = TimeSpan.FromSeconds(totalSeconds);

        var ex = new RateLimitedException(retryAfter, storeUnavailable: false, RateLimitWindow.Minute);

        // Body and header must agree.
        var headerSeconds = RateLimitResponseHelper.RetryAfterSeconds(ex);
        headerSeconds.Should().Be(expectedSeconds);
        ex.Message.Should().Contain($"retry after {expectedSeconds}s.");
    }

    [Fact]
    public void Message_StoreUnavailable_FormatsRetryAfterConsistentlyWithHeader()
    {
        var retryAfter = TimeSpan.FromSeconds(59.5);

        var ex = new RateLimitedException(retryAfter, storeUnavailable: true, RateLimitWindow.Minute);

        var headerSeconds = RateLimitResponseHelper.RetryAfterSeconds(ex);
        headerSeconds.Should().Be(60);
        ex.Message.Should().Be("Rate-limit store unavailable; retry after 60s.");
    }
}
