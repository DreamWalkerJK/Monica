using Monica.Repository.Inbox.Models;

namespace Monica.Repository.Inbox.Services;

internal sealed record InboxRegistration<TDbContext>(RepositoryInboxOptions Options);
