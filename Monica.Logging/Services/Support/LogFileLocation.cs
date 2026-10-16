using Monica.Modules;

namespace Monica.Logging.Services.Support;

/// <summary>Resolves the default Monica Serilog file sink location for writers and local inspection.</summary>
public static class LogFileLocation
{
    /// <summary>
    /// Returns an absolute sink path. A nonblank explicit file path wins; otherwise the file is beneath
    /// the Logs child of the configured base directory, or AppContext.BaseDirectory when no base is supplied.
    /// Custom logger factories may write elsewhere and must configure local inspection explicitly.
    /// </summary>
    public static string Resolve(ModuleLoggingOption options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Path.GetFullPath(!string.IsNullOrWhiteSpace(options.LogFilePath)
            ? options.LogFilePath
            : Path.Combine(string.IsNullOrWhiteSpace(options.LogDirectory) ? AppContext.BaseDirectory : options.LogDirectory,
                "Logs", options.LogFileName));
    }
}
