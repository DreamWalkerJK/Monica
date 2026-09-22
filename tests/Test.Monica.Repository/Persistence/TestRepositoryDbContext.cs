using Microsoft.EntityFrameworkCore;
using Monica.DependencyInjection.Abstractions;
using Monica.Repository.Entity.Abstractions;
using Monica.Repository.Entity.Abstractions.Auditing;
using Monica.Repository.Persistence.Services;
using Monica.Repository.Snowflake.Abstractions;

namespace Test.Monica.Repository.Persistence;

/// <summary>
/// Fully audited soft-delete entity used by repository concept tests.
/// </summary>
public sealed class SoftDeleteAuditRow : FullAuditedEntity<long>
{
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Entity without the soft-delete contract, used to verify hard deletes keep hard semantics.
/// </summary>
public sealed class HardDeleteRow : Entity<long>, IHasCreationTime
{
    public string Title { get; set; } = string.Empty;

    public DateTime CreationTime { get; set; }
}

/// <summary>
/// Repository DbContext hosting the concept-test entities.
/// </summary>
public sealed class TestRepositoryDbContext(
    DbContextOptions<TestRepositoryDbContext> options,
    ICachedServiceProvider serviceProvider)
    : RepositoryDbContext<TestRepositoryDbContext>(options, serviceProvider)
{
    public DbSet<SoftDeleteAuditRow> SoftDeleteRows => Set<SoftDeleteAuditRow>();

    public DbSet<HardDeleteRow> HardDeleteRows => Set<HardDeleteRow>();
}

/// <summary>
/// Deterministic snowflake generator for test hosts and fixtures; each database only needs uniqueness.
/// </summary>
public sealed class SequentialTestIdGenerator : ISnowflakeIdGenerator
{
    private long _next;

    public long GenerateId() => Interlocked.Increment(ref _next);
}
