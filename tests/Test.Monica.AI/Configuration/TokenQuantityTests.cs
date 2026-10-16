using AwesomeAssertions;
using Monica.AI.Configuration.Models;

namespace Test.Monica.AI.Configuration;

public sealed class TokenQuantityTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("  ", null)]
    [InlineData("256K", 262144)]
    [InlineData("1m", 1048576)]
    [InlineData("1.5M", 1572864)]
    [InlineData(" 32 k ", 32768)]
    [InlineData("1000000", 1000000)]
    [InlineData("2147483647", int.MaxValue)]
    public void TryParse_WhenQuantityIsExact_ShouldReturnTokenCount(string? value, int? expected)
    {
        TokenQuantity.TryParse(value, out var tokens).Should().BeTrue();
        tokens.Should().Be(expected);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1K")]
    [InlineData("1,5M")]
    [InlineData("1.1")]
    [InlineData("0.001K")]
    [InlineData("2048M")]
    [InlineData("99999999999999999999999999999999999M")]
    [InlineData("2KK")]
    [InlineData("1e6")]
    [InlineData("NaN")]
    public void TryParse_WhenQuantityIsInvalid_ShouldRejectWithoutTruncation(string value)
    {
        TokenQuantity.TryParse(value, out var tokens).Should().BeFalse();
        tokens.Should().BeNull();
    }

    [Theory]
    [InlineData(262144, "256K")]
    [InlineData(1048576, "1M")]
    [InlineData(1000000, "1000000")]
    [InlineData(1572864, "1536K")]
    [InlineData(int.MaxValue, "2147483647")]
    public void Format_WhenCapacityIsConfigured_ShouldRoundTripWithoutRounding(int tokens, string expected)
    {
        var text = TokenQuantity.Format(tokens);
        text.Should().Be(expected);
        TokenQuantity.TryParse(text, out var parsed).Should().BeTrue();
        parsed.Should().Be(tokens);
    }
}
