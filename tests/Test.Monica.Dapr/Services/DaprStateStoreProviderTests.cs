using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Dapr;
using Dapr.Client;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.Core.JsonSerialization.Models;
using Monica.Core.JsonSerialization.Services;
using Monica.Dapr.Services;
using Monica.Modules;
using NSubstitute;
using Xunit;

namespace Test.Monica.Dapr.Services;

public sealed class DaprStateStoreProviderTests
{
    [Fact]
    public async Task SaveStateAsync_ShouldUseTypedSdkOperationAndPreserveTtlMetadata()
    {
        var dapr = Substitute.For<DaprClient>();
        var provider = CreateProvider(dapr);
        var value = new TestState("saved");

        await provider.SaveStateAsync(
            "state-key",
            value,
            TestContext.Current.CancellationToken,
            TimeSpan.FromSeconds(30));

        await dapr.Received(1).SaveStateAsync(
            "test-state-store",
            "state-key",
            value,
            null!,
            Arg.Is<IReadOnlyDictionary<string, string>>(metadata => metadata["ttlInSeconds"] == "30"),
            TestContext.Current.CancellationToken);
        await dapr.DidNotReceiveWithAnyArgs().SaveByteStateAsync(
            default!,
            default!,
            default,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetBulkStateAsync_ShouldUseTypedSdkOperationAndHonorEmptyFiltering()
    {
        var dapr = Substitute.For<DaprClient>();
        dapr.GetBulkStateAsync<TestState>(
                "test-state-store",
                Arg.Any<IReadOnlyList<string>>(),
                3,
                null!,
                Arg.Any<CancellationToken>())
            .Returns([
                new BulkStateItem<TestState>("found", new TestState("value"), "etag-1"),
                new BulkStateItem<TestState>("missing", null!, "etag-2")
            ]);
        var provider = CreateProvider(dapr, defaultBulkParallelism: 3);

        var results = await provider.GetBulkStateAsync<TestState>(
            ["found", "missing"],
            removeEmptyValue: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new TestState("value"), results["found"]);
        Assert.DoesNotContain("missing", results);
        await dapr.Received(1).GetBulkStateAsync<TestState>(
            "test-state-store",
            Arg.Is<IReadOnlyList<string>>(keys => keys.SequenceEqual(new[] { "found", "missing" })),
            3,
            null!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetStateAsync_ShouldUseTypedSdkOperation()
    {
        var dapr = Substitute.For<DaprClient>();
        dapr.GetStateAsync<TestState>(
                "test-state-store",
                "state-key",
                null!,
                null!,
                Arg.Any<CancellationToken>())
            .Returns(new TestState("loaded"));
        var provider = CreateProvider(dapr);

        var result = await provider.GetStateAsync<TestState>(
            "state-key",
            TestContext.Current.CancellationToken);

        Assert.Equal(new TestState("loaded"), result);
        await dapr.Received(1).GetStateAsync<TestState>(
            "test-state-store",
            "state-key",
            null!,
            null!,
            TestContext.Current.CancellationToken);
        await dapr.DidNotReceiveWithAnyArgs().GetByteStateAsync(
            default!,
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ReadOperations_WhenRedisReportsMissingKey_ShouldReturnEmptyState()
    {
        var dapr = Substitute.For<DaprClient>();
        ConfigureReadFailure(dapr, CreateReadFailure(StatusCode.Internal,
            "fail to get state-key from state store test-state-store: redis: nil"));
        var provider = CreateProvider(dapr);
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Null(await provider.GetStateAsync<TestState>("state-key", cancellationToken));
        Assert.Null(await provider.GetRawStateAsync("state-key", cancellationToken));
        var (value, etag) = await provider.GetStateAndETagAsync<TestState>("state-key", cancellationToken);
        Assert.Null(value);
        Assert.Equal(string.Empty, etag);
        Assert.False(await provider.ExistAsync("state-key", cancellationToken));
    }

    [Fact]
    public async Task GetStateAsync_WhenRedisReportsMissingValueType_ShouldReturnDefault()
    {
        var dapr = Substitute.For<DaprClient>();
        dapr.GetStateAsync<int>(
                "test-state-store", "state-key", null!, null!, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new RpcException(new Status(StatusCode.Internal,
                "fail to get state-key from state store test-state-store: redis: nil"))));

        var result = await CreateProvider(dapr).GetStateAsync<int>(
            "state-key", TestContext.Current.CancellationToken);

        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(StatusCode.Internal, "fail to get state-key from state store test-state-store: dial tcp: connection refused")]
    [InlineData(StatusCode.Internal, "fail to get state-key from state store test-state-store: WRONGTYPE Operation against a key holding the wrong kind of value")]
    [InlineData(StatusCode.Internal, "fail to get state-key from state store test-state-store: NOAUTH Authentication required")]
    [InlineData(StatusCode.Internal, "fail to get different-key from state store test-state-store: redis: nil")]
    [InlineData(StatusCode.Internal, "fail to get state-key from state store different-store: redis: nil")]
    [InlineData(StatusCode.Internal, "fail to get state-key from state store test-state-store: redis: nil: unexpected failure")]
    [InlineData(StatusCode.Unavailable, "fail to get state-key from state store test-state-store: redis: nil")]
    [InlineData(StatusCode.Cancelled, "fail to get state-key from state store test-state-store: redis: nil")]
    [InlineData(StatusCode.DeadlineExceeded, "fail to get state-key from state store test-state-store: redis: nil")]
    public async Task ReadOperations_WhenFailureDoesNotIdentifyMissingKey_ShouldPropagateFailure(
        StatusCode statusCode, string detail)
    {
        var dapr = Substitute.For<DaprClient>();
        var failure = CreateReadFailure(statusCode, detail);
        ConfigureReadFailure(dapr, failure);
        var provider = CreateProvider(dapr);
        var cancellationToken = TestContext.Current.CancellationToken;

        var typedError = await Assert.ThrowsAsync<Exception>(() =>
            provider.GetStateAsync<TestState>("state-key", cancellationToken));
        var rawError = await Assert.ThrowsAsync<Exception>(() =>
            provider.GetRawStateAsync("state-key", cancellationToken));
        var etagError = await Assert.ThrowsAsync<Exception>(() =>
            provider.GetStateAndETagAsync<TestState>("state-key", cancellationToken));
        var createError = await Assert.ThrowsAsync<Exception>(() =>
            provider.TrySaveStateIfNotExistsAsync("state-key", new TestState("ignored"), cancellationToken));

        Assert.Same(failure, typedError.InnerException);
        Assert.Same(failure, rawError.InnerException);
        Assert.Same(failure, etagError.InnerException);
        Assert.Same(failure, createError.InnerException);
        await dapr.DidNotReceiveWithAnyArgs().TrySaveStateAsync(
            default!, default!, default(TestState)!, default!, default!, default!, cancellationToken);
    }

    [Fact]
    public async Task GetStateAsync_WhenUnstructuredErrorMentionsRedisNil_ShouldPropagateFailure()
    {
        var dapr = Substitute.For<DaprClient>();
        var failure = new InvalidOperationException(
            "fail to get state-key from state store test-state-store: redis: nil");
        ConfigureReadFailure(dapr, failure);

        var error = await Assert.ThrowsAsync<Exception>(() => CreateProvider(dapr).GetStateAsync<TestState>(
            "state-key", TestContext.Current.CancellationToken));

        Assert.Same(failure, error.InnerException);
    }

    [Fact]
    public async Task TrySaveStateIfNotExistsAsync_WhenRedisReportsMissingKey_ShouldAttemptConditionalCreate()
    {
        var dapr = Substitute.For<DaprClient>();
        ConfigureReadFailure(dapr, CreateReadFailure(StatusCode.Internal,
            "fail to get state-key from state store test-state-store: redis: nil"));
        var value = new TestState("created");
        dapr.TrySaveStateAsync("test-state-store", "state-key", value, string.Empty, null!,
                null!, Arg.Any<CancellationToken>())
            .Returns(true);

        var success = await CreateProvider(dapr).TrySaveStateIfNotExistsAsync(
            "state-key", value, TestContext.Current.CancellationToken);

        Assert.True(success);
        await dapr.Received(1).TrySaveStateAsync("test-state-store", "state-key", value, string.Empty,
            null!, null!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TrySaveStateIfNotExistsAsync_WhenWriteFailsWithRedisNil_ShouldPropagateFailure()
    {
        var dapr = Substitute.For<DaprClient>();
        var failure = CreateReadFailure(StatusCode.Internal,
            "fail to get state-key from state store test-state-store: redis: nil");
        ConfigureReadFailure(dapr, failure);
        var value = new TestState("created");
        dapr.TrySaveStateAsync("test-state-store", "state-key", value, string.Empty, null!,
                null!, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(failure));

        var error = await Assert.ThrowsAsync<Exception>(() => CreateProvider(dapr).TrySaveStateIfNotExistsAsync(
            "state-key", value, TestContext.Current.CancellationToken));

        Assert.Same(failure, error.InnerException);
    }

    [Fact]
    public async Task TrySaveStateWithETagAsync_WhenStateExpiresBeforeReadBack_ShouldReturnSuccessWithEmptyETag()
    {
        var dapr = Substitute.For<DaprClient>();
        ConfigureReadFailure(dapr, CreateReadFailure(StatusCode.Internal,
            "fail to get state-key from state store test-state-store: redis: nil"));
        var value = new TestState("updated");
        dapr.TrySaveStateAsync("test-state-store", "state-key", value, "etag-1", null!,
                null!, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateProvider(dapr).TrySaveStateWithETagAsync(
            "state-key", value, "etag-1", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(string.Empty, result.NewETag);
    }

    [Fact]
    public async Task TrySaveStateWithETagWithoutReadBackAsync_WhenSaveSucceeds_ShouldUseTypedSdkOperationWithoutReadBack()
    {
        var dapr = Substitute.For<DaprClient>();
        var value = new TestState("updated");
        dapr.TrySaveStateAsync(
                "test-state-store",
                "flight-key",
                value,
                "etag-1",
                null!,
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var provider = CreateProvider(dapr);

        var success = await provider.TrySaveStateWithETagWithoutReadBackAsync(
            "flight-key",
            value,
            "etag-1",
            TestContext.Current.CancellationToken);

        Assert.True(success);
        await dapr.Received(1).TrySaveStateAsync(
            "test-state-store",
            "flight-key",
            value,
            "etag-1",
            null!,
            null!,
            TestContext.Current.CancellationToken);
        await dapr.DidNotReceiveWithAnyArgs().GetStateAndETagAsync<TestState>(
            default!,
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TrySaveStateWithETagWithoutReadBackAsync_WhenETagDoesNotMatch_ShouldReturnFalse()
    {
        var dapr = Substitute.For<DaprClient>();
        var value = new TestState("updated");
        dapr.TrySaveStateAsync(
                "test-state-store",
                "flight-key",
                value,
                "stale-etag",
                null!,
                null!,
                Arg.Any<CancellationToken>())
            .Returns(false);
        var provider = CreateProvider(dapr);

        var success = await provider.TrySaveStateWithETagWithoutReadBackAsync(
            "flight-key",
            value,
            "stale-etag",
            TestContext.Current.CancellationToken);

        Assert.False(success);
        await dapr.DidNotReceiveWithAnyArgs().GetStateAndETagAsync<TestState>(
            default!,
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TrySaveStateWithETagAsync_WhenSaveSucceeds_ShouldReadTheNewETagWithoutDeserializingState()
    {
        var dapr = Substitute.For<DaprClient>();
        var value = new TestState("updated");
        dapr.TrySaveStateAsync(
                "test-state-store",
                "flight-key",
                value,
                "etag-1",
                null!,
                null!,
                Arg.Any<CancellationToken>())
            .Returns(true);
        dapr.GetByteStateAndETagAsync(
                "test-state-store",
                "flight-key",
                null!,
                null!,
                Arg.Any<CancellationToken>())
            .Returns((ReadOnlyMemory<byte>.Empty, "etag-2"));
        var provider = CreateProvider(dapr);

        var result = await provider.TrySaveStateWithETagAsync(
            "flight-key",
            value,
            "etag-1",
            TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("etag-2", result.NewETag);
        await dapr.Received(1).GetByteStateAndETagAsync(
            "test-state-store",
            "flight-key",
            null!,
            null!,
            TestContext.Current.CancellationToken);
        await dapr.DidNotReceiveWithAnyArgs().GetStateAndETagAsync<TestState>(
            default!,
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TrySaveStateIfNotExistsAsync_WhenKeyExists_ShouldInspectOnlyTheETag()
    {
        var dapr = Substitute.For<DaprClient>();
        dapr.GetByteStateAndETagAsync(
                "test-state-store",
                "existing-key",
                null!,
                null!,
                Arg.Any<CancellationToken>())
            .Returns((new ReadOnlyMemory<byte>([0xFF]), "etag-existing"));
        var provider = CreateProvider(dapr);

        var success = await provider.TrySaveStateIfNotExistsAsync(
            "existing-key",
            new TestState("ignored"),
            TestContext.Current.CancellationToken);

        Assert.False(success);
        await dapr.DidNotReceiveWithAnyArgs().GetStateAndETagAsync<TestState>(
            default!,
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
        await dapr.DidNotReceiveWithAnyArgs().TrySaveStateAsync(
            default!,
            default!,
            default(TestState)!,
            default!,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SaveBulkStateAsync_ShouldUseTypedSdkBulkOperationAndPreserveTtlMetadata()
    {
        var dapr = Substitute.For<DaprClient>();
        var provider = CreateProvider(dapr);
        var first = new TestState("first");
        var second = new TestState("second");

        await provider.SaveBulkStateAsync(
            [("first-key", first), ("second-key", second)],
            TestContext.Current.CancellationToken,
            TimeSpan.FromSeconds(45));

        await dapr.Received(1).SaveBulkStateAsync(
            "test-state-store",
            Arg.Is<IReadOnlyList<SaveStateItem<TestState>>>(items =>
                items.Count == 2
                && items[0].Key == "first-key"
                && items[0].Value == first
                && items[0].ETag == null
                && items[0].Metadata!["ttlInSeconds"] == "45"
                && items[1].Key == "second-key"
                && items[1].Value == second
                && items[1].ETag == null
                && items[1].Metadata!["ttlInSeconds"] == "45"),
            TestContext.Current.CancellationToken);
        await dapr.DidNotReceiveWithAnyArgs().SaveByteStateAsync(
            default!,
            default!,
            default,
            default!,
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task QueryStateAsync_ShouldRenderWithCanonicalOptionsAndUseTypedSdkOperation()
    {
        var dapr = Substitute.For<DaprClient>();
        string? renderedQuery = null;
        dapr.QueryStateAsync<TestState>(
                "test-state-store",
                Arg.Do<string>(query => renderedQuery = query),
                null!,
                Arg.Any<CancellationToken>())
            .Returns(new StateQueryResponse<TestState>(
                [new StateQueryItem<TestState>("found", new TestState("matched"), "etag", string.Empty)],
                string.Empty,
                new Dictionary<string, string>()));
        var provider = CreateProvider(dapr);

        var results = await provider.QueryStateAsync<TestState>(
            builder => builder.Where(filter => filter.Eq(state => state.Value, "match")).Build(),
            TestContext.Current.CancellationToken);

        Assert.Equal(new TestState("matched"), results["found"]);
        var query = Assert.IsType<string>(renderedQuery);
        Assert.Contains("\"EQ\":{\"value\":\"match\"}", query, StringComparison.Ordinal);
        await dapr.Received(1).QueryStateAsync<TestState>(
            "test-state-store",
            query,
            null!,
            TestContext.Current.CancellationToken);
    }

    private static DaprException CreateReadFailure(StatusCode statusCode, string detail)
        => new("State operation failed: the Dapr endpoint indicated a failure.",
            new RpcException(new Status(statusCode, detail)));

    private static void ConfigureReadFailure(DaprClient dapr, Exception failure)
    {
        dapr.GetStateAsync<TestState>("test-state-store", "state-key", null!, null!, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TestState>(failure));
        dapr.GetStateAsync<object>("test-state-store", "state-key", null!, null!, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<object>(failure));
        dapr.GetByteStateAsync("test-state-store", "state-key", null!, null!, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReadOnlyMemory<byte>>(failure));
        dapr.GetStateAndETagAsync<TestState>("test-state-store", "state-key", null!, null!, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<(TestState, string)>(failure));
        dapr.GetByteStateAndETagAsync("test-state-store", "state-key", null!, null!, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<(ReadOnlyMemory<byte>, string)>(failure));
    }

    private static DaprStateStoreProvider CreateProvider(
        DaprClient dapr,
        int? defaultBulkParallelism = null)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        serializerOptions.MakeReadOnly();

        return new DaprStateStoreProvider(
            dapr,
            NullLogger<DaprStateStoreProvider>.Instance,
            Options.Create(new ModuleDaprStateStoreOption
            {
                StateStoreName = "test-state-store",
                DefaultBulkParallelism = defaultBulkParallelism
            }),
            new JsonSerializerOptionsProvider(serializerOptions, DateTimeWireFormat.Iso8601WallClock));
    }

    public sealed record TestState(string Value);
}
