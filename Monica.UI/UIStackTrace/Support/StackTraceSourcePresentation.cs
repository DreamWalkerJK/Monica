using Monica.UI.UIStackTrace.Models;

namespace Monica.UI.UIStackTrace.Support;

/// <summary>
/// Adds conservative reading emphasis to original diagnostic source. Parser results are hints only:
/// every returned range refers to unchanged source, including all unrecognized text and line endings.
/// </summary>
internal static class StackTraceSourcePresentation
{
    internal static IReadOnlyList<StackTraceSourceSpan> Create(string? source, StackTraceParser parser)
    {
        ArgumentNullException.ThrowIfNull(parser);
        if (string.IsNullOrEmpty(source)) return [];

        var highlights = new List<StackTraceSourceSpan>();
        var offset = 0;
        while (offset < source.Length)
        {
            var newline = source.AsSpan(offset).IndexOfAny('\r', '\n');
            var length = newline < 0 ? source.Length - offset : newline;
            HighlightLine(source.Substring(offset, length), offset, parser, highlights);
            offset += length;
            if (offset < source.Length)
            {
                offset += source[offset] == '\r' && offset + 1 < source.Length && source[offset + 1] == '\n' ? 2 : 1;
            }
        }

        var spans = new List<StackTraceSourceSpan>();
        var cursor = 0;
        foreach (var highlight in highlights)
        {
            // A doubtful hint loses its emphasis, never its original evidence.
            if (highlight.Start < cursor || highlight.Length <= 0 || highlight.End > source.Length) continue;
            if (highlight.Start > cursor) spans.Add(new(cursor, highlight.Start - cursor, StackTraceSourceRole.Plain));
            spans.Add(highlight);
            cursor = highlight.End;
        }
        if (cursor < source.Length) spans.Add(new(cursor, source.Length - cursor, StackTraceSourceRole.Plain));
        return spans.AsReadOnly();
    }

    private static void HighlightLine(string line, int offset, StackTraceParser parser, List<StackTraceSourceSpan> highlights)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        var start = SkipWhitespace(line, 0);
        var boundary = line.AsSpan(start).TrimEnd();
        if (boundary.SequenceEqual("--- End of inner exception stack trace ---") ||
            boundary.SequenceEqual("--- End of stack trace from previous location ---"))
        {
            highlights.Add(new(offset + start, boundary.Length, StackTraceSourceRole.Muted));
            return;
        }

        var parsed = parser.Parse(line);
        if (!parsed.Success) return;
        var header = parsed.Lines.FirstOrDefault(static value => value.LineType == StackLineType.ExceptionHeader);
        if (header?.ExceptionType is { Length: > 0 } exceptionType)
        {
            if (line.AsSpan(start).StartsWith("--->", StringComparison.Ordinal)) start = SkipWhitespace(line, start + 4);
            if (!line.AsSpan(start).StartsWith(exceptionType, StringComparison.Ordinal)) return;
            var colon = SkipWhitespace(line, start + exceptionType.Length);
            if (colon < line.Length && line[colon] == ':')
                highlights.Add(new(offset + start, exceptionType.Length, StackTraceSourceRole.ExceptionType));
            return;
        }

        var frame = parsed.Lines.FirstOrDefault(static value => value.LineType == StackLineType.StackFrame);
        if (frame?.FullMethod is not { Length: > 0 } fullMethod || frame.MethodName is not { Length: > 0 } methodName) return;
        if (!line.AsSpan(start).StartsWith("at", StringComparison.Ordinal) || start + 2 >= line.Length || !char.IsWhiteSpace(line[start + 2])) return;
        var methodStart = SkipWhitespace(line, start + 2);
        if (!line.AsSpan(methodStart).StartsWith(fullMethod, StringComparison.Ordinal) ||
            !fullMethod.EndsWith("." + methodName, StringComparison.Ordinal)) return;
        var methodEnd = methodStart + fullMethod.Length;
        if (methodEnd < line.Length && !char.IsWhiteSpace(line[methodEnd]) && line[methodEnd] != '(') return;

        highlights.Add(new(offset + start, 2, StackTraceSourceRole.Muted));
        highlights.Add(new(offset + methodStart + fullMethod.Length - methodName.Length, methodName.Length, StackTraceSourceRole.Method));
        HighlightLocation(line, offset, SkipWhitespace(line, methodEnd), frame, highlights);
    }

    private static void HighlightLocation(string line, int offset, int start, StackTraceLine frame, List<StackTraceSourceSpan> highlights)
    {
        if (frame.Parameters is { Length: > 0 } parameters)
        {
            if (!line.AsSpan(start).StartsWith(parameters, StringComparison.Ordinal)) return;
            start = SkipWhitespace(line, start + parameters.Length);
        }

        if (frame.FilePath is not { Length: > 0 } path ||
            !line.AsSpan(start).StartsWith("in", StringComparison.Ordinal) || start + 2 >= line.Length || !char.IsWhiteSpace(line[start + 2])) return;
        var pathStart = SkipWhitespace(line, start + 2);
        if (!line.AsSpan(pathStart).StartsWith(path, StringComparison.Ordinal)) return;
        var locationStart = SkipWhitespace(line, pathStart + path.Length);
        if (!line.AsSpan(locationStart).StartsWith(":line", StringComparison.Ordinal)) return;
        var digitStart = SkipWhitespace(line, locationStart + 5);
        if (digitStart == locationStart + 5) return;
        var digitEnd = digitStart;
        while (digitEnd < line.Length && char.IsDigit(line[digitEnd])) digitEnd++;
        if (digitEnd == digitStart) return;

        highlights.Add(new(offset + start, 2, StackTraceSourceRole.Muted));
        var filenameStart = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\')) + 1;
        if (filenameStart > 0) highlights.Add(new(offset + pathStart, filenameStart, StackTraceSourceRole.Muted));
        if (filenameStart < path.Length) highlights.Add(new(offset + pathStart + filenameStart, path.Length - filenameStart, StackTraceSourceRole.Location));
        if (frame.LineNumber.HasValue && int.TryParse(line.AsSpan(digitStart, digitEnd - digitStart), out var number) && number == frame.LineNumber.Value)
            highlights.Add(new(offset + digitStart, digitEnd - digitStart, StackTraceSourceRole.Location));
    }

    private static int SkipWhitespace(string source, int start)
    {
        while (start < source.Length && char.IsWhiteSpace(source[start])) start++;
        return start;
    }
}

internal readonly record struct StackTraceSourceSpan(int Start, int Length, StackTraceSourceRole Role)
{
    internal int End => Start + Length;
}

internal enum StackTraceSourceRole
{
    Plain,
    ExceptionType,
    Method,
    Location,
    Muted
}
