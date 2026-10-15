using Monica.Profiling.ExecutionTiming.Models;

namespace Monica.Profiling.UIExecutionTiming.Components;

/// <summary>Completed evidence and its successful page capture for derived values.</summary>
public sealed record TimingCompletedEvidence(ExecutionTimingStatistics Statistics, DateTimeOffset CapturedAt);

/// <summary>Concrete invocation evidence and its successful page capture for derived age.</summary>
public sealed record TimingRunningEvidence(RunningExecutionTimingInfo Operation, DateTimeOffset CapturedAt);
