using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Monica.Profiling.MemoryDiagnostics.Providers.DotNetTools;

internal sealed class DotNetGcDumpProvider(ILogger<DotNetGcDumpProvider> logger)
{
    public async Task<string> CollectAsync(string? outputPath = null)
    {
        var processId = Environment.ProcessId;
        var dumpPath = ResolveOutputPath(outputPath, processId);
        using var process = StartProcess(CreateStartInfo(processId, dumpPath));

        // Both redirected pipes must drain while the child runs, before waiting for its exit.
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(process.WaitForExitAsync(), standardOutput, standardError);

        var completedPath = ValidateCompletion(
            process.ExitCode, await standardOutput, await standardError, dumpPath);
        logger.LogInformation("GC dump created at {Path}", completedPath);
        return completedPath;
    }

    internal static string ResolveOutputPath(string? outputPath, int processId)
    {
        var requestedPath = outputPath ?? Path.Combine(Path.GetTempPath(),
            $"gcdump_{processId}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fffffff}_{Guid.NewGuid():N}.gcdump");
        var fullPath = Path.GetFullPath(requestedPath);
        // dotnet-gcdump appends this exact suffix to an absent or differently cased extension.
        return string.Equals(Path.GetExtension(fullPath), ".gcdump", StringComparison.Ordinal)
            ? fullPath
            : fullPath + ".gcdump";
    }

    internal static ProcessStartInfo CreateStartInfo(int processId, string dumpPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet-gcdump",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("collect");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(processId.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(dumpPath);
        return startInfo;
    }

    private static Process StartProcess(ProcessStartInfo startInfo)
    {
        try
        {
            return Process.Start(startInfo)
                ?? throw new InvalidOperationException("Unable to start the dotnet-gcdump process.");
        }
        catch (Win32Exception exception)
        {
            throw CreateStartFailure(exception);
        }
    }

    internal static InvalidOperationException CreateStartFailure(Win32Exception exception)
    {
        var executableNotFound = exception.NativeErrorCode == 2
            || (OperatingSystem.IsWindows() && exception.NativeErrorCode == 3);
        return executableNotFound
            ? new InvalidOperationException(
                "dotnet-gcdump could not be found on the host process PATH. Install it with: dotnet tool install -g dotnet-gcdump",
                exception)
            : new InvalidOperationException(
                $"Unable to start dotnet-gcdump (native error {exception.NativeErrorCode}): {exception.Message}",
                exception);
    }

    internal static string ValidateCompletion(int exitCode, string standardOutput, string standardError, string dumpPath)
    {
        var outputEvidence = $"Standard output:\n{standardOutput}\nStandard error:\n{standardError}";
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"GC dump creation failed (exit code {exitCode}).\n{outputEvidence}");
        }

        var outputFile = new FileInfo(dumpPath);
        if (!outputFile.Exists || outputFile.Length == 0)
        {
            throw new InvalidOperationException(
                $"dotnet-gcdump exited successfully but did not create a nonempty file at '{dumpPath}'.\n{outputEvidence}");
        }

        return outputFile.FullName;
    }
}
