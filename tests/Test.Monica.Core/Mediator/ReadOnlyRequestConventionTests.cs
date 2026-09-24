using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monica.Core.Execution;
using Monica.Core.Mediator;
using Monica.Core.Modularity.Extensions;
using Monica.Modules;
using Xunit;

namespace Test.Monica.Core.Mediator;

public sealed class ReadOnlyRequestConventionTests
{
    [Fact]
    public async Task Send_WhenConventionMarksRequestReadOnly_ShouldSkipAutomaticTransaction()
    {
        using var host = BuildHost<ConventionMarkedRequest>();
        using var scope = host.Services.CreateScope();

        (await SendAsync<ConventionMarkedRequest>(scope))
            .Should().Be(nameof(ExecutionTransactionMode.None));
    }

    [Fact]
    public async Task Send_WhenConventionMarksRequestReadOnly_ShouldStillHonorExplicitTransactionOverride()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddTransient<IRequestHandler<ConventionMarkedRequest, string>, ForcedWriteHandler<ConventionMarkedRequest>>();
        builder.Services.AddSingleton<IReadOnlyRequestConvention>(
            new DelegatingReadOnlyConvention(requestType => requestType == typeof(ConventionMarkedRequest)));
        builder.AddMonica(monica =>
        {
            monica.AddMediator();
            monica.AddExecutionPipeline().AddBehavior<TransactionModeBehavior<ConventionMarkedRequest>>();
        });

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();

        (await SendAsync<ConventionMarkedRequest>(scope))
            .Should().Be(nameof(ExecutionTransactionMode.Automatic));
    }

    [Fact]
    public async Task Send_WhenNoConventionOrAttributeApplies_ShouldDefaultToAutomatic()
    {
        using var host = BuildHost<PlainRequest>();
        using var scope = host.Services.CreateScope();

        (await SendAsync<PlainRequest>(scope))
            .Should().Be(nameof(ExecutionTransactionMode.Automatic));
    }

    private static IHost BuildHost<TRequest>() where TRequest : IRequest<string>, new()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddTransient<IRequestHandler<TRequest, string>, NoOpHandler<TRequest>>();
        builder.Services.AddSingleton<IReadOnlyRequestConvention>(
            new DelegatingReadOnlyConvention(requestType => requestType == typeof(ConventionMarkedRequest)));
        builder.AddMonica(monica =>
        {
            monica.AddMediator();
            monica.AddExecutionPipeline().AddBehavior<TransactionModeBehavior<TRequest>>();
        });
        return builder.Build();
    }

    private static Task<string> SendAsync<TRequest>(IServiceScope scope) where TRequest : IRequest<string>, new()
        => scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new TRequest(), TestContext.Current.CancellationToken);

    private sealed record ConventionMarkedRequest : IRequest<string>;

    private sealed record PlainRequest : IRequest<string>;

    private sealed class NoOpHandler<TRequest> : IRequestHandler<TRequest, string>
        where TRequest : IRequest<string>
    {
        public Task<string> Handle(TRequest request, CancellationToken cancellationToken) => Task.FromResult("handled");
    }

    private sealed class ForcedWriteHandler<TRequest> : IRequestHandler<TRequest, string>
        where TRequest : IRequest<string>
    {
        [ExecutionTransaction(ExecutionTransactionMode.Automatic)]
        public Task<string> Handle(TRequest request, CancellationToken cancellationToken) => Task.FromResult("handled");
    }

    private sealed class DelegatingReadOnlyConvention(Func<Type, bool> predicate) : IReadOnlyRequestConvention
    {
        public bool IsReadOnly(Type requestType) => predicate(requestType);
    }

    private sealed class TransactionModeBehavior<TRequest> : IExecutionBehavior<TRequest, string>
        where TRequest : IRequest<string>
    {
        public Task<string> ExecuteAsync(
            ExecutionContext<TRequest> context,
            ExecutionDelegate<string> next)
            => Task.FromResult(context.Descriptor.TransactionMode.ToString());
    }
}
