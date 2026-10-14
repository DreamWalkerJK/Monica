using Monica.Configuration.Models;

namespace Monica.Configuration.UI.Support;

/// <summary>Owns one frozen History request pair so late replacements and cursor pages cannot cross query scope.</summary>
/// <remarks>Even an identical refresh supersedes earlier work. Adoption contains only synchronous state updates;
/// producers, subscribers, native dialog work and cancellation must run outside this gate.</remarks>
internal sealed class ConfigurationHistoryQuerySession
{
    private readonly object _gate = new();
    private Query? _current;

    internal sealed record Query(ConfigurationHistoryPageRequest History, ConfigurationMutationGroupPageRequest Groups);

    internal Query Begin(ConfigurationHistoryPageRequest history, ConfigurationMutationGroupPageRequest groups)
    {
        var query = new Query(history, groups);
        lock (_gate) _current = query;
        return query;
    }

    internal bool IsCurrent(Query query) { lock (_gate) return ReferenceEquals(query, _current); }

    internal bool TryApply(Query query, Action update)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(query, _current)) return false;
            update();
            return true;
        }
    }

    internal void Invalidate() { lock (_gate) _current = null; }
}
