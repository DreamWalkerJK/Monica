using AwesomeAssertions;
using Monica.AI.Abstractions;
using Monica.AI.Models;
using Monica.AI.Services;
using NSubstitute;
using Test.Monica.AI.Configuration;
using Test.Monica.AI.Support;

namespace Test.Monica.AI.Services;

public sealed class AIProviderRegistryTests
{
    [Fact]
    public void Constructor_WhenProvidersAreValid_ShouldCreateImmutableSnapshotAndResolveDefault()
    {
        var providers = new List<IAIProvider>
        {
            CreateProvider("secondary", isDefault: false),
            CreateProvider("primary", isDefault: true)
        };

        using var workspace = new ConfigurationTestWorkspace();
        using var registry = new AIProviderRegistry(providers, workspace.CreateService(), workspace.Catalog);
        providers.Clear();

        registry.GetAllProviderInfos().Select(static info => info.ProviderId)
            .Should().Equal("secondary", "primary");
        registry.GetDefaultProviderInfo()!.ProviderId.Should().Be("primary");
    }

    [Fact]
    public void Constructor_WhenProviderIdsAreDuplicated_ShouldRejectAmbiguousRegistration()
    {
        var providers = new[]
        {
            CreateProvider("duplicate", isDefault: false),
            CreateProvider("DUPLICATE", isDefault: false)
        };

        using var workspace = new ConfigurationTestWorkspace();
        var action = () => new AIProviderRegistry(providers, workspace.CreateService(), workspace.Catalog);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate AI provider identifier*");
    }

    [Fact]
    public async Task AcquireProvider_WhenSettingsChange_ShouldRetainOldGenerationUntilFinalLeaseEnds()
    {
        using var workspace = new ConfigurationTestWorkspace();
        var configuration = workspace.CreateService();
        await configuration.UpsertAsync(AIConfigurationFacadeTests.Provider("provider"), "key", false, 0, TestContext.Current.CancellationToken);
        using var registry = new AIProviderRegistry([], configuration, workspace.Catalog);
        var first = registry.AcquireProvider("provider")!;
        var retained = registry.AcquireProvider("provider")!;
        var oldProvider = first.Provider;

        await configuration.UpsertAsync(AIConfigurationFacadeTests.Provider("provider") with { DisplayName = "Changed" }, null, false, 1, TestContext.Current.CancellationToken);
        using var latest = registry.AcquireProvider("provider")!;

        latest.Provider.Should().NotBeSameAs(oldProvider);
        latest.ConfigurationRevision.Should().Be(2);
        first.ConfigurationRevision.Should().Be(1);
        first.Dispose();
        oldProvider.GetChatClient("custom-model").Should().NotBeNull();
        retained.Dispose();
        var action = () => oldProvider.GetChatClient("custom-model");
        action.Should().Throw<ObjectDisposedException>();
        latest.Provider.GetChatClient("custom-model").Should().NotBeNull();
    }

    [Fact]
    public async Task AcquireProvider_WhenProviderIsDisabled_ShouldKeepRunningLeaseAndRejectNewClient()
    {
        using var workspace = new ConfigurationTestWorkspace();
        var configuration = workspace.CreateService();
        var provider = AIConfigurationFacadeTests.Provider("provider");
        await configuration.UpsertAsync(provider, "key", false, 0, TestContext.Current.CancellationToken);
        using var registry = new AIProviderRegistry([], configuration, workspace.Catalog);
        using var running = registry.AcquireProvider("provider")!;

        await configuration.UpsertAsync(provider with { Enabled = false }, null, false, 1, TestContext.Current.CancellationToken);
        using var disabled = registry.AcquireProvider("provider")!;

        running.Provider.GetChatClient().Should().NotBeNull();
        disabled.Provider.Info.IsValid.Should().BeFalse();
        var action = () => disabled.Provider.GetChatClient();
        action.Should().Throw<InvalidOperationException>();
        registry.GetDefaultProviderInfo().Should().BeNull();
    }

    [Fact]
    public async Task RedactDiagnostic_WhenKeyRotates_ShouldUseTheCapturedGenerationKey()
    {
        using var workspace = new ConfigurationTestWorkspace();
        var configuration = workspace.CreateService();
        var provider = AIConfigurationFacadeTests.Provider("provider");
        await configuration.UpsertAsync(provider, "old-sensitive-key", false, 0, TestContext.Current.CancellationToken);
        using var registry = new AIProviderRegistry([], configuration, workspace.Catalog);
        using var running = registry.AcquireProvider("provider")!;
        await configuration.UpsertAsync(provider, "new-sensitive-key", false, 1, TestContext.Current.CancellationToken);

        running.RedactDiagnostic("Provider rejected old-sensitive-key").Should().Be("Provider rejected [redacted]");
        using var latest = registry.AcquireProvider("provider")!;
        latest.RedactDiagnostic("Provider rejected new-sensitive-key").Should().Be("Provider rejected [redacted]");
    }

    private static IAIProvider CreateProvider(string providerId, bool isDefault)
    {
        var provider = Substitute.For<IAIProvider>();
        provider.ProviderId.Returns(providerId);
        provider.Info.Returns(new AIProviderInfo
        {
            ProviderId = providerId,
            DisplayName = providerId,
            ProviderType = "Test",
            IsDefault = isDefault,
            IsValid = true
        });
        return provider;
    }
}
