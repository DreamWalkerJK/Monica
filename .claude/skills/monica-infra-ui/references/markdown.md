# Serve and search Markdown documents

`AddMarkdown()` registers the document catalog, search, and `MarkdownFacade`; its default provider scans the filesystem. Give it an explicit discovery root with `AddDocumentGroup(key, title, basePath)`. `AddMarkdownUI()` adds the viewer page, shell Markdown rendering, localization, and a local image endpoint. The viewer needs a Web host completed with `app.UseMonica()` and `app.MapMonica()`.

```csharp
builder.AddMonica(monica =>
{
    monica.AddMarkdown()
        .AddDocumentGroup("handbook", "Handbook", "docs/handbook")
        .EnableMultilingualDocuments();
    monica.AddMarkdownUI(options => options.EnableMinimalApi = true);
});
```

The catalog recognizes `.md` and `.markdown` by default, parses front matter for titles and navigation metadata, and excludes common tool/build folders. `AddExcludedFolders(...)` extends those exclusions; `WithExcludedFolders(...)` replaces them. `EnableMultilingualDocuments()` treats top-level supported culture folders such as `en-US` and `zh-CN` as language roots; when such roots are detected, documents should live below a supported root. `AddDocumentGroup` can also specify per-group exclusions. Use `UseDocumentProvider<TProvider>()` or `UseDocumentTitleProvider<TProvider>()` only when filesystem discovery or filename titles do not fit the host.

Code consumers use `MarkdownFacade`: `GetAllDocumentGroupsAsync()` reads the catalog for group selection, `GetDocumentTreeAsync(groupKey, culture)` gets its hierarchy, `GetDocumentContentAsync(document)` reads source, and `SearchDocumentsAsync(new MarkdownDocumentSearchRequest(query, CurrentGroupKey: groupKey))` searches it. Search defaults to keyword fuzzy matching, a minimum normalized query length of 2, at most 50 results, and 180 preview characters. `RefreshGroupAsync(groupKey)` or `RefreshAllAsync()` rescans after files change. Each method returns `Res` and reports a failed read/search through that envelope; check it before using `Data`.

## Viewer navigation and panes

The viewer remains at `/markdown-docs`. `MarkdownViewerLocation` owns shareable navigation through the `group`, `document`, and `culture` query parameters and heading fragment. Use the document's `NavigationRelativePath` for viewer routes; use its `RelativePath`, including the language-root folder where applicable, for filesystem-relative rendering and assets. A local relative `.md` link is rewritten to the viewer route with the current group and document culture. The document language is separate from the interface localization culture. Switching document language retains the navigation path and shows a missing-translation state when that language has no matching document.

```csharp
using Monica.Markdown.UIMarkdown.Models;

var href = new MarkdownViewerLocation(
    "handbook", "getting-started.md", "installation", "en-US").ToRelativeUri();
```

The viewer uses independent outlined library, article, and outline cards sharing the theme's Surface background. The library chooser belongs at the top of the document sidebar, above its tree; the article contains its own search, language, and pane commands above the breadcrumb and reading area. Pane visibility and all commands remain page-owned.

Measure the workspace width and the marked reading-stage height below the article's inline toolbar, after the shell and page gutters have consumed their space. The existing instance-owned interop session observes both through one `ResizeObserver`; it rebinds the reading-stage target only when that element's identity changes. Do not introduce another observer owner or subtract the toolbar height a second time. The library can be pinned at an owner width of at least 1040px and reading-stage height of at least 560px. The outline can be pinned at at least 1280px and 560px when the rendered document has headings at levels 1–3. Smaller or shorter owners use temporary native drawers; these start closed. Separate in-visit preferences restore each pinned pane when space becomes available. A document without qualifying headings has no open outline. When both panes are temporary, opening one closes the other.

Selecting a document closes its temporary library and queues focus/reveal of that document's heading after the matching content renders. Selecting an outline heading closes the temporary outline and navigates to the anchor. Opening group selection or search closes temporary panes. Resize transitions close temporary panes while retaining the independent pinned preferences. Keep pane visibility with the page; child components request actions and do not own navigation or retries.

Reported catalog, tree, content, and search errors display their actual error text and remain distinct from successfully empty results, an empty Markdown file, and missing translations. The catalog failure/empty-state refresh rereads the catalog and reloads the latest route even if its URL has not changed. The group switcher's refresh performs `RefreshAllAsync()` and then reads the new catalog; choosing the currently selected group reloads its tree and content. Hosts that change files programmatically should still call the facade's rescan methods explicitly.

## Composing viewer components

Consumers that embed the public components must migrate the toolbar and metadata parameters:

| Previous composition | Current owner |
| --- | --- |
| `DocumentContentViewer.OnToggleSidebar`, `ShowSidebarToggle` | `DocumentViewerToolbar.OnToggleSidebar` and `IsSidebarVisible`; toolbar composition supplies the action |
| `DocumentContentViewer.OnOpenDocumentSearch`, `OnLanguageChanged`, `ShowLanguageSwitcher` | The same callbacks/flag on `DocumentViewerToolbar` |
| `DocumentContentViewer.OnToggleTableOfContents`, `IsTableOfContentsVisible` | `DocumentViewerToolbar.OnToggleOutline`, `IsOutlineVisible`, and `HasOutline` |
| `DocumentMetadataPanel.FrontMatter`, `FileSize` | Required `DocumentMetadataPanel.Document`, containing the full selected catalog document |

`DocumentSidebar.OnOpenGroupSwitcher` requests the page-owned library chooser. The sidebar and optional toolbar chooser reuse `DocumentLibrarySwitcher`, whose `CurrentGroup` and `CurrentCulture` provide the selected library identity and culture-scoped document count; `OnOpen` requests the owner's chooser without acquiring dialog or document-loading ownership. `DocumentViewerToolbar.ShowLibrarySwitcher` defaults to `true` for embedded compositions. The routed page sets it to `false` because the sidebar supplies the chooser, and passes the accepted `Document` to the toolbar for its modification-date context. Keep one visible library chooser per composition.

`DocumentContentViewer` renders the article, breadcrumb, metadata, loading/error/missing-translation states, and reports `HeadingsChanged`. Supply `ContentError` and `DocumentSidebar.TreeError` from the page's load owner; both default to null and neither child retries the load. `DocumentTableOfContentsPanel.OnClose` is optional: without a callback, it omits the close control; with one, the page closes the pane and handles focus restoration as appropriate.

`DocumentMetadataPanel` displays authored tags and author, followed by the remaining non-null front-matter fields directly as compact inline chips. It excludes the reserved keys `title`, `date`, `tags`, and `author` from custom chips case-insensitively. Values retain Razor encoding; structured values use `MarkdownMetadataFormatter`, and native titles retain the full displayed values. A document without front matter displays no metadata. Pass the actual selected document through the required `Document` parameter. The panel has no About disclosure or path/byte/timestamp inspector; `DocumentViewerToolbar.Document` supplies the actual file modification date and native date/time context. No reading-time estimate is derived from file size.

## Local image assets

`EnableLocalImageAssetEndpoint` defaults to `true`, but mapping the local image endpoint also requires the module's effective Minimal API setting to be enabled. `ModuleMarkdownUIOption.EnableMinimalApi` defaults to null and inherits the host's `EnableMinimalApiByDefault`, whose default is `false`. Set `AddMarkdownUI(options => options.EnableMinimalApi = true)` when the viewer should serve local images independently of that host default. `EnableLocalImageAssetEndpoint = false` disables this endpoint even when Minimal API is enabled.

When mapped, the endpoint uses `/markdown-ui/assets/{groupKey}`; `AssetEndpointBasePath` changes its base path. `AllowedImageExtensions` defaults to `.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`, `.svg`, `.bmp`, `.ico`, and `.avif`. Serving requires both an allowed extension and a known image content type.

The renderer rewrites relative image URLs using the document's group and filesystem-relative path. It leaves already browser-safe absolute/root-relative URLs unchanged. HTTP query binding removes the endpoint transport encoding; `OpenImageAsync` then decodes the authored image URL's percent escapes once before path validation. It does not URL-decode the catalog document path again. For example, `images/release%20notes.png` resolves to a filename containing a space; `images/100%25.png` resolves to a filename containing a literal percent sign. Direct callers of `OpenImageAsync` supply the authored relative asset URL after transport decoding, not a repeatedly decoded filesystem path.

The service resolves an image relative to the document directory, validates its extension, and rejects paths resolving outside the registered group root. Missing files/groups and rejected paths return 404 from the endpoint. Keep assets within that group; a relative parent segment is usable only when the resolved file remains inside it. The shell alone can render basic Markdown components when `EnableMarkdown = true`, but it does not provide searchable groups or the document viewer.

Source and behavioral checks: `Monica.Markdown/Modules/ModuleMarkdown.cs` and `ModuleMarkdownUI.cs`, `Monica.Markdown/Facades/MarkdownFacade.cs`, `Monica.Markdown/Models/MarkdownDocumentSearchRequest.cs`, the components under `Monica.Markdown/UIMarkdown/Components/`, `MarkdownViewerLocation`, `MarkdownViewerPageState`, `MarkdownLocalAssetService`, and the matching non-rendering checks under `tests/Test.Monica.Markdown/UIMarkdown/`.
