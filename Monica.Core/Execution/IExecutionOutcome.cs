namespace Monica.Core.Execution;

/// <summary>Allows an adapter to report a returned failure to transactional behaviors.</summary>
public interface IExecutionOutcome
{
    /// <summary>True when the operation returned a failure and its writes must roll back.</summary>
    bool ShouldRollback { get; }
}
