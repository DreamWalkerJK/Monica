namespace Monica.Core.Execution;

/// <summary>
/// Overrides automatic transaction participation at a mediator or MVC boundary.
/// Use <see cref="ExecutionTransactionMode.None"/> for orchestration that resolves each independently
/// committed operation from a fresh service scope. It does not suppress an already active transaction.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, Inherited = true)]
public sealed class ExecutionTransactionAttribute(ExecutionTransactionMode mode) : Attribute
{
    /// <summary>Gets the transaction policy owned by this boundary.</summary>
    public ExecutionTransactionMode Mode { get; } = mode;
}
