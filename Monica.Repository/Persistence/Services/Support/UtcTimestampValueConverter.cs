using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Monica.Repository.Persistence.Services.Support;

/// <summary>
/// Stores UTC clock values in timestamp columns without changing ticks, and restores their UTC kind on reads.
/// Only properties whose contract already requires UTC should use this converter.
/// </summary>
internal sealed class UtcTimestampValueConverter() : ValueConverter<DateTime, DateTime>(
    value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
