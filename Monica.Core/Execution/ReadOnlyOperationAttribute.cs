namespace Monica.Core.Execution;

/// <summary>
/// Declares an operation that does not require an automatic write transaction.
/// Apply to a mediator request/handler type or an MVC action. Nested reads still use their enclosing operation's scope.
/// </summary>
/// <remarks>
/// This attribute is the explicit declaration. Mediated requests are additionally classified by registered
/// <see cref="Monica.Core.Mediator.IReadOnlyRequestConvention"/> implementations, such as the GET endpoint-binding
/// convention supplied by the Web API module, so transport metadata can imply read-only without this attribute.
/// An explicit <see cref="ExecutionTransactionAttribute"/> still decides the transaction mode first.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, Inherited = true)]
public sealed class ReadOnlyOperationAttribute : Attribute;
