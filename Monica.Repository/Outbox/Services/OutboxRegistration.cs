using Monica.Repository.Outbox.Models;

namespace Monica.Repository.Outbox.Services;

/// <summary>Host-owned operational settings for one durable event store.</summary>
public sealed record OutboxRegistration<TDbContext>(RepositoryOutboxOptions Options);
