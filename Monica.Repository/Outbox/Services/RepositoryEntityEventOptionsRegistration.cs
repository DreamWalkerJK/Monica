using Monica.Repository.Outbox.Models;

namespace Monica.Repository.Outbox.Services;

internal sealed record RepositoryEntityEventOptionsRegistration<TDbContext>(RepositoryEntityEventOptions Options);
