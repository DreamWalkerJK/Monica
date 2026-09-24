using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.AI;

namespace Monica.AI.Providers.OpenAI;

/// <summary>Separates an observed leading think block for explicitly reasoning-capable Chat models.</summary>
internal sealed class TaggedReasoningChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        var normalized = new List<ChatMessage>(response.Messages.Count);
        var changed = false;
        foreach (var message in response.Messages)
        {
            var parser = new LeadingThinkParser();
            var contents = parser.Append(message.Contents);
            contents.AddRange(parser.Complete());
            if (contents.SequenceEqual(message.Contents)) normalized.Add(message);
            else
            {
                var copy = message.Clone();
                copy.Contents = contents;
                copy.RawRepresentation = null;
                normalized.Add(copy);
                changed = true;
            }
        }

        return !changed ? response : new ChatResponse(normalized)
        {
            ResponseId = response.ResponseId, ConversationId = response.ConversationId,
            ModelId = response.ModelId, CreatedAt = response.CreatedAt, FinishReason = response.FinishReason,
            Usage = response.Usage, ContinuationToken = response.ContinuationToken,
            AdditionalProperties = response.AdditionalProperties?.Clone()
        };
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var parser = new LeadingThinkParser();
        ChatResponseUpdate? last = null;
        Exception? failure = null;
        await using var enumerator = base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            bool hasNext;
            try { hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false); }
            catch (Exception ex) { failure = ex; break; }
            if (!hasNext) break;

            last = enumerator.Current;
            var contents = parser.Append(last.Contents);
            if (last.FinishReason is not null) contents.AddRange(parser.Complete());
            if (contents.SequenceEqual(last.Contents)) yield return last;
            else
            {
                var copy = last.Clone();
                copy.Contents = contents;
                copy.RawRepresentation = null;
                yield return copy;
            }
        }

        // Flush already received text before propagating failure, including cancellation. A partial opening
        // tag is ordinary text; a partial closing tag belongs to the reasoning already in progress.
        var remainder = parser.Complete();
        if (remainder.Count > 0)
        {
            var copy = last?.Clone() ?? new ChatResponseUpdate { Role = ChatRole.Assistant };
            copy.Contents = remainder;
            copy.RawRepresentation = null;
            copy.FinishReason = null;
            yield return copy;
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
