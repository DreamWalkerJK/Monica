using System.Reflection;
using Monica.Core.Mediator;
using Monica.WebApi.Annotations;

namespace Monica.WebApi.AutoControllers.Services.Support;

/// <summary>
/// Treats requests whose request-owned endpoint is bound to HTTP GET as read-only.
/// </summary>
/// <remarks>
/// Generated GET endpoints dispatch through the mediator, whose boundary must not open an automatic write
/// transaction. Attaching the rule to the request type also covers direct <c>IMediator.Send</c> calls from
/// jobs, tests, or other handlers. DELETE uses query binding but performs writes, so only GET qualifies;
/// requests needing a transaction despite a GET binding use <see cref="Monica.Core.Execution.ExecutionTransactionAttribute"/>.
/// </remarks>
public sealed class ApiEndpointReadOnlyConvention : IReadOnlyRequestConvention
{
    /// <inheritdoc />
    public bool IsReadOnly(Type requestType)
        => requestType.GetCustomAttribute<ApiEndpointAttribute>(inherit: false) is { Method: ApiHttpMethod.Get };
}
