using Monica.Framework.UI.UILogging.Models;
using Monica.Framework.UI.UILogging.Support;

namespace Monica.Framework.UI.UILogging.State;

/// <summary>Immutable, circuit-local handoff between the built-in reader and file catalog.</summary>
internal sealed record LogReaderViewState(
    string? Keyword,
    LogSeverity? Severity,
    bool OnlyMatches,
    bool AutoScroll,
    int LineLimit,
    ContextLogResult? Context,
    long ContextVersion,
    LogViewerPosition? Position,
    LogViewerPosition? LivePosition);
