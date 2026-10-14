using Monica.Configuration.Models;
using Test.Monica.Configuration.UI.Support;
using Panel = global::Monica.Configuration.UI.Components.ConfigurationUnifiedVersionDiffPanel;

namespace Test.Monica.Configuration.UI.Components;

public sealed class ConfigurationUnifiedVersionDiffPreparationTests
{
    private static readonly Panel.DisplayLabels Labels = new("sensitive", "hidden", "hidden before change", "hidden after change");

    [Fact]
    public async Task Preparation_WhenOlderSchemaReadCompletesLast_ShouldKeepNewPairAndValues()
    {
        var pending = NewCompletion<ConfigurationDefinition?>();
        var calls = 0;
        await using var owner = new Panel.PreparationSession(_ => ++calls == 1
            ? pending.Task : Task.FromResult<ConfigurationDefinition?>(UnifiedVersionPrivacyTestData.Definition()));
        var older = owner.PrepareAsync(UnifiedVersionPrivacyTestData.Comparison(), Labels);
        await ObserveAsync(owner.PrepareAsync(UnifiedVersionPrivacyTestData.Comparison(target: "{\"Value\":\"new marker\"}", targetVersion: 3), Labels));
        pending.SetResult(UnifiedVersionPrivacyTestData.Definition());
        await ObserveAsync(older);
        var prepared = Assert.IsType<Panel.PreparedComparison>(owner.Prepared);
        Assert.Equal(3, prepared.TargetVersion);
        Assert.Contains("new marker", Assert.Single(prepared.Rows).TargetText);
        Assert.False(owner.Loading);
    }

    [Fact]
    public async Task Preparation_ShouldCopySnapshotCollectionsBeforeAwaitingSchema()
    {
        var pending = NewCompletion<ConfigurationDefinition?>();
        await using var owner = new Panel.PreparationSession(_ => pending.Task);
        var comparison = UnifiedVersionPrivacyTestData.Comparison();
        var changes = comparison.Changes.ToList();
        var originDocuments = comparison.Origin.Definitions.ToList();
        var targetDocuments = comparison.Target.Definitions.ToList();
        comparison = comparison with { Changes = changes, Origin = comparison.Origin with { Definitions = originDocuments },
            Target = comparison.Target with { Definitions = targetDocuments } };
        var preparing = owner.PrepareAsync(comparison, Labels);
        changes.Clear();
        originDocuments.Clear();
        targetDocuments.Clear();
        pending.SetResult(UnifiedVersionPrivacyTestData.Definition());
        await ObserveAsync(preparing);
        var row = Assert.Single(Assert.IsType<Panel.PreparedComparison>(owner.Prepared).Rows);
        Assert.False(row.HasSchemaDrift);
        Assert.Contains("before-marker", row.OriginText);
        Assert.Contains("after-marker", row.TargetText);
    }

    [Fact]
    public async Task Preparation_WhenSamePairAndCountHaveNewValues_ShouldReloadAuthorityAndPrepareNewValues()
    {
        var calls = 0;
        await using var owner = new Panel.PreparationSession(_ => Task.FromResult<ConfigurationDefinition?>(++calls == 1
            ? UnifiedVersionPrivacyTestData.Definition()
            : UnifiedVersionPrivacyTestData.Definition() with { Root = UnifiedVersionPrivacyTestData.Node("Root") with { IsSensitive = true } }));
        await ObserveAsync(owner.PrepareAsync(UnifiedVersionPrivacyTestData.Comparison(), Labels));
        Assert.Contains("after-marker", Assert.Single(Assert.IsType<Panel.PreparedComparison>(owner.Prepared).Rows).TargetText);
        await ObserveAsync(owner.PrepareAsync(UnifiedVersionPrivacyTestData.Comparison(target: "{\"Value\":\"changed marker\"}"), Labels));
        var row = Assert.Single(Assert.IsType<Panel.PreparedComparison>(owner.Prepared).Rows);
        Assert.Equal(2, calls);
        Assert.DoesNotContain("changed marker", row.TargetText);
        Assert.True(row.HasHiddenChanges);
    }

    [Fact]
    public async Task Preparation_WhenSchemaUnavailable_ShouldKeepRedactedChangesInsteadOfEmptySuccess()
    {
        await using var owner = new Panel.PreparationSession(_ => Task.FromResult<ConfigurationDefinition?>(null));
        await ObserveAsync(owner.PrepareAsync(UnifiedVersionPrivacyTestData.Comparison(), Labels));
        var row = Assert.Single(Assert.IsType<Panel.PreparedComparison>(owner.Prepared).Rows);
        Assert.True(row.HasSchemaDrift);
        Assert.DoesNotContain("before-marker", row.OriginText);
        Assert.DoesNotContain("after-marker", row.TargetText);
    }

    [Fact]
    public async Task Preparation_WhenComparisonReallyHasNoChanges_ShouldPublishSuccessfulEmpty()
    {
        var invoked = false;
        await using var owner = new Panel.PreparationSession(_ => { invoked = true; return Task.FromResult<ConfigurationDefinition?>(null); });
        await ObserveAsync(owner.PrepareAsync(UnifiedVersionPrivacyTestData.Comparison() with { Changes = [] }, Labels));
        Assert.Empty(Assert.IsType<Panel.PreparedComparison>(owner.Prepared).Rows);
        Assert.False(owner.Loading);
        Assert.False(invoked);
    }

    [Fact]
    public async Task Retirement_ShouldJoinLateActualSchemaReadWithoutPublishingItsValues()
    {
        var pending = NewCompletion<ConfigurationDefinition?>();
        var owner = new Panel.PreparationSession(_ => pending.Task);
        var preparing = owner.PrepareAsync(UnifiedVersionPrivacyTestData.Comparison(), Labels);
        var retirement = owner.DisposeAsync().AsTask();
        Assert.Same(retirement, owner.DisposeAsync().AsTask());
        Assert.False(retirement.IsCompleted);
        pending.SetResult(UnifiedVersionPrivacyTestData.Definition());
        await ObserveAsync(preparing);
        await ObserveAsync(retirement);
        Assert.Null(owner.Prepared);
    }

    private static TaskCompletionSource<T> NewCompletion<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task ObserveAsync(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
}
