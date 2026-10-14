using AwesomeAssertions;
using Monica.EventBus.Kafka.Models;
using Monica.EventBus.Kafka.Providers.EfCore;
using Xunit;

namespace Test.Monica.EventBus.Kafka.Providers.EfCore;

public class KafkaPerformanceSnapshotEntityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToModel_WhenMetadataValidityIsSet_ShouldPreserveVerifiedEmptyOrFailedDefaultCounts(bool hasMetadata)
    {
        var snapshot = new KafkaPerformanceSnapshot
        {
            ClusterId = "primary", HasMetadataSnapshot = hasMetadata,
            BrokerCount = 1, TopicCount = 0, ConsumerGroupCount = 0,
            Message = hasMetadata ? "Optional offsets were incomplete." : "Topic metadata could not be queried."
        };

        var entity = KafkaPerformanceSnapshotEntity.FromModel(snapshot);
        var restored = entity.ToModel();

        entity.HasMetadataSnapshot.Should().Be(hasMetadata);
        restored.HasMetadataSnapshot.Should().Be(hasMetadata);
        restored.TopicCount.Should().Be(0);
        restored.ConsumerGroupCount.Should().Be(0);
        restored.Message.Should().Be(snapshot.Message);
    }

    [Fact]
    public void ToModel_WhenConsumerGroupMetricsWereSerialized_ShouldRestoreNestedMemberMetrics()
    {
        var snapshot = new KafkaPerformanceSnapshot
        {
            ClusterId = "cluster-a",
            CapturedAt = new DateTimeOffset(2026, 7, 31, 8, 0, 0, TimeSpan.Zero),
            TotalRetainedMessageCount = 120,
            ConsumerGroupMetrics =
            [
                new KafkaConsumerGroupTopicMetrics
                {
                    TopicName = "orders",
                    GroupId = "billing",
                    State = "Stable",
                    TotalCurrentOffset = 80,
                    TotalLogEndOffset = 120,
                    TotalRetainedMessageCount = 120,
                    TotalLag = 40,
                    ConsumeRatePerSecond = 3.5,
                    WriteRatePerSecond = 4.5,
                    Members =
                    [
                        new KafkaConsumerMemberMetrics
                        {
                            ConsumerId = "consumer-a",
                            Host = "/10.0.0.12",
                            ClientId = "billing-worker",
                            ConsumeRatePerSecond = 3.5,
                            Partitions =
                            [
                                new KafkaConsumerPartitionMetrics
                                {
                                    Partition = 2,
                                    CurrentOffset = 80,
                                    LogEndOffset = 120,
                                    Lag = 40,
                                    ConsumeRatePerSecond = 3.5,
                                    WriteRatePerSecond = 4.5
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var restored = KafkaPerformanceSnapshotEntity.FromModel(snapshot).ToModel();

        restored.TotalRetainedMessageCount.Should().Be(120);
        restored.ConsumerGroupMetrics.Should().BeEquivalentTo(snapshot.ConsumerGroupMetrics);
    }
}
