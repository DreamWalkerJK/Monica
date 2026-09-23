namespace Monica.AI.UI.UIChat.Models;

/// <summary>Identifies a non-destructive durable-history warning raised by the UI workspace.</summary>
public enum ChatHistoryWorkspaceWarningKind
{
    /// <summary>Persisted history could not be loaded.</summary>
    LoadFailed,

    /// <summary>A stale browser tab attempted to overwrite a newer catalog revision.</summary>
    RevisionConflict,

    /// <summary>The storage provider did not have enough capacity for the active conversation.</summary>
    QuotaExceeded,

    /// <summary>Persistence was unavailable while the in-memory conversation remained usable.</summary>
    StorageUnavailable,

    /// <summary>Older inactive conversations were pruned to satisfy retention or quota constraints.</summary>
    SessionsPruned,

    /// <summary>A catalog entry no longer had a readable snapshot.</summary>
    SessionUnavailable,

    /// <summary>A committed write returned additional diagnostic information from the storage provider.</summary>
    PersistenceWarning
}

/// <summary>Describes a warning that should be presented without adding a transcript error.</summary>
/// <param name="Kind">Warning category used for localization.</param>
/// <param name="Details">Optional diagnostic details.</param>
public sealed record ChatHistoryWorkspaceWarning(
    ChatHistoryWorkspaceWarningKind Kind,
    string? Details = null);
