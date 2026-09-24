using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Monica.Core.Execution;
using Monica.Core.Mediator;
using Monica.Core.Modularity.Extensions;
using Monica.Modules;
using Monica.WebApi.Annotations;
using Monica.WebApi.AutoControllers.Services.Support;
using Xunit;

namespace Test.Monica.WebApi.Mediator;

public sealed class ApiEndpointReadOnlyConventionTests
{
    [Fact]
    public void IsReadOnly_ShouldMarkOnlyGetBoundRequests()
    {
        var convention = new ApiEndpointReadOnlyConvention();
        var cases = new (Type RequestType, bool Expected)[]
        {
            (typeof(GetBoundRequest), true),
            (typeof(PostBoundRequest), false),
            (typeof(PutBoundRequest), false),
            (typeof(PatchBoundRequest), false),
            (typeof(DeleteBoundRequest), false),
            (typeof(UnboundRequest), false),
            (typeof(DerivedGetBoundRequest), false),
        };

        foreach (var (requestType, expected) in cases)
            convention.IsReadOnly(requestType).Should().Be(expected, $"for request {requestType.Name}");
    }

    [Fact]
    public async Task MediatedRequests_WithAutoControllersHost_ShouldInferTransactionModeFromEndpointBinding()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddTransient<IRequestHandler<GetBoundRequest, string>, NoOpHandler<GetBoundRequest>>();
        builder.Services.AddTransient<IRequestHandler<PostBoundRequest, string>, NoOpHandler<PostBoundRequest>>();
        builder.AddMonica(monica =>
        {
            monica.AddAutoControllers();
            monica.AddMediator();
            monica.AddExecutionPipeline()
                .AddBehavior<TransactionModeBehavior<GetBoundRequest>>()
                .AddBehavior<TransactionModeBehavior<PostBoundRequest>>();
        });

        using var application = builder.Build();
        using var scope = application.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        (await mediator.Send(new GetBoundRequest(), TestContext.Current.CancellationToken))
            .Should().Be(nameof(ExecutionTransactionMode.None));
        (await mediator.Send(new PostBoundRequest(), TestContext.Current.CancellationToken))
            .Should().Be(nameof(ExecutionTransactionMode.Automatic));
    }

    private sealed class NoOpHandler<TRequest> : IRequestHandler<TRequest, string>
        where TRequest : IRequest<string>
    {
        public Task<string> Handle(TRequest request, CancellationToken cancellationToken) => Task.FromResult("handled");
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

[ApiEndpoint(ApiHttpMethod.Get, "read-only-probe")]
public record GetBoundRequest : IRequest<string>;

public sealed record DerivedGetBoundRequest : GetBoundRequest;

[ApiEndpoint(ApiHttpMethod.Post, "write-probe")]
public sealed record PostBoundRequest : IRequest<string>;

[ApiEndpoint(ApiHttpMethod.Put, "write-probe")]
public sealed record PutBoundRequest : IRequest<string>;

[ApiEndpoint(ApiHttpMethod.Patch, "write-probe")]
public sealed record PatchBoundRequest : IRequest<string>;

[ApiEndpoint(ApiHttpMethod.Delete, "write-probe")]
public sealed record DeleteBoundRequest : IRequest<string>;

public sealed record UnboundRequest : IRequest<string>;
