using Monica.DevOps.K8S.Models;

namespace Monica.DevOps.K8S.State;

internal readonly record struct K8SResourceIdentity(string Namespace, K8SResourceType ResourceType, string Name)
{
    internal static K8SResourceIdentity From(K8SResourceSummary row) => new(row.Namespace, row.ResourceType, row.Name);

    /// <summary>Keeps native grid selection aligned with resource identity across recreated evidence rows.</summary>
    internal static IEqualityComparer<K8SResourceSummary> Comparer { get; } = new RowComparer();

    private sealed class RowComparer : IEqualityComparer<K8SResourceSummary>
    {
        public bool Equals(K8SResourceSummary? left, K8SResourceSummary? right) =>
            ReferenceEquals(left, right) || left is not null && right is not null && From(left) == From(right);

        public int GetHashCode(K8SResourceSummary row) => From(row).GetHashCode();
    }
}

internal enum K8SOperationScope { Single, Selected, Namespace }
internal enum K8SInventoryOperation { Restart, ScaleDown, ScaleUp }
internal enum K8SInventoryActivity { None, Reading, Previewing, Reviewing, Inspecting, Executing }

/// <summary>Freezes the requested identity, not backend configuration or the eventual execution plan.</summary>
internal sealed record K8SOperationRequest(
    string Namespace, K8SResourceType ResourceType, K8SOperationScope Scope,
    K8SInventoryOperation Operation, IReadOnlyList<string> Names)
{
    internal static K8SOperationRequest Create(string namespaceName, K8SResourceType type,
        K8SOperationScope scope, K8SInventoryOperation operation, IEnumerable<string> names) =>
        new(namespaceName, type, scope, operation, Array.AsReadOnly(names.Distinct(StringComparer.Ordinal).ToArray()));
}

internal sealed record K8SReadFailure(string Namespace, K8SResourceType ResourceType, string Message);

internal sealed record K8SOperationOutcome(K8SOperationRequest Request, DateTimeOffset CompletedAtUtc,
    K8SRestartResult? Restart, K8SScaleResult? Scale, string? Failure)
{
    internal IReadOnlyList<string> Commands => Restart?.ExecutedCommands ?? Scale?.ExecutedCommands ?? [];
    internal IReadOnlyList<string> AffectedTargets => Restart?.AffectedTargets ?? Scale?.AffectedTargets ?? [];
    internal string Namespace => Restart?.Namespace ?? Scale?.Namespace ?? Request.Namespace;
    internal string ScopeName => Restart?.ScopeName ?? Scale?.ScopeName ?? string.Empty;
}
