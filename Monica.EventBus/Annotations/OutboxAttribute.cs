namespace Monica.EventBus.Annotations;

/// <summary>
/// Requires publication of this event to be staged in the current local transaction.
/// Publishing completes after staging; delivery follows a successful commit.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OutboxAttribute : Attribute;
