using System.Collections.Immutable;
using Monica.Core.ObjectMapping.Models;

namespace Monica.Framework.UI.UIMapper.Models;

/// <summary>Owns copied diagnostic evidence and local receipt identity for one successful page read.</summary>
internal sealed record MapperRegistryRead(long Identity, DateTimeOffset ReceivedAt, int ReportedCount, ImmutableArray<MapperRegistryRow> Rows)
{
    internal static MapperRegistryRead Capture(long identity, ObjectMapperStatusResponse response) => new(
        identity, DateTimeOffset.Now, response.Count,
        response.Mappings.Select((row, ordinal) => new MapperRegistryRow(
            ordinal, row.SourceType, row.DestinationType, row.MapExpression)).ToImmutableArray());
}

/// <summary>The original read ordinal distinguishes equal-looking rows without inventing backend identity.</summary>
internal sealed record MapperRegistryRow(int Ordinal, string SourceType, string DestinationType, string Expression);
