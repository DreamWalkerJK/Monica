using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Monica.ProjectUnits.CodeAnalysis.Services;
using Xunit;

namespace Test.Monica.ProjectUnits.CodeAnalysis.Services;

public sealed class LockFreeAnalyzerAssemblyLoaderTests
{
    [Fact]
    public void LoadFromPath_WhileTheAssemblyStaysLoaded_ShouldNotLockTheFile()
    {
        using var directory = TemporaryDirectory.Create();
        var assemblyPath = directory.CopyFromTestOutput("Monica.ProjectUnits.CodeAnalysis.dll");
        var loader = new LockFreeAnalyzerAssemblyLoader();

        var loaded = loader.LoadFromPath(assemblyPath);

        loaded.GetName().Name.Should().Be("Monica.ProjectUnits.CodeAnalysis");
        using (var overwrite = new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            overwrite.Length.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void LoadFromPath_WhenCalledTwice_ShouldReturnTheSameAssemblyInstance()
    {
        using var directory = TemporaryDirectory.Create();
        var assemblyPath = directory.CopyFromTestOutput("Monica.ProjectUnits.CodeAnalysis.dll");
        var loader = new LockFreeAnalyzerAssemblyLoader();

        var first = loader.LoadFromPath(assemblyPath);

        first.Should().BeSameAs(loader.LoadFromPath(assemblyPath));
    }

    [Fact]
    public void Dependencies_ShouldResolveFromRegisteredLocationsWithoutLockingAnyFile()
    {
        using var directory = TemporaryDirectory.Create();
        var (primaryPath, dependencyPath) = CompileReferencingPair(directory.RootDirectory);
        var loader = new LockFreeAnalyzerAssemblyLoader();
        loader.AddDependencyLocation(directory.RootDirectory);

        var loaded = loader.LoadFromPath(primaryPath);
        var holder = loaded.GetType("Monica.LoaderTest.Primary.Holder");

        holder.Should().NotBeNull();
        holder!.BaseType.Should().NotBeNull();
        holder.BaseType!.Assembly.GetName().Name.Should().Be("Monica.LoaderTest.Dependency");
        // Stream-loaded assemblies carry no file location; resolution alone proves the probe,
        // because the compiled pair exists nowhere outside the registered directory.
        holder.BaseType.Assembly.Location.Should().BeEmpty();
        using (var overwrite = new FileStream(primaryPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            overwrite.Length.Should().BeGreaterThan(0);
        }

        using (var overwrite = new FileStream(dependencyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            overwrite.Length.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void CreateWorkspace_ShouldComposeTheLockFreeAnalyzerService()
    {
        using var workspace = LockFreeAnalyzerAssemblyLoader.CreateWorkspace();

        workspace.Services.GetService<IAnalyzerService>().Should().BeOfType<LockFreeAnalyzerService>();
    }

    /// <summary>
    /// Emits a primary assembly whose type inherits from a dependency assembly, neither of which
    /// the test host has loaded, so resolution can only succeed through the loader's registered
    /// dependency locations.
    /// </summary>
    private static (string PrimaryPath, string DependencyPath) CompileReferencingPair(string directory)
    {
        var coreLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var dependencyPath = Path.Combine(directory, "Monica.LoaderTest.Dependency.dll");
        CSharpCompilation.Create(
                "Monica.LoaderTest.Dependency",
                [SyntaxFactory.ParseSyntaxTree("namespace Monica.LoaderTest.Dependency { public class Marker { } }")],
                [coreLib],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .Emit(dependencyPath)
            .Success.Should().BeTrue();
        var primaryPath = Path.Combine(directory, "Monica.LoaderTest.Primary.dll");
        CSharpCompilation.Create(
                "Monica.LoaderTest.Primary",
                [SyntaxFactory.ParseSyntaxTree(
                    "namespace Monica.LoaderTest.Primary { public class Holder : Monica.LoaderTest.Dependency.Marker { } }")],
                [coreLib, MetadataReference.CreateFromFile(dependencyPath)],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .Emit(primaryPath)
            .Success.Should().BeTrue();
        return (primaryPath, dependencyPath);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string rootDirectory) => RootDirectory = rootDirectory;

        internal string RootDirectory { get; }

        internal static TemporaryDirectory Create()
        {
            var rootDirectory = Path.Combine(
                Path.GetTempPath(),
                $"monica-lock-free-loader-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(rootDirectory);
            return new TemporaryDirectory(rootDirectory);
        }

        internal string CopyFromTestOutput(string fileName)
        {
            var source = Path.Combine(
                Path.GetDirectoryName(typeof(LockFreeAnalyzerAssemblyLoaderTests).Assembly.Location)!,
                fileName);
            var destination = Path.Combine(RootDirectory, fileName);
            File.Copy(source, destination);
            return destination;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Antivirus scanners can briefly retain handles on Windows test cleanup.
            }
        }
    }
}
