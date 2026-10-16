using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Monica.Framework.UI.UISwagger.Support;
using Xunit;

namespace Test.Monica.Framework.UI.UISwagger.Support;

public sealed class SwaggerUiUrlResolverTests
{
    [Theory]
    [InlineData("", "", "/")]
    [InlineData("/mounted", "system-info", "/mounted/system-info")]
    [InlineData("/mounted", "/system-info", "/mounted/system-info")]
    [InlineData("/mounted", "~/system-info?view=api#details", "/mounted/system-info?view=api#details")]
    [InlineData("/mounted", "/mounted/child", "/mounted/mounted/child")]
    [InlineData("/mounted", "https://example.test/api", "https://example.test/api")]
    [InlineData("/mounted", "HTTP://example.test/api", "HTTP://example.test/api")]
    [InlineData("/mounted", "javascript:alert(1)", "/mounted/javascript:alert(1)")]
    public void Navigation_WhenResolvingConfiguredPath_ShouldUseApplicationRootOrPreserveHttpDestination(
        string pathBase,
        string path,
        string expected)
    {
        SwaggerUiUrlResolver.Navigation(new PathString(pathBase), path).Should().Be(expected);
    }
}
