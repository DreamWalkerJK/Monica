namespace Monica.DevOps.Terminal.Models;

/// <summary>
/// Request used to create a terminal session.
/// </summary>
public sealed class TerminalSessionCreateRequest
{
    /// <summary>
    /// Gets or sets the optional display name. A generated name is used when omitted.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the optional initial working directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }
}

/// <summary>
/// Request used to execute a command in a terminal session.
/// </summary>
public sealed class TerminalCommandRequest
{
    /// <summary>
    /// Gets or sets command text; surrounding whitespace is trimmed at admission.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command timeout in seconds. Defaults to the module option when omitted.
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// A single output frame emitted by a terminal command.
/// </summary>
public sealed record TerminalOutputLine
{
    /// <summary>
    /// Gets the stream that produced the output. Expected values are stdout, stderr, system, and input.
    /// </summary>
    public string Stream { get; init; } = TerminalOutputStreams.System;

    /// <summary>
    /// Gets the line content.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the line was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
}

/// <summary>
/// Detached final command result; its mutable output list belongs to the caller.
/// </summary>
public sealed class TerminalCommandResult
{
    /// <summary>
    /// Gets or sets the executed command text.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the working directory used to start the command.
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the process exit code.
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Gets or sets whether the command was canceled before normal process exit.
    /// </summary>
    public bool Canceled { get; set; }

    /// <summary>
    /// Gets or sets whether the command timed out.
    /// </summary>
    public bool TimedOut { get; set; }

    /// <summary>
    /// Gets or sets duration through process, output and callback drainage.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the captured output lines.
    /// </summary>
    public List<TerminalOutputLine> Output { get; set; } = [];
}

/// <summary>
/// Well-known terminal output stream identifiers.
/// </summary>
public static class TerminalOutputStreams
{
    /// <summary>
    /// Standard output stream.
    /// </summary>
    public const string Stdout = "stdout";

    /// <summary>
    /// Standard error stream.
    /// </summary>
    public const string Stderr = "stderr";

    /// <summary>
    /// User-entered command stream.
    /// </summary>
    public const string Input = "input";

    /// <summary>
    /// Module-generated diagnostic stream.
    /// </summary>
    public const string System = "system";
}
