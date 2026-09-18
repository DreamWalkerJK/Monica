using System.Collections.Concurrent;
using System.Dynamic;
using System.Text.Json.Serialization;

namespace Monica.Framework.ChainTracing.Models;

/// <summary>
/// Stores the state for a single call chain. Nodes are linked into a tree when they begin; the ambient
/// current node tracked by <see cref="Services.AsyncLocalChainTracingService" /> decides parentage, so
/// concurrent flows that fork from the same parent never disturb each other.
/// </summary>
public class ChainTraceContext
{
    /// <summary>
    /// Extra-info key used to store serialized chain data.
    /// </summary>
    public const string CHAIN_KEY = "chain";

    /// <summary>
    /// HttpContext.Items key under which request-boundary scopes publish the active chain. Boundaries that
    /// run after the pipeline unwinds — the exception handler cannot observe AsyncLocal mutations made
    /// downstream — recover the chain from there.
    /// </summary>
    public const string HTTP_ITEM_KEY = "Monica.ChainTraceContext";

    /// <summary>
    /// Root node of the chain.
    /// </summary>
    public ChainTraceNode? Root { get; set; }

    /// <summary>
    /// Nodes that became detached because their ancestor scope closed before they did.
    /// </summary>
    public List<ChainTraceNode>? IsolatedNodes { get; set; }

    /// <summary>
    /// Lookup table for trace nodes by identifier.
    /// </summary>
    [JsonIgnore]
    public ConcurrentDictionary<string, ChainTraceNode> NodeMap { get; set; } = new();

    /// <summary>
    /// Time when the chain started.
    /// </summary>
    [JsonIgnore]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Time when the chain completed.
    /// </summary>
    [JsonIgnore]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Additional metadata for the chain.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExpandoObject? OtherInfo { get; set; }

    /// <summary>
    /// Returns whether nodes of the specified type should become the ambient current node.
    /// High-volume operations without child calls (database commands) skip ambient tracking so they
    /// never disturb the enclosing scope.
    /// </summary>
    /// <param name="type">The trace node type.</param>
    /// <returns><see langword="true" /> when nodes of this type can parent child operations.</returns>
    public static bool CanHaveChildOperations(EChainTracingType type)
    {
        return type != EChainTracingType.Database;
    }

    /// <summary>
    /// Adds a new node to the current chain beneath the specified parent. A node arriving without a
    /// parent attaches to the root when one exists, so late starters are never orphaned.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <param name="parent">The ambient parent node, or <see langword="null" /> for a chain root.</param>
    public void AddNode(ChainTraceNode node, ChainTraceNode? parent)
    {
        if (parent is null && Root is { } root)
        {
            parent = root;
        }

        if (parent is null)
        {
            Root = node;
            node.Depth = 1;
        }
        else
        {
            // Parallel flows may complete child calls concurrently; children lists are plain lists.
            lock (parent)
            {
                parent.Children ??= [];
                parent.Children.Add(node);
                node.SetParent(parent);
            }
        }

        NodeMap[node.TraceId] = node;
    }

    /// <summary>
    /// Completes a node by filling in its outcome. Stack or pointer state is owned by the tracing
    /// service and is not touched here.
    /// </summary>
    /// <param name="traceId">The trace identifier.</param>
    /// <param name="result">A description of the result.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="exception">The captured exception, if any.</param>
    /// <param name="extraInfo">Optional completion metadata; existing metadata is preserved when null.</param>
    public void CompleteNode(string traceId, string? result = null, bool success = true, Exception? exception = null, object? extraInfo = null)
    {
        if (!NodeMap.TryGetValue(traceId, out var node))
        {
            return;
        }

        node.EndTime = DateTime.UtcNow;
        node.Result = result;
        node.Exception = exception;
        node.EndExtraInfo = extraInfo ?? node.EndExtraInfo;

        if (exception != null || !success)
        {
            node.IsFailed = true;
        }
    }

    /// <summary>
    /// Records a node as detached because an ancestor scope closed before it did.
    /// </summary>
    /// <param name="node">The leaked node.</param>
    public void MarkIsolated(ChainTraceNode node)
    {
        IsolatedNodes ??= [];
        IsolatedNodes.Add(node);
    }

    /// <summary>
    /// Marks the chain as completed.
    /// </summary>
    public void MarkComplete()
    {
        EndTime = DateTime.UtcNow;
    }
}
