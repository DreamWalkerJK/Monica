namespace Monica.Framework.UI.UIEventBus.Models;

/// <summary>Identifies an independently addressable EventBus diagnostic workflow.</summary>
public enum EventBusWorkbenchView
{
    /// <summary>Host-wide registration and reported-topic evidence.</summary>
    Overview,
    /// <summary>Actual registered providers and their capabilities.</summary>
    Providers,
    /// <summary>Registered handlers, filters, and lifecycle commands.</summary>
    Subscriptions,
    /// <summary>Bounded registry notifications observed by the current circuit.</summary>
    Changes,
    /// <summary>Publishing and receiving diagnostic messages.</summary>
    TestTools
}
