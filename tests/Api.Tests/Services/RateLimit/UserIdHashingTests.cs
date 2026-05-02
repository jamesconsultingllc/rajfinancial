using FluentAssertions;
using RajFinancial.Api.Services.RateLimit.Storage;

namespace RajFinancial.Api.Tests.Services.RateLimit;

public sealed class UserIdHashingTests
{
    [Fact]
    public void Hash_SameInput_ReturnsSameOutput()
    {
        var a = UserIdHashing.Hash("alice@example.com");
        var b = UserIdHashing.Hash("alice@example.com");
        a.Should().Be(b);
    }

    [Fact]
    public void Hash_DifferentInputs_ReturnDifferentOutputs()
    {
        UserIdHashing.Hash("alice").Should().NotBe(UserIdHashing.Hash("bob"));
    }

    [Fact]
    public void Hash_DoesNotContainRawInput()
    {
        var hash = UserIdHashing.Hash("alice@example.com");
        hash.Should().NotContain("alice");
        hash.Should().NotContain("example");
    }

    [Fact]
    public void Hash_Returns32LowercaseHexChars()
    {
        var hash = UserIdHashing.Hash("user-42");
        hash.Should().HaveLength(32);
        hash.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Hash_NullOrWhitespace_Throws(string? input)
    {
        var act = () => UserIdHashing.Hash(input!);
        act.Should().Throw<ArgumentException>();
    }
}
