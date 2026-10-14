using Monica.DevOps.K8S.State;
using Test.Monica.DevOps.K8S.Support;
using Xunit;

namespace Test.Monica.DevOps.K8S.State;

public sealed class K8SRuntimeConfigVisitControllerTests
{
    [Fact]
    public async Task Save_WhenDraftChanges_ShouldNormalizeStoreWithoutConnecting()
    {
        var fixture = new K8SStateFixture();
        await using var visit = new K8SRuntimeConfigVisitController(fixture.Facade, () => Task.CompletedTask);
        await visit.LoadAsync();
        visit.Draft!.Password = string.Empty;
        visit.NamespaceScope = "alpha; ALPHA, beta";
        visit.NameKeywords = "api; API";
        visit.ShowPassword = true;

        await visit.SaveAsync();

        var config = fixture.Store.GetCurrent();
        Assert.Equal(new[] { "alpha", "beta" }, config.NamespaceScope);
        Assert.Equal("api", Assert.Single(config.SensitiveOperationNameRestrictions));
        Assert.Empty(config.Password);
        Assert.Empty(fixture.Provider.Commands);
        Assert.True(visit.Saved);
        Assert.False(visit.IsDirty);
        Assert.False(visit.ShowPassword);
    }

    [Fact]
    public async Task Load_WhenDiscardReloadFails_ShouldKeepEnteredDraft()
    {
        var fixture = new K8SStateFixture();
        await using var visit = new K8SRuntimeConfigVisitController(fixture.Facade, () => Task.CompletedTask);
        await visit.LoadAsync();
        var draft = visit.Draft!;
        draft.ExecutionNode = "unsaved-node";
        visit.NameKeywords = "unsaved-keyword";
        fixture.Store.FailRead = true;

        await visit.LoadAsync();

        Assert.Same(draft, visit.Draft);
        Assert.Equal("unsaved-node", visit.Draft!.ExecutionNode);
        Assert.Equal("unsaved-keyword", visit.NameKeywords);
        Assert.True(visit.ReloadFailed);
        Assert.True(visit.IsDirty);
        Assert.NotNull(visit.Failure);
    }

    [Fact]
    public async Task Load_WhenDiscardSucceeds_ShouldAdoptCurrentSharedConfigAndRemask()
    {
        var fixture = new K8SStateFixture();
        await using var visit = new K8SRuntimeConfigVisitController(fixture.Facade, () => Task.CompletedTask);
        await visit.LoadAsync();
        visit.Draft!.ExecutionNode = "discarded-node";
        visit.ShowPassword = true;
        var changed = fixture.Store.GetCurrent();
        changed.ExecutionNode = "another-session-node";
        fixture.Store.Update(changed);

        await visit.LoadAsync();

        Assert.Equal("another-session-node", visit.Draft!.ExecutionNode);
        Assert.False(visit.IsDirty);
        Assert.False(visit.ShowPassword);
        Assert.Empty(fixture.Provider.Commands);
    }

    [Fact]
    public async Task Save_WhenValidationFails_ShouldRetainDraftAndStoredConfig()
    {
        var fixture = new K8SStateFixture();
        await using var visit = new K8SRuntimeConfigVisitController(fixture.Facade, () => Task.CompletedTask);
        await visit.LoadAsync();
        var originalNode = fixture.Store.GetCurrent().ExecutionNode;
        visit.Draft!.ExecutionNode = string.Empty;

        await visit.SaveAsync();

        Assert.Empty(visit.Draft!.ExecutionNode);
        Assert.Equal(originalNode, fixture.Store.GetCurrent().ExecutionNode);
        Assert.True(visit.IsDirty);
        Assert.NotNull(visit.Failure);
        Assert.False(visit.Saved);
    }

    [Fact]
    public async Task Dispose_WhenDraftWasRevealed_ShouldClearOwnedCredentialReferences()
    {
        var fixture = new K8SStateFixture();
        var visit = new K8SRuntimeConfigVisitController(fixture.Facade, () => Task.CompletedTask);
        await visit.LoadAsync();
        var draft = visit.Draft!;
        visit.ShowPassword = true;

        await visit.DisposeAsync();

        Assert.Empty(draft.Password);
        Assert.Null(visit.Draft);
        Assert.NotEmpty(fixture.Store.GetCurrent().Password);
    }
}
