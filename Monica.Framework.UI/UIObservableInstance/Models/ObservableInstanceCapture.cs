using Microsoft.Extensions.Logging;
using Monica.Core.ObservableInstance.Models;

namespace Monica.Framework.UI.UIObservableInstance.Models;

/// <summary>A detached registry read shared by the four observable workspace views.</summary>
/// <param name="ReadAt">UTC time when the completed registry capture was adopted.</param>
/// <param name="Instances">Independently coherent tracker captures from one membership read.</param>
public sealed record ObservableInstanceCapture(DateTime ReadAt, IReadOnlyList<ObservableInstanceSnapshot> Instances)
{
    /// <summary>Gets the number of retained exception records, independent of current health.</summary>
    public int RetainedExceptions => Instances.Sum(i => i.History.Count(e => e.Exception is not null));
    /// <summary>Gets the total number of retained records.</summary>
    public int RetainedRecords => Instances.Sum(i => i.History.Count);
    /// <summary>Gets lifetime recorded entries, including message-only records.</summary>
    public long LifetimeRecords => Instances.Sum(i => (long)i.TotalRecords);
    /// <summary>Gets lifetime exception occurrences.</summary>
    public long LifetimeExceptions => Instances.Sum(i => (long)i.TotalExceptions);
    /// <summary>Gets current registrations mapped to Warning, Error, or Critical.</summary>
    public int UnhealthyCount => Instances.Count(i => Health(i) == HealthState.Unhealthy);
    /// <summary>Gets current registrations mapped to an informational level.</summary>
    public int HealthyCount => Instances.Count(i => Health(i) == HealthState.Healthy);
    /// <summary>Gets registrations with no level or a None level.</summary>
    public int UnknownCount => Instances.Count(i => Health(i) == HealthState.Unknown);
    /// <summary>Classifies current mapped severity, without inferring health from exception history.</summary>
    public static HealthState Health(ObservableInstanceSnapshot instance) => instance.CurrentLogLevel switch
    {
        LogLevel.Warning or LogLevel.Error or LogLevel.Critical => HealthState.Unhealthy,
        null or LogLevel.None => HealthState.Unknown,
        _ => HealthState.Healthy
    };
}
