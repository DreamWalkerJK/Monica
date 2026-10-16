using AwesomeAssertions;
using Monica.EventBus.Kafka.Models;
using Monica.EventBus.Kafka.Services;
using Xunit;

namespace Test.Monica.EventBus.Kafka.Services;

/// <summary>Verifies deterministic topic-group evidence projection without invoking broker or host composition.</summary>
public sealed class KafkaConsumerGroupServiceTests
{
    [Fact]
    public void ProjectTopicConsumerGroups_WhenOneDescriptionFails_ShouldKeepItsDiagnosticWithoutContaminatingVerifiedEmptyGroup()
    {
        const string failedId = "review:worker/上海?region=APAC#v2%25";
        var snapshot = new KafkaConsumerMetricsSnapshot
        {
            Message = "One optional offset batch was incomplete.",
            ConsumerGroups =
            [
                new KafkaConsumerGroupDescription { GroupId = failedId, ErrorMessage = "Member assignment could not be decoded." },
                new KafkaConsumerGroupDescription { GroupId = failedId.ToUpperInvariant() }
            ],
            GroupMetrics =
            [
                new KafkaConsumerGroupTopicMetrics { GroupId = failedId, TopicName = "orders", TotalLag = 8 },
                new KafkaConsumerGroupTopicMetrics { GroupId = failedId.ToUpperInvariant(), TopicName = "orders", TotalLag = 0 }
            ]
        };

        var summaries = KafkaConsumerGroupService.ProjectTopicConsumerGroups(snapshot);

        var failed = summaries.Single(group => group.GroupId == failedId);
        failed.TopicName.Should().Be("orders");
        failed.MemberCount.Should().Be(0);
        failed.ErrorMessage.Should().Be("Member assignment could not be decoded.");
        failed.TotalLag.Should().Be(8, "independent committed-offset evidence remains available");
        var verifiedEmpty = summaries.Single(group => group.GroupId == failedId.ToUpperInvariant());
        verifiedEmpty.MemberCount.Should().Be(0);
        verifiedEmpty.ErrorMessage.Should().BeNull("a snapshot-wide diagnostic must not invalidate another exact group's description");
        verifiedEmpty.TotalLag.Should().Be(0);
    }

    [Fact]
    public void ProjectTopicConsumerGroups_WhenDescriptionIsMissing_ShouldNotTreatDefaultMemberCountAsVerifiedEmpty()
    {
        var snapshot = new KafkaConsumerMetricsSnapshot
        {
            GroupMetrics = [new KafkaConsumerGroupTopicMetrics { GroupId = "missing", TopicName = "orders" }]
        };

        var summary = KafkaConsumerGroupService.ProjectTopicConsumerGroups(snapshot).Single();

        summary.GroupId.Should().Be("missing");
        summary.MemberCount.Should().Be(0);
        summary.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        summary.TotalLag.Should().BeNull();
    }
}
