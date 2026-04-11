using FluentAssertions;
using RajFinancial.Api.Validators;
using RajFinancial.Shared.Contracts.Auth;

namespace RajFinancial.Api.Tests.Validators;

/// <summary>
/// Unit tests for <see cref="UpdateProfileRequestValidator"/>.
/// </summary>
public class UpdateProfileRequestValidatorTests
{
    private readonly UpdateProfileRequestValidator validator = new();

    private static UpdateProfileRequest CreateValidRequest() => new()
    {
        DisplayName = "Jane Advisor",
        Locale = "en-US",
        Timezone = "America/New_York",
        Currency = "USD"
    };

    // =========================================================================
    // Valid request
    // =========================================================================

    [Fact]
    public void Valid_Request_Passes()
    {
        var result = validator.Validate(CreateValidRequest());
        result.IsValid.Should().BeTrue();
    }

    // =========================================================================
    // DisplayName
    // =========================================================================

    [Fact]
    public void EmptyDisplayName_Fails()
    {
        var request = CreateValidRequest() with { DisplayName = "" };
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "DISPLAY_NAME_REQUIRED");
    }

    [Fact]
    public void DisplayName_Over200Chars_Fails()
    {
        var request = CreateValidRequest() with { DisplayName = new string('A', 201) };
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "DISPLAY_NAME_MAX_LENGTH");
    }

    // =========================================================================
    // Locale
    // =========================================================================

    [Fact]
    public void EmptyLocale_Fails()
    {
        var request = CreateValidRequest() with { Locale = "" };
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidLocale_Fails()
    {
        var request = CreateValidRequest() with { Locale = "xx-XX" };
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "LOCALE_INVALID");
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("es-MX")]
    [InlineData("fr-FR")]
    public void ValidLocales_Pass(string locale)
    {
        var request = CreateValidRequest() with { Locale = locale };
        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    // =========================================================================
    // Timezone
    // =========================================================================

    [Fact]
    public void EmptyTimezone_Fails()
    {
        var request = CreateValidRequest() with { Timezone = "" };
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidTimezone_Fails()
    {
        var request = CreateValidRequest() with { Timezone = "Mars/Olympus" };
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "TIMEZONE_INVALID");
    }

    // =========================================================================
    // Currency
    // =========================================================================

    [Fact]
    public void EmptyCurrency_Fails()
    {
        var request = CreateValidRequest() with { Currency = "" };
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidCurrency_Fails()
    {
        var request = CreateValidRequest() with { Currency = "ZZZ" };
        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CURRENCY_INVALID");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("MXN")]
    public void ValidCurrencies_Pass(string currency)
    {
        var request = CreateValidRequest() with { Currency = currency };
        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }
}
