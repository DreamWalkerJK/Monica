using Monica.Core.Execution;

namespace Monica.Core.Mediator;

/// <summary>
/// Classifies mediator request types as read-only so dispatch skips the automatic write transaction.
/// </summary>
/// <remarks>
/// Register stateless singleton implementations. A convention only supplies the default: an explicit
/// <see cref="ReadOnlyOperationAttribute"/> or <see cref="ExecutionTransactionAttribute"/> on the request,
/// its handler, or the handle method still decides the transaction mode first. Transport modules use
/// conventions to derive read-only intent from metadata the request already carries, such as its
/// endpoint binding, so both generated endpoints and direct <see cref="IMediator.Send{TResponse}"/> calls
/// observe the same classification.
/// </remarks>
public interface IReadOnlyRequestConvention
{
    /// <summary>
    /// Returns <c>true</c> when requests of <paramref name="requestType"/> perform no relational writes
    /// and must not open an automatic unit of work.
    /// </summary>
    /// <param name="requestType">The closed request type being dispatched.</param>
    bool IsReadOnly(Type requestType);
}
