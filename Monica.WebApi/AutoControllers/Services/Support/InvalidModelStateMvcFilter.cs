using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Monica.Core.Results;

namespace Monica.WebApi.AutoControllers.Services.Support;

/// <summary>
/// Rejects requests whose body binding failed with a friendly 400 response instead of invoking the action
/// with unbound (null) arguments.
/// </summary>
/// <remarks>
/// CRUD application services are not marked with [ApiController], so MVC records a model-state error and
/// still invokes the action when a JSON body cannot be deserialized (for example an unparsable date), which
/// used to surface as a NullReferenceException. MVC does not add parameters that failed to bind to
/// <see cref="ActionExecutingContext.ActionArguments" />, so failure is detected by comparing the bound
/// body parameters against the bound arguments. Partial field-level binding failures (an invalid query or
/// form value falling back to its default) keep the previous tolerant behavior.
/// </remarks>
public class InvalidModelStateMvcFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid || !UnboundBodyParameters(context, out var unboundBodyParameterNames))
        {
            return;
        }

        // Errors keyed by an unbound body parameter name (for example "The input field is required.")
        // describe the same failure as the JSON-path errors, so only the field-level errors are reported.
        var errors = string.Join("; ", context.ModelState
            .Where(pair => pair.Value is { Errors.Count: > 0 }
                && !unboundBodyParameterNames.Contains(pair.Key))
            .Select(pair => DescribeErrors(pair.Key, pair.Value!)));
        if (errors.Length == 0)
        {
            errors = "request body could not be parsed";
        }

        context.Result = new ObjectResult(Res.Fail($"Invalid request arguments: {errors}"))
        {
            StatusCode = (int) HttpStatusCode.BadRequest
        };
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static bool UnboundBodyParameters(ActionExecutingContext context, out HashSet<string> parameterNames)
    {
        parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (parameter.BindingInfo?.BindingSource != BindingSource.Body
                || (context.ActionArguments.TryGetValue(parameter.Name, out var value) && value is not null))
            {
                continue;
            }

            parameterNames.Add(parameter.Name);
        }

        return parameterNames.Count > 0;
    }

    private static string DescribeErrors(string key, ModelStateEntry entry)
    {
        var errors = string.Join(", ", entry.Errors
            .Select(static error => string.IsNullOrEmpty(error.ErrorMessage)
                ? error.Exception?.Message
                : error.ErrorMessage)
            .Where(static message => !string.IsNullOrEmpty(message))
            .Select(static message => message!.Split(" Path: ")[0].TrimEnd()));
        return string.IsNullOrEmpty(key) ? errors : $"{key}: {errors}";
    }
}
