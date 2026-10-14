using Monica.JobScheduler.Models.Execution;
using Monica.JobScheduler.UI.Localization;
using Monica.JobScheduler.UI.UIJobScheduler.Executions.State;
using Monica.JobScheduler.UI.UIJobScheduler.Executions.Support;
using Monica.Testing.Localization;
using MudBlazor;
using Xunit;

namespace Test.Monica.JobScheduler.UI.UIJobScheduler.Executions.Support;

public sealed class ExecutionUiPresentationTests
{
    [Fact]
    public void GetCancellationFeedback_WhenAuthorizationProviderFaults_ShouldPreserveItsDiagnosticInsteadOfGenericDeniedText()
    {
        var feedback = ExecutionUiPresentation.GetCancellationFeedback(new(false, null, "Authorization service unavailable"),
            new EchoStringLocalizer<JobSchedulerResource>());
        Assert.Equal("Authorization service unavailable", feedback.Message);
        Assert.Equal(Severity.Error, feedback.Severity);
    }

    [Fact]
    public void GetCancellationFeedback_WhenAuthorizationIsDeniedWithoutFault_ShouldKeepOrdinaryDeniedFeedback()
    {
        var feedback = ExecutionUiPresentation.GetCancellationFeedback(new(false, null, null),
            new EchoStringLocalizer<JobSchedulerResource>());
        Assert.Equal("Access:DeniedDescription", feedback.Message);
        Assert.Equal(Severity.Error, feedback.Severity);
    }

    [Theory]
    [InlineData(JobCancellationStatus.Cancelled, "Executions:Cancel:Cancelled", Severity.Success)]
    [InlineData(JobCancellationStatus.CancellationRequested, "Executions:Cancel:Requested", Severity.Success)]
    [InlineData(JobCancellationStatus.AlreadyTerminal, "Executions:Cancel:AlreadyTerminal", Severity.Info)]
    [InlineData(JobCancellationStatus.NotFound, "Executions:Cancel:NotFound", Severity.Warning)]
    public void GetCancellationFeedback_WhenStoreReturnsAnOutcome_ShouldRetainItsActualMeaning(JobCancellationStatus status, string text, Severity severity)
    {
        var feedback = ExecutionUiPresentation.GetCancellationFeedback(new(true, status, null),
            new EchoStringLocalizer<JobSchedulerResource>());
        Assert.Equal(text, feedback.Message);
        Assert.Equal(severity, feedback.Severity);
    }
}
