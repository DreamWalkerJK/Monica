using System.Globalization;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Monica.UI.Shell.Models;
using Monica.UI.Shell.Support;
using Xunit;

namespace Test.Monica.UI.Shell.Support;

public sealed class NavigationSearchMatcherTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public void Search_WhenQueryIsEmpty_ShouldPreserveCatalogOrderAndApplyLimit(string? query)
    {
        var entries = new[] { Entry("last", "Zulu"), Entry("first", "Alpha"), Entry("middle", "Beta") };

        var matches = NavigationSearchMatcher.Search(entries, query, CultureInfo.InvariantCulture, maximumResults: 2);

        matches.Should().Equal(entries[0], entries[1]);
    }

    [Fact]
    public void Search_WhenWordsMatchDifferentFields_ShouldRequireEveryWord()
    {
        var match = Entry("metrics/cpu", "Processor usage", "Machine telemetry", "Monitoring › Infrastructure");
        var missingDescription = Entry("metrics/health", "Health checks", "Readiness probes", "Monitoring");
        var missingCategory = Entry("metrics/summary", "Metrics summary", "Machine telemetry", "Configuration");

        var matches = NavigationSearchMatcher.Search(
            [missingDescription, match, missingCategory], "  monitoring\ttelemetry  metrics ", CultureInfo.InvariantCulture);

        matches.Should().Equal(match);
    }

    [Fact]
    public void Search_WhenFieldsHaveDifferentRelevance_ShouldPrioritizeTitleThenCategoryDescriptionAndRoute()
    {
        var exactTitle = Entry("task-list", "Jobs");
        var titlePrefix = Entry("task-dashboard", "Jobs dashboard");
        var titleSubstring = Entry("task-schedule", "Recurring jobs");
        var categoryMatch = Entry("task-calendar", "Calendar", category: "Jobs › Scheduling");
        var descriptionMatch = Entry("task-history", "History", "Completed jobs and their outcomes");
        var routeMatch = Entry("jobs-reports", "Reports");

        var matches = NavigationSearchMatcher.Search(
            [routeMatch, descriptionMatch, titleSubstring, categoryMatch, titlePrefix, exactTitle],
            "jobs", CultureInfo.InvariantCulture);

        matches.Should().Equal(exactTitle, titlePrefix, titleSubstring, categoryMatch, descriptionMatch, routeMatch);
    }

    [Theory]
    [InlineData("en-US", "Café settings", "CAFE")]
    [InlineData("fr-FR", "Événements", "evenements")]
    [InlineData("tr-TR", "KIR", "kır")]
    [InlineData("zh-CN", "配置中心", "配置")]
    public void Search_WhenQueryUsesLocalizedText_ShouldRespectCultureCaseAndAccents(
        string cultureName,
        string title,
        string query)
    {
        var entry = Entry("localized-page", title);

        var matches = NavigationSearchMatcher.Search([entry], query, CultureInfo.GetCultureInfo(cultureName));

        matches.Should().Equal(entry);
    }

    [Fact]
    public void Search_WhenScoresAreEqual_ShouldPreserveCatalogOrder()
    {
        var first = Entry("second-route", "Event logs");
        var second = Entry("first-route", "Event logs");

        var matches = NavigationSearchMatcher.Search([first, second], "logs", CultureInfo.InvariantCulture);

        matches.Should().Equal(first, second);
    }

    [Fact]
    public void Search_WhenQueryMatchesRegisteredRoute_ShouldReturnItsDestination()
    {
        var entry = Entry("workbench/modules", "Module overview");

        var matches = NavigationSearchMatcher.Search([entry], "WORKBENCH/modules", CultureInfo.InvariantCulture);

        matches.Should().Equal(entry);
    }

    [Theory]
    [InlineData("configuration/state")]
    [InlineData("/configuration/state")]
    public void Search_WhenRouteQueryHasAnOptionalLeadingSlash_ShouldMatchTheRegisteredDestination(string query)
    {
        var entry = Entry("configuration/state", "Host settings");
        var other = Entry("configuration/options", "Module options");

        var matches = NavigationSearchMatcher.Search([other, entry], query, CultureInfo.InvariantCulture);

        matches.Should().Equal(entry);
        matches[0].Item.Href.Should().Be("configuration/state");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Search_WhenLimitIsNotPositive_ShouldReturnNoDestinations(int maximumResults)
    {
        var matches = NavigationSearchMatcher.Search(
            [Entry("overview", "Overview")], "overview", CultureInfo.InvariantCulture, maximumResults);

        matches.Should().BeEmpty();
    }

    [Fact]
    public void Search_WhenMatchingResultsExceedLimit_ShouldReturnTheMostRelevantDestinations()
    {
        var exactTitle = Entry("logs", "Logs");
        var prefixTitle = Entry("log-dashboard", "Logs dashboard");
        var routeMatch = Entry("reports/logs", "Reports");

        var matches = NavigationSearchMatcher.Search(
            [routeMatch, prefixTitle, exactTitle], "logs", CultureInfo.InvariantCulture, maximumResults: 2);

        matches.Should().Equal(exactTitle, prefixTitle);
    }

    [Fact]
    public void Search_WhenUsingDefaultLimit_ShouldReturnTheFirstThirtyCatalogEntries()
    {
        var entries = Enumerable.Range(0, 35).Select(index => Entry($"page-{index}", $"Page {index}")).ToArray();

        var matches = NavigationSearchMatcher.Search(entries, query: null, CultureInfo.InvariantCulture);

        matches.Should().HaveCount(30).And.Equal(entries.Take(30));
    }

    private static NavigationSearchEntry Entry(
        string route,
        string title,
        string description = "",
        string category = "")
    {
        var page = new PageDefinition(route, typeof(TestPage), UIRegistryText.Literal(title), accessPolicyType: null);
        var item = new NavigationItem(page, icon: null, categoryId: BuiltInNavigationCategoryIds.Uncategorized, order: 0);
        return new NavigationSearchEntry(item, title, description, category, category, Icon: null);
    }

    private sealed class TestPage : ComponentBase;
}
