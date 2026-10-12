using Monica.DevOps.FileOps.Dialogs;
using Monica.DevOps.FileOps.Support;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Test.Monica.DevOps.FileOps.Support;

public sealed class FileOpsDialogProviderRetirementTests
{
    [Fact]
    public async Task DisposeAsync_WhenNativeProviderHasUnsubscribed_ShouldCompleteActualReferenceResult()
    {
        var dialogs = Substitute.For<IDialogService>();
        // The real v9 reference's Close raises the service event; no provider remains to dismiss it.
        var reference = new DialogReference(Guid.NewGuid(), dialogs);
        dialogs.ShowAsync<FileOpsCreateDirectoryDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<IDialogReference>(reference));
        var owner = new FileOpsDialogOwner(dialogs, action => { action(); return Task.CompletedTask; });
        var showing = owner.ShowAsync<FileOpsCreateDirectoryDialog>("Create", new DialogParameters());
        Assert.False(reference.Result.IsCompleted);

        var retirement = owner.DisposeAsync().AsTask();
        await retirement.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.Null(await showing);
        Assert.True((await reference.Result)!.Canceled);
        Assert.Same(retirement, owner.DisposeAsync().AsTask());
        dialogs.Received(1).Close(reference, Arg.Any<DialogResult>());
    }
}
