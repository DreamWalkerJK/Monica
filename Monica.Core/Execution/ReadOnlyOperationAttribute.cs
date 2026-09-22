namespace Monica.Core.Execution;

/// <summary>
/// Declares an operation that does not require an automatic write transaction.
/// Apply to a mediator request/handler type or an MVC action. Nested reads still use their enclosing operation's scope.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, Inherited = true)]
public sealed class ReadOnlyOperationAttribute : Attribute;
