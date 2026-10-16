using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Monica.UI.UIStackTrace.Support;
using Xunit;

namespace Test.Monica.UI.UIStackTrace.Support;

public sealed class StackTraceSourcePresentationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n\r\n\n  ")]
    [InlineData("\r\n\tleading\r\n\runknown prose\ntrailing  \t\r\n")]
    [InlineData("---> malformed marker retains <script> and & suffix\n--->\n")]
    [InlineData("System.AggregateException: One or more errors occurred. (outer)\n ---> System.InvalidOperationException: inner\n   --- End of inner exception stack trace ---\n--- End of stack trace from previous location ---\n尾部 😀 <tag> & \"quotes\"")]
    [InlineData("   at Demo.Worker.Run(ref List<string> values, out int result, in int[][] input) unknown suffix\n   at Demo.Container+Nested.<Run>d__7.MoveNext() in /src/Worker.cs:line 42\n")]
    [InlineData("   at Demo.Worker.Run((string, int) tuple, Dictionary<string, List<int[,]>> values) in C:\\source\\Worker.cs:line 9 [compiler suffix]\n")]
    [InlineData("same line\r\nsame line\nsame line\r")]
    public void Create_WhenSourceIsEmptyPartialOrUnusual_ShouldRetainEveryOriginalCodeUnit(string? source)
    {
        AssertCompleteSource(source);
    }

    [Theory]
    [InlineData("C:\\source\\nested\\Worker.cs", "Worker.cs")]
    [InlineData("\\\\server\\share\\Worker.cs", "Worker.cs")]
    [InlineData("/src/nested/Worker.cs", "Worker.cs")]
    [InlineData("C:\\source/nested\\Worker.cs", "Worker.cs")]
    public void Create_WhenRecognizedFrameContainsLocation_ShouldHighlightOriginalPortablePathAndDigits(string path, string filename)
    {
        var source = $"\t at Demo.Worker.Run(ref int value) in {path}:line 0042 [unknown suffix]";
        var spans = AssertCompleteSource(source);

        TextFor(source, spans, StackTraceSourceRole.Method).Should().Equal("Run");
        TextFor(source, spans, StackTraceSourceRole.Location).Should().Equal(filename, "0042");
        TextFor(source, spans, StackTraceSourceRole.Muted).Should().Contain("at").And.Contain("in");
        string.Concat(TextFor(source, spans, StackTraceSourceRole.Plain)).Should().Contain("(ref int value)")
            .And.EndWith(" [unknown suffix]");
    }

    [Fact]
    public void Create_WhenHeaderHasRealInnerPrefix_ShouldEmphasizeOnlyItsOriginalType()
    {
        const string source = "  ---> System.InvalidOperationException: <script>alert('x')</script> & 消息 😀\r\n";
        var spans = AssertCompleteSource(source);

        TextFor(source, spans, StackTraceSourceRole.ExceptionType).Should().Equal("System.InvalidOperationException");
        string.Concat(TextFor(source, spans, StackTraceSourceRole.Plain)).Should().StartWith("  ---> ")
            .And.EndWith(": <script>alert('x')</script> & 消息 😀\r\n");
    }

    [Theory]
    [InlineData("---> payload that is not an exception header")]
    [InlineData("   at Demo.Container+Nested.Run() in /src/Worker.cs:line 1")]
    [InlineData("   at Demo.Worker.Run$unknown()")]
    [InlineData("at Demo.Worker.Run()")]
    [InlineData("--- End of an invented relationship ---")]
    public void Create_WhenHintIsUnsupportedOrOnlyMethodPrefix_ShouldLeaveTheWholeSourceOrdinary(string source)
    {
        var spans = AssertCompleteSource(source);

        spans.Should().ContainSingle().Which.Role.Should().Be(StackTraceSourceRole.Plain);
    }

    [Fact]
    public void Create_WhenSourceIsLong_ShouldKeepFullTokenAndUniqueTail()
    {
        var longToken = new string('x', 16_384);
        var source = "System.Exception: " + longToken + "\r\n" + string.Concat(Enumerable.Repeat(
            "   at Demo.Worker.Run() in /src/Worker.cs:line 42 [retained suffix]\r\n", 512)) + "\r\n UNIQUE TAIL 😀 \t";
        var spans = AssertCompleteSource(source);

        string.Concat(TextFor(source, spans, StackTraceSourceRole.Plain)).Should().Contain(longToken).And.EndWith("\r\n UNIQUE TAIL 😀 \t");
    }

    [Fact]
    public void Create_WhenGivenRuntimeExceptionText_ShouldPreserveItsCompleteCapturedEvidence()
    {
        var exception = CaptureException();
        var source = exception.ToString();

        source.Should().Contain(nameof(ThrowForCapture));
        AssertCompleteSource(source);
    }

    private static IReadOnlyList<StackTraceSourceSpan> AssertCompleteSource(string? source)
    {
        var spans = StackTraceSourcePresentation.Create(source, new StackTraceParser());
        var cursor = 0;
        foreach (var span in spans)
        {
            span.Start.Should().Be(cursor);
            span.Length.Should().BePositive();
            span.End.Should().BeLessThanOrEqualTo(source!.Length);
            cursor = span.End;
        }
        cursor.Should().Be(source?.Length ?? 0);
        string.Concat(spans.Select(span => source!.Substring(span.Start, span.Length))).Should().Be(source ?? string.Empty);
        return spans;
    }

    private static IEnumerable<string> TextFor(string source, IEnumerable<StackTraceSourceSpan> spans, StackTraceSourceRole role)
        => spans.Where(span => span.Role == role).Select(span => source.Substring(span.Start, span.Length));

    private static Exception CaptureException()
    {
        try
        {
            ThrowForCapture();
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
        throw new InvalidOperationException("Expected diagnostic capture to throw.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowForCapture() => throw new InvalidOperationException("Runtime capture <tag> & 尾部 😀", new ArgumentException("Inner evidence"));
}
