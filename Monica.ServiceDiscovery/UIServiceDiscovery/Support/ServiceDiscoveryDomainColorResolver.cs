using Monica.ServiceDiscovery.Models;

namespace Monica.ServiceDiscovery.UIServiceDiscovery.Support;

public sealed class ServiceDiscoveryDomainColorResolver
{
    private static readonly string[] DomainRoles =
    [
        "primary",
        "secondary",
        "tertiary",
        "info",
        "success",
        "warning"
    ];

    private const string DefaultColorRole = "default";
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, string> _domainColors = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> AssignColors(IEnumerable<DomainInfo> domains)
    {
        var domainNames = domains
            .Select(domain => domain.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        lock (_syncRoot)
        {
            foreach (var name in domainNames)
            {
                if (!_domainColors.ContainsKey(name))
                {
                    _domainColors[name] = DomainRoles[_domainColors.Count % DomainRoles.Length];
                }
            }

            return new Dictionary<string, string>(_domainColors, StringComparer.OrdinalIgnoreCase);
        }
    }

    public string GetDomainColorRole(string? domainName)
    {
        if (string.IsNullOrWhiteSpace(domainName))
        {
            return DefaultColorRole;
        }

        lock (_syncRoot)
        {
            return _domainColors.GetValueOrDefault(domainName, DefaultColorRole);
        }
    }
}
