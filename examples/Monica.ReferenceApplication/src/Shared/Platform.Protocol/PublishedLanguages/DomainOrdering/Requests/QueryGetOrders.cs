using Monica.WebApi.Abstractions;
using Monica.WebApi.Annotations;
using Platform.Protocol.PublishedLanguages.DomainOrdering.Models;

namespace Platform.Protocol.PublishedLanguages.DomainOrdering.Requests;

/// <summary>
/// Requests all orders in reverse creation order. The GET binding classifies this request as read-only,
/// so mediated dispatch opens no automatic write transaction.
/// </summary>
[ApiEndpoint(
    ApiHttpMethod.Get,
    "orders",
    Binding = ApiRequestBinding.Query)]
public sealed record QueryGetOrders : IResultRequest<IReadOnlyList<OrderDto>>;
