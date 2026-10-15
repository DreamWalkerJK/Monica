using AwesomeAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Monica.Markdown.Abstractions;
using Monica.Markdown.Models;
using Monica.Markdown.UIMarkdown.Models;
using Monica.Markdown.UIMarkdown.Support;
using Monica.Modules;
using Monica.Tool.Algorithms.Trees;
using NSubstitute;

namespace Test.Monica.Markdown.UIMarkdown.Support;

public sealed class MarkdownLocalAssetServiceTests : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("monica-markdown-assets-");

    [Theory]
    [InlineData("assets/diagram.svg", "assets/diagram.svg")]
    [InlineData("assets/演练 图.svg", "assets/演练 图.svg")]
    [InlineData("assets/%E6%BC%94%E7%BB%83%20%E5%9B%BE.svg", "assets/演练 图.svg")]
    [InlineData("assets/review%20sequence.svg", "assets/review sequence.svg")]
    [InlineData("assets%5Creview%20sequence.svg", "assets/review sequence.svg")]
    [InlineData("assets/diagram%2Esvg", "assets/diagram.svg")]
    [InlineData("assets/review+sequence.svg", "assets/review+sequence.svg")]
    public async Task OpenImageAsync_WhenRelativeUrlPassesThroughRenderQuery_ShouldResolveRealFile(
        string authoredUrl,
        string relativeFilePath)
    {
        var service = CreateService();
        var filePath = WriteGroupFile("en-US/" + relativeFilePath);

        var asset = await OpenFromRenderedUrlAsync(service, authoredUrl);

        asset.FilePath.Should().Be(filePath);
        asset.ContentType.Should().Be("image/svg+xml");
    }

    [Fact]
    public async Task OpenImageAsync_WhenAuthoredPercentIsEscaped_ShouldDecodeOnlyOnce()
    {
        var service = CreateService();
        var filePath = WriteGroupFile("en-US/assets/diagram%20draft.svg");

        var asset = await OpenFromRenderedUrlAsync(service, "assets/diagram%2520draft.svg");

        asset.FilePath.Should().Be(filePath);
    }

    [Fact]
    public async Task OpenImageAsync_WhenCatalogDocumentDirectoryContainsPercent_ShouldKeepPhysicalDirectory()
    {
        var service = CreateService();
        var filePath = WriteGroupFile("en-US/review%20folder/assets/diagram.svg");

        var asset = await OpenFromRenderedUrlAsync(service, "assets/diagram.svg", "en-US/review%20folder/guide.md");

        asset.FilePath.Should().Be(filePath);
    }

    [Theory]
    [InlineData("assets/diagram%.svg")]
    [InlineData("assets/diagram%2.svg")]
    [InlineData("assets/diagram%GG.svg")]
    public async Task OpenImageAsync_WhenPercentEscapeIsMalformed_ShouldPreserveLiteralFilename(string authoredUrl)
    {
        var service = CreateService();
        var filePath = WriteGroupFile("en-US/" + authoredUrl);

        var asset = await OpenFromRenderedUrlAsync(service, authoredUrl);

        asset.FilePath.Should().Be(filePath);
    }

    [Fact]
    public async Task OpenImageAsync_WhenMalformedEscapeHasNoFile_ShouldReportMissingFile()
    {
        var service = CreateService();

        var action = () => OpenFromRenderedUrlAsync(service, "assets/missing%GG.svg");

        await action.Should().ThrowAsync<FileNotFoundException>();
    }

    [Theory]
    [InlineData("%2Foutside.svg")]
    [InlineData("%5Coutside.svg")]
    [InlineData("%2F%2Fexample.invalid%2Fdiagram.svg")]
    [InlineData("C%3A%5Coutside.svg")]
    [InlineData("https%3A%2F%2Fexample.invalid%2Fdiagram.svg")]
    [InlineData("assets/diagram%00.svg")]
    [InlineData("%2e%2e%2f%2e%2e%2foutside.svg")]
    [InlineData("%2e%2e%5c%2e%2e%5coutside.svg")]
    public async Task OpenImageAsync_WhenDecodedUrlIsInvalidOrEscapesGroup_ShouldReject(string authoredUrl)
    {
        var service = CreateService();
        File.WriteAllText(Path.Combine(_directory.FullName, "outside.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");

        var action = () => OpenFromRenderedUrlAsync(service, authoredUrl);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task OpenImageAsync_WhenDecodedParentReferenceStaysInsideGroup_ShouldResolveFile()
    {
        var service = CreateService();
        var filePath = WriteGroupFile("shared/diagram.svg");

        var asset = await OpenFromRenderedUrlAsync(service, "%2e%2e/shared/diagram.svg");

        asset.FilePath.Should().Be(filePath);
    }

    [Theory]
    [InlineData("assets/diagram%2Ehtml")]
    [InlineData("assets/diagram.svg%2Ehtml")]
    public async Task OpenImageAsync_WhenDecodedExtensionIsDisallowed_ShouldReject(string authoredUrl)
    {
        var service = CreateService();

        var action = () => OpenFromRenderedUrlAsync(service, authoredUrl);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task OpenImageAsync_WhenDecodedExtensionIsExcludedByHost_ShouldRespectConfiguredAllowList()
    {
        var service = CreateService(new ModuleMarkdownUIOption { AllowedImageExtensions = [".png"] });
        WriteGroupFile("en-US/assets/diagram.svg");

        var action = () => OpenFromRenderedUrlAsync(service, "assets/diagram%2Esvg");

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose() => _directory.Delete(recursive: true);

    private MarkdownLocalAssetService CreateService(ModuleMarkdownUIOption? options = null)
    {
        var root = Path.Combine(_directory.FullName, "group");
        Directory.CreateDirectory(root);
        var group = new MarkdownDocumentGroup
        {
            Key = "review",
            Title = "Review",
            BasePath = root,
            IsValid = true,
            DocumentCount = 0,
            RootNode = new TreeNode<MarkdownDocumentNodeData>(new("Review", false))
        };
        var catalog = Substitute.For<IMarkdownDocumentCatalog>();
        catalog.GetDocumentGroupAsync("review").Returns(Task.FromResult(group));
        return new(catalog, Options.Create(options ?? new ModuleMarkdownUIOption()));
    }

    private string WriteGroupFile(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(_directory.FullName, "group",
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        return path;
    }

    private static async Task<MarkdownLocalImageAsset> OpenFromRenderedUrlAsync(
        MarkdownLocalAssetService service,
        string authoredUrl,
        string documentPath = "en-US/guide.md")
    {
        var renderUrl = service.BuildRenderUrl(authoredUrl, true, "review", documentPath)!;
        var query = QueryHelpers.ParseQuery(new Uri("http://localhost" + renderUrl).Query);
        // Simulate only standard HTTP query binding, retaining the authored URL's encoding.
        query["assetPath"].ToString().Should().Be(authoredUrl);
        query["documentRelativePath"].ToString().Should().Be(documentPath);
        return await service.OpenImageAsync("review", query["documentRelativePath"].ToString(), query["assetPath"].ToString());
    }
}
