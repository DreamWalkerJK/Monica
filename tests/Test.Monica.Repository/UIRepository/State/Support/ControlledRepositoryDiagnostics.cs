using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Monica.Modules;
using Monica.Repository.Persistence.Abstractions;
using Monica.Repository.Persistence.Models;

namespace Test.Monica.Repository.UIRepository.State.Support;

/// <summary>
/// Scenario-owned diagnostic boundary for deterministic state/facade interleavings. It is not a database,
/// registration, or migration integration fixture; those contracts remain covered by Repository scenarios.
/// </summary>
internal sealed class ControlledRepositoryDiagnostics : IRepositoryDbContextDiagnosticsService
{
    public ControlledRepositoryDiagnostics()
    {
        RegistryRead = (_, _) => Task.FromResult<IReadOnlyList<RepositoryDbContextSnapshot>>([Snapshot(First), Snapshot(Second)]);
        MigrationRead = (id, _) => Task.FromResult(Migration(id));
        ActivityRead = (id, _) => Task.FromResult(Activity(id));
        SelectedUpdate = (id, _, _) => Task.FromResult(Update(id));
        BulkUpdate = (_, _) => Task.FromResult<IReadOnlyList<RepositoryMigrationUpdateResult>>([Update(First.ContextId), Update(Second.ContextId)]);
    }

    public RepositoryDbContextRegistration First { get; } = new(typeof(PrimaryDbContext), DbContextProviderType.UnitOfWork);
    public RepositoryDbContextRegistration Second { get; } = new(typeof(SecondaryDbContext), DbContextProviderType.Default);
    public ConcurrentQueue<string> DetailCalls { get; } = new();
    public ConcurrentQueue<string> UpdatedContextIds { get; } = new();
    public ConcurrentQueue<TimeSpan> CommandTimeouts { get; } = new();
    public int BulkUpdateCalls { get; private set; }

    public Func<bool, CancellationToken, Task<IReadOnlyList<RepositoryDbContextSnapshot>>> RegistryRead { get; set; }
    public Func<string, CancellationToken, Task<RepositoryMigrationStatus>> MigrationRead { get; set; }
    public Func<string, CancellationToken, Task<RepositoryActivityResult>> ActivityRead { get; set; }
    public Func<string, TimeSpan, CancellationToken, Task<RepositoryMigrationUpdateResult>> SelectedUpdate { get; set; }
    public Func<TimeSpan, CancellationToken, Task<IReadOnlyList<RepositoryMigrationUpdateResult>>> BulkUpdate { get; set; }

    public Task<IReadOnlyList<RepositoryDbContextSnapshot>> GetRegisteredContextsAsync(bool revealConnectionStrings = false, CancellationToken cancellationToken = default) =>
        RegistryRead(revealConnectionStrings, cancellationToken);

    public Task<RepositoryActivityResult> GetActivityAsync(string contextId, CancellationToken cancellationToken = default)
    {
        DetailCalls.Enqueue("activity:" + contextId);
        return ActivityRead(contextId, cancellationToken);
    }

    public Task<RepositoryMigrationStatus> GetMigrationStatusAsync(string contextId, CancellationToken cancellationToken = default)
    {
        DetailCalls.Enqueue("migrations:" + contextId);
        return MigrationRead(contextId, cancellationToken);
    }

    public Task<RepositoryMigrationUpdateResult> UpdateMigrationAsync(string contextId, TimeSpan commandTimeout, CancellationToken cancellationToken = default)
    {
        UpdatedContextIds.Enqueue(contextId);
        CommandTimeouts.Enqueue(commandTimeout);
        return SelectedUpdate(contextId, commandTimeout, cancellationToken);
    }

    public Task<IReadOnlyList<RepositoryMigrationUpdateResult>> UpdateAllPendingMigrationsAsync(TimeSpan commandTimeout, CancellationToken cancellationToken = default)
    {
        BulkUpdateCalls++;
        CommandTimeouts.Enqueue(commandTimeout);
        return BulkUpdate(commandTimeout, cancellationToken);
    }

    public RepositoryDbContextSnapshot Snapshot(RepositoryDbContextRegistration registration) => new()
    {
        Registration = registration,
        CanConnect = true,
        ProviderName = "controlled-provider",
        MigrationCount = 1,
        PendingMigrationCount = 1
    };

    public RepositoryMigrationStatus Migration(string id) => new()
    {
        Registration = Registration(id),
        AllMigrations = ["20261009080000_" + Registration(id).DbContextName],
        PendingMigrations = ["20261009080000_" + Registration(id).DbContextName]
    };

    public RepositoryActivityResult Activity(string id) => new()
    {
        Registration = Registration(id),
        Support = RepositoryActivitySupport.Supported,
        Rows = [new RepositoryActivityRow { ApplicationName = Registration(id).DbContextName, Query = "evidence:" + id }]
    };

    public RepositoryMigrationUpdateResult Update(string id) => new()
    {
        Registration = Registration(id),
        Succeeded = true,
        AttemptedMigrations = ["20261009080000_" + Registration(id).DbContextName],
        Message = "Provider command completed."
    };

    private RepositoryDbContextRegistration Registration(string id) =>
        string.Equals(id, First.ContextId, StringComparison.Ordinal) ? First
        : string.Equals(id, Second.ContextId, StringComparison.Ordinal) ? Second
        : throw new KeyNotFoundException("No controlled context has the requested exact ID.");

    private sealed class PrimaryDbContext : DbContext;
    private sealed class SecondaryDbContext : DbContext;
}
