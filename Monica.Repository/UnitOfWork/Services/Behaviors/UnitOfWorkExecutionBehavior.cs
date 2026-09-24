using Monica.Core.Execution;
using Monica.Repository.UnitOfWork.Abstractions;
using Monica.Repository.UnitOfWork.Models;
using Monica.Repository.UnitOfWork.Annotations;
using System.Reflection;

namespace Monica.Repository.UnitOfWork.Services.Behaviors;

/// <summary>
/// Executes one business operation inside the current scope's Monica transaction.
/// </summary>
/// <remarks>
/// Nested execution adapters join the current unit of work. The outermost behavior owns the commit; failure handling
/// is delegated to <see cref="IUnitOfWorkManager.RunAsync{T}(Func{Task{T}},Monica.Repository.UnitOfWork.Models.UnitOfWorkScopeOptions?,CancellationToken)"/>.
/// </remarks>
public sealed class UnitOfWorkExecutionBehavior<TInput, TResult>(IUnitOfWorkManager unitOfWorkManager)
    : IExecutionBehavior<TInput, TResult>
{
    /// <inheritdoc />
    public Task<TResult> ExecuteAsync(
        ExecutionContext<TInput> context,
        ExecutionDelegate<TResult> next)
    {
        context.Features.TryGet<UnitOfWorkScopeOptions>(out var options);
        if (options is null)
        {
            var selected = context.Descriptor.EntryMethod?.GetCustomAttribute<UnitOfWorkContextAttribute>(inherit: true)
                ?? context.Descriptor.ComponentType.GetCustomAttribute<UnitOfWorkContextAttribute>(inherit: true);
            if (selected is not null) options = new UnitOfWorkScopeOptions(selected.DbContextTypes);
        }
        return unitOfWorkManager.RunAsync(next.Invoke, options, context.CancellationToken);
    }
}
