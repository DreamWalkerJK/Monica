using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using System.Composition;

namespace Monica.ProjectUnits.CodeAnalysis.Services;

/// <summary>
/// Loads analyzer and source-generator assemblies from byte streams so workspace analysis never
/// locks the analyzed repository's build outputs. Roslyn's default loader keeps loaded analyzer
/// files open in a non-collectible context for the process lifetime, which blocks every later
/// rebuild of the projects that produce those assemblies.
/// </summary>
public sealed class LockFreeAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
    private static readonly HostServices HOST_SERVICES = MefHostServices.Create(
        MSBuildMefHostServices.DefaultAssemblies.Add(typeof(LockFreeAnalyzerAssemblyLoader).Assembly));

    private readonly Lock _guard = new();
    private readonly ByteAssemblyLoadContext _context = new();
    private readonly Dictionary<string, Assembly> _assembliesByPath = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void AddDependencyLocation(string fullPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
        _context.AddDependencyDirectory(Path.GetDirectoryName(Path.GetFullPath(fullPath)));
    }

    /// <inheritdoc />
    public Assembly LoadFromPath(string fullPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
        lock (_guard)
        {
            if (_assembliesByPath.TryGetValue(fullPath, out var cached))
            {
                return cached;
            }

            var assembly = _context.LoadFromPathLockFree(Path.GetFullPath(fullPath));
            _assembliesByPath[fullPath] = assembly;
            return assembly;
        }
    }

    /// <summary>
    /// Creates an MSBuild workspace whose analyzer service loads analyzer and generator assemblies
    /// through the lock-free byte-stream loader instead of Roslyn's file-locking default.
    /// </summary>
    public static MSBuildWorkspace CreateWorkspace()
        => MSBuildWorkspace.Create(HOST_SERVICES);

    /// <summary>
    /// One load context keeps a single <see cref="Assembly"/> instance per simple name, mirroring
    /// the identity semantics of Roslyn's default single-context loader while reading every file
    /// into memory instead of retaining a write-blocking handle on it.
    /// </summary>
    private sealed class ByteAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly Lock _guard = new();
        private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Assembly> _assembliesByName = new(StringComparer.OrdinalIgnoreCase);

        public void AddDependencyDirectory(string? directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            lock (_guard)
            {
                _ = _directories.Add(directory);
            }
        }

        public Assembly LoadFromPathLockFree(string fullPath)
        {
            var name = Path.GetFileNameWithoutExtension(fullPath);
            lock (_guard)
            {
                if (_assembliesByName.TryGetValue(name, out var cached))
                {
                    return cached;
                }

                AddDependencyDirectory(Path.GetDirectoryName(fullPath));
                var assembly = LoadFromStream(new MemoryStream(File.ReadAllBytes(fullPath)));
                _assembliesByName[name] = assembly;
                return assembly;
            }
        }

        protected override Assembly? Load(AssemblyName name)
        {
            if (name.Name is null)
            {
                return null;
            }

            lock (_guard)
            {
                if (_assembliesByName.TryGetValue(name.Name, out var cached))
                {
                    return cached;
                }

                foreach (var directory in _directories)
                {
                    var candidate = Path.Combine(directory, $"{name.Name}.dll");
                    if (!File.Exists(candidate))
                    {
                        continue;
                    }

                    var assembly = LoadFromStream(new MemoryStream(File.ReadAllBytes(candidate)));
                    _assembliesByName[name.Name] = assembly;
                    return assembly;
                }

                // Unresolved here falls back to the default context, which supplies the host's own
                // framework assemblies, including Microsoft.CodeAnalysis itself.
                return null;
            }
        }
    }
}

/// <summary>
/// Replaces Roslyn's default analyzer service in composed MSBuild workspace services so every
/// analyzer and source-generator assembly loads through <see cref="LockFreeAnalyzerAssemblyLoader"/>.
/// </summary>
[ExportWorkspaceService(typeof(IAnalyzerService), ServiceLayer.Host)]
[Shared]
public sealed class LockFreeAnalyzerService : IAnalyzerService
{
    private static readonly LockFreeAnalyzerAssemblyLoader SHARED_LOADER = new();

    /// <summary>One shared loader keeps a single <see cref="Assembly"/> instance per simple name, as Roslyn's default does.</summary>
    public IAnalyzerAssemblyLoader GetLoader() => SHARED_LOADER;
}
