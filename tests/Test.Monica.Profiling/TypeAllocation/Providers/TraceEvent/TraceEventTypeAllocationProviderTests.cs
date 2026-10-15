using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Monica.Profiling.TypeAllocation.Models;
using Monica.Profiling.TypeAllocation.Providers.TraceEvent;
using Xunit;

namespace Test.Monica.Profiling.TypeAllocation.Providers.TraceEvent;

/// <summary>
/// Exercises pure payload/notification/retirement policies without creating an ETW session.
/// Native integration belongs to a separately owned disposable host.
/// </summary>
public sealed class TraceEventTypeAllocationProviderTests
{
    [Theory]
    [InlineData(2, 6_000_000_000L, 100, 6_000_000_000L)]
    [InlineData(4, long.MaxValue, -1, long.MaxValue)]
    [InlineData(1, 6_000_000_000L, -1, 4_294_967_295L)]
    [InlineData(0, 0L, 123, 123L)]
    public void TryReadAllocationBytes_WhenPayloadVersionChanges_ShouldPreserveItsOwnedUnscaledValue(
        int version, long amount64, int amount32, long expected)
    {
        TraceEventTypeAllocationProvider.TryReadAllocationBytes(version, amount64, amount32, out var actual)
            .Should().BeTrue();
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(2, -1L, 100)]
    [InlineData(2, 0L, 100)]
    [InlineData(4, long.MinValue, -1)]
    [InlineData(0, 0L, 0)]
    public void TryReadAllocationBytes_WhenOwnedPayloadIsInvalid_ShouldRejectWithoutClampingOrFallback(
        int version, long amount64, int amount32)
    {
        TraceEventTypeAllocationProvider.TryReadAllocationBytes(version, amount64, amount32, out _)
            .Should().BeFalse();
    }

    [Fact]
    public void GetCurrentSnapshot_WhenNeverStartedOrReset_ShouldDistinguishMissingCollectionFromMeasuredEvidence()
    {
        using var provider = Create();
        var initial = provider.GetCurrentSnapshot();
        initial.DataSource.Should().Be(TypeAllocationDataSource.None);
        initial.IsCollecting.Should().BeFalse();
        initial.SamplingMode.Should().Be(AllocationSamplingMode.Disabled);
        initial.Duration.Should().Be(TimeSpan.Zero);
        initial.Types.Should().BeEmpty();

        provider.ResetData();
        var cleared = provider.GetCurrentSnapshot();
        cleared.DataSource.Should().Be(TypeAllocationDataSource.None);
        cleared.TotalBytes.Should().Be(0);
        cleared.TotalAllocationCount.Should().Be(0);
        provider.GetHistory().Should().BeEmpty();
    }

    [Fact]
    public void ResetData_WhenOneSubscriberThrows_ShouldStillNotifyTheHealthySubscriber()
    {
        using var provider = Create();
        TypeAllocationSnapshot? received = null;
        provider.OnSnapshotUpdated += _ => throw new InvalidOperationException("Consumer failure");
        provider.OnSnapshotUpdated += snapshot => received = snapshot;

        provider.ResetData();

        received.Should().NotBeNull();
        received!.DataSource.Should().Be(TypeAllocationDataSource.None);
        received.IsCollecting.Should().BeFalse();
    }

    [Fact]
    public void ResetData_WhenSubscriberReentersLifecycle_ShouldFailDescriptivelyWithoutDeadlockingOtherSubscribers()
    {
        using var provider = Create();
        Exception? rejected = null;
        var healthyNotifications = 0;
        provider.OnSnapshotUpdated += _ => rejected = Record.Exception(provider.StopCollection);
        provider.OnSnapshotUpdated += _ => healthyNotifications++;

        provider.ResetData();

        rejected.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Contain("inside a snapshot notification");
        healthyNotifications.Should().Be(1);
    }

    [Fact]
    public void ResetData_WhenSubscriberIsRemoved_ShouldStopSendingItNotifications()
    {
        using var provider = Create();
        var notifications = 0;
        void Subscriber(TypeAllocationSnapshot _) => notifications++;
        provider.OnSnapshotUpdated += Subscriber;
        provider.ResetData();
        provider.OnSnapshotUpdated -= Subscriber;

        provider.ResetData();

        notifications.Should().Be(1);
    }

    [Theory]
    [InlineData(AllocationSamplingMode.Disabled)]
    [InlineData((AllocationSamplingMode)99)]
    public void StartCollection_WhenModeIsNotEnabled_ShouldRejectBeforeCreatingNativeResources(AllocationSamplingMode mode)
    {
        using var provider = Create();

        var action = () => provider.StartCollection(mode);

        action.Should().Throw<ArgumentOutOfRangeException>();
        provider.IsCollecting.Should().BeFalse();
        provider.GetCurrentSnapshot().DataSource.Should().Be(TypeAllocationDataSource.None);
    }

    [Fact]
    public async Task DisposeAsync_WhenCalledTwice_ShouldShareItsDrainAndRejectLaterReads()
    {
        var provider = Create();
        var first = provider.DisposeAsync().AsTask();
        var second = provider.DisposeAsync().AsTask();
        second.Should().BeSameAs(first);
        await first;

        var read = () => provider.GetCurrentSnapshot();
        var reset = () => provider.ResetData();
        read.Should().Throw<ObjectDisposedException>();
        reset.Should().Throw<ObjectDisposedException>();
        provider.StopCollection();
    }

    [Fact]
    public void SnapshotOwnership_WhenCallersAndSubscribersMutateRows_ShouldIsolateRetainedHistoryAndEveryRecipient()
    {
        using var provider = Create();
        // This is structural ownership evidence, not a measurement or native collection fixture.
        var input = OwnershipSnapshot();
        var expected = OwnershipSnapshot();
        provider.AddHistory(input);
        TypeAllocationSnapshot? firstRecipient = null;
        TypeAllocationSnapshot? secondRecipient = null;
        provider.OnSnapshotUpdated += snapshot =>
        {
            firstRecipient = snapshot;
            Mutate(snapshot.Types[0]);
        };
        provider.OnSnapshotUpdated += snapshot => secondRecipient = snapshot;

        provider.Notify(input, generation: 0);

        secondRecipient.Should().NotBeNull().And.BeEquivalentTo(expected);
        secondRecipient.Should().NotBeSameAs(input).And.NotBeSameAs(firstRecipient);
        var received = secondRecipient!;
        received.Types[0].Should().NotBeSameAs(input.Types[0]).And.NotBeSameAs(firstRecipient!.Types[0]);
        input.Should().BeEquivalentTo(expected);
        Mutate(input.Types[0]);
        Mutate(received.Types[1]);
        var firstRead = provider.GetHistory().Should().ContainSingle().Which;
        firstRead.Should().BeEquivalentTo(expected);
        firstRead.Should().NotBeSameAs(input);
        firstRead.Types[0].Should().NotBeSameAs(input.Types[0]);
        Mutate(firstRead.Types[0]);

        var nextRead = provider.GetHistory().Single();
        nextRead.Should().BeEquivalentTo(expected);
        nextRead.Types[0].Should().NotBeSameAs(firstRead.Types[0]);
        provider.IsCollecting.Should().BeFalse();
        provider.GetCurrentSnapshot().DataSource.Should().Be(TypeAllocationDataSource.None);
    }

    [Fact]
    public void Constructor_WhenRetentionIsZero_ShouldAcceptTotalsOnlyConfigurationAndRejectNegativeCapacity()
    {
        using var totalsOnly = new TraceEventTypeAllocationProvider(Logger, maxTrackedTypes: 0);
        var idle = totalsOnly.GetCurrentSnapshot();
        idle.DataSource.Should().Be(TypeAllocationDataSource.None);
        idle.Types.Should().BeEmpty();
        idle.TotalAllocationCount.Should().Be(0);
        idle.TotalBytes.Should().Be(0);
        totalsOnly.IsCollecting.Should().BeFalse();

        var negativeCapacity = () => new TraceEventTypeAllocationProvider(Logger, maxTrackedTypes: -1);
        negativeCapacity.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("maxTrackedTypes");
    }

    [Fact]
    public void Constructor_WhenTimerCannotBeScheduled_ShouldRejectBeforeAnyNativeSessionExists()
    {
        var zeroInterval = () => new TraceEventTypeAllocationProvider(Logger, snapshotInterval: TimeSpan.Zero);
        var oversizedExpiry = () => new TraceEventTypeAllocationProvider(Logger, autoStopAfter: TimeSpan.FromDays(100));
        var negativeExpiry = () => new TraceEventTypeAllocationProvider(Logger, autoStopAfter: TimeSpan.FromSeconds(-1));

        zeroInterval.Should().Throw<ArgumentOutOfRangeException>();
        oversizedExpiry.Should().Throw<ArgumentOutOfRangeException>();
        negativeExpiry.Should().Throw<ArgumentOutOfRangeException>();
        using var manual = new TraceEventTypeAllocationProvider(Logger, autoStopAfter: null);
        manual.IsCollecting.Should().BeFalse();
    }

    private static NullLogger<TraceEventTypeAllocationProvider> Logger => NullLogger<TraceEventTypeAllocationProvider>.Instance;
    private static TraceEventTypeAllocationProvider Create() => new(Logger);

    private static TypeAllocationSnapshot OwnershipSnapshot() => new()
    {
        Timestamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        Duration = TimeSpan.FromTicks(123),
        Types =
        [
            new TypeAllocationInfo
            {
                TypeName = "Fixture.First.Type", AllocationCount = 3, TotalBytes = 300,
                AllocationPercentage = 30, BytesPercentage = 60,
                LastSeenUtc = new DateTime(2026, 1, 2, 3, 4, 4, DateTimeKind.Utc)
            },
            new TypeAllocationInfo
            {
                TypeName = "Fixture.Second.Type", AllocationCount = 7, TotalBytes = 200,
                AllocationPercentage = 70, BytesPercentage = 40,
                LastSeenUtc = new DateTime(2026, 1, 2, 3, 4, 3, DateTimeKind.Utc)
            }
        ],
        TotalAllocationCount = 10,
        TotalBytes = 500,
        IsCollecting = false,
        SamplingMode = AllocationSamplingMode.High,
        DroppedEventCount = 2,
        DataSource = TypeAllocationDataSource.AllocationTracking,
        ErrorMessage = "Structural terminal evidence"
    };

    private static void Mutate(TypeAllocationInfo row)
    {
        row.AllocationCount = -1;
        row.TotalBytes = -1;
        row.AllocationPercentage = -1;
        row.BytesPercentage = -1;
        row.LastSeenUtc = DateTime.MinValue;
    }
}
