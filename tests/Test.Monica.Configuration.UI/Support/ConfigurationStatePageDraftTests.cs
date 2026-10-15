using Monica.Configuration.Models;
using Monica.Configuration.UI.Pages;
using Monica.Configuration.UI.State;
using Monica.Configuration.UI.Support;

namespace Test.Monica.Configuration.UI.Support;

public sealed class ConfigurationStatePageDraftTests
{
    [Fact]
    public void AppliedOutcome_RemovesOnlyUnchangedCapturedDraftAndIsIdempotent()
    {
        var store = new ConfigurationStateStore();
        var captured = Change("value", "1");
        var untouched = Change("other", "3");
        store.Stage(captured);
        store.Stage(untouched);
        var result = Applied(captured);

        ConfigurationStatePage.ReconcileAppliedChanges(store, [captured], result);
        ConfigurationStatePage.ReconcileAppliedChanges(store, [captured], result);

        Assert.Null(store.Get(captured.DefinitionKey, captured.LogicalPath));
        Assert.Same(untouched, Assert.Single(store.GetPending()));
    }

    [Fact]
    public void AppliedOutcome_PreservesNewerDraftAtTheSameRequestIdentity()
    {
        var store = new ConfigurationStateStore();
        var captured = Change("value", "1");
        var newer = captured with { NewValue = ConfigurationStoredValue.FromJson("2") };
        store.Stage(newer);
        ConfigurationStatePage.ReconcileAppliedChanges(store, [captured], Applied(captured));
        Assert.Same(newer, Assert.Single(store.GetPending()));
    }

    [Fact]
    public void RejectedOutcome_DoesNotRemoveCapturedDraft()
    {
        var store = new ConfigurationStateStore();
        var captured = Change("value", "1");
        store.Stage(captured);
        ConfigurationStatePage.ReconcileAppliedChanges(store, [captured], new()
        {
            Status = ConfigurationMutationGroupApplyStatus.Rejected,
            Outcomes = []
        });
        Assert.Same(captured, Assert.Single(store.GetPending()));
    }

    [Fact]
    public void BaselineRefresh_MergesOnlyUnchangedDraftsAndNeverRestoresAnUndoneDraft()
    {
        var unchanged = Change("unchanged", "1");
        var modified = Change("modified", "2");
        var undone = Change("undone", "3");
        var newDraft = Change("added", "4");
        var newer = modified with { NewValue = ConfigurationStoredValue.FromJson("22") };
        var refreshed = unchanged with { ExpectedValueVersion = 7 };
        var result = ConfigurationStatePage.MergeRefreshedPendingChanges(
            [unchanged, modified, undone],
            [refreshed, modified with { ExpectedValueVersion = 7 }, undone with { ExpectedValueVersion = 7 }],
            [unchanged, newer, newDraft]);

        Assert.Equal(3, result.Count);
        Assert.Same(refreshed, result[0]);
        Assert.Same(newer, result[1]);
        Assert.Same(newDraft, result[2]);
        Assert.DoesNotContain(result, change => change.RequestId == undone.RequestId);
    }

    [Fact]
    public async Task QueuedReviewUndo_FromRetiredRoute_DoesNotRemoveCurrentDraft()
    {
        var route = new ConfigurationOperationLifetime();
        var store = new ConfigurationStateStore();
        var captured = Change("value", "1");
        store.Stage(captured);
        await route.RetireAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        ConfigurationStatePage.ReconcileReviewedUndo(route, store, [captured], [],
            ConfigurationPendingReviewUndoRequest.ForChange(captured));

        Assert.Same(captured, Assert.Single(store.GetPending()));
    }

    [Fact]
    public async Task DefinitionReviewUndo_RemovesOnlyCapturedStillCurrentDraftsAndIssues()
    {
        var route = new ConfigurationOperationLifetime();
        try
        {
            var store = new ConfigurationStateStore();
            var unchanged = Change("unchanged", "1");
            var modified = Change("modified", "2");
            var newer = modified with { NewValue = ConfigurationStoredValue.FromJson("22") };
            var added = Change("added", "3");
            var issue = Issue(unchanged, "issue", "invalid original");
            var editedIssue = Issue(unchanged, "editedIssue", "invalid old");
            var newerIssue = editedIssue with { ValidationError = "invalid new" };
            var addedIssue = Issue(unchanged, "addedIssue", "invalid added");
            store.Stage(unchanged);
            store.Stage(newer);
            store.Stage(added);
            store.ReportValidationIssue(issue);
            store.ReportValidationIssue(newerIssue);
            store.ReportValidationIssue(addedIssue);

            ConfigurationStatePage.ReconcileReviewedUndo(route, store, [unchanged, modified], [issue, editedIssue],
                ConfigurationPendingReviewUndoRequest.ForDefinition(unchanged.DefinitionKey));

            Assert.Null(store.Get(unchanged.DefinitionKey, unchanged.LogicalPath));
            Assert.Same(newer, store.Get(newer.DefinitionKey, newer.LogicalPath));
            Assert.Same(added, store.Get(added.DefinitionKey, added.LogicalPath));
            Assert.Null(store.GetValidationIssue(issue.DefinitionKey, issue.LogicalPath));
            Assert.Same(newerIssue, store.GetValidationIssue(newerIssue.DefinitionKey, newerIssue.LogicalPath));
            Assert.Same(addedIssue, store.GetValidationIssue(addedIssue.DefinitionKey, addedIssue.LogicalPath));
        }
        finally
        {
            await route.RetireAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task PathReviewUndo_PreservesSiblingValidationAndNewDraftAtAnOldIssuePath()
    {
        var route = new ConfigurationOperationLifetime();
        try
        {
            var store = new ConfigurationStateStore();
            var change = Change("value", "1");
            var sibling = Issue(change, "sibling", "invalid sibling");
            store.Stage(change);
            store.ReportValidationIssue(sibling);
            ConfigurationStatePage.ReconcileReviewedUndo(route, store, [change], [sibling],
                ConfigurationPendingReviewUndoRequest.ForChange(change));
            Assert.Empty(store.GetPending());
            Assert.Same(sibling, Assert.Single(store.GetValidationIssues()));

            var repaired = Change("sibling", "2");
            store.Stage(repaired);
            ConfigurationStatePage.ReconcileReviewedUndo(route, store, [], [sibling],
                ConfigurationPendingReviewUndoRequest.ForValidationIssue(sibling));
            Assert.Same(repaired, Assert.Single(store.GetPending()));
        }
        finally
        {
            await route.RetireAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
    }

    private static ConfigurationValidationIssue Issue(PendingChange change, string property, string error) => new()
    {
        DefinitionKey = change.DefinitionKey,
        DefinitionDisplayName = change.DefinitionDisplayName,
        LogicalPath = LogicalPath.FromProperties(property),
        NodeDisplayName = property,
        ValidationError = error
    };

    private static PendingChange Change(string property, string json) => new()
    {
        DefinitionKey = " owner:上海/config?revision=1#opaque%25 ",
        DefinitionDisplayName = "Configuration",
        LogicalPath = LogicalPath.FromProperties(property),
        NodeDisplayName = property,
        NewValue = ConfigurationStoredValue.FromJson(json),
        ExpectedValueVersion = 1
    };

    private static ConfigurationMutationGroupApplyResult Applied(PendingChange change) => new()
    {
        Status = ConfigurationMutationGroupApplyStatus.Applied,
        Outcomes = [new() { RequestId = change.RequestId, Status = ConfigurationMutationOutcomeStatus.Applied }]
    };
}
