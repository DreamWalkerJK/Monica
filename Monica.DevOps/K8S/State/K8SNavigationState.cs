using Monica.DevOps.K8S.Models;

namespace Monica.DevOps.K8S.State;

/// <summary>Passive circuit-local view preferences; contains no credentials, evidence or live work.</summary>
public sealed class K8SNavigationState
{
    /// <summary>Gets or sets the namespace to revalidate on a subsequent inventory visit.</summary>
    public string Namespace { get; set; } = string.Empty;
    /// <summary>Gets or sets the inventory resource kind.</summary>
    public K8SResourceType ResourceType { get; set; } = K8SResourceType.Service;
    /// <summary>Gets or sets the local inventory search.</summary>
    public string Search { get; set; } = string.Empty;
    /// <summary>Gets or sets whether this circuit has an explicit return context.</summary>
    public bool HasContext { get; set; }
}

internal sealed record K8SViewContext(string Namespace, K8SResourceType ResourceType, string Search);
