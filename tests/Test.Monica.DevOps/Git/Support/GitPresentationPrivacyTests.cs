using Monica.DevOps.Git.Models;
using Monica.DevOps.Git.Support;
using Xunit;

namespace Test.Monica.DevOps.Git.Support;

public sealed class GitPresentationPrivacyTests
{
    [Fact]
    public void Redact_WhenKnownTokenAndPasswordAppearLiterallyOrEscaped_ShouldRemoveConfiguredValues()
    {
        const string token = "t0ken/with?reserved=characters";
        const string password = "pass word&secret";
        var privacy = new GitPresentationPrivacy([
            new() { Id = "token", AuthenticationType = GitAuthenticationType.Token, Token = token },
            new() { Id = "password", AuthenticationType = GitAuthenticationType.UserPassword, Password = password }]);

        var output = privacy.Redact($"{token} | {Uri.EscapeDataString(token)} | {password} | {Uri.EscapeDataString(password)}");

        Assert.Equal("[redacted] | [redacted] | [redacted] | [redacted]", output);
    }

    [Fact]
    public void Redact_WhenUrlContainsUserInfoAndSecretQueryKeys_ShouldRetainNonSecretLocation()
    {
        var privacy = new GitPresentationPrivacy([]);

        var output = privacy.Redact("https://alice:unknown-password@example.invalid/repo?ACCESS_TOKEN=private&branch=main&api_key=key#anchor");

        Assert.Equal("https://[redacted]@example.invalid/repo?ACCESS_TOKEN=[redacted]&branch=main&api_key=[redacted]#anchor", output);
    }

    [Theory]
    [InlineData("token")]
    [InlineData("password")]
    [InlineData("passwd")]
    [InlineData("secret")]
    [InlineData("apikey")]
    [InlineData("private_token")]
    [InlineData("auth")]
    [InlineData("authorization")]
    [InlineData("signature")]
    [InlineData("sig")]
    [InlineData("credential")]
    public void Redact_WhenCommonSecretQueryKeyAppears_ShouldRedactItsValue(string key)
    {
        var privacy = new GitPresentationPrivacy([]);

        Assert.Equal($"https://example.invalid/?{key}=[redacted]&branch=main",
            privacy.Redact($"https://example.invalid/?{key}=sensitive&branch=main"));
    }

    [Fact]
    public void Redact_WhenValueIsNotRegisteredOrRecognized_ShouldAvoidClaimingArbitrarySecretDetection()
    {
        var privacy = new GitPresentationPrivacy([]);

        Assert.Equal("An unregistered value outside a URL", privacy.Redact("An unregistered value outside a URL"));
        Assert.Equal("<script>alert('text')</script>", privacy.Redact("<script>alert('text')</script>"));
        Assert.Equal("", privacy.Redact(null));
    }

    [Theory]
    [InlineData(32766)]
    [InlineData(32767)]
    public void Redact_WhenTextExceedsLimitAtSurrogatePair_ShouldBoundInclusiveLengthWithoutSplittingPair(int prefixLength)
    {
        var privacy = new GitPresentationPrivacy([]);
        var input = new string('x', prefixLength) + "\U0001F600" + new string('y', 10);

        var output = privacy.Redact(input);

        Assert.True(output.Length <= 32768);
        Assert.EndsWith("…", output);
        Assert.False(char.IsHighSurrogate(output[^2]));
        Assert.Equal(new string('x', prefixLength) + "…", output);
    }

    [Fact]
    public void Redact_WhenTextFitsLimit_ShouldPreserveEntireText()
    {
        var privacy = new GitPresentationPrivacy([]);
        var input = new string('x', 32766) + "\U0001F600";

        Assert.Equal(input, privacy.Redact(input));
    }
}
