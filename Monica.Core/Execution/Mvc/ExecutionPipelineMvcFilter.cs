using System.Runtime.ExceptionServices;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Monica.Core.Execution;

namespace Monica.Core.Execution.Mvc;

/// <summary>
/// Adapts direct MVC actions into Monica's shared execution pipeline while leaving mediated actions untouched.
/// </summary>
internal sealed class ExecutionPipelineMvcFilter(IExecutionPipeline executionPipeline) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
        {
            await next().ConfigureAwait(false);
            return;
        }

        var controllerType = actionDescriptor.ControllerTypeInfo.AsType();
        if (controllerType.IsDefined(typeof(MediatedControllerAttribute), inherit: false)
            || actionDescriptor.MethodInfo.IsDefined(typeof(MediatedControllerAttribute), inherit: false))
        {
            await next().ConfigureAwait(false);
            return;
        }

        var descriptor = ExecutionDescriptor.ForMethod<MvcActionExecutionInput, MvcActionExecutionResult>(
            MvcExecutionPoints.Action,
            controllerType,
            actionDescriptor.MethodInfo,
            isBusinessOperation: true,
            transactionMode: (actionDescriptor.MethodInfo.GetCustomAttribute<ExecutionTransactionAttribute>(inherit: true)
                ?? controllerType.GetCustomAttribute<ExecutionTransactionAttribute>(inherit: true))?.Mode
                ?? (IsReadOnly(context, actionDescriptor) ? ExecutionTransactionMode.None : ExecutionTransactionMode.Automatic));
        var input = new MvcActionExecutionInput(
            context.HttpContext,
            context.Controller,
            actionDescriptor,
            context.ActionArguments);
        var result = await executionPipeline.ExecuteAsync(
                descriptor,
                input,
                context.Controller,
                async () => CreateResult(await next().ConfigureAwait(false)),
                context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (result.ExecutedContext is null)
        {
            context.Result = result.Result;
        }
    }

    private static MvcActionExecutionResult CreateResult(ActionExecutedContext context)
    {
        if (context.Exception is not null && !context.ExceptionHandled)
        {
            ExceptionDispatchInfo.Capture(context.Exception).Throw();
        }

        return MvcActionExecutionResult.FromExecutedAction(context);
    }

    private static bool IsReadOnly(ActionExecutingContext context, ControllerActionDescriptor action)
        => HttpMethods.IsGet(context.HttpContext.Request.Method)
            || HttpMethods.IsHead(context.HttpContext.Request.Method)
            || HttpMethods.IsOptions(context.HttpContext.Request.Method)
            || action.MethodInfo.IsDefined(typeof(ReadOnlyOperationAttribute), inherit: true)
            || action.ControllerTypeInfo.IsDefined(typeof(ReadOnlyOperationAttribute), inherit: true);
}
