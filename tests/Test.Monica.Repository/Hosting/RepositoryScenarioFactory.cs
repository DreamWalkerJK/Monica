using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Monica.Core.Modularity.Abstractions;
using Monica.Modules;
using Monica.Repository.Outbox.Models;
using Monica.Repository.Snowflake.Abstractions;
using Monica.Testing.Hosting;
using Test.Monica.Repository.Persistence;

namespace Test.Monica.Repository.Hosting;

internal sealed class RepositoryScenarioFactory(
    bool outbox = true,
    IInterceptor? interceptor = null,
    Action<IServiceCollection>? configure = null,
    Action<RepositoryOutboxOptions>? configureOutbox = null) : MonicaTestApplicationFactory<TestRepositoryDbContext>
{
    protected override void ConfigureMonica(IMonicaBuilder builder)
    {
        var repository = builder.AddRepository()
            .AddRepositoryDbContext<TestRepositoryDbContext>((_, db) => db.UseSqlite("Data Source=unused"));
        if (outbox)
            repository.AddOutbox<TestRepositoryDbContext>(options =>
            {
                options.RegisterEntity<SoftDeleteAuditRow, RowProjection>("tests.row-change.v1",
                    row => new RowProjection(row.Id, row.Title), OutboxDestination.Local);
                options.RegisterEntity<GeneratedKeyRow, GeneratedProjection>("tests.generated-change.v1",
                    row => new GeneratedProjection(row.Id, row.Title), OutboxDestination.Local);
                options.Register<IntegrationNotice>("tests.notice.v1", OutboxDestination.Local);
                configureOutbox?.Invoke(options);
            });
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton<ISnowflakeIdGenerator, SequentialTestIdGenerator>();
        services.UseTestDatabase<TestRepositoryDbContext>((_, options) =>
        {
            if (interceptor is not null) options.AddInterceptors(interceptor);
        });
        configure?.Invoke(services);
    }
}

public sealed record RowProjection(long Id, string Title);
public sealed record GeneratedProjection(int Id, string Title);
public sealed record IntegrationNotice(string Value);
