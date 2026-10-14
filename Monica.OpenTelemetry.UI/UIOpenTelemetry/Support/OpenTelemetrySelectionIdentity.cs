using Monica.OpenTelemetry.InProcessCollector.Models;

namespace Monica.OpenTelemetry.UI.UIOpenTelemetry.Support;

/// <summary>Reconciles a selection only when its source identity resolves to exactly one current item.</summary>
internal static class OpenTelemetrySelectionIdentity
{
    public static InstrumentSnapshot? ReconcileInstrument(
        InstrumentSnapshot previous, IEnumerable<InstrumentSnapshot> candidates)
    {
        var matches = candidates.Where(candidate => HasId(previous)
            ? string.Equals(previous.InstrumentId, candidate.InstrumentId, StringComparison.Ordinal)
            : !HasId(candidate) && MetadataEqual(previous, candidate));
        return Unique(matches);
    }

    public static TagSetSamples? ReconcileSeries(TagSetSamples previous, IEnumerable<TagSetSamples> candidates) =>
        Unique(candidates.Where(candidate => string.Equals(previous.Key, candidate.Key, StringComparison.Ordinal) &&
            TagsEqual(previous.Tags, candidate.Tags)));

    private static bool HasId(InstrumentSnapshot instrument) => !string.IsNullOrEmpty(instrument.InstrumentId);

    private static bool MetadataEqual(InstrumentSnapshot left, InstrumentSnapshot right) =>
        string.Equals(left.MeterName, right.MeterName, StringComparison.Ordinal) &&
        string.Equals(left.MeterVersion, right.MeterVersion, StringComparison.Ordinal) &&
        string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
        string.Equals(left.Kind, right.Kind, StringComparison.Ordinal) &&
        string.Equals(left.Unit, right.Unit, StringComparison.Ordinal) &&
        string.Equals(left.Description, right.Description, StringComparison.Ordinal) &&
        left.IsObservable == right.IsObservable;

    private static bool TagsEqual(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
    {
        if (left.Count != right.Count) return false;
        // Do not inherit a provider dictionary's comparer: normalized tag identity is ordinal.
        var rightOrdinal = right.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
        return left.All(pair => rightOrdinal.TryGetValue(pair.Key, out var value) &&
            string.Equals(pair.Value, value, StringComparison.Ordinal));
    }

    private static T? Unique<T>(IEnumerable<T> candidates) where T : class
    {
        using var iterator = candidates.GetEnumerator();
        if (!iterator.MoveNext()) return null;
        var result = iterator.Current;
        return iterator.MoveNext() ? null : result;
    }
}
