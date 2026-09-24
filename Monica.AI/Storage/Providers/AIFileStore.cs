using System.Text.Json;
using Microsoft.Extensions.Options;
using Monica.Modules;

namespace Monica.AI.Storage.Providers;

/// <summary>Owns rooted, atomic file access shared by the AI file providers.</summary>
internal sealed class AIFileStore(IOptions<ModuleAIOption> options)
{
    private static readonly JsonSerializerOptions JSON_OPTIONS = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly string _root = Path.GetFullPath(options.Value.StorageRootPath);

    public string GetPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("AI storage paths must be relative to the configured root.", nameof(relativePath));
        }

        var path = Path.GetFullPath(Path.Combine(_root, relativePath));
        var relative = Path.GetRelativePath(_root, path);
        if (relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new ArgumentException("AI storage paths cannot leave the configured root.", nameof(relativePath));
        }

        return path;
    }

    public T? Read<T>(string relativePath)
    {
        var path = GetPath(relativePath);
        if (!File.Exists(path))
        {
            return default;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        return JsonSerializer.Deserialize<T>(stream, JSON_OPTIONS)
               ?? throw new InvalidDataException($"AI storage file '{relativePath}' contains no document.");
    }

    public async Task<T?> ReadAsync<T>(string relativePath, CancellationToken ct = default)
    {
        var path = GetPath(relativePath);
        if (!File.Exists(path))
        {
            return default;
        }

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read | FileShare.Delete, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await JsonSerializer.DeserializeAsync<T>(stream, JSON_OPTIONS, ct)
               ?? throw new InvalidDataException($"AI storage file '{relativePath}' contains no document.");
    }

    public async Task WriteAsync<T>(string relativePath, T value, CancellationToken ct = default)
    {
        var path = GetPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, 65536, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, value, JSON_OPTIONS, ct);
                await stream.FlushAsync(ct);
                stream.Flush(flushToDisk: true);
            }

            ct.ThrowIfCancellationRequested();
            // Renaming within one directory publishes a complete document to concurrent readers.
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public async Task<T> WithLockAsync<T>(
        string relativePath,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        var path = GetPath(relativePath + ".lock");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var deadline = Environment.TickCount64 + 30000;
        FileStream lease;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                lease = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                break;
            }
            catch (IOException ex) when (Environment.TickCount64 < deadline && IsSharingViolation(ex))
            {
                // The lock file is kept in place so another process cannot acquire a different inode.
                await Task.Delay(25, ct);
            }
        }

        await using (lease)
        {
            return await action(ct);
        }
    }

    private static bool IsSharingViolation(IOException exception)
        => (exception.HResult & 0xFFFF) is 11 or 32 or 33;
}
