using Monica.EventBus.Models;

namespace Monica.Framework.UI.UIEventBus.Models;

/// <summary>
/// Subscription filters
/// </summary>
public class SubscriptionFilter
{
    /// <summary>
    /// Filter by status
    /// </summary>
    public EventSubscriptionState? State { get; set; }

    /// <summary>
    /// Filter by the runtime state of the underlying topic subscription
    /// (recovering, failed, and so on). Only distributed subscriptions with a reported
    /// runtime state can match.
    /// </summary>
    public TopicSubscriptionRuntimeState? TopicRuntimeState { get; set; }

    /// <summary>
    /// Filter by range
    /// </summary>
    public EventSubscriptionScope? Scope { get; set; }

    /// <summary>
    /// Filters by an exact ordinal service key. Null leaves this filter unset; empty and whitespace keys remain exact values.
    /// </summary>
    public string? ServiceKey { get; set; }

    /// <summary>
    /// Filter by whether to automatically discover
    /// </summary>
    public bool? IsAutoDiscovered { get; set; }

    /// <summary>
    /// Case-insensitive search over event/handler names, topic, key, handler factory, action metadata, and subscription ID.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Selected Provider (for filtering by Provider)
    /// </summary>
    public EventBusProviderInfo? SelectedProvider { get; set; }

    /// <summary>
    /// Gets whether any filter is set, including an exact empty or whitespace service key.
    /// </summary>
    public bool HasAnyFilter =>
        State.HasValue ||
        TopicRuntimeState.HasValue ||
        Scope.HasValue ||
        ServiceKey != null ||
        IsAutoDiscovered.HasValue ||
        !string.IsNullOrWhiteSpace(SearchText) ||
        SelectedProvider != null;

    /// <summary>
    /// Clears all filters, restoring the standalone service key to null (unset).
    /// </summary>
    public void Clear()
    {
        State = null;
        TopicRuntimeState = null;
        Scope = null;
        ServiceKey = null;
        IsAutoDiscovered = null;
        SearchText = null;
        SelectedProvider = null;
    }

    /// <summary>
    /// Copies filter values without normalizing or localizing the exact service key.
    /// </summary>
    /// <returns>An independent filter whose selected-provider snapshot is shared and treated as read-only.</returns>
    public SubscriptionFilter Clone()
    {
        return new SubscriptionFilter
        {
            State = State,
            TopicRuntimeState = TopicRuntimeState,
            Scope = Scope,
            ServiceKey = ServiceKey,
            IsAutoDiscovered = IsAutoDiscovered,
            SearchText = SearchText,
            SelectedProvider = SelectedProvider
        };
    }
}
