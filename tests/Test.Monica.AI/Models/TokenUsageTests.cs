using AwesomeAssertions;
using Monica.AI.Models;

namespace Test.Monica.AI.Models;

public sealed class TokenUsageTests
{
    [Fact]
    public void Aggregate_WhenCountsExceedIntegerRange_ShouldClampWithoutWrapping()
    {
        var request = new TokenUsage { InputTokens = int.MaxValue, OutputTokens = int.MaxValue };

        request.EffectiveTotalTokens.Should().Be(int.MaxValue);
        var aggregate = TokenUsage.Sum([request, request]);
        aggregate.InputTokens.Should().Be(int.MaxValue);
        aggregate.OutputTokens.Should().Be(int.MaxValue);
        aggregate.EffectiveTotalTokens.Should().Be(int.MaxValue);
    }

    [Fact]
    public void Aggregate_WhenAProviderOmitsMeasurements_ShouldKeepThemUnknown()
    {
        var aggregate = TokenUsage.Sum([
            new TokenUsage { InputTokens = 10, OutputTokens = 5, CachedInputTokens = 0 },
            new TokenUsage { InputTokens = 12, OutputTokens = 4 }]);

        aggregate.EffectiveTotalTokens.Should().Be(31);
        aggregate.CachedInputTokens.Should().BeNull();
        aggregate.ReasoningTokens.Should().BeNull();
        new TokenUsage { InputTokens = 10 }.EffectiveTotalTokens.Should().BeNull();
    }
}
