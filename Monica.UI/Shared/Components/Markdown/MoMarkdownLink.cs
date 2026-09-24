using Microsoft.AspNetCore.Components;

namespace Monica.UI.Shared.Components.Markdown;

/// <summary>A Markdown link before URL resolution, including its formatted label.</summary>
/// <param name="Url">The authored link destination.</param>
/// <param name="Title">The optional authored title.</param>
/// <param name="ChildContent">The label rendered with the document's inline formatting.</param>
public sealed record MoMarkdownLink(string? Url, string? Title, RenderFragment ChildContent);
