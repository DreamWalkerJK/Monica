namespace Monica.DevOps.FileOps.State;

/// <summary>Passive circuit-local identities used to revalidate a return to Files.</summary>
/// <remarks>Contains no drafts, content, producers or disposable owners. Each Files visit reads current
/// policy and disk evidence before restoring these identities. Paths never enter URLs or browser storage.</remarks>
public sealed class FileOpsNavigationState
{
    /// <summary>The last accepted directory, or null for the normalized startup directory.</summary>
    public string? DirectoryPath { get; set; }
    /// <summary>The client sort key for the accepted displayed subset.</summary>
    public FileOpsSortField SortField { get; set; }
    /// <summary>Whether to reverse the selected key while retaining directories first.</summary>
    public bool SortDescending { get; set; }
    /// <summary>Checked identities pruned against fresh returned entries on restoration.</summary>
    public List<string> CheckedPaths { get; set; } = [];
    /// <summary>A clean inspected path to reopen through a fresh read; never cached text.</summary>
    public string? InspectedPath { get; set; }
}
