using AwesomeAssertions;
using Monica.EventBus.Kafka.Models;
using Monica.EventBus.Kafka.Services;
using Xunit;

namespace Test.Monica.EventBus.Kafka.Services;

public sealed class InMemoryKafkaConsoleRepositoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplacePerformanceSnapshot_WhenMetadataValidityIsSet_ShouldPreserveItInDetachedReads(bool hasMetadata)
    {
        var repository = new InMemoryKafkaConsoleRepository();
        var snapshot = new KafkaPerformanceSnapshot { ClusterId = "primary", HasMetadataSnapshot = hasMetadata };

        await repository.ReplacePerformanceSnapshotAsync(snapshot, TestContext.Current.CancellationToken);
        snapshot.HasMetadataSnapshot = !hasMetadata;
        var firstRead = await repository.GetLatestPerformanceSnapshotAsync("primary", TestContext.Current.CancellationToken);
        firstRead!.HasMetadataSnapshot.Should().Be(hasMetadata);
        firstRead.HasMetadataSnapshot = !hasMetadata;
        var secondRead = await repository.GetLatestPerformanceSnapshotAsync("primary", TestContext.Current.CancellationToken);

        secondRead!.HasMetadataSnapshot.Should().Be(hasMetadata);
    }
}
