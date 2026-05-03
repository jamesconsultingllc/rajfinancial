using FluentAssertions;
using RajFinancial.Api.Services.RateLimit;

namespace RajFinancial.Api.Tests.Services.RateLimit;

public sealed class RateLimitOptionsValidatorTests
{
    private static RateLimitOptions ValidOptions(string tableName = "RateLimitCounters") => new()
    {
        Enabled = true,
        StorageConnectionName = "AzureWebJobsStorage",
        TableName = tableName,
        RetryAttempts = 5,
        JitterMaxMs = 50,
        CleanupRetention = TimeSpan.FromDays(7),
        AiPolicy = new AiPolicyOptions
        {
            RequestsPerMinute = 60,
            RequestsPerHour = 1000,
            FailureMode = RateLimitFailureMode.FailClosed,
        },
    };

    [Fact]
    public void Disabled_AllowsAnyShape()
    {
        var validator = new RateLimitOptionsValidator();
        var options = ValidOptions(tableName: "1invalid"); // would fail if Enabled
        options.Enabled = false;

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("RateLimitCounters")]
    [InlineData("abc")]
    [InlineData("AaBbCc123")]
    [InlineData("T" + "0123456789012345678901234567890123456789012345678901234567890123")] // 65 with leading T → still 65, but trim
    public void ValidTableNames_Pass(string tableName)
    {
        if (tableName.Length > 63) tableName = tableName[..63];
        var validator = new RateLimitOptionsValidator();
        var options = ValidOptions(tableName);

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]                   // too short
    [InlineData("1abc")]                  // starts with digit
    [InlineData("table-name")]            // hyphen
    [InlineData("table_name")]            // underscore
    [InlineData("table name")]            // space
    [InlineData("table.name")]            // period
    [InlineData("0123456789012345678901234567890123456789012345678901234567890123")] // 64, starts digit
    public void InvalidTableNames_FailWithDescriptiveMessage(string tableName)
    {
        var validator = new RateLimitOptionsValidator();
        var options = ValidOptions(tableName);

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle(f =>
            f.Contains("TableName", StringComparison.Ordinal) &&
            f.Contains("Azure table naming rules", StringComparison.Ordinal));
    }

    [Fact]
    public void TooLongTableName_Fails()
    {
        var validator = new RateLimitOptionsValidator();
        var options = ValidOptions(new string('a', 64));

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
    }
}
