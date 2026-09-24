using Microsoft.Extensions.AI;
using Monica.AI.Chat.Services;

namespace Monica.AI.Providers.OpenAI;

/// <summary>Recognizes only the first non-whitespace, exact think opener; later literal tags stay visible.</summary>
internal sealed class LeadingThinkParser
{
    private const string OPEN_TAG = "<think>";
    private const string CLOSE_TAG = "</think>";
    private string _pending = string.Empty;
    private Phase _phase;
    private bool _previousWasReasoning;
    private string? _previousFormat;

    internal List<AIContent> Append(IList<AIContent> contents)
    {
        var output = new List<AIContent>();
        // Native reasoning is authoritative when it arrives before the text. Do not reinterpret
        // a literal <think> example in the visible answer of a native-reasoning response.
        if (_phase == Phase.Prefix && contents.Any(static content => content is TextReasoningContent))
            FinishInto(output);
        foreach (var content in contents)
        {
            if (content is TextContent text && _phase != Phase.Text)
            {
                // Citation offsets refer to the original visible text. Keep annotated content literal
                // instead of stripping characters and leaving those offsets invalid.
                if (text.Annotations is { Count: > 0 })
                {
                    FinishInto(output);
                    Add(text, output);
                }
                else if (_phase == Phase.Prefix && _pending.Length == 0 && !CouldStartBlock(text.Text))
                {
                    if (!string.IsNullOrWhiteSpace(text.Text)) _phase = Phase.Text;
                    Add(text, output);
                }
                else AppendText(text.Text, output);
            }
            else
            {
                if (content is FunctionCallContent) FinishInto(output);
                Add(content, output);
            }
        }
        return output;
    }

    private static bool CouldStartBlock(string text)
    {
        var candidate = text.AsSpan().TrimStart();
        return !candidate.IsEmpty && (candidate.StartsWith(OPEN_TAG, StringComparison.Ordinal)
            || OPEN_TAG.AsSpan().StartsWith(candidate, StringComparison.Ordinal));
    }

    internal List<AIContent> Complete()
    {
        var output = new List<AIContent>();
        FinishInto(output);
        return output;
    }

    private void AppendText(string text, List<AIContent> output)
    {
        if (_phase == Phase.Prefix)
        {
            if (_pending.Length == 0)
            {
                var whitespace = 0;
                while (whitespace < text.Length && char.IsWhiteSpace(text[whitespace])) whitespace++;
                AddText(text[..whitespace], output);
                text = text[whitespace..];
            }
            var match = 0;
            while (match < text.Length && _pending.Length + match < OPEN_TAG.Length
                   && text[match] == OPEN_TAG[_pending.Length + match]) match++;
            _pending += text[..match];
            text = text[match..];
            if (_pending.Length == OPEN_TAG.Length)
            {
                _pending = string.Empty;
                _phase = Phase.Reasoning;
                // An empty block is still an observed transport format and must survive replay.
                AddReasoning(string.Empty, output);
            }
            else if (text.Length == 0) return;
            else
            {
                AddText(_pending + text, output);
                _pending = string.Empty;
                _phase = Phase.Text;
                return;
            }
        }

        if (_phase == Phase.Reasoning)
        {
            text = _pending + text;
            var closing = text.IndexOf(CLOSE_TAG, StringComparison.Ordinal);
            if (closing >= 0)
            {
                AddReasoning(text[..closing], output);
                AddText(text[(closing + CLOSE_TAG.Length)..], output);
                _pending = string.Empty;
                _phase = Phase.Text;
                return;
            }
            var retained = Math.Min(CLOSE_TAG.Length - 1, text.Length);
            while (retained > 0 && !text.AsSpan(text.Length - retained).SequenceEqual(CLOSE_TAG.AsSpan(0, retained))) retained--;
            if (text.Length > retained) AddReasoning(text[..(text.Length - retained)], output);
            _pending = retained == 0 ? string.Empty : text[^retained..];
        }
        else AddText(text, output);
    }

    private void FinishInto(List<AIContent> output)
    {
        if (_pending.Length > 0)
        {
            if (_phase == Phase.Reasoning) AddReasoning(_pending, output);
            else AddText(_pending, output);
        }
        _pending = string.Empty;
        _phase = Phase.Text;
    }

    private void AddText(string text, List<AIContent> output)
    {
        if (text.Length > 0) Add(new TextContent(text), output);
    }

    private void AddReasoning(string text, List<AIContent> output)
        => Add(new TextReasoningContent(text)
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [ChatReasoningFormat.PROPERTY_KEY] = ChatReasoningFormat.THINK_TAGS
            }
        }, output);

    private void Add(AIContent content, List<AIContent> output)
    {
        if (content is TextReasoningContent)
        {
            var format = ChatReasoningFormat.Read(content);
            // M.E.AI coalesces neighboring reasoning regardless of AdditionalProperties. A zero-width
            // text boundary keeps two observed transports distinct through the agent's tool loop.
            if (_previousWasReasoning && _previousFormat != format) output.Add(new TextContent(string.Empty));
            _previousWasReasoning = true;
            _previousFormat = format;
        }
        else if (content is not UsageContent) _previousWasReasoning = false;
        output.Add(content);
    }

    private enum Phase { Prefix, Reasoning, Text }
}
