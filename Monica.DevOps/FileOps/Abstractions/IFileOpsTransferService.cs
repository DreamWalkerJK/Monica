using Monica.DevOps.FileOps.Models;

namespace Monica.DevOps.FileOps.Abstractions;

/// <summary>Provides file uploads and owned download streams under the current host-wide policy.</summary>
/// <remarks>The module registers a scoped implementation which callers borrow. Operations throw on failure.
/// The caller owns upload input streams and must dispose each successful download descriptor's content stream,
/// including a stream produced after cancellation or after its UI owner retires.</remarks>
public interface IFileOpsTransferService
{
    /// <summary>Copies a caller-owned stream into a writable child and returns metadata after closing the target.</summary>
    /// <remarks>The declared length is checked against the upload limit. Overwrite truncates an existing writable
    /// file; copy failure or cancellation may leave partial content and does not provide rollback.</remarks>
    Task<FileOpsEntrySummary> SaveUploadAsync(string directoryPath, string fileName, long contentLength,
        Stream content, bool overwrite, CancellationToken cancellationToken = default);

    /// <summary>Opens a downloadable file stream whose disposal belongs to the caller.</summary>
    /// <remarks>Metadata is a snapshot; the file can change after opening.</remarks>
    Task<FileOpsDownloadDescriptor> OpenDownloadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Creates a ZIP of the selected visible descendants and returns a caller-owned temporary stream.</summary>
    /// <remarks>Policy limits each file, not total archive size or recursive entry count. Disposing the successful
    /// stream removes its temporary archive; failed construction attempts clean up its temporary file.</remarks>
    Task<FileOpsDownloadDescriptor> OpenArchiveDownloadAsync(FileOpsArchiveDownloadRequest request,
        CancellationToken cancellationToken = default);
}
