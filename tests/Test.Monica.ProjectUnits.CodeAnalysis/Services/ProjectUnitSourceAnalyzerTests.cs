using System.Diagnostics;
using AwesomeAssertions;
using Monica.ProjectUnits.CodeAnalysis.Models;
using Monica.ProjectUnits.CodeAnalysis.Services;
using Xunit;

namespace Test.Monica.ProjectUnits.CodeAnalysis.Services;

public sealed class ProjectUnitSourceAnalyzerTests
{
    [Fact]
    public void Constructor_ShouldBePublicForWorkflowComposition()
    {
        typeof(ProjectUnitSourceAnalyzer).IsPublic.Should().BeTrue();
        new ProjectUnitSourceAnalyzer().Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_WhenASelectedProjectIsMissing_ShouldReturnStablePartialCatalog()
    {
        using var fixture = new TemporaryProjectFixture();
        await fixture.RestoreAsync(TestContext.Current.CancellationToken);
        var progress = new ProgressRecorder();
        var analyzer = new ProjectUnitSourceAnalyzer();

        var result = await analyzer.AnalyzeAsync(
            new ProjectUnitSourceAnalysisRequest(
                fixture.Root,
                [fixture.ProjectPath, Path.Combine(fixture.Root, "Missing.csproj")]),
            progress,
            TestContext.Current.CancellationToken);

        result.ContractVersion.Should().Be(ProjectUnitSourceAnalysisContract.Version);
        result.RequestedProjectCount.Should().Be(2);
        result.AnalyzedProjectCount.Should().Be(1);
        result.IsPartial.Should().BeTrue();
        result.Diagnostics.Should().NotContain(diagnostic =>
            diagnostic.Code == "ProjectUnit.Analysis.MSBuild.Failure");
        result.Units.Should().ContainSingle();
        result.Units[0].Should().Match<ProjectUnitSourceUnit>(unit =>
            unit.CatalogKey == "Sample.csproj::Sample.ManagedUnit"
            && unit.RuntimeKey == "Sample.ManagedUnit"
            && unit.UnitType == ProjectUnitSourceType.DomainService
            && unit.Title == "Managed unit"
            && unit.Description == "Coordinates the sample workflow."
            && unit.Owner == "Sample Team"
            && unit.ExecutionPoints.Count == 0
            && unit.Source.RelativePath == "ManagedUnit.cs"
            && unit.Source.Line > 0);
        result.TestClasses.Should().BeEmpty();
        result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Code == "ProjectUnit.Analysis.Project.Missing"
            && diagnostic.Severity == ProjectUnitSourceDiagnosticSeverity.Error);
        progress.Items.Should().Contain(item => item.Stage == ProjectUnitSourceAnalysisStage.LoadingProjects);
        progress.Items.Should().Contain(item => item.Stage == ProjectUnitSourceAnalysisStage.AnalyzingProjects);
        progress.Items.Last().Should().Match<ProjectUnitSourceAnalysisProgress>(item =>
            item.Stage == ProjectUnitSourceAnalysisStage.Completed && item.Percentage == 100m);
    }

    [Fact]
    public async Task AnalyzeAsync_WithTestProjects_ShouldCollectTestClassesWithoutUnitsFromThem()
    {
        using var fixture = new TemporaryProjectFixture();
        await fixture.RestoreAsync(TestContext.Current.CancellationToken);
        var analyzer = new ProjectUnitSourceAnalyzer();

        var result = await analyzer.AnalyzeAsync(
            new ProjectUnitSourceAnalysisRequest(
                fixture.Root,
                [fixture.ProjectPath],
                [fixture.TestProjectPath]),
            null,
            TestContext.Current.CancellationToken);

        result.ContractVersion.Should().Be(ProjectUnitSourceAnalysisContract.Version);
        result.RequestedProjectCount.Should().Be(2);
        result.AnalyzedProjectCount.Should().Be(2);
        result.IsPartial.Should().BeFalse();
        result.Units.Should().ContainSingle()
            .Which.RuntimeKey.Should().Be("Sample.ManagedUnit");
        result.TestClasses.Should().ContainSingle().Which.Should().Match<ProjectUnitSourceTestClass>(testClass =>
            testClass.RuntimeKey == "Sample.ManagedUnitTests"
            && testClass.ProjectPath == "Sample.Tests.csproj"
            && testClass.ProjectName == "Sample.Tests"
            && testClass.Namespace == "Sample"
            && testClass.Name == "ManagedUnitTests"
            && testClass.TestMethodCount == 2
            && testClass.Source.RelativePath == "ManagedUnitTests.cs"
            && testClass.Traits.Count == 2);
        result.TestClasses[0].Traits.Should().BeEquivalentTo(
        [
            new ProjectUnitSourceTestTrait("REQ", "REQ-SAMPLE-1"),
            new ProjectUnitSourceTestTrait("Unit", "Sample.ManagedUnit")
        ]);
        result.Diagnostics.Should().NotContain(diagnostic =>
            diagnostic.Severity == ProjectUnitSourceDiagnosticSeverity.Error);
    }

    private sealed class ProgressRecorder : IProgress<ProjectUnitSourceAnalysisProgress>
    {
        public List<ProjectUnitSourceAnalysisProgress> Items { get; } = [];

        public void Report(ProjectUnitSourceAnalysisProgress value) => Items.Add(value);
    }

    private sealed class TemporaryProjectFixture : IDisposable
    {
        public TemporaryProjectFixture()
        {
            Root = Directory.CreateTempSubdirectory("monica-project-unit-analysis-").FullName;
            ProjectPath = Path.Combine(Root, "Sample.csproj");
            File.WriteAllText(ProjectPath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(Root, "ManagedUnit.cs"), """
                namespace Monica.WebApi.Abstractions
                {
                    public abstract class DomainService { }
                }

                namespace Monica.ProjectUnits.Annotations
                {
                    [System.AttributeUsage(System.AttributeTargets.Class)]
                    public sealed class ProjectUnitMetadataAttribute(string title) : System.Attribute
                    {
                        public string Title { get; } = title;
                        public string? Owner { get; set; }
                        public string? Description { get; set; }
                        public string[] Tags { get; set; } = [];
                    }
                }

                namespace Sample
                {
                    using Monica.ProjectUnits.Annotations;
                    using Monica.WebApi.Abstractions;

                    /// <summary>Fallback documentation.</summary>
                    [ProjectUnitMetadata("Managed unit", Owner = "Sample Team",
                        Description = "Coordinates the sample workflow.")]
                    public sealed class ManagedUnit : DomainService { }
                }
                """);
            TestProjectPath = Path.Combine(Root, "Sample.Tests.csproj");
            File.WriteAllText(TestProjectPath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <IsTestProject>true</IsTestProject>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="Sample.csproj" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(Root, "ManagedUnitTests.cs"), """
                namespace Xunit
                {
                    [System.AttributeUsage(
                        System.AttributeTargets.Class | System.AttributeTargets.Method,
                        AllowMultiple = true)]
                    public sealed class TraitAttribute(string key, string value) : System.Attribute { }

                    public sealed class FactAttribute : System.Attribute { }
                }

                namespace Sample
                {
                    using Xunit;

                    [Trait("REQ", "REQ-SAMPLE-1")]
                    [Trait("Unit", "Sample.ManagedUnit")]
                    public sealed class ManagedUnitTests
                    {
                        [Fact]
                        public void Creates_the_unit() { }

                        [Fact]
                        public void Coordinates_the_workflow() { }
                    }
                }
                """);
        }

        public string Root { get; }

        public string ProjectPath { get; }

        public string TestProjectPath { get; }

        public async Task RestoreAsync(CancellationToken cancellationToken)
        {
            // MSBuildWorkspace needs restored framework references to bind attribute arguments.
            // This package-free fixture restores against an empty local source to stay offline.
            // Restoring the test project restores its production reference transitively.
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var argument in new[] { "restore", TestProjectPath, "--source", Root, "--nologo", "-p:NuGetAudit=false" })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)!;
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }

            Assert.True(process.ExitCode == 0, $"Fixture restore failed: {await output}\n{await error}");
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // MSBuild may briefly retain a file handle after workspace disposal on Windows.
            }
        }
    }
}
