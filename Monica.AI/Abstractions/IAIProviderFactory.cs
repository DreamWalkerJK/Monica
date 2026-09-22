using Monica.AI.Models;

namespace Monica.AI.Abstractions;

/// <summary>Resolves current provider metadata and owns the lifetime of captured provider generations.</summary>
public interface IAIProviderFactory
{
    /// <summary>
    /// Captures a provider for one operation. Null or empty identifiers select the current default.
    /// Dispose the returned lease after all streaming, tools, or embedding work has completed.
    /// Provider clients are borrowed: do not dispose them separately or retain them beyond the lease.
    /// Returns null if no matching provider is configured; inspect metadata for a configured but disabled provider.
    /// </summary>
    IAIProviderLease? AcquireProvider(string? providerId = null);

    /// <summary>Gets current credential-free metadata, including disabled providers, or null if absent.</summary>
    AIProviderInfo? GetProviderInfo(string providerId);

    /// <summary>Gets current metadata for all configured providers.</summary>
    IReadOnlyList<AIProviderInfo> GetAllProviderInfos();

    /// <summary>Gets the selected valid default provider's metadata, or null when none is usable.</summary>
    AIProviderInfo? GetDefaultProviderInfo();

    /// <summary>Whether this provider identifier is currently configured.</summary>
    bool HasProvider(string providerId);
}

/// <summary>
/// A captured provider generation that remains valid through a concurrent settings edit.
/// Leases are idempotently disposable; accessing the provider after disposal is invalid.
/// </summary>
public interface IAIProviderLease : IDisposable
{
    /// <summary>The captured provider. Callers borrow its clients for the lifetime of this lease.</summary>
    IAIProvider Provider { get; }

    /// <summary>Host configuration revision captured when the lease was acquired; safe for trajectory records.</summary>
    long ConfigurationRevision { get; }

    /// <summary>Removes credentials captured by this generation from a diagnostic before persistence or display.</summary>
    string RedactDiagnostic(string message);
}
