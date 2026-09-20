using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Monica.AI.Mcp.Internal;
using Monica.Modules;

namespace Test.Monica.AI.Mcp;

public sealed class McpToolErrorDetailTests
{
    [Fact]
    public void ModuleMcpOption_ShouldExposeToolErrorDetailByDefault()
        => new ModuleMcpOption().ExposeToolErrorDetail.Should().BeTrue();

    [Fact]
    public void ConfigureToolErrorDetail_ShouldInstallTheFilterWhenEnabled()
    {
        var options = new McpServerOptions();
        var filtersBefore = options.Filters.Request.CallToolFilters.Count;

        McpServerOptionsConfigurator.ConfigureToolErrorDetail(options, new ModuleMcpOption());

        options.Filters.Request.CallToolFilters.Count.Should().Be(filtersBefore + 1);
    }

    [Fact]
    public void ConfigureToolErrorDetail_ShouldNotInstallTheFilterWhenDisabled()
    {
        var options = new McpServerOptions();
        var filtersBefore = options.Filters.Request.CallToolFilters.Count;

        McpServerOptionsConfigurator.ConfigureToolErrorDetail(
            options,
            new ModuleMcpOption { ExposeToolErrorDetail = false });

        options.Filters.Request.CallToolFilters.Count.Should().Be(filtersBefore);
    }

    [Theory]
    [InlineData("Change 'WORKFLOW-CHANGE-1' is completed.")]
    [InlineData("Downstream agent rejected the request.")]
    public async Task WrapToolErrorDetail_ShouldCarryDomainGuardMessagesToTheClient(string message)
    {
        var wrapped = McpServerOptionsConfigurator.WrapToolErrorDetail(
            (_, _) => throw new InvalidOperationException(message));
        Func<Task> invocation = () => wrapped(null!, CancellationToken.None).AsTask();

        var assertion = await invocation.Should().ThrowAsync<McpProtocolException>(
            "the SDK's generic invocation error hides the failure reason");
        assertion.Which.Message.Should().Contain(message);
    }

    [Fact]
    public async Task WrapToolErrorDetail_ShouldCarryArgumentBindingFailuresToTheClient()
    {
        var wrapped = McpServerOptionsConfigurator.WrapToolErrorDetail((_, _) =>
            throw new JsonException(
                "The JSON value could not be converted to System.String. Path: $.excerpts | LineNumber: 4 | BytePositionInLine: 18."));
        Func<Task> invocation = () => wrapped(null!, CancellationToken.None).AsTask();

        var assertion = await invocation.Should().ThrowAsync<McpProtocolException>(
            "a wrong-typed tool argument must tell the caller which value failed to bind");
        assertion.Which.Message.Should().Contain("JsonException").And.Contain("$.excerpts");
    }

    [Fact]
    public async Task WrapToolErrorDetail_ShouldCarryUnexpectedFailuresToTheClient()
    {
        var wrapped = McpServerOptionsConfigurator.WrapToolErrorDetail(
            (_, _) => throw new NullReferenceException("Object reference not set to an instance of an object."));
        Func<Task> invocation = () => wrapped(null!, CancellationToken.None).AsTask();

        var assertion = await invocation.Should().ThrowAsync<McpProtocolException>();
        assertion.Which.Message.Should()
            .Contain("NullReferenceException")
            .And.Contain("Object reference not set to an instance of an object.");
    }

    [Fact]
    public async Task WrapToolErrorDetail_ShouldUnwrapNestedReasons()
    {
        var wrapped = McpServerOptionsConfigurator.WrapToolErrorDetail((_, _) => throw new ArgumentException(
            "Failed to bind tool arguments.",
            new JsonException("The JSON value could not be converted to System.String. Path: $.excerpts | LineNumber: 4 | BytePositionInLine: 18.")));
        Func<Task> invocation = () => wrapped(null!, CancellationToken.None).AsTask();

        var assertion = await invocation.Should().ThrowAsync<McpProtocolException>();
        assertion.Which.Message.Should()
            .Contain("ArgumentException")
            .And.Contain("Failed to bind tool arguments.")
            .And.Contain("Path: $.excerpts");
    }

    [Fact]
    public async Task WrapToolErrorDetail_ShouldKeepProtocolExceptionsAndClientCancellationUntouched()
    {
        var protocol = new McpProtocolException("already carries its reason", McpErrorCode.InternalError);
        var wrappedProtocol = McpServerOptionsConfigurator.WrapToolErrorDetail((_, _) => throw protocol);
        (await ((Func<Task>)(() => wrappedProtocol(null!, CancellationToken.None).AsTask()))
            .Should().ThrowAsync<McpProtocolException>()).Which.Should().Be(protocol);

        var wrappedCancellation = McpServerOptionsConfigurator.WrapToolErrorDetail(
            (_, _) => throw new OperationCanceledException());
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await ((Func<Task>)(() => wrappedCancellation(null!, cancelled.Token).AsTask()))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WrapToolErrorDetail_ShouldCarryServerSideAbortsThatWereNotClientCancellation()
    {
        var wrapped = McpServerOptionsConfigurator.WrapToolErrorDetail(
            (_, _) => throw new OperationCanceledException("The upstream HTTP client timed out."));
        Func<Task> invocation = () => wrapped(null!, CancellationToken.None).AsTask();

        var assertion = await invocation.Should().ThrowAsync<McpProtocolException>(
            "an abort that the caller did not request is a failure like any other");
        assertion.Which.Message.Should()
            .Contain("OperationCanceledException")
            .And.Contain("The upstream HTTP client timed out.");
    }

    [Fact]
    public void ComposeErrorDetail_ShouldBoundRunawayMessages()
    {
        var detail = McpServerOptionsConfigurator.ComposeErrorDetail(
            new InvalidOperationException(string.Join(string.Empty, Enumerable.Repeat('x', 5000))));

        detail.Length.Should().BeLessThanOrEqualTo(2001);
        detail.Should().EndWith("…");
    }
}
