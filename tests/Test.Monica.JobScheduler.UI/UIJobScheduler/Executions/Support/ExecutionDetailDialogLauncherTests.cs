using Monica.JobScheduler.UI.Components;
using Monica.JobScheduler.UI.Localization;
using Monica.JobScheduler.UI.UIJobScheduler.Executions.Support;
using Monica.Testing.Localization;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Test.Monica.JobScheduler.UI.UIJobScheduler.Executions.Support;

public sealed class ExecutionDetailDialogLauncherTests
{
    [Theory]
    [InlineData(" execution ")]
    [InlineData(" \towner:上海/job?revision=2026#execution%25 \r\n")]
    public async Task OpenAsync_WhenIdentityContainsWhitespaceUnicodeOrReservedCharacters_ShouldPassItVerbatimToNativeService(
        string instanceId)
    {
        var service = Substitute.For<IDialogService>();
        var reference = Substitute.For<IDialogReference>();
        DialogParameters? capturedParameters = null;
        service.ShowAsync<ExecutionDetailDialog>(
                Arg.Any<string?>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions?>())
            .Returns(call =>
            {
                capturedParameters = call.Arg<DialogParameters>();
                return Task.FromResult(reference);
            });

        var actual = await ExecutionDetailDialogLauncher.OpenAsync(
            service, new EchoStringLocalizer<JobSchedulerResource>(), instanceId);

        Assert.Same(reference, actual);
        Assert.Equal(instanceId, Assert.IsAssignableFrom<DialogParameters>(capturedParameters)
            .Get<string>(nameof(ExecutionDetailDialog.InstanceId)));
        Assert.Single(service.ReceivedCalls());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public async Task OpenAsync_WhenIdentityIsBlank_ShouldRejectBeforeCallingNativeService(string? instanceId)
    {
        var service = Substitute.For<IDialogService>();

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            ExecutionDetailDialogLauncher.OpenAsync(
                service, new EchoStringLocalizer<JobSchedulerResource>(), instanceId!));

        Assert.Empty(service.ReceivedCalls());
    }
}
