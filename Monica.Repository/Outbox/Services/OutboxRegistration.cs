using Monica.Repository.Outbox.Models;

namespace Monica.Repository.Outbox.Services;

internal sealed record OutboxRegistration<TDbContext>(RepositoryOutboxOptions Options);
