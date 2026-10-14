using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Monica.DevOps.Terminal.Models;

/// <summary>Detached immutable host environment evidence.</summary>
public sealed record TerminalEnvironmentSnapshot(
    string OperatingSystemFamily, string OperatingSystemDescription, Architecture Architecture,
    string RuntimeIdentifier, string ShellPath, ImmutableArray<string> ShellArguments,
    string DefaultWorkingDirectory, string ApplicationBaseDirectory)
{
    internal static TerminalEnvironmentSnapshot From(TerminalEnvironmentInfo environment) => new(
        environment.OperatingSystemFamily, environment.OperatingSystemDescription, environment.Architecture,
        environment.RuntimeIdentifier, environment.ShellPath, [.. environment.ShellArguments],
        environment.DefaultWorkingDirectory, environment.ApplicationBaseDirectory);
}

/// <summary>Detached immutable evidence of a retained host-memory session.</summary>
/// <remarks>The browser owner partition is not authorization. Execution continues after a UI visit leaves.</remarks>
public sealed record TerminalSessionSnapshot(
    string Id, string Name, string WorkingDirectory, DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt, long Revision, ImmutableArray<string> History,
    ImmutableArray<TerminalOutputLine> Output, TerminalActiveCommandSnapshot? ActiveCommand,
    TerminalCommandResultSnapshot? LastResult, TerminalExecutionFailureSnapshot? LastExecutionFailure)
{
    /// <summary>Gets whether execution, output, or command callback drainage is active.</summary>
    public bool IsRunning => ActiveCommand is not null;
}

/// <summary>An admitted command and its cancellation request state.</summary>
public sealed record TerminalActiveCommandSnapshot(
    string CommandId, string Command, string WorkingDirectory, bool CancellationRequested);

/// <summary>Immutable completed command evidence, preserving prior results while a later command runs.</summary>
/// <remarks>Duration includes actual process/output/observer drainage. Cancellation does not undo effects.
/// A successful built-in cd reports its resulting directory. Timeout takes precedence over cancellation.</remarks>
public sealed record TerminalCommandResultSnapshot(
    string CommandId, string Command, string WorkingDirectory, int ExitCode, bool Canceled,
    bool TimedOut, TimeSpan Duration, ImmutableArray<TerminalOutputLine> Output);

/// <summary>A failed execution attempt, separate from an earlier completed command result.</summary>
public sealed record TerminalExecutionFailureSnapshot(
    string CommandId, string Command, string WorkingDirectory, string Message, DateTimeOffset OccurredAt);

/// <summary>Host command acceptance, not a guarantee of process start or successful completion.</summary>
/// <remarks>A fast command may have completed in Session already. Observe later snapshots using CommandId.
/// Route departure does not revoke acceptance.</remarks>
public sealed record TerminalCommandAcceptance(string CommandId, TerminalSessionSnapshot Session);
