using System.Text;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;

namespace Monica.UI.Shared.Components.Markdown;

/// <summary>
/// Extends <see cref="MudMarkdown"/> with Mermaid diagrams and selective link templates.
/// </summary>
public class MoMudMarkdown : MudMarkdown
{
    private IReadOnlyList<MoMarkdownHeading> _headings = [];
    private int _tableOfContentsHeadingCount;

    /// <summary>
    /// Gets or sets whether Mermaid fenced code blocks should be rendered as diagrams.
    /// </summary>
    [Parameter]
    public bool EnableMermaid { get; set; } = true;

    /// <summary>
    /// Raised when the parsed markdown headings change.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<MoMarkdownHeading>> HeadingsChanged { get; set; }

    /// <summary>
    /// Optionally renders selected non-image links before asset URL resolution.
    /// Return null to preserve the default renderer for a link.
    /// </summary>
    [Parameter]
    public Func<MoMarkdownLink, RenderFragment?>? LinkTemplate { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);

        var headings = MoMarkdownHeadingParser.Parse(Value);
        _tableOfContentsHeadingCount = headings.Count(x => x.Level <= 3);

        if (_headings.SequenceEqual(headings))
        {
            return;
        }

        _headings = headings;

        if (HeadingsChanged.HasDelegate)
        {
            await HeadingsChanged.InvokeAsync(_headings);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var originalHasTableOfContents = HasTableOfContents;
        HasTableOfContents = HasTableOfContents && _tableOfContentsHeadingCount > 0;

        try
        {
            base.BuildRenderTree(builder);
        }
        finally
        {
            HasTableOfContents = originalHasTableOfContents;
        }
    }

    /// <inheritdoc />
    protected override void RenderInlines(RenderTreeBuilder builder, ref int elementIndex, ContainerInline inlines)
    {
        if (LinkTemplate is not null)
        {
            foreach (var link in inlines.OfType<LinkInline>().Where(static link => !link.IsImage).ToArray())
            {
                RenderFragment label = childBuilder =>
                {
                    var childIndex = 0;
                    RenderInlines(childBuilder, ref childIndex, link);
                };
                var content = LinkTemplate(new MoMarkdownLink(link.Url, link.Title, label));
                if (content is not null)
                {
                    link.ReplaceBy(new TemplatedLinkInline(content), copyChildren: false);
                }
            }
        }

        base.RenderInlines(builder, ref elementIndex, inlines);
    }

    /// <inheritdoc />
    protected override void OnRenderInlinesDefault(RenderTreeBuilder builder, ref int elementIndex, Inline inline)
    {
        if (inline is TemplatedLinkInline link)
        {
            builder.AddContent(0, link.Content);
            elementIndex++;
            return;
        }

        base.OnRenderInlinesDefault(builder, ref elementIndex, inline);
    }

    private sealed class TemplatedLinkInline(RenderFragment content) : Inline
    {
        public RenderFragment Content { get; } = content;
    }

    protected override void RenderCodeBlock(in RenderTreeBuilder builder, ref int elementIndex, in CodeBlock code, in string? info)
    {
        if (EnableMermaid && string.Equals(info?.Trim(), "mermaid", StringComparison.OrdinalIgnoreCase))
        {
            builder.OpenComponent<MoMarkdownMermaidBlock>(0);
            builder.AddComponentParameter(1, nameof(MoMarkdownMermaidBlock.Definition), CreateCodeBlockText(code));
            builder.CloseComponent();
            elementIndex += 2;
            return;
        }

        base.RenderCodeBlock(builder, ref elementIndex, code, info);
    }

    private static string CreateCodeBlockText(CodeBlock code)
    {
        if (code.Lines.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var line in code.Lines)
        {
            if (builder.Length != 0)
            {
                builder.AppendLine();
            }

            builder.Append(line.ToString());
        }

        return builder.ToString();
    }
}
