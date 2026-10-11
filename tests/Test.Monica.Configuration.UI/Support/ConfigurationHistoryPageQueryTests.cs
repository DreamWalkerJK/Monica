using Monica.Configuration.Exceptions;
using Monica.Configuration.Models;
using Monica.Configuration.UI.Pages;

namespace Test.Monica.Configuration.UI.Support;

public sealed class ConfigurationHistoryPageQueryTests
{
    [Theory]
    [InlineData("all", null)]
    [InlineData("monica", ConfigurationMutationTargetKind.MonicaEffectiveStore)]
    [InlineData("external", ConfigurationMutationTargetKind.ExternalConfigurationSource)]
    public void CreateRequests_WhenFiltersAreAdmitted_ShouldPreserveExactIdentityAndScopeTargetsToHistory(
        string target, ConfigurationMutationTargetKind? expectedTarget)
    {
        const string definitionKey = " owner:上海/config?revision=1#opaque%25 ";
        var path = LogicalPath.FromProperties("flight.上海", "revision[2]?x=%#");

        var requests = ConfigurationHistoryPage.CreateRequests(definitionKey, path.ToCanonicalString(), target, null, null);

        Assert.Equal(definitionKey, requests.History.DefinitionKey);
        Assert.Equal(definitionKey, requests.Groups.DefinitionKey);
        Assert.Equal(path, requests.History.LogicalPath);
        Assert.Equal(expectedTarget, requests.History.TargetKind);
        Assert.Equal(50, requests.History.PageSize);
        Assert.Equal(50, requests.Groups.PageSize);
        Assert.Null(requests.History.Cursor);
        Assert.Null(requests.Groups.Cursor);
        requests.History.Validate();
        requests.Groups.Validate();
    }

    [Fact]
    public void CreateRequests_WhenDatesAreSelected_ShouldShareInclusiveCalendarBoundsAcrossBothQueries()
    {
        var from = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc);

        var requests = ConfigurationHistoryPage.CreateRequests("definition", "", "all", from, to);

        Assert.Equal(new DateTimeOffset(from), requests.History.From);
        Assert.Equal(new DateTimeOffset(to.AddDays(1).AddTicks(-1)), requests.History.To);
        Assert.Equal(requests.History.From, requests.Groups.From);
        Assert.Equal(requests.History.To, requests.Groups.To);
        requests.History.Validate();
        requests.Groups.Validate();
    }

    [Fact]
    public void CreateRequests_WhenFiltersAreEmpty_ShouldRequestUnfilteredFirstPages()
    {
        var requests = ConfigurationHistoryPage.CreateRequests(null, "  ", "all", null, null);

        Assert.Null(requests.History.DefinitionKey);
        Assert.Null(requests.History.LogicalPath);
        Assert.Null(requests.History.TargetKind);
        Assert.Null(requests.History.From);
        Assert.Null(requests.History.To);
        Assert.Null(requests.Groups.DefinitionKey);
        Assert.Null(requests.Groups.From);
        Assert.Null(requests.Groups.To);
    }

    [Theory]
    [InlineData("flight[$unterminated")]
    [InlineData("flight[@-1]")]
    public void CreateRequests_WhenLogicalPathIsInvalid_ShouldRejectInsteadOfBroadeningTheQuery(string path)
    {
        Assert.Throws<ConfigurationPathFormatException>(() =>
            ConfigurationHistoryPage.CreateRequests("definition", path, "all", null, null));
    }
}
